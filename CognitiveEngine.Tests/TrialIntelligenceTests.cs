using System;
using CognitiveEngine.Core.TrialIntelligence;
using Newtonsoft.Json;
using Xunit;

namespace CognitiveEngine.Tests;

public class TrialIntelligenceTests
{
    private const string FixedSessionJson =
        "{\"schema_version\":\"1.0.0\",\"session_id\":\"sess-001\",\"exported_at_utc\":\"2025-03-24T12:00:00.0000000Z\",\"interaction_signals\":[{\"signal_id\":\"a1b2c3d4e5f6478990a1b2c3d4e5f601\",\"occurred_at_utc\":\"2025-03-24T12:00:01.0000000Z\",\"event_type\":\"compare\",\"product_id\":\"p-a\",\"intensity\":0.5,\"duration_ms\":1200}],\"preference_signals\":[{\"signal_id\":\"b2c3d4e5f6478990a1b2c3d4e5f6012\",\"derived_at_utc\":\"2025-03-24T12:00:02.0000000Z\",\"product_id\":\"p-a\",\"preference_strength\":0.7,\"basis\":\"dwell_weighted\"}],\"leaning_indicators\":[{\"product_id\":\"p-a\",\"leaning_score\":0.8,\"confidence\":0.6,\"rank\":1},{\"product_id\":\"p-b\",\"leaning_score\":0.2,\"confidence\":0.6,\"rank\":2}],\"friction_episodes\":[],\"decision_readiness\":{\"readiness_score\":0.5,\"readiness_level\":\"medium\",\"is_ready_to_confirm\":false,\"dominant_product_id\":\"p-a\",\"basis\":\"v1\"},\"confidence_interpretation\":{\"stability_score\":0.6,\"trend\":\"improving\",\"interpretation\":\"confidence is forming but still variable\",\"basis\":\"leaning_friction_proxy_v1\"},\"struggle_decision_summary\":{\"journey_classification\":\"balanced\",\"struggle_score\":0.2,\"decision_signal_score\":0.5,\"summary_tag\":\"mixed-signals\"}}";

    private const string FixedAggregateJson =
        "{\"schema_version\":\"1.0.0\",\"aggregate_id\":\"agg-trial-1\",\"exported_at_utc\":\"2025-03-24T12:00:00.0000000Z\",\"product_id\":\"p-a\",\"sessions_contributed\":3,\"attraction\":{\"aggregate_attraction_score\":0.42,\"selection_count\":5,\"first_touch_rank\":2},\"engagement\":{\"total_dwell_ms\":9000,\"focused_view_count\":4,\"return_visit_count\":1},\"comparison_patterns\":{\"compare_events_count\":2,\"unique_comparison_partner_product_ids\":[\"p-b\",\"p-c\"],\"hesitation_aligned_event_count\":0},\"friction_hotspots\":{\"total_friction_episodes_count\":0,\"total_friction_event_count\":0,\"total_friction_dwell_ms\":0,\"hotspots\":[]},\"struggle_trends\":{\"average_product_struggle_score\":0.0,\"struggle_trend_direction\":\"stable\",\"journey_classification_counts\":{\"indecisive_count\":0,\"struggling_count\":0,\"balanced_count\":0,\"decisive_count\":0}},\"readiness_trends\":{\"dominant_product_sessions_contributing\":0,\"average_readiness_score_when_dominant\":0.0,\"ready_to_confirm_sessions_count_when_dominant\":0,\"readiness_level_counts_when_dominant\":{\"low_count\":0,\"medium_count\":0,\"high_count\":0},\"readiness_trend_direction\":\"stable\"}}";

