using System;
using System.Collections.Generic;
using CognitiveEngine.Core;
using CognitiveEngine.Core.TrialIntelligence;
using Xunit;

namespace CognitiveEngine.Tests;

public class FrictionDetectionEngineTests
{
    [Fact]
    public void DetectFrictionEpisodes_ComparisonLoop_YieldsEpisode()
    {
        var signals = new List<InteractionSignal>
        {
            new InteractionSignal
            {
                SignalId = "s1",
                OccurredAtUtc = "2025-03-24T12:00:00.0000000Z",
                EventType = InteractionEventKind.Compare,
                ProductId = "p-a",
                ComparisonPartnerProductId = "p-b"
            },
            new InteractionSignal
            {
                SignalId = "s2",
                OccurredAtUtc = "2025-03-24T12:00:01.0000000Z",
                EventType = InteractionEventKind.Compare,
                ProductId = "p-a",
                ComparisonPartnerProductId = "p-c"
            },
            new InteractionSignal
            {
                SignalId = "s3",
                OccurredAtUtc = "2025-03-24T12:00:02.0000000Z",
                EventType = InteractionEventKind.Compare,
                ProductId = "p-a",
                ComparisonPartnerProductId = "p-b"
            }
        };

        var states = new List<(string, StateType)>
        {
            ("2025-03-24T12:00:00.0000000Z", StateType.Comparison),
            ("2025-03-24T12:00:01.0000000Z", StateType.Comparison),
            ("2025-03-24T12:00:02.0000000Z", StateType.Comparison)
        };

        var episodes = FrictionDetectionEngine.DetectFrictionEpisodes("sess-1", signals, states);

        Assert.Single(episodes);
        var e = episodes[0];
        Assert.Equal("p-a", e.ProductId);
        Assert.Equal(FrictionKind.ComparisonLoop, e.FrictionKind);
        Assert.Equal("comparison-loop", e.BottleneckTag);
        Assert.Equal(3, e.EventCount);
    }

    [Fact]
    public void DetectFrictionEpisodes_HesitationBurst_YieldsEpisode()
    {
        var signals = new List<InteractionSignal>
        {
            new InteractionSignal
            {
                SignalId = "d1",
                OccurredAtUtc = "2025-03-24T12:00:00.0000000Z",
                EventType = InteractionEventKind.Dwell,
                ProductId = "p-x",
                DurationMs = 2000
            },
            new InteractionSignal
            {
                SignalId = "d2",
                OccurredAtUtc = "2025-03-24T12:00:01.0000000Z",
                EventType = InteractionEventKind.Dwell,
                ProductId = "p-x",
                DurationMs = 3000
            }
        };

        var states = new List<(string, StateType)>
        {
            ("2025-03-24T12:00:00.0000000Z", StateType.Hesitation),
            ("2025-03-24T12:00:01.0000000Z", StateType.Hesitation)
        };

        var episodes = FrictionDetectionEngine.DetectFrictionEpisodes("sess-2", signals, states);

        Assert.Single(episodes);
        var e = episodes[0];
        Assert.Equal("p-x", e.ProductId);
        Assert.Equal(FrictionKind.HesitationBurst, e.FrictionKind);
        Assert.Equal("hesitation-on-shortlist", e.BottleneckTag);
        Assert.Equal(2, e.EventCount);
        Assert.Equal(5000, e.TotalDwellMs);
    }

    [Fact]
    public void DetectFrictionEpisodes_DecisiveJourney_NoEpisodes()
    {
        var signals = new List<InteractionSignal>
        {
            new InteractionSignal
            {
                SignalId = "sel1",
                OccurredAtUtc = "2025-03-24T12:00:00.0000000Z",
                EventType = InteractionEventKind.Selection,
                ProductId = "p-z"
            },
            new InteractionSignal
            {
                SignalId = "conf1",
                OccurredAtUtc = "2025-03-24T12:00:01.0000000Z",
                EventType = InteractionEventKind.ConfirmIntent,
                ProductId = "p-z"
            }
        };

        var states = new List<(string, StateType)>
        {
            ("2025-03-24T12:00:00.0000000Z", StateType.Exploration),
            ("2025-03-24T12:00:01.0000000Z", StateType.ReadyToConfirm)
        };

        var episodes = FrictionDetectionEngine.DetectFrictionEpisodes("sess-3", signals, states);
        Assert.Empty(episodes);
    }

    [Fact]
    public void DetectFrictionEpisodes_PostReadyBacktrack_YieldsEpisode()
    {
        var signals = new List<InteractionSignal>
        {
            new InteractionSignal
            {
                SignalId = "c1",
                OccurredAtUtc = "2025-03-24T12:00:02.0000000Z",
                EventType = InteractionEventKind.Compare,
                ProductId = "p-a",
                ComparisonPartnerProductId = "p-b"
            },
            new InteractionSignal
            {
                SignalId = "d1",
                OccurredAtUtc = "2025-03-24T12:00:03.0000000Z",
                EventType = InteractionEventKind.Dwell,
                ProductId = "p-a",
                DurationMs = 2200
            }
        };

        var states = new List<(string, StateType)>
        {
            ("2025-03-24T12:00:01.0000000Z", StateType.ReadyToConfirm),
            ("2025-03-24T12:00:02.0000000Z", StateType.Comparison)
        };

        var episodes = FrictionDetectionEngine.DetectFrictionEpisodes("sess-4", signals, states);
        Assert.Contains(episodes, e => e.FrictionKind == FrictionKind.PostReadyBacktrack);
    }

    [Fact]
    public void DetectFrictionEpisodes_HesitationTooShort_NoHesitationBurst()
    {
        var signals = new List<InteractionSignal>
        {
            new InteractionSignal
            {
                SignalId = "d1",
                OccurredAtUtc = "2025-03-24T12:00:00.0000000Z",
                EventType = InteractionEventKind.Dwell,
                ProductId = "p-x",
                DurationMs = 800
            },
            new InteractionSignal
            {
                SignalId = "d2",
                OccurredAtUtc = "2025-03-24T12:00:01.0000000Z",
                EventType = InteractionEventKind.Dwell,
                ProductId = "p-x",
                DurationMs = 900
            }
        };

        var states = new List<(string, StateType)>
        {
            ("2025-03-24T12:00:00.0000000Z", StateType.Hesitation),
            ("2025-03-24T12:00:01.0000000Z", StateType.Hesitation)
        };

        var episodes = FrictionDetectionEngine.DetectFrictionEpisodes("sess-5", signals, states);
        Assert.DoesNotContain(episodes, e => e.FrictionKind == FrictionKind.HesitationBurst);
    }
}

