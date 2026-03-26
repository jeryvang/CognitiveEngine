using System;
using System.Collections.Generic;
using CognitiveEngine.Core.TrialIntelligence;
using Xunit;

namespace CognitiveEngine.Tests;

public class ProductAggregationEngineTests
{
    [Fact]
    public void AggregateByProduct_ComputesCountsAcrossSessions()
    {
        const string exportedAt = "2025-03-24T12:00:00.0000000Z";

        var s1 = new SessionContract
        {
            SessionId = "s1",
            ExportedAtUtc = exportedAt,
            InteractionSignals =
            {
                new InteractionSignal
                {
                    SignalId = "11111111111111111111111111111111",
                    OccurredAtUtc = "2025-03-24T12:00:01.0000000Z",
                    EventType = InteractionEventKind.Selection,
                    ProductId = "p-a"
                },
                new InteractionSignal
                {
                    SignalId = "22222222222222222222222222222222",
                    OccurredAtUtc = "2025-03-24T12:00:02.0000000Z",
                    EventType = InteractionEventKind.Dwell,
                    ProductId = "p-a",
                    DurationMs = 2500
                },
                new InteractionSignal
                {
                    SignalId = "33333333333333333333333333333333",
                    OccurredAtUtc = "2025-03-24T12:00:03.0000000Z",
                    EventType = InteractionEventKind.Compare,
                    ProductId = "p-a"
                }
            },
            PreferenceSignals = { new PreferenceSignal { SignalId = "ps1", DerivedAtUtc = exportedAt, ProductId = "p-a", PreferenceStrength = 0.7, Basis = "test" } },
            LeaningIndicators = { new LeaningIndicator { ProductId = "p-a", LeaningScore = 0.7, Confidence = 0.7, Rank = 1 } }
        };

        var s2 = new SessionContract
        {
            SessionId = "s2",
            ExportedAtUtc = exportedAt,
            InteractionSignals =
            {
                new InteractionSignal
                {
                    SignalId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                    OccurredAtUtc = "2025-03-24T12:00:01.5000000Z",
                    EventType = InteractionEventKind.Selection,
                    ProductId = "p-a"
                },
                new InteractionSignal
                {
                    SignalId = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                    OccurredAtUtc = "2025-03-24T12:00:02.5000000Z",
                    EventType = InteractionEventKind.Dwell,
                    ProductId = "p-a",
                    DurationMs = 6000
                },
                new InteractionSignal
                {
                    SignalId = "cccccccccccccccccccccccccccccccc",
                    OccurredAtUtc = "2025-03-24T12:00:04.0000000Z",
                    EventType = InteractionEventKind.Selection,
                    ProductId = "p-b"
                }
            },
            PreferenceSignals =
            {
                new PreferenceSignal { SignalId = "ps2a", DerivedAtUtc = exportedAt, ProductId = "p-a", PreferenceStrength = 0.2, Basis = "test" },
                new PreferenceSignal { SignalId = "ps2b", DerivedAtUtc = exportedAt, ProductId = "p-b", PreferenceStrength = 0.9, Basis = "test" }
            },
            LeaningIndicators =
            {
                new LeaningIndicator { ProductId = "p-b", LeaningScore = 0.9, Confidence = 0.7, Rank = 1 },
                new LeaningIndicator { ProductId = "p-a", LeaningScore = 0.2, Confidence = 0.7, Rank = 2 }
            }
        };

        var aggregates = ProductAggregationEngine.AggregateByProduct("agg-1", exportedAt, new[] { s1, s2 });
        Assert.Equal(2, aggregates.Count);

        var a = aggregates.Find(x => x.ProductId == "p-a")!;
        Assert.Equal(2, a.SessionsContributed);
        Assert.Equal(2, a.Attraction.SelectionCount);
        Assert.Equal(8500, a.Engagement.TotalDwellMs);
        Assert.Equal(2, a.Engagement.FocusedViewCount); // 2500 + 6000 both >= 2000
        Assert.Equal(0, a.Engagement.ReturnVisitCount);
        Assert.Equal(1 + 0, a.ComparisonPatterns.CompareEventsCount); // one compare in s1
        Assert.Equal(1, a.ComparisonPatterns.HesitationAlignedEventCount); // one dwell >= 5000 (6000)
        Assert.Empty(a.ComparisonPatterns.UniqueComparisonPartnerProductIds);

        var b = aggregates.Find(x => x.ProductId == "p-b")!;
        Assert.Equal(1, b.SessionsContributed);
        Assert.Equal(1, b.Attraction.SelectionCount);
    }

