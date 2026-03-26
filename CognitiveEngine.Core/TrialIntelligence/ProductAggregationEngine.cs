using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CognitiveEngine.Core.TrialIntelligence;

public static class ProductAggregationEngine
{
    public static List<ProductAggregate> AggregateByProduct(
        string aggregateId,
        string exportedAtUtc,
        IEnumerable<SessionContract> sessions)
    {
        return AggregateByProduct(aggregateId, exportedAtUtc, sessions, TrialIntelligenceOptions.Default);
    }

    public static List<ProductAggregate> AggregateByProduct(
        string aggregateId,
        string exportedAtUtc,
        IEnumerable<SessionContract> sessions,
        TrialIntelligenceOptions options)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
            throw new ArgumentException("aggregateId is required.", nameof(aggregateId));
        ThrowIfUtcMissing(nameof(exportedAtUtc), exportedAtUtc);
        if (sessions == null)
            throw new ArgumentNullException(nameof(sessions));
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        options.ValidateOrThrow();

        var sessionList = sessions.Where(s => s != null).ToList();

        var productIds = sessionList
            .SelectMany(s => (s.InteractionSignals ?? new List<InteractionSignal>()).Select(x => x.ProductId)
                             .Concat((s.PreferenceSignals ?? new List<PreferenceSignal>()).Select(x => x.ProductId))
                             .Concat((s.LeaningIndicators ?? new List<LeaningIndicator>()).Select(x => x.ProductId)))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        var output = new List<ProductAggregate>(productIds.Count);
        foreach (var productId in productIds)
        {
            var contributedSessions = sessionList
                .Where(s => SessionContainsProduct(s, productId))
                .OrderBy(s => s.SessionId, StringComparer.Ordinal)
                .ToList();

            var attractionScore = Round4(MeanOrZero(contributedSessions
                .Select(s => (s.PreferenceSignals ?? new List<PreferenceSignal>())
                    .Where(p => p.ProductId == productId)
                    .Select(p => p.PreferenceStrength)
                    .DefaultIfEmpty(0.0)
                    .Max())));

            int selectionCount = contributedSessions
                .SelectMany(s => s.InteractionSignals ?? new List<InteractionSignal>())
                .Count(x => x.ProductId == productId && x.EventType == InteractionEventKind.Selection);

            var firstTouchRanks = contributedSessions
                .Select(s => GetFirstTouchRankWithinSession(s, productId))
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .OrderBy(r => r)
                .ToList();
            int? firstTouchRank = firstTouchRanks.Count == 0 ? null : firstTouchRanks[(firstTouchRanks.Count - 1) / 2];

            long totalDwell = contributedSessions
                .SelectMany(s => s.InteractionSignals ?? new List<InteractionSignal>())
                .Where(x => x.ProductId == productId && x.EventType == InteractionEventKind.Dwell)
                .Select(x => (long)(x.DurationMs ?? 0))
                .Sum();

            int focusedViewCount = contributedSessions
                .SelectMany(s => s.InteractionSignals ?? new List<InteractionSignal>())
                .Count(x => x.ProductId == productId
                            && x.EventType == InteractionEventKind.Dwell
                            && (x.DurationMs ?? 0) >= options.FocusedViewThresholdMs);

            int returnVisitCount = contributedSessions
                .Select(s => CountReturnVisitsWithinSession(s, productId))
                .Sum();

            int compareEvents = contributedSessions
                .SelectMany(s => s.InteractionSignals ?? new List<InteractionSignal>())
                .Count(x => x.ProductId == productId && x.EventType == InteractionEventKind.Compare);

            int hesitationAligned = contributedSessions
                .SelectMany(s => s.InteractionSignals ?? new List<InteractionSignal>())
                .Count(x => x.ProductId == productId
                            && x.EventType == InteractionEventKind.Dwell
                            && (x.DurationMs ?? 0) >= options.HesitationAlignedDwellThresholdMs);

            var uniqueComparisonPartners = contributedSessions
                .SelectMany(s => s.InteractionSignals ?? new List<InteractionSignal>())
                .Where(x => x.ProductId == productId && x.EventType == InteractionEventKind.Compare)
                .SelectMany(GetComparisonPartners)
                .Where(id => !string.IsNullOrWhiteSpace(id) && !string.Equals(id, productId, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();

            output.Add(new ProductAggregate
            {
                AggregateId = aggregateId,
                ExportedAtUtc = exportedAtUtc,
                ProductId = productId,
                SessionsContributed = contributedSessions.Count,
                Attraction = new ProductAttraction
                {
                    AggregateAttractionScore = attractionScore,
                    SelectionCount = selectionCount,
                    FirstTouchRank = firstTouchRank
                },
                Engagement = new ProductEngagement
                {
                    TotalDwellMs = totalDwell,
                    FocusedViewCount = focusedViewCount,
                    ReturnVisitCount = returnVisitCount
                },
                ComparisonPatterns = new ProductComparison
                {
                    CompareEventsCount = compareEvents,
                    UniqueComparisonPartnerProductIds = uniqueComparisonPartners,
                    HesitationAlignedEventCount = hesitationAligned
                }
            });
        }

        return output;
    }

    private static bool SessionContainsProduct(SessionContract s, string productId)
    {
        if (s.InteractionSignals != null && s.InteractionSignals.Any(x => x.ProductId == productId)) return true;
        if (s.PreferenceSignals != null && s.PreferenceSignals.Any(x => x.ProductId == productId)) return true;
        if (s.LeaningIndicators != null && s.LeaningIndicators.Any(x => x.ProductId == productId)) return true;
        return false;
    }

    private static int? GetFirstTouchRankWithinSession(SessionContract s, string productId)
    {
        var interactions = (s.InteractionSignals ?? new List<InteractionSignal>())
            .Where(x => !string.IsNullOrWhiteSpace(x.ProductId))
            .OrderBy(x => x.OccurredAtUtc, StringComparer.Ordinal)
            .ThenBy(x => x.ProductId, StringComparer.Ordinal)
            .ThenBy(x => x.SignalId, StringComparer.Ordinal)
            .ToList();

        if (interactions.Count == 0) return null;

        var firstTouchByProduct = interactions
            .GroupBy(x => x.ProductId, StringComparer.Ordinal)
            .Select(g => new
            {
                ProductId = g.Key,
                FirstAtUtc = g.Min(x => x.OccurredAtUtc)
            })
            .OrderBy(x => x.FirstAtUtc, StringComparer.Ordinal)
            .ThenBy(x => x.ProductId, StringComparer.Ordinal)
            .ToList();

        int idx = firstTouchByProduct.FindIndex(x => x.ProductId == productId);
        return idx < 0 ? null : idx + 1;
    }

    private static int CountReturnVisitsWithinSession(SessionContract s, string productId)
    {
        var interactions = (s.InteractionSignals ?? new List<InteractionSignal>())
            .Where(x => !string.IsNullOrWhiteSpace(x.ProductId))
            .OrderBy(x => x.OccurredAtUtc, StringComparer.Ordinal)
            .ThenBy(x => x.ProductId, StringComparer.Ordinal)
            .ThenBy(x => x.SignalId, StringComparer.Ordinal)
            .ToList();

        int visits = 0;
        string? last = null;
        foreach (var i in interactions)
        {
            if (i.ProductId != last && i.ProductId == productId)
                visits++;
            last = i.ProductId;
        }
        return Math.Max(0, visits - 1);
    }

    private static IEnumerable<string> GetComparisonPartners(InteractionSignal signal)
    {
        if (!string.IsNullOrWhiteSpace(signal.ComparisonPartnerProductId))
            yield return signal.ComparisonPartnerProductId!;

        if (signal.ComparisonPartnerProductIds == null)
            yield break;

        foreach (var id in signal.ComparisonPartnerProductIds)
        {
            if (!string.IsNullOrWhiteSpace(id))
                yield return id;
        }
    }

    private static double MeanOrZero(IEnumerable<double> values)
    {
        double sum = 0.0;
        int n = 0;
        foreach (var v in values)
        {
            sum += v;
            n++;
        }
        return n == 0 ? 0.0 : sum / n;
    }

    private static double Round4(double v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);

    private static void ThrowIfUtcMissing(string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{fieldName} is required.", fieldName);
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            throw new ArgumentException($"{fieldName} must be ISO 8601 parseable ({Schema.UtcTimestampFormatDescription}).", fieldName);
        if (dt.Kind != DateTimeKind.Utc)
            throw new ArgumentException($"{fieldName} must be UTC (Kind=Utc or Z offset).", fieldName);
    }
}

