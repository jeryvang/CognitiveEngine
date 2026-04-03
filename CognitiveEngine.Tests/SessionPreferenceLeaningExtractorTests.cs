using System;
using System.Collections.Generic;
using CognitiveEngine.Core.TrialIntelligence;
using Xunit;

namespace CognitiveEngine.Tests;

public class SessionPreferenceLeaningExtractorTests
{
    [Fact]
    public void BuildSessionIntelligence_EmptySignals_EmitsEmptyDerivedLists()
    {
        var exportedAt = new DateTime(2025, 3, 24, 12, 0, 0, DateTimeKind.Utc).ToString("o");
        var c = SessionPreferenceLeaningExtractor.BuildSessionIntelligence("sess-0", exportedAt, new List<InteractionSignal>());

        Assert.Equal("sess-0", c.SessionId);
        Assert.Equal(exportedAt, c.ExportedAtUtc);
        Assert.Empty(c.PreferenceSignals);
        Assert.Empty(c.LeaningIndicators);
        Assert.Empty(c.InteractionSignals);
    }

    [Fact]
    public void BuildSessionIntelligence_DerivesPreferenceAndStableRanking()
    {
        const string sessionId = "sess-123";
        var exportedAt = new DateTime(2025, 3, 24, 12, 0, 0, DateTimeKind.Utc).ToString("o");

        // Intentionally unsorted input to verify deterministic normalization + ordering.
        var signals = new List<InteractionSignal>
        {
            new InteractionSignal
            {
                SignalId = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                OccurredAtUtc = new DateTime(2025, 3, 24, 12, 0, 2, DateTimeKind.Utc).ToString("o"),
                EventType = InteractionEventKind.Dwell,
                ProductId = "p-b",
                DurationMs = 4000
            },
            new InteractionSignal
            {
                SignalId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                OccurredAtUtc = new DateTime(2025, 3, 24, 12, 0, 1, DateTimeKind.Utc).ToString("o"),
                EventType = InteractionEventKind.Selection,
                ProductId = "p-a"
            },
            new InteractionSignal
            {
                SignalId = "cccccccccccccccccccccccccccccccc",
                OccurredAtUtc = new DateTime(2025, 3, 24, 12, 0, 3, DateTimeKind.Utc).ToString("o"),
                EventType = InteractionEventKind.ConfirmIntent,
                ProductId = "p-a"
            }
        };

        var c = SessionPreferenceLeaningExtractor.BuildSessionIntelligence(sessionId, exportedAt, signals);

        Assert.Equal(3, c.InteractionSignals.Count);
        Assert.Equal("p-a", c.InteractionSignals[0].ProductId); // earliest occurred_at

        Assert.Equal(2, c.PreferenceSignals.Count);
        Assert.Contains(c.PreferenceSignals, p => p.ProductId == "p-a");
        Assert.Contains(c.PreferenceSignals, p => p.ProductId == "p-b");

        Assert.Equal(2, c.LeaningIndicators.Count);
        Assert.Equal(1, c.LeaningIndicators[0].Rank);
        Assert.Equal(2, c.LeaningIndicators[1].Rank);
        Assert.True(c.LeaningIndicators[0].LeaningScore >= c.LeaningIndicators[1].LeaningScore);

        // Leaning confidence is session-level and should be identical across products.
        Assert.Equal(c.LeaningIndicators[0].Confidence, c.LeaningIndicators[1].Confidence);
    }