    [Fact]
    public void SessionContract_RoundTrip_PreservesValues()
    {
        var original = new SessionContract
        {
            SessionId = "s1",
            ExportedAtUtc = new DateTime(2025, 3, 24, 10, 0, 0, DateTimeKind.Utc).ToString("o"),
            InteractionSignals =
            {
                new InteractionSignal
                {
                    SignalId = Guid.Parse("11111111-1111-1111-1111-111111111111").ToString("N"),
                    OccurredAtUtc = new DateTime(2025, 3, 24, 10, 0, 1, DateTimeKind.Utc).ToString("o"),
                    EventType = InteractionEventKind.Selection,
                    ProductId = "prod-1",
                    Intensity = 0.25,
                    DurationMs = 500
                }
            },
            PreferenceSignals =
            {
                new PreferenceSignal
                {
                    SignalId = Guid.Parse("22222222-2222-2222-2222-222222222222").ToString("N"),
                    DerivedAtUtc = new DateTime(2025, 3, 24, 10, 0, 2, DateTimeKind.Utc).ToString("o"),
                    ProductId = "prod-1",
                    PreferenceStrength = 0.9,
                    Basis = "selection_burst"
                }
            },
            LeaningIndicators =
            {
                new LeaningIndicator { ProductId = "prod-1", LeaningScore = 1.0, Confidence = 0.5, Rank = 1 }
            },
            FrictionEpisodes =
            {
                new FrictionEpisode
                {
                    EpisodeId = "ep-1",
                    ProductId = "prod-1",
                    StartedAtUtc = new DateTime(2025, 3, 24, 10, 0, 1, DateTimeKind.Utc).ToString("o"),
                    EndedAtUtc = new DateTime(2025, 3, 24, 10, 0, 3, DateTimeKind.Utc).ToString("o"),
                    FrictionKind = FrictionKind.HesitationBurst,
                    BottleneckTag = "hesitation-on-shortlist",
                    EventCount = 2,
                    TotalDwellMs = 1500
                }
            },
            DecisionReadiness = new DecisionReadinessAssessment
            {
                ReadinessScore = 0.8,
                ReadinessLevel = DecisionReadinessLevel.High,
                IsReadyToConfirm = true,
                DominantProductId = "prod-1",
                Basis = "v1(confirm,leaning,gap,friction_penalty)"
            },
            ConfidenceInterpretation = new ConfidenceInterpretation
            {
                StabilityScore = 0.75,
                Trend = ConfidenceTrend.Stabilized,
                Interpretation = "confidence has stabilized with low volatility",
                Basis = "leaning_friction_proxy_v1"
            },
            StruggleDecisionSummary = new StruggleDecisionSummary
            {
                JourneyClassification = JourneyClassification.Decisive,
                StruggleScore = 0.1,
                DecisionSignalScore = 0.8,
                SummaryTag = "decision-led"
            }
        };

        var json = ExportJson.Serialize(original);
        var back = ExportJson.Deserialize<SessionContract>(json);

        Assert.Equal(original.SchemaVersion, back.SchemaVersion);
        Assert.Equal(original.SessionId, back.SessionId);
        Assert.Equal(original.ExportedAtUtc, back.ExportedAtUtc);
        Assert.Single(back.InteractionSignals);
        Assert.Equal(InteractionEventKind.Selection, back.InteractionSignals[0].EventType);
        Assert.Equal(0.25, back.InteractionSignals[0].Intensity);
        Assert.Single(back.PreferenceSignals);
        Assert.Single(back.LeaningIndicators);
        Assert.Single(back.FrictionEpisodes);
        Assert.NotNull(back.DecisionReadiness);
        Assert.NotNull(back.ConfidenceInterpretation);
        Assert.NotNull(back.StruggleDecisionSummary);
    }

    [Fact]
    public void SessionContract_GoldenJson_MatchesExpectedShape()
    {
        var parsed = ExportJson.Deserialize<SessionContract>(FixedSessionJson);
        ContractValidation.ValidateSessionOrThrow(parsed);
        var again = ExportJson.Serialize(parsed);
        Assert.Equal(FixedSessionJson, again);
    }

    [Fact]
    public void ProductAggregate_RoundTrip_PreservesValues()
    {
        var original = new ProductAggregate
        {
            AggregateId = "run-1",
            ExportedAtUtc = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc).ToString("o"),
            ProductId = "p-x",
            SessionsContributed = 10,
            Attraction = new ProductAttraction
            {
                AggregateAttractionScore = 0.1,
                SelectionCount = 2,
                FirstTouchRank = null
            },
            Engagement = new ProductEngagement
            {
                TotalDwellMs = 100,
                FocusedViewCount = 3,
                ReturnVisitCount = 0
            },
            ComparisonPatterns = new ProductComparison
            {
                CompareEventsCount = 1,
                UniqueComparisonPartnerProductIds = { "z", "a" },
                HesitationAlignedEventCount = 7
            }
        };

        var json = ExportJson.Serialize(original);
        var back = ExportJson.Deserialize<ProductAggregate>(json);