    [Fact]
    public void AggregateByProduct_IsDeterministicAcrossSessionOrdering_AndGoldenJsonLocksShape()
    {
        const string exportedAt = "2025-03-24T12:00:00.0000000Z";

        var s1 = new SessionContract
        {
            SessionId = "s1",
            ExportedAtUtc = exportedAt,
            InteractionSignals =
            {
                new InteractionSignal
                {
                    SignalId = "11111111111111111111111111111111",
                    OccurredAtUtc = "2025-03-24T12:00:01.0000000Z",
                    EventType = InteractionEventKind.Selection,
                    ProductId = "p-a"
                }
            },
            PreferenceSignals = { new PreferenceSignal { SignalId = "ps1", DerivedAtUtc = exportedAt, ProductId = "p-a", PreferenceStrength = 0.4, Basis = "test" } },
            LeaningIndicators = { new LeaningIndicator { ProductId = "p-a", LeaningScore = 0.4, Confidence = 0.4, Rank = 1 } }
        };

        var s2 = new SessionContract
        {
            SessionId = "s2",
            ExportedAtUtc = exportedAt,
            InteractionSignals =
            {
                new InteractionSignal
                {
                    SignalId = "22222222222222222222222222222222",
                    OccurredAtUtc = "2025-03-24T12:00:01.0000000Z",
                    EventType = InteractionEventKind.Dwell,
                    ProductId = "p-a",
                    DurationMs = 2000
                },
                new InteractionSignal
                {
                    SignalId = "33333333333333333333333333333333",
                    OccurredAtUtc = "2025-03-24T12:00:02.0000000Z",
                    EventType = InteractionEventKind.Selection,
                    ProductId = "p-b"
                }
            },
            PreferenceSignals =
            {
                new PreferenceSignal { SignalId = "ps2a", DerivedAtUtc = exportedAt, ProductId = "p-a", PreferenceStrength = 0.6, Basis = "test" },
                new PreferenceSignal { SignalId = "ps2b", DerivedAtUtc = exportedAt, ProductId = "p-b", PreferenceStrength = 0.2, Basis = "test" }
            },
            LeaningIndicators =
            {
                new LeaningIndicator { ProductId = "p-a", LeaningScore = 0.6, Confidence = 0.6, Rank = 1 },
                new LeaningIndicator { ProductId = "p-b", LeaningScore = 0.2, Confidence = 0.6, Rank = 2 }
            }
        };

        var a1 = ProductAggregationEngine.AggregateByProduct("agg-x", exportedAt, new[] { s1, s2 });
        var a2 = ProductAggregationEngine.AggregateByProduct("agg-x", exportedAt, new[] { s2, s1 });

        Assert.Equal(ExportJson.Serialize(a1), ExportJson.Serialize(a2));

        // Lock exact JSON for the first product in stable product_id order (p-a then p-b).
        const string expectedFirst =
            "{\"schema_version\":\"1.0.0\",\"aggregate_id\":\"agg-x\",\"exported_at_utc\":\"2025-03-24T12:00:00.0000000Z\",\"product_id\":\"p-a\",\"sessions_contributed\":2,\"attraction\":{\"aggregate_attraction_score\":0.5,\"selection_count\":1,\"first_touch_rank\":1},\"engagement\":{\"total_dwell_ms\":2000,\"focused_view_count\":1,\"return_visit_count\":0},\"comparison_patterns\":{\"compare_events_count\":0,\"unique_comparison_partner_product_ids\":[],\"hesitation_aligned_event_count\":0}}";

        Assert.Equal(expectedFirst, ExportJson.Serialize(a1[0]));
    }

    [Fact]
    public void AggregateByProduct_CollectsComparisonPartners_AndHonorsThresholdOptions()
    {
        const string exportedAt = "2025-03-24T12:00:00.0000000Z";
        var s1 = new SessionContract
        {
            SessionId = "s1",
            ExportedAtUtc = exportedAt,
            InteractionSignals =
            {
                new InteractionSignal
                {
                    SignalId = "44444444444444444444444444444444",
                    OccurredAtUtc = "2025-03-24T12:00:01.0000000Z",
                    EventType = InteractionEventKind.Compare,
                    ProductId = "p-a",
                    ComparisonPartnerProductId = "p-b",
                    ComparisonPartnerProductIds = new List<string> { "p-c", "p-b" }
                },
                new InteractionSignal
                {
                    SignalId = "55555555555555555555555555555555",
                    OccurredAtUtc = "2025-03-24T12:00:02.0000000Z",
                    EventType = InteractionEventKind.Dwell,
                    ProductId = "p-a",
                    DurationMs = 3500
                }
            },
            PreferenceSignals = { new PreferenceSignal { SignalId = "ps", DerivedAtUtc = exportedAt, ProductId = "p-a", PreferenceStrength = 0.5, Basis = "test" } },
            LeaningIndicators = { new LeaningIndicator { ProductId = "p-a", LeaningScore = 0.5, Confidence = 0.5, Rank = 1 } }
        };

        var options = new TrialIntelligenceOptions
        {
            FocusedViewThresholdMs = 3000,
            HesitationAlignedDwellThresholdMs = 3000
        };

        var aggregates = ProductAggregationEngine.AggregateByProduct("agg-opts", exportedAt, new[] { s1 }, options);
        var a = Assert.Single(aggregates);

        Assert.Equal(1, a.Engagement.FocusedViewCount);
        Assert.Equal(1, a.ComparisonPatterns.HesitationAlignedEventCount);
        Assert.Equal(new[] { "p-b", "p-c" }, a.ComparisonPatterns.UniqueComparisonPartnerProductIds);
    }
}

