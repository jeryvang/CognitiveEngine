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

            var frictionEpisodesForProduct = contributedSessions
                .SelectMany(s => s.FrictionEpisodes ?? new List<FrictionEpisode>())
                .Where(e => e.ProductId == productId)
                .ToList();

            var frictionHotspots = frictionEpisodesForProduct
                .GroupBy(e => new { e.FrictionKind, e.BottleneckTag })
                .Select(g => new ProductFrictionHotspot
                {
                    FrictionKind = g.Key.FrictionKind,
                    BottleneckTag = g.Key.BottleneckTag,
                    EpisodesCount = g.Count(),
                    TotalEventCount = g.Sum(x => x.EventCount),
                    TotalDwellMs = g.Sum(x => (long)x.TotalDwellMs)
                })
                .OrderByDescending(h => h.EpisodesCount)
                .ThenByDescending(h => h.TotalDwellMs)
                .ThenBy(h => h.FrictionKind)
                .ThenBy(h => h.BottleneckTag, StringComparer.Ordinal)
                .ToList();

            var sessionsForProductStruggle = contributedSessions
                .Where(s =>
                    (s.FrictionEpisodes ?? new List<FrictionEpisode>()).Any(e => e.ProductId == productId) ||
                    string.Equals(s.DecisionReadiness?.DominantProductId, productId, StringComparison.Ordinal))
                .OrderBy(s => s.SessionId, StringComparer.Ordinal)
                .ToList();

            var productStruggleScores = sessionsForProductStruggle
                .Select(s => Clamp01(s.StruggleDecisionSummary?.StruggleScore ?? 0.0))
                .ToList();

            double averageProductStruggleScore = productStruggleScores.Count == 0
                ? 0.0
                : Round4(MeanOrZero(productStruggleScores));

            ProductTrendDirection struggleDirection;
            if (productStruggleScores.Count < 2)
            {
                struggleDirection = ProductTrendDirection.Stable;
            }
            else
            {
                int k = productStruggleScores.Count / 2;
                double firstAvg = MeanOrZero(productStruggleScores.Take(k));
                double secondAvg = MeanOrZero(productStruggleScores.Skip(k));
                double delta = secondAvg - firstAvg;
                struggleDirection = delta >= 0.05
                    ? ProductTrendDirection.Worsening
                    : delta <= -0.05
                        ? ProductTrendDirection.Improving
                        : ProductTrendDirection.Stable;
            }

            var journeyCounts = new ProductJourneyClassificationCounts
            {
                IndecisiveCount = sessionsForProductStruggle.Count(s =>
                    s.StruggleDecisionSummary.JourneyClassification == JourneyClassification.Indecisive),
                StrugglingCount = sessionsForProductStruggle.Count(s =>
                    s.StruggleDecisionSummary.JourneyClassification == JourneyClassification.Struggling),
                BalancedCount = sessionsForProductStruggle.Count(s =>
                    s.StruggleDecisionSummary.JourneyClassification == JourneyClassification.Balanced),
                DecisiveCount = sessionsForProductStruggle.Count(s =>
                    s.StruggleDecisionSummary.JourneyClassification == JourneyClassification.Decisive)
            };

            var dominantSessions = contributedSessions
                .Where(s => string.Equals(s.DecisionReadiness?.DominantProductId, productId, StringComparison.Ordinal))
                .OrderBy(s => s.SessionId, StringComparer.Ordinal)
                .ToList();

            var dominantReadinessScores = dominantSessions
                .Select(s => s.DecisionReadiness.ReadinessScore)
                .ToList();

            double averageReadinessScoreWhenDominant = dominantReadinessScores.Count == 0
                ? 0.0
                : Round4(MeanOrZero(dominantReadinessScores));

            ProductTrendDirection readinessDirection;
            if (dominantReadinessScores.Count < 2)
            {
                readinessDirection = ProductTrendDirection.Stable;
            }
            else
            {
                int k = dominantReadinessScores.Count / 2;
                double firstAvg = MeanOrZero(dominantReadinessScores.Take(k));
                double secondAvg = MeanOrZero(dominantReadinessScores.Skip(k));
                double delta = secondAvg - firstAvg;
                readinessDirection = delta >= 0.05
                    ? ProductTrendDirection.Improving
                    : delta <= -0.05
                        ? ProductTrendDirection.Worsening
                        : ProductTrendDirection.Stable;
            }

            var readinessCounts = new ProductReadinessLevelCounts
            {
                LowCount = dominantSessions.Count(s => s.DecisionReadiness.ReadinessLevel == DecisionReadinessLevel.Low),
                MediumCount = dominantSessions.Count(s =>
                    s.DecisionReadiness.ReadinessLevel == DecisionReadinessLevel.Medium),
                HighCount = dominantSessions.Count(s => s.DecisionReadiness.ReadinessLevel == DecisionReadinessLevel.High)
            };

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
                },
                FrictionHotspots = new ProductFrictionHotspots
                {
                    TotalFrictionEpisodesCount = frictionEpisodesForProduct.Count,
                    TotalFrictionEventCount = frictionEpisodesForProduct.Sum(e => e.EventCount),
                    TotalFrictionDwellMs = frictionEpisodesForProduct.Sum(e => (long)e.TotalDwellMs),
                    Hotspots = frictionHotspots
                },
                StruggleTrends = new ProductStruggleTrends
                {
                    AverageProductStruggleScore = averageProductStruggleScore,
                    StruggleTrendDirection = struggleDirection,
                    JourneyClassificationCounts = journeyCounts
                },
                ReadinessTrends = new ProductReadinessTrends
                {
                    DominantProductSessionsContributing = dominantSessions.Count,
                    AverageReadinessScoreWhenDominant = averageReadinessScoreWhenDominant,
                    ReadyToConfirmSessionsCountWhenDominant = dominantSessions.Count(s =>
                        s.DecisionReadiness.IsReadyToConfirm),
                    ReadinessLevelCountsWhenDominant = readinessCounts,
                    ReadinessTrendDirection = readinessDirection
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

    private static double Clamp01(double v)
    {
        if (v < 0.0) return 0.0;
        if (v > 1.0) return 1.0;
        return v;
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

