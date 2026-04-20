using CognitiveEngine.Core.DecisionGuidance;
using Xunit;

namespace CognitiveEngine.Tests;

public class DecisionGuidanceSessionCoordinatorTests
{
    private static DecisionGuidanceConfig SessionConfig()
    {
        var c = DecisionGuidanceConfig.CreateDefault();
        c.Mode = DecisionGuidanceMode.Full;
        c.AppearanceDelayMs = 10;
        c.PrimaryVisibleMs = 200;
        c.SoftFadeOrCollapseMs = 20;
        c.RepeatGuardMs = 500;
        c.PanelCloseCooldownMs = 5;
        c.SustainedStayAfterOutputMs = 40;
        c.ValidateOrThrow();
        return c;
    }

    private static DecisionOutputBuildResult DwellOutput(string productId)
    {
        var t = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, productId);
        return DecisionOutputBuilder.Build(t, new DecisionOutputContent
        {
            WhatThisGivesYou = "g",
            WhatYouTradeOff = "t"
        });
    }

    [Fact]
    public void Select_Suppresses_Nudges_While_Focus_Remains_On_Product()
    {
        var s = new DecisionGuidanceSessionCoordinator(SessionConfig());
        s.NotifyProductFocusChanged("p1");
        var t = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "p1");
        Assert.True(s.TryEnqueuePrimaryOutput(t, DwellOutput("p1")));

        s.NotifySelect("p1");
        Assert.False(s.TryEnqueuePrimaryOutput(t, DwellOutput("p1")));
        Assert.True(s.IsNudgeSuppressedForCurrentFocus);
    }

    [Fact]
    public void Focus_Leaving_Confirmed_Product_Clears_Suppression()
    {
        var s = new DecisionGuidanceSessionCoordinator(SessionConfig());
        s.NotifyProductFocusChanged("p1");
        s.NotifySelect("p1");
        Assert.False(s.TryEnqueuePrimaryOutput(ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "p1"), DwellOutput("p1")));

        s.NotifyProductFocusChanged("p2");
        Assert.False(s.IsNudgeSuppressedForCurrentFocus);
        Assert.True(s.TryEnqueuePrimaryOutput(ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "p2"), DwellOutput("p2")));
    }

    [Fact]
    public void Sustained_Stay_After_Visible_Suppresses_Like_Select()
    {
        var s = new DecisionGuidanceSessionCoordinator(SessionConfig());
        s.NotifyProductFocusChanged("a");
        var t = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "a");
        Assert.True(s.TryEnqueuePrimaryOutput(t, DwellOutput("a")));

        s.AdvanceMs(9);
        Assert.False(s.IsNudgeSuppressedForCurrentFocus);

        s.AdvanceMs(1);
        Assert.Equal(DecisionPresentationPhase.PrimaryVisible, s.Presentation.Phase);

        s.AdvanceMs(39);
        Assert.False(s.IsNudgeSuppressedForCurrentFocus);

        s.AdvanceMs(1);
        Assert.True(s.IsNudgeSuppressedForCurrentFocus);
        Assert.False(s.TryEnqueuePrimaryOutput(t, DwellOutput("a")));
    }

    [Fact]
    public void Fast_Product_Switch_Cancels_Pending_Pipeline()
    {
        var s = new DecisionGuidanceSessionCoordinator(SessionConfig());
        s.NotifyProductFocusChanged("a");
        var t = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "a");
        Assert.True(s.TryEnqueuePrimaryOutput(t, DwellOutput("a")));
        s.AdvanceMs(5);
        Assert.Equal(DecisionPresentationPhase.AppearancePending, s.Presentation.Phase);

        s.NotifyProductFocusChanged("b");
        Assert.Equal(DecisionPresentationPhase.Idle, s.Presentation.Phase);

        var t2 = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "b");
        Assert.True(s.TryEnqueuePrimaryOutput(t2, DwellOutput("b")));
    }

    [Fact]
    public void ResetSession_Clears_Resolver_Presentation_And_Focus()
    {
        var s = new DecisionGuidanceSessionCoordinator(SessionConfig());
        s.NotifyProductFocusChanged("x");
        s.NotifyProductFocusChanged("y");
        Assert.NotNull(s.Resolver.Advance(DecisionTriggerInput.CompareInvoked()));
        Assert.Null(s.Resolver.Advance(DecisionTriggerInput.CompareInvoked()));

        s.ResetSession();
        Assert.Null(s.CurrentFocusedProductId);
        Assert.False(s.IsNudgeSuppressedForCurrentFocus);
        Assert.Equal(DecisionPresentationPhase.Idle, s.Presentation.Phase);

        s.NotifyProductFocusChanged("a");
        s.NotifyProductFocusChanged("b");
        Assert.NotNull(s.Resolver.Advance(DecisionTriggerInput.CompareInvoked()));
        Assert.Null(s.Resolver.Advance(DecisionTriggerInput.CompareInvoked()));
    }

    [Fact]
    public void New_Context_After_Leaving_Confirmed_Product_Reactivates_Enqueue()
    {
        var s = new DecisionGuidanceSessionCoordinator(SessionConfig());
        s.NotifyProductFocusChanged("p1");
        s.NotifySelect("p1");
        s.NotifyProductFocusChanged("p2");

        var t = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "p2");
        Assert.True(s.TryEnqueuePrimaryOutput(t, DwellOutput("p2")));
    }

    [Fact]
    public void NotifySelect_Cancels_Active_Pipeline()
    {
        var s = new DecisionGuidanceSessionCoordinator(SessionConfig());
        s.NotifyProductFocusChanged("p1");
        var t = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "p1");
        s.TryEnqueuePrimaryOutput(t, DwellOutput("p1"));
        s.AdvanceMs(50);
        Assert.NotEqual(DecisionPresentationPhase.Idle, s.Presentation.Phase);

        s.NotifySelect("p1");
        Assert.Equal(DecisionPresentationPhase.Idle, s.Presentation.Phase);
    }
}
