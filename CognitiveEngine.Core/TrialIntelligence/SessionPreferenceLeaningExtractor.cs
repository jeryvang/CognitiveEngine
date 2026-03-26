using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CognitiveEngine.Core;

namespace CognitiveEngine.Core.TrialIntelligence;

public static class SessionPreferenceLeaningExtractor
{
    /// <summary>
    /// Derives session-level preference signals and leaning indicators from interaction events.
    /// Deterministic by construction: stable ordering, stable ids, no randomness.
    /// </summary>
    public static SessionContract BuildSessionIntelligence(
        string sessionId,
        string exportedAtUtc,
        IEnumerable<InteractionSignal> interactionSignals)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("sessionId is required.", nameof(sessionId));
        if (interactionSignals == null)
            throw new ArgumentNullException(nameof(interactionSignals));
        ThrowIfUtcMissing(nameof(exportedAtUtc), exportedAtUtc);

        var signals = interactionSignals
            .Select(NormalizeOrThrow)
            .OrderBy(s => s.OccurredAtUtc, StringComparer.Ordinal)
            .ThenBy(s => s.ProductId, StringComparer.Ordinal)
            .ThenBy(s => s.EventType)
            .ThenBy(s => s.SignalId, StringComparer.Ordinal)
            .ToList();

        var preferenceSignals = DerivePreferenceSignals(sessionId, signals);
        var leaning = DeriveLeaningIndicators(preferenceSignals);