        Assert.Equal(10, back.SessionsContributed);
        Assert.Null(back.Attraction.FirstTouchRank);
        Assert.Equal(2, back.ComparisonPatterns.UniqueComparisonPartnerProductIds.Count);
    }

    [Fact]
    public void ProductAggregate_GoldenJson_MatchesExpectedShape()
    {
        var parsed = ExportJson.Deserialize<ProductAggregate>(FixedAggregateJson);
        ContractValidation.ValidateAggregateOrThrow(parsed);
        var again = ExportJson.Serialize(parsed);
        Assert.Equal(FixedAggregateJson, again);
    }

    [Fact]
    public void Deserialize_Session_MissingRequired_Throws()
    {
        const string bad = "{\"session_id\":\"x\",\"exported_at_utc\":\"2025-03-24T12:00:00.0000000Z\",\"interaction_signals\":[],\"preference_signals\":[],\"leaning_indicators\":[]}";
        Assert.Throws<JsonSerializationException>(() => ExportJson.Deserialize<SessionContract>(bad));
    }

    [Fact]
    public void ValidateSession_InvalidUtc_Throws()
    {
        var c = new SessionContract
        {
            SessionId = "s",
            ExportedAtUtc = "not-a-date",
            InteractionSignals = new(),
            PreferenceSignals = new(),
            LeaningIndicators = new()
        };
        Assert.Throws<ArgumentException>(() => ContractValidation.ValidateSessionOrThrow(c));
    }

    [Fact]
    public void ValidateSession_NonUtcTimestamp_Throws()
    {
        var local = new DateTime(2025, 3, 24, 12, 0, 0, DateTimeKind.Local).ToString("o");
        var c = new SessionContract
        {
            SessionId = "s",
            ExportedAtUtc = local,
            InteractionSignals = new(),
            PreferenceSignals = new(),
            LeaningIndicators = new()
        };
        Assert.Throws<ArgumentException>(() => ContractValidation.ValidateSessionOrThrow(c));
    }

    [Fact]
    public void ValidateAggregate_MissingNested_Throws()
    {
        var c = new ProductAggregate
        {
            AggregateId = "a",
            ExportedAtUtc = new DateTime(2025, 3, 24, 12, 0, 0, DateTimeKind.Utc).ToString("o"),
            ProductId = "p",
            SessionsContributed = 1,
            Attraction = null!
        };
        Assert.Throws<ArgumentException>(() => ContractValidation.ValidateAggregateOrThrow(c));
    }

    [Fact]
    public void ExportJson_UsesSameRulesAsExposedSettings()
    {
        var s = ExportJson.CreateSettings();
        Assert.Equal(ExportJson.Settings.NullValueHandling, s.NullValueHandling);
        Assert.Equal(ExportJson.Settings.MissingMemberHandling, s.MissingMemberHandling);
    }

    [Fact]
    public void SessionContract_ComparePartnerFields_RoundTripWithNewtonsoft()
    {
        var c = new SessionContract
        {
            SessionId = "s-compare",
            ExportedAtUtc = new DateTime(2025, 3, 24, 12, 0, 0, DateTimeKind.Utc).ToString("o"),
            InteractionSignals =
            {
                new InteractionSignal
                {
                    SignalId = Guid.Parse("33333333-3333-3333-3333-333333333333").ToString("N"),
                    OccurredAtUtc = new DateTime(2025, 3, 24, 12, 0, 1, DateTimeKind.Utc).ToString("o"),
                    EventType = InteractionEventKind.Compare,
                    ProductId = "p-a",
                    ComparisonPartnerProductId = "p-b",
                    ComparisonPartnerProductIds = new() { "p-c", "p-d" }
                }
            },
            PreferenceSignals = new(),
            LeaningIndicators = new()
        };

        var json = ExportJson.Serialize(c);
        Assert.Contains("\"comparison_partner_product_id\":\"p-b\"", json);
        Assert.Contains("\"comparison_partner_product_ids\":[\"p-c\",\"p-d\"]", json);

        var back = ExportJson.Deserialize<SessionContract>(json);
        Assert.Equal("p-b", back.InteractionSignals[0].ComparisonPartnerProductId);
        Assert.Equal(2, back.InteractionSignals[0].ComparisonPartnerProductIds!.Count);
    }
}