    [Fact]
    public void BuildSessionIntelligence_GoldenJson_IsDeterministic()
    {
        const string sessionId = "sess-golden";
        const string exportedAt = "2025-03-24T12:00:00.0000000Z";

        var signals = new List<InteractionSignal>
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
                ProductId = "p-b",
                DurationMs = 6000
            },
            new InteractionSignal
            {
                SignalId = "33333333333333333333333333333333",
                OccurredAtUtc = "2025-03-24T12:00:03.0000000Z",
                EventType = InteractionEventKind.ConfirmIntent,
                ProductId = "p-a"
            }
        };

        var c1 = SessionPreferenceLeaningExtractor.BuildSessionIntelligence(sessionId, exportedAt, signals);
        var c2 = SessionPreferenceLeaningExtractor.BuildSessionIntelligence(sessionId, exportedAt, signals);

        var j1 = ExportJson.Serialize(c1);
        var j2 = ExportJson.Serialize(c2);

        Assert.Equal(j1, j2);

        // Lock exact shape/value determinism for trial analysis.
        Assert.Contains("\"session_id\":\"sess-golden\"", j1);
        Assert.Contains("\"preference_signals\":[", j1);
        Assert.Contains("\"leaning_indicators\":[", j1);
        Assert.Contains("\"friction_episodes\":[", j1);
        Assert.Contains("\"decision_readiness\":{", j1);
        Assert.Contains("\"confidence_interpretation\":{", j1);
        Assert.Contains("\"struggle_decision_summary\":{", j1);
        Assert.Contains("\"basis\":\"leaning_friction_proxy_v1\"", j1);
    }

    [Fact]
    public void BuildSessionIntelligence_WithConfidenceTrace_UsesTraceBasis()
    {
        var signals = new List<InteractionSignal>
        {
            new()
            {
                SignalId = "11111111111111111111111111111111",
                OccurredAtUtc = "2025-03-24T12:00:00.0000000Z",
                EventType = InteractionEventKind.Selection,
                ProductId = "p-a"
            }
        };
        var trace = new List<(string, float, CognitiveEngine.Core.StateType)>
        {
            ("2025-03-24T12:00:00.0000000Z", 0.7f, CognitiveEngine.Core.StateType.Exploration),
            ("2025-03-24T12:00:01.0000000Z", 0.71f, CognitiveEngine.Core.StateType.Exploration)
        };

        var c = SessionPreferenceLeaningExtractor.BuildSessionIntelligence(
            "sess-p4-conf",
            "2025-03-24T12:00:05.0000000Z",
            signals,
            null,
            trace);

        Assert.Equal("confidence_trace_v1(mean,variance,half_delta,friction_penalty)", c.ConfidenceInterpretation.Basis);
    }

    [Fact]
    public void BuildSessionIntelligence_UsesProvidedP4StateTimeline_ForFrictionDetection()
    {
        var signals = new List<InteractionSignal>
        {
            new InteractionSignal
            {
                SignalId = "i1",
                OccurredAtUtc = "2025-03-24T12:00:00.0000000Z",
                EventType = InteractionEventKind.Selection,
                ProductId = "p-a"
            },
            new InteractionSignal
            {
                SignalId = "i2",
                OccurredAtUtc = "2025-03-24T12:00:01.0000000Z",
                EventType = InteractionEventKind.Compare,
                ProductId = "p-a",
                ComparisonPartnerProductId = "p-b"
            },
            new InteractionSignal
            {
                SignalId = "i3",
                OccurredAtUtc = "2025-03-24T12:00:02.0000000Z",
                EventType = InteractionEventKind.Dwell,
                ProductId = "p-a"
                ,
                DurationMs = 1200
            }
        };

        var p4StateTimeline = new List<(string occurredAtUtc, CognitiveEngine.Core.StateType state)>
        {
            ("2025-03-24T12:00:00.0000000Z", CognitiveEngine.Core.StateType.ReadyToConfirm),
            ("2025-03-24T12:00:01.0000000Z", CognitiveEngine.Core.StateType.Comparison)
        };

        var c = SessionPreferenceLeaningExtractor.BuildSessionIntelligence(
            "sess-p4",
            "2025-03-24T12:00:05.0000000Z",
            signals,
            p4StateTimeline);

        Assert.Contains(c.FrictionEpisodes, e => e.FrictionKind == FrictionKind.PostReadyBacktrack);
    }
}