        return new SessionContract
        {
            SessionId = sessionId,
            ExportedAtUtc = exportedAtUtc,
            InteractionSignals = signals,
            PreferenceSignals = preferenceSignals,
            LeaningIndicators = leaning
        };
    }

    private static InteractionSignal NormalizeOrThrow(InteractionSignal s)
    {
        if (s == null) throw new ArgumentNullException(nameof(s), "interaction signal is null");
        if (string.IsNullOrWhiteSpace(s.SignalId))
            throw new ArgumentException("interaction_signals.signal_id is required.");
        if (string.IsNullOrWhiteSpace(s.OccurredAtUtc))
            throw new ArgumentException("interaction_signals.occurred_at_utc is required.");
        ThrowIfUtcMissing("interaction_signals.occurred_at_utc", s.OccurredAtUtc);
        if (string.IsNullOrWhiteSpace(s.ProductId))
            throw new ArgumentException("interaction_signals.product_id is required.");
        if (s.DurationMs.HasValue && s.DurationMs.Value < 0)
            throw new ArgumentException("interaction_signals.duration_ms cannot be negative.");
        if (s.Intensity.HasValue && (s.Intensity.Value < 0.0 || s.Intensity.Value > 1.0))
            throw new ArgumentException("interaction_signals.intensity must be within [0,1].");
        return s;
    }

    private static List<PreferenceSignal> DerivePreferenceSignals(string sessionId, List<InteractionSignal> signals)
    {
        if (signals.Count == 0) return new List<PreferenceSignal>();

        var perProduct = signals
            .GroupBy(s => s.ProductId, StringComparer.Ordinal)
            .Select(g => new ProductEvidence(
                productId: g.Key,
                selectionCount: g.Count(x => x.EventType == InteractionEventKind.Selection),
                confirmCount: g.Count(x => x.EventType == InteractionEventKind.ConfirmIntent),
                compareCount: g.Count(x => x.EventType == InteractionEventKind.Compare),
                dwellMs: g.Where(x => x.EventType == InteractionEventKind.Dwell)
                    .Select(x => (long)(x.DurationMs ?? 0))
                    .Sum(),
                lastOccurredAtUtc: g.Max(x => x.OccurredAtUtc)
            ))
            .OrderBy(x => x.ProductId, StringComparer.Ordinal)
            .ToList();

        double maxAttraction = perProduct.Max(p => (double)(p.SelectionCount + 2 * p.ConfirmCount));
        double maxEngagement = perProduct.Max(p => (double)p.DwellMs);
        double maxComparison = perProduct.Max(p => (double)p.CompareCount);

        var output = new List<PreferenceSignal>(perProduct.Count);
        foreach (var p in perProduct)
        {
            double attractionNorm = SafeNorm(p.SelectionCount + 2 * p.ConfirmCount, maxAttraction);
            double engagementNorm = SafeNorm(p.DwellMs, maxEngagement);
            double comparisonNorm = SafeNorm(p.CompareCount, maxComparison);

            // Interpretable weights: selection/confirm dominates, dwell supports, compare lightly contributes.
            double strength = Clamp01(0.55 * attractionNorm + 0.30 * engagementNorm + 0.15 * comparisonNorm);

            output.Add(new PreferenceSignal
            {
                SignalId = DeterministicGuidN($"p5:pref:{sessionId}:{p.ProductId}:{Schema.CurrentVersion}"),
                DerivedAtUtc = p.LastOccurredAtUtc,
                ProductId = p.ProductId,
                PreferenceStrength = strength,
                Basis = "weighted_norm_v1(attraction,engagement,comparison)"
            });
        }

        return output;
    }

    private static List<LeaningIndicator> DeriveLeaningIndicators(List<PreferenceSignal> preferenceSignals)
    {
        if (preferenceSignals.Count == 0) return new List<LeaningIndicator>();

        double total = preferenceSignals.Sum(p => Math.Max(0.0, p.PreferenceStrength));
        // Avoid floating point representation drift in exported JSON (e.g. 0.8500000000000001).
        double confidence = Round4(Clamp01(total)); // session-relative: more net preference signal => higher confidence.

        var ordered = preferenceSignals
            .OrderByDescending(p => p.PreferenceStrength)
            .ThenBy(p => p.ProductId, StringComparer.Ordinal)
            .ToList();

        var output = new List<LeaningIndicator>(ordered.Count);
        for (int i = 0; i < ordered.Count; i++)
        {
            output.Add(new LeaningIndicator
            {
                ProductId = ordered[i].ProductId,
                LeaningScore = Clamp01(ordered[i].PreferenceStrength),
                Confidence = confidence,
                Rank = i + 1
            });
        }

        return output;
    }

    private static double SafeNorm(long value, double max) => SafeNorm((double)value, max);
    private static double SafeNorm(double value, double max) => max <= 0.0 ? 0.0 : Clamp01(value / max);

    private static double Clamp01(double v)
    {
        if (v < 0.0) return 0.0;
        if (v > 1.0) return 1.0;
        return v;
    }

    private static double Round4(double v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);

    private static string DeterministicGuidN(string input)
    {
        // Stable across runs and platforms; used for signal ids.
        using var md5 = MD5.Create();
        byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return new Guid(bytes).ToString("N").ToLowerInvariant();
    }

    private static void ThrowIfUtcMissing(string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{fieldName} is required.", fieldName);
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            throw new ArgumentException($"{fieldName} must be ISO 8601 parseable ({Schema.UtcTimestampFormatDescription}).", fieldName);
        if (dt.Kind != DateTimeKind.Utc)
            throw new ArgumentException($"{fieldName} must be UTC (Kind=Utc or Z offset).", fieldName);
    }

    private readonly struct ProductEvidence
    {
        public string ProductId { get; }
        public int SelectionCount { get; }
        public int ConfirmCount { get; }
        public int CompareCount { get; }
        public long DwellMs { get; }
        public string LastOccurredAtUtc { get; }

        public ProductEvidence(
            string productId,
            int selectionCount,
            int confirmCount,
            int compareCount,
            long dwellMs,
            string lastOccurredAtUtc)
        {
            ProductId = productId;
            SelectionCount = selectionCount;
            ConfirmCount = confirmCount;
            CompareCount = compareCount;
            DwellMs = dwellMs;
            LastOccurredAtUtc = lastOccurredAtUtc;
        }
    }
}

