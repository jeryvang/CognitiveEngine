using CognitiveEngine.Core.DecisionGuidance;
using Xunit;

namespace CognitiveEngine.Tests;

public class DecisionGuidancePresentationControllerTests
{
    private static DecisionGuidanceConfig FastConfig()
    {
        var c = DecisionGuidanceConfig.CreateDefault();
        c.Mode = DecisionGuidanceMode.Full;
        c.AppearanceDelayMs = 100;
        c.PrimaryVisibleMs = 200;
        c.SoftFadeOrCollapseMs = 50;
        c.RepeatGuardMs = 300;
        c.PanelCloseCooldownMs = 40;
        c.ValidateOrThrow();
        return c;
    }

    private static DecisionOutputBuildResult SampleOutput(string productId = "p1")
    {
        var trigger = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, productId);
        return DecisionOutputBuilder.Build(trigger, new DecisionOutputContent
        {
            WhatThisGivesYou = "gives",
            WhatYouTradeOff = "trade"
        });
    }

    [Fact]
    public void Phases_Idle_Pending_Visible_Fading_Idle()
    {
        var c = new DecisionGuidancePresentationController(FastConfig());
        var br = SampleOutput();
        var trigger = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "p1");

        Assert.True(c.TryEnqueuePrimaryOutput(trigger, br));
        Assert.Equal(DecisionPresentationPhase.AppearancePending, c.Phase);
        Assert.Null(c.CurrentPrimaryOutput);

        c.AdvanceMs(99);
        Assert.Equal(DecisionPresentationPhase.AppearancePending, c.Phase);
        Assert.Null(c.CurrentPrimaryOutput);
        Assert.Equal(0f, c.GetPrimaryPresentationAlpha());

        c.AdvanceMs(1);
        Assert.Equal(DecisionPresentationPhase.PrimaryVisible, c.Phase);
        Assert.NotNull(c.CurrentPrimaryOutput);
        Assert.Equal(1f, c.GetPrimaryPresentationAlpha());

        c.AdvanceMs(199);
        Assert.Equal(DecisionPresentationPhase.PrimaryVisible, c.Phase);

        c.AdvanceMs(1);
        Assert.Equal(DecisionPresentationPhase.PrimaryFading, c.Phase);
        Assert.NotNull(c.CurrentPrimaryOutput);

        c.AdvanceMs(25);
        var a = c.GetPrimaryPresentationAlpha();
        Assert.InRange(a, 0.4f, 0.6f);

        c.AdvanceMs(25);
        Assert.Equal(DecisionPresentationPhase.Idle, c.Phase);
        Assert.Null(c.CurrentPrimaryOutput);
        Assert.Equal(0f, c.GetPrimaryPresentationAlpha());
    }

    [Fact]
    public void TryEnqueue_Rejected_WhilePrimaryPipelineActive()
    {
        var c = new DecisionGuidancePresentationController(FastConfig());
        var br = SampleOutput();
        var t = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "p1");
        Assert.True(c.TryEnqueuePrimaryOutput(t, br));
        c.AdvanceMs(50);

        var t2 = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Revisit, "p2");
        var br2 = DecisionOutputBuilder.Build(t2, new DecisionOutputContent { WhatThisGivesYou = "a" });
        Assert.False(c.TryEnqueuePrimaryOutput(t2, br2));
    }

    [Fact]
    public void RepeatGuard_Blocks_Same_TriggerSignature_UntilElapsed()
    {
        var c = new DecisionGuidancePresentationController(FastConfig());
        var t = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "p1");
        Assert.True(c.TryEnqueuePrimaryOutput(t, SampleOutput()));
        c.AdvanceMs(10_000);

        Assert.False(c.TryEnqueuePrimaryOutput(t, SampleOutput()));
        c.AdvanceMs(299);
        Assert.False(c.TryEnqueuePrimaryOutput(t, SampleOutput()));
        c.AdvanceMs(1);
        Assert.True(c.TryEnqueuePrimaryOutput(t, SampleOutput()));
    }

    [Fact]
    public void Different_Signature_Not_Blocked_By_OtherRepeatGuard()
    {
        var c = new DecisionGuidancePresentationController(FastConfig());
        var dwell = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "p1");
        Assert.True(c.TryEnqueuePrimaryOutput(dwell, SampleOutput()));
        c.AdvanceMs(10_000);

        var revisit = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Revisit, "p1");
        var br2 = DecisionOutputBuilder.Build(revisit, new DecisionOutputContent { WhatThisGivesYou = "r" });
        Assert.True(c.TryEnqueuePrimaryOutput(revisit, br2));
    }

    [Fact]
    public void PanelOpen_Reject_New_Enqueue()
    {
        var c = new DecisionGuidancePresentationController(FastConfig());
        c.SetPanelOpen(true);
        var t = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "p1");
        Assert.False(c.TryEnqueuePrimaryOutput(t, SampleOutput()));
    }

    [Fact]
    public void PanelOpen_Aborts_InFlight_Pipeline_Without_RepeatGuard()
    {
        var c = new DecisionGuidancePresentationController(FastConfig());
        var t = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "p1");
        Assert.True(c.TryEnqueuePrimaryOutput(t, SampleOutput()));
        Assert.Equal(DecisionPresentationPhase.AppearancePending, c.Phase);

        c.SetPanelOpen(true);
        Assert.Equal(DecisionPresentationPhase.Idle, c.Phase);
        Assert.Null(c.CurrentPrimaryOutput);

        c.SetPanelOpen(false);
        c.AdvanceMs(1000);
        Assert.True(c.TryEnqueuePrimaryOutput(t, SampleOutput()));
    }

    [Fact]
    public void PanelClose_Starts_Cooldown_Before_Next_Enqueue()
    {
        var c = new DecisionGuidancePresentationController(FastConfig());
        c.SetPanelOpen(true);
        c.SetPanelOpen(false);

        var t = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "p1");
        Assert.False(c.TryEnqueuePrimaryOutput(t, SampleOutput()));

        c.AdvanceMs(39);
        Assert.False(c.TryEnqueuePrimaryOutput(t, SampleOutput()));

        c.AdvanceMs(1);
        Assert.True(c.TryEnqueuePrimaryOutput(t, SampleOutput()));
    }

    [Fact]
    public void Reset_Clears_Timers_And_State()
    {
        var c = new DecisionGuidancePresentationController(FastConfig());
        var t = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "p1");
        c.TryEnqueuePrimaryOutput(t, SampleOutput());
        c.AdvanceMs(5000);
        c.Reset();
        Assert.Equal(DecisionPresentationPhase.Idle, c.Phase);
        Assert.Equal(0, c.LogicalNowMs);
        Assert.True(c.TryEnqueuePrimaryOutput(t, SampleOutput()));
    }
}
