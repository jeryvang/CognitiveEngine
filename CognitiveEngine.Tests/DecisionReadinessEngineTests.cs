using System.Collections.Generic;
using CognitiveEngine.Core;
using CognitiveEngine.Core.TrialIntelligence;
using Xunit;

namespace CognitiveEngine.Tests;

public class DecisionReadinessEngineTests
{
    [Fact]
    public void Evaluate_DecisiveJourney_ProducesHighReadinessAndDecisiveSummary()
    {
        var signals = new List<InteractionSignal>
        {
            new() { SignalId = "1", OccurredAtUtc = "2025-03-24T12:00:00.0000000Z", EventType = InteractionEventKind.Selection, ProductId = "p-a" },
            new() { SignalId = "2", OccurredAtUtc = "2025-03-24T12:00:01.0000000Z", EventType = InteractionEventKind.ConfirmIntent, ProductId = "p-a" }
        };
        var leaning = new List<LeaningIndicator>
        {
            new() { ProductId = "p-a", LeaningScore = 0.95, Confidence = 0.9, Rank = 1 },
            new() { ProductId = "p-b", LeaningScore = 0.10, Confidence = 0.9, Rank = 2 }
        };

        var (readiness, _, summary) = DecisionReadinessEngine.Evaluate(signals, leaning, new List<FrictionEpisode>());

        Assert.Equal(DecisionReadinessLevel.High, readiness.ReadinessLevel);
        Assert.True(readiness.IsReadyToConfirm);
        Assert.Equal(JourneyClassification.Decisive, summary.JourneyClassification);
    }

    [Fact]
    public void Evaluate_IndecisiveJourney_WithFriction_ProducesLowReadiness()
    {
        var signals = new List<InteractionSignal>
        {
            new() { SignalId = "1", OccurredAtUtc = "2025-03-24T12:00:00.0000000Z", EventType = InteractionEventKind.Compare, ProductId = "p-a", ComparisonPartnerProductId = "p-b" },
            new() { SignalId = "2", OccurredAtUtc = "2025-03-24T12:00:01.0000000Z", EventType = InteractionEventKind.Dwell, ProductId = "p-a", DurationMs = 3000 }
        };
        var leaning = new List<LeaningIndicator>
        {
            new() { ProductId = "p-a", LeaningScore = 0.45, Confidence = 0.4, Rank = 1 },
            new() { ProductId = "p-b", LeaningScore = 0.40, Confidence = 0.4, Rank = 2 }
        };
        var friction = new List<FrictionEpisode>
        {
            new() { EpisodeId = "f1", ProductId = "p-a", StartedAtUtc = "2025-03-24T12:00:00.0000000Z", EndedAtUtc = "2025-03-24T12:00:01.0000000Z", FrictionKind = FrictionKind.ComparisonLoop, BottleneckTag = "comparison-loop", EventCount = 3, TotalDwellMs = 0 },
            new() { EpisodeId = "f2", ProductId = "p-a", StartedAtUtc = "2025-03-24T12:00:02.0000000Z", EndedAtUtc = "2025-03-24T12:00:03.0000000Z", FrictionKind = FrictionKind.HesitationBurst, BottleneckTag = "hesitation-on-shortlist", EventCount = 2, TotalDwellMs = 3000 }
        };

        var (readiness, confidence, summary) = DecisionReadinessEngine.Evaluate(signals, leaning, friction);

        Assert.Equal(DecisionReadinessLevel.Low, readiness.ReadinessLevel);
        Assert.Equal(ConfidenceTrend.Unstable, confidence.Trend);
        Assert.True(summary.JourneyClassification == JourneyClassification.Indecisive || summary.JourneyClassification == JourneyClassification.Struggling);
    }

    [Fact]
    public void Evaluate_SameInput_ProducesStableDeterministicOutput()
    {
        var signals = new List<InteractionSignal>
        {
            new() { SignalId = "1", OccurredAtUtc = "2025-03-24T12:00:00.0000000Z", EventType = InteractionEventKind.Selection, ProductId = "p-a" }
        };
        var leaning = new List<LeaningIndicator> { new() { ProductId = "p-a", LeaningScore = 0.7, Confidence = 0.6, Rank = 1 } };
        var friction = new List<FrictionEpisode>();

        var r1 = DecisionReadinessEngine.Evaluate(signals, leaning, friction);
        var r2 = DecisionReadinessEngine.Evaluate(signals, leaning, friction);

        Assert.Equal(r1.readiness.ReadinessScore, r2.readiness.ReadinessScore);
        Assert.Equal(r1.confidence.StabilityScore, r2.confidence.StabilityScore);
        Assert.Equal(r1.summary.SummaryTag, r2.summary.SummaryTag);
    }

