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
    private const string CompareTimeBasis = "inferred_compare_window_v1(start=compare,end=selection|confirmIntent|contextChange|session_end)";
    private const string DecisionConvergenceBasis = "convergence_v1(dwell_concentration,compare_switch_penalty,exploration_switch_penalty,selection_presence)";

    /// <summary>
    /// Derives session-level preference signals and leaning indicators from interaction events.
    /// Deterministic by construction: stable ordering, stable ids, no randomness.
    /// </summary>
    public static SessionContract BuildSessionIntelligence(
        string sessionId,
        string exportedAtUtc,
        IEnumerable<InteractionSignal> interactionSignals)
    {
        return BuildSessionIntelligence(sessionId, exportedAtUtc, interactionSignals, null, null);
    }

    public static SessionContract BuildSessionIntelligence(
        string sessionId,
        string exportedAtUtc,
        IEnumerable<InteractionSignal> interactionSignals,
        IEnumerable<(string occurredAtUtc, StateType state)>? stateTimeline)
    {
        return BuildSessionIntelligence(sessionId, exportedAtUtc, interactionSignals, stateTimeline, null);
    }

    /// <summary>
    /// Optional per-tick confidence trace: same ordering as engine ticks; used for Step 5 confidence interpretation when provided.
    /// </summary>
    public static SessionContract BuildSessionIntelligence(
        string sessionId,
        string exportedAtUtc,
        IEnumerable<InteractionSignal> interactionSignals,
        IEnumerable<(string occurredAtUtc, StateType state)>? stateTimeline,
        IEnumerable<(string occurredAtUtc, float confidence, StateType state)>? confidenceTrace)
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
        var derivedMetrics = DeriveSessionMetrics(signals);

        var normalizedStateTimeline = stateTimeline?.ToList() ?? signals
            .Select(s => (s.OccurredAtUtc, InferStateFromInteraction(s)))
            .ToList();
        var frictionEpisodes = FrictionDetectionEngine.DetectFrictionEpisodes(sessionId, signals, normalizedStateTimeline);
        var traceList = confidenceTrace?.ToList();
        var (readiness, confidenceInterpretation, struggleSummary) =
            DecisionReadinessEngine.Evaluate(signals, leaning, frictionEpisodes, traceList);

        return new SessionContract
        {
            SessionId = sessionId,
            ExportedAtUtc = exportedAtUtc,
            InteractionSignals = signals,
            PreferenceSignals = preferenceSignals,
            LeaningIndicators = leaning,
            FrictionEpisodes = frictionEpisodes,
            DecisionReadiness = readiness,
            ConfidenceInterpretation = confidenceInterpretation,
            StruggleDecisionSummary = struggleSummary,
            DerivedMetrics = derivedMetrics
        };
    }

    private static SessionDerivedMetrics DeriveSessionMetrics(List<InteractionSignal> signals)
    {
        if (signals.Count == 0)
        {
            return new SessionDerivedMetrics
            {
                SwitchCount = 0,
                ExplorationSwitchCount = 0,
                SelectionEventsCount = 0,
                TotalCompareTimeMs = 0,
                CompareTimeBasis = CompareTimeBasis,
                FinalSelectedProductId = null,
                LongestDwellProductId = null,
                DecisionConvergenceScore = 0.0,
                DecisionConvergenceLevel = DecisionConvergenceLevel.Low,
                DecisionConvergenceBasis = DecisionConvergenceBasis
            };
        }

        int selectionEventsCount = signals.Count(s => s.EventType == InteractionEventKind.Selection);
        var compareComputation = ComputeCompareAndSwitchMetrics(signals);
        string? finalSelectedProductId = FindFinalSelectedProductId(signals);
        string? longestDwellProductId = FindLongestDwellProductId(signals);
        var convergence = ComputeDecisionConvergence(signals, compareComputation, finalSelectedProductId);

        return new SessionDerivedMetrics
        {
            // switch_count is compare-scoped by design (A<->B behavior inside compare windows).
            SwitchCount = compareComputation.CompareSwitchCount,
            ExplorationSwitchCount = compareComputation.ExplorationSwitchCount,
            SelectionEventsCount = selectionEventsCount,
            TotalCompareTimeMs = compareComputation.TotalCompareTimeMs,
            CompareTimeBasis = CompareTimeBasis,
            FinalSelectedProductId = finalSelectedProductId,
            LongestDwellProductId = longestDwellProductId,
            DecisionConvergenceScore = convergence.Score,
            DecisionConvergenceLevel = convergence.Level,
            DecisionConvergenceBasis = DecisionConvergenceBasis
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

    private static CompareComputation ComputeCompareAndSwitchMetrics(List<InteractionSignal> signals)
    {
        long totalMs = 0;
        bool inCompare = false;
        DateTime compareStart = default;
        int compareSwitchCount = 0;
        int explorationSwitchCount = 0;
        string? lastCompareProductId = null;
        string? lastExplorationProductId = null;
        DateTime lastSeen = ParseUtcOrThrow(signals[0].OccurredAtUtc);

        foreach (var signal in signals)
        {
            var ts = ParseUtcOrThrow(signal.OccurredAtUtc);
            if (ts > lastSeen)
                lastSeen = ts;

            if (!inCompare && signal.EventType == InteractionEventKind.Compare)
            {
                inCompare = true;
                compareStart = ts;
                lastCompareProductId = string.IsNullOrWhiteSpace(signal.ProductId) ? null : signal.ProductId;
                continue;
            }

            if (inCompare)
            {
                if (IsCompareSwitchCandidateEvent(signal.EventType) && !string.IsNullOrWhiteSpace(signal.ProductId))
                {
                    if (lastCompareProductId != null
                        && !string.Equals(lastCompareProductId, signal.ProductId, StringComparison.Ordinal))
                    {
                        compareSwitchCount++;
                    }

                    lastCompareProductId = signal.ProductId;
                }

                if (IsCompareExitEvent(signal.EventType))
                {
                    totalMs += PositiveDurationMs(compareStart, ts);
                    inCompare = false;

                    if (!string.IsNullOrWhiteSpace(signal.ProductId))
                        lastExplorationProductId = signal.ProductId;
                }
                continue;
            }

            if (string.IsNullOrWhiteSpace(signal.ProductId))
                continue;

            if (!IsSwitchCandidateEvent(signal.EventType))
                continue;

            if (lastExplorationProductId != null
                && !string.Equals(lastExplorationProductId, signal.ProductId, StringComparison.Ordinal))
            {
                explorationSwitchCount++;
            }

            lastExplorationProductId = signal.ProductId;
        }

        if (inCompare)
            totalMs += PositiveDurationMs(compareStart, lastSeen);

        return new CompareComputation(
            compareSwitchCount: compareSwitchCount,
            explorationSwitchCount: explorationSwitchCount,
            totalCompareTimeMs: totalMs);
    }

    private static string? FindFinalSelectedProductId(List<InteractionSignal> signals)
    {
        var ordered = signals
            .Where(s => !string.IsNullOrWhiteSpace(s.ProductId))
            .OrderBy(s => s.OccurredAtUtc, StringComparer.Ordinal)
            .ThenBy(s => s.ProductId, StringComparer.Ordinal)
            .ThenBy(s => s.EventType)
            .ThenBy(s => s.SignalId, StringComparer.Ordinal)
            .ToList();

        var lastSelection = ordered
            .LastOrDefault(s => s.EventType == InteractionEventKind.Selection);
        if (lastSelection == null)
            return null;

        string productId = lastSelection.ProductId;

        bool hasConfirmForProduct = ordered.Any(s =>
            string.Equals(s.ProductId, productId, StringComparison.Ordinal) &&
            s.EventType == InteractionEventKind.ConfirmIntent);
        if (hasConfirmForProduct)
            return productId;

        long dwellForProduct = ordered
            .Where(s => string.Equals(s.ProductId, productId, StringComparison.Ordinal) &&
                        s.EventType == InteractionEventKind.Dwell)
            .Sum(s => (long)Math.Max(0, s.DurationMs ?? 0));
        if (dwellForProduct >= 2000)
            return productId;

        // Last selection is only treated as final when it aligns with dominant dwell attention.
        string? longestDwellProduct = FindLongestDwellProductId(ordered);
        return string.Equals(longestDwellProduct, productId, StringComparison.Ordinal) ? productId : null;
    }

    private static string? FindLongestDwellProductId(List<InteractionSignal> signals)
    {
        return signals
            .Where(s => s.EventType == InteractionEventKind.Dwell && !string.IsNullOrWhiteSpace(s.ProductId))
            .GroupBy(s => s.ProductId, StringComparer.Ordinal)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalDwellMs = g.Sum(x => Math.Max(0, x.DurationMs ?? 0))
            })
            .OrderByDescending(x => x.TotalDwellMs)
            .ThenBy(x => x.ProductId, StringComparer.Ordinal)
            .Select(x => x.ProductId)
            .FirstOrDefault();
    }

    private static bool IsCompareExitEvent(InteractionEventKind eventType)
    {
        return eventType == InteractionEventKind.Selection
               || eventType == InteractionEventKind.ConfirmIntent
               || eventType == InteractionEventKind.ContextChange;
    }

    private static bool IsSwitchCandidateEvent(InteractionEventKind eventType)
    {
        // Keep switch metrics tied to discrete intent/navigation events and ignore noisy micro-gestures
        // (e.g., swipe or variant interactions mapped through non-navigation event types).
        return eventType == InteractionEventKind.Selection
               || eventType == InteractionEventKind.Compare
               || eventType == InteractionEventKind.ConfirmIntent
               || eventType == InteractionEventKind.ContextChange;
    }

    private static bool IsCompareSwitchCandidateEvent(InteractionEventKind eventType)
    {
        // During compare windows, dwell product transitions carry real attention shifts between A/B products.
        return IsSwitchCandidateEvent(eventType) || eventType == InteractionEventKind.Dwell;
    }

    private static DecisionConvergenceComputation ComputeDecisionConvergence(
        List<InteractionSignal> signals,
        CompareComputation compareComputation,
        string? finalSelectedProductId)
    {
        long totalDwellMs = signals
            .Where(s => s.EventType == InteractionEventKind.Dwell)
            .Sum(s => (long)Math.Max(0, s.DurationMs ?? 0));

        long dominantDwellMs = signals
            .Where(s => s.EventType == InteractionEventKind.Dwell && !string.IsNullOrWhiteSpace(s.ProductId))
            .GroupBy(s => s.ProductId, StringComparer.Ordinal)
            .Select(g => g.Sum(x => (long)Math.Max(0, x.DurationMs ?? 0)))
            .DefaultIfEmpty(0L)
            .Max();

        double dwellConcentration = totalDwellMs <= 0 ? 0.0 : Clamp01((double)dominantDwellMs / totalDwellMs);
        double compareSwitchPenalty = NormalizePenalty(compareComputation.CompareSwitchCount);
        double explorationSwitchPenalty = NormalizePenalty(compareComputation.ExplorationSwitchCount);
        double selectionPresence = string.IsNullOrWhiteSpace(finalSelectedProductId) ? 0.0 : 1.0;

        double score = Clamp01(
            0.40 * dwellConcentration +
            0.30 * (1.0 - compareSwitchPenalty) +
            0.20 * (1.0 - explorationSwitchPenalty) +
            0.10 * selectionPresence);
        score = Round4(score);

        return new DecisionConvergenceComputation(
            score,
            score >= 0.67 ? DecisionConvergenceLevel.High :
            score >= 0.34 ? DecisionConvergenceLevel.Medium :
            DecisionConvergenceLevel.Low);
    }

    private static double NormalizePenalty(int count)
    {
        if (count <= 0) return 0.0;
        return Clamp01(count / (count + 2.0));
    }

    private static long PositiveDurationMs(DateTime start, DateTime end)
    {
        if (end <= start) return 0;
        return (long)(end - start).TotalMilliseconds;
    }

    private static DateTime ParseUtcOrThrow(string value)
    {
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            throw new ArgumentException($"Timestamp must be ISO 8601 parseable ({Schema.UtcTimestampFormatDescription}).");
        if (dt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Timestamp must be UTC (Kind=Utc or Z offset).");
        return dt;
    }

    private static StateType InferStateFromInteraction(InteractionSignal signal)
    {
        return signal.EventType switch
        {
            InteractionEventKind.Compare => StateType.Comparison,
            InteractionEventKind.Dwell => StateType.Hesitation,
            InteractionEventKind.Selection => StateType.Exploration,
            InteractionEventKind.ConfirmIntent => StateType.ReadyToConfirm,
            _ => StateType.Neutral
        };
    }

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

    private readonly struct CompareComputation
    {
        public int CompareSwitchCount { get; }
        public int ExplorationSwitchCount { get; }
        public long TotalCompareTimeMs { get; }

        public CompareComputation(
            int compareSwitchCount,
            int explorationSwitchCount,
            long totalCompareTimeMs)
        {
            CompareSwitchCount = compareSwitchCount;
            ExplorationSwitchCount = explorationSwitchCount;
            TotalCompareTimeMs = totalCompareTimeMs;
        }
    }

    private readonly struct DecisionConvergenceComputation
    {
        public double Score { get; }
        public DecisionConvergenceLevel Level { get; }

        public DecisionConvergenceComputation(double score, DecisionConvergenceLevel level)
        {
            Score = score;
            Level = level;
        }
    }
}

