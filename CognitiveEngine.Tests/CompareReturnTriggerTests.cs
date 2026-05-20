using System.Collections.Generic;
using System.Linq;
using CognitiveEngine.Core.DecisionGuidance;
using Xunit;

namespace CognitiveEngine.Tests;

public class CompareReturnTriggerTests
{
    private static DecisionGuidanceConfig FastConfig()
    {
        var c = DecisionGuidanceConfig.CreateDefault();
        c.Mode = DecisionGuidanceMode.Full;
        c.AppearanceDelayMs = 10;
        c.PrimaryVisibleMs = 100;
        c.SoftFadeOrCollapseMs = 10;
        c.RepeatGuardMs = 400;
        c.PanelCloseCooldownMs = 5;
        c.ValidateOrThrow();
        return c;
    }

    [Fact]
    public void CompareReturn_Fires_AfterCompareExit_And_Focus_On_Pair_Product()
    {
        var events = new List<DecisionGuidanceEvent>();
        var rt = new DecisionGuidanceRuntime(FastConfig(), _ => new DecisionOutputContent
        {
            WhatThisGivesYou = "g",
            WhatYouTradeOff = "t"
        }, events.Add);

        rt.NotifyProductFocusChanged("alpha");
        rt.NotifyProductFocusChanged("beta");
        rt.ProcessTriggerInput(DecisionTriggerInput.CompareInvoked());
        rt.Tick(150);
        rt.NotifyCompareEntered();
        rt.NotifyCompareExited();

        var emitted = rt.ProcessTriggerInput(DecisionTriggerInput.FocusChanged("alpha"));
        rt.NotifyProductFocusChanged("alpha");

        Assert.NotNull(emitted);
        Assert.Equal(DecisionTriggerKind.CompareReturn, emitted.Value.Kind);
        Assert.Equal("alpha", emitted.Value.ProductIdLow);
        Assert.Equal("beta", emitted.Value.ProductIdHigh);

        var enqueued = events.Last(e => e.Kind == DecisionGuidanceEventKind.OutputEnqueued);
        Assert.NotNull(enqueued.BehaviorSnapshot);
        Assert.Equal("compare_return", enqueued.BehaviorSnapshot!.Signal);
        Assert.False(string.IsNullOrWhiteSpace(enqueued.BehaviorSnapshot.WhyThisMattersNow));
    }

    [Fact]
    public void CompareReturn_Wins_Over_Revisit_On_Same_Focus()
    {
        var r = new DecisionTriggerResolver();
        r.Advance(DecisionTriggerInput.FocusChanged("a"));
        r.Advance(DecisionTriggerInput.FocusChanged("b"));
        r.ArmCompareReturn("a", "b");

        var t = r.Advance(DecisionTriggerInput.FocusChanged("a"));
        Assert.NotNull(t);
        Assert.Equal(DecisionTriggerKind.CompareReturn, t.Value.Kind);
    }

    [Fact]
    public void CompareReturn_Signature_Is_Stable()
    {
        var t = ResolvedDecisionTrigger.ForCompareReturn("Nike_Air", "Crocs_Echo");
        Assert.Equal("compare_return:Nike_Air|Crocs_Echo", ResolvedDecisionTrigger.Signature(t));
    }
}