    [Fact]
    public void Evaluate_WithConfidenceTrace_UsesTraceBasisAndStabilizedTrend()
    {
        var signals = new List<InteractionSignal>
        {
            new() { SignalId = "1", OccurredAtUtc = "2025-03-24T12:00:00.0000000Z", EventType = InteractionEventKind.Selection, ProductId = "p-a" }
        };
        var leaning = new List<LeaningIndicator> { new() { ProductId = "p-a", LeaningScore = 0.5, Confidence = 0.4, Rank = 1 } };
        var friction = new List<FrictionEpisode>();

        var trace = new List<(string, float, StateType)>
        {
            ("2025-03-24T12:00:00.0000000Z", 0.64f, StateType.Exploration),
            ("2025-03-24T12:00:01.0000000Z", 0.63f, StateType.Exploration),
            ("2025-03-24T12:00:02.0000000Z", 0.62f, StateType.Comparison)
        };

        var (_, confidence, _) = DecisionReadinessEngine.Evaluate(signals, leaning, friction, trace);

        Assert.Equal("confidence_trace_v1(mean,variance,half_delta,friction_penalty)", confidence.Basis);
        Assert.Equal(ConfidenceTrend.Stabilized, confidence.Trend);
    }

    [Fact]
    public void Evaluate_WithConfidenceTrace_HighVariance_Unstable()
    {
        var signals = new List<InteractionSignal>();
        var leaning = new List<LeaningIndicator> { new() { ProductId = "p-a", LeaningScore = 0.5, Confidence = 0.8, Rank = 1 } };
        var friction = new List<FrictionEpisode>();

        var trace = new List<(string, float, StateType)>
        {
            ("2025-03-24T12:00:00.0000000Z", 0.1f, StateType.Neutral),
            ("2025-03-24T12:00:01.0000000Z", 0.9f, StateType.Hesitation),
            ("2025-03-24T12:00:02.0000000Z", 0.15f, StateType.Comparison),
            ("2025-03-24T12:00:03.0000000Z", 0.85f, StateType.Exploration)
        };

        var (_, confidence, _) = DecisionReadinessEngine.Evaluate(signals, leaning, friction, trace);

        Assert.Equal(ConfidenceTrend.Unstable, confidence.Trend);
        Assert.Contains("Confidence trace", confidence.Interpretation);
    }

    [Fact]
    public void Evaluate_HealthyCompareFlow_DoesNotOverPenalizeReadiness()
    {
        var signals = new List<InteractionSignal>
        {
            new() { SignalId = "1", OccurredAtUtc = "2025-03-24T12:00:00.0000000Z", EventType = InteractionEventKind.Compare, ProductId = "nike", ComparisonPartnerProductId = "crocs" },
            new() { SignalId = "2", OccurredAtUtc = "2025-03-24T12:00:01.0000000Z", EventType = InteractionEventKind.Dwell, ProductId = "nike", DurationMs = 1800 },
            new() { SignalId = "3", OccurredAtUtc = "2025-03-24T12:00:02.0000000Z", EventType = InteractionEventKind.Selection, ProductId = "nike" }
        };
        var leaning = new List<LeaningIndicator>
        {
            new() { ProductId = "nike", LeaningScore = 0.82, Confidence = 0.7, Rank = 1 },
            new() { ProductId = "crocs", LeaningScore = 0.62, Confidence = 0.7, Rank = 2 }
        };
        var friction = new List<FrictionEpisode>
        {
            new() { EpisodeId = "f1", ProductId = "nike", StartedAtUtc = "2025-03-24T12:00:00.0000000Z", EndedAtUtc = "2025-03-24T12:00:02.0000000Z", FrictionKind = FrictionKind.ComparisonLoop, BottleneckTag = "comparison-loop", EventCount = 2, TotalDwellMs = 1800 }
        };

        var (readiness, _, summary) = DecisionReadinessEngine.Evaluate(signals, leaning, friction);

        Assert.True(readiness.ReadinessScore >= 0.45);
        Assert.NotEqual(DecisionReadinessLevel.Low, readiness.ReadinessLevel);
        Assert.NotEqual(JourneyClassification.Indecisive, summary.JourneyClassification);
    }
}
