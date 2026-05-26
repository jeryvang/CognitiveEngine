using System.Collections.Generic;
using System.Linq;
using CognitiveEngine.Core.DecisionGuidance;
using Xunit;

namespace CognitiveEngine.Tests;

public class GuidanceBehaviorSnapshotTests
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
    public void OutputEnqueued_Includes_BehaviorSnapshot_With_Rationale_And_Confidence()
    {
        var events = new List<DecisionGuidanceEvent>();
        var rt = new DecisionGuidanceRuntime(FastConfig(), _ => new DecisionOutputContent
        {
            WhatThisGivesYou = "gives",
            WhatYouTradeOff = "trade"
        }, events.Add);

        rt.NotifyProductFocusChanged("a");
        rt.NotifyProductFocusChanged("b");
        var emitted = rt.ProcessTriggerInput(DecisionTriggerInput.FocusChanged("a"));
        rt.NotifyProductFocusChanged("a");
        Assert.NotNull(emitted);
        Assert.Equal(DecisionTriggerKind.Revisit, emitted.Value.Kind);

        var enqueued = events.Last(e => e.Kind == DecisionGuidanceEventKind.OutputEnqueued);
        Assert.NotNull(enqueued.BehaviorSnapshot);
        Assert.Equal("revisit", enqueued.BehaviorSnapshot!.Signal);
        Assert.Equal("a", enqueued.BehaviorSnapshot.ProductId);
        Assert.False(string.IsNullOrWhiteSpace(enqueued.BehaviorSnapshot.WhyThisMattersNow));
        Assert.InRange(enqueued.BehaviorSnapshot.Confidence, 0.0, 1.0);
        Assert.NotNull(enqueued.Trigger);
        Assert.Equal(DecisionTriggerKind.Revisit, enqueued.Trigger!.Value.Kind);
    }

    [Fact]
    public void OutputBecameVisible_Includes_Same_BehaviorSnapshot()
    {
        var events = new List<DecisionGuidanceEvent>();
        var rt = new DecisionGuidanceRuntime(FastConfig(), _ => new DecisionOutputContent
        {
            WhatThisGivesYou = "g",
            WhatYouTradeOff = "t"
        }, events.Add);

        rt.NotifyProductFocusChanged("solo");
        rt.ProcessTriggerInput(DecisionTriggerInput.DwellThresholdMet("solo"));
        rt.Tick(15);

        var enqueued = events.Single(e => e.Kind == DecisionGuidanceEventKind.OutputEnqueued);
        var visible = events.Single(e => e.Kind == DecisionGuidanceEventKind.OutputBecameVisible);
        Assert.NotNull(enqueued.BehaviorSnapshot);
        Assert.NotNull(visible.BehaviorSnapshot);
        Assert.Equal(enqueued.BehaviorSnapshot!.WhyThisMattersNow, visible.BehaviorSnapshot!.WhyThisMattersNow);
        Assert.Equal("dwell", visible.BehaviorSnapshot.Signal);
    }

    [Fact]
    public void Compare_OutputEnqueued_Snapshot_Includes_Both_Product_Ids()
    {
        var events = new List<DecisionGuidanceEvent>();
        var rt = new DecisionGuidanceRuntime(FastConfig(), _ => new DecisionOutputContent
        {
            KeyDifference = "d",
            WhichToChooseIf = "w"
        }, events.Add);

        rt.NotifyProductFocusChanged("alpha");
        rt.NotifyProductFocusChanged("beta");
        rt.ProcessTriggerInput(DecisionTriggerInput.CompareInvoked());

        var enqueued = events.Single(e => e.Kind == DecisionGuidanceEventKind.OutputEnqueued);
        Assert.NotNull(enqueued.BehaviorSnapshot);
        Assert.Equal("compare", enqueued.BehaviorSnapshot!.Signal);
        Assert.Equal("alpha", enqueued.BehaviorSnapshot.ProductId);
        Assert.Equal("beta", enqueued.BehaviorSnapshot.ProductIdHigh);
    }

    [Fact]
    public void OutputEnqueued_Includes_PrimaryOutput_With_Catalog_And_BehaviorContext()
    {
        var events = new List<DecisionGuidanceEvent>();
        var rt = new DecisionGuidanceRuntime(FastConfig(), _ => new DecisionOutputContent
        {
            WhatThisGivesYou = "catalog-gives",
            WhatYouTradeOff = "catalog-trade"
        }, events.Add);

        rt.NotifyProductFocusChanged("solo");
        rt.ProcessTriggerInput(DecisionTriggerInput.DwellThresholdMet("solo"));

        var enqueued = events.Single(e => e.Kind == DecisionGuidanceEventKind.OutputEnqueued);
        Assert.NotNull(enqueued.PrimaryOutput);
        Assert.Equal("catalog-gives", enqueued.PrimaryOutput!.Single!.WhatThisGivesYou);
        Assert.NotNull(enqueued.PrimaryOutput.Single.BehaviorContext);
        Assert.False(string.IsNullOrWhiteSpace(enqueued.PrimaryOutput.Single.BehaviorContext!.WhyThisMattersNow));
    }

    [Fact]
    public void BehaviorUpdated_Fires_When_Pipeline_Busy_With_Snapshot_And_PrimaryOutput()
    {
        var events = new List<DecisionGuidanceEvent>();
        var rt = new DecisionGuidanceRuntime(FastConfig(), _ => new DecisionOutputContent
        {
            WhatThisGivesYou = "busy-copy",
            WhatYouTradeOff = "trade"
        }, events.Add);

        rt.NotifyProductFocusChanged("a");
        rt.NotifyProductFocusChanged("b");
        rt.ProcessTriggerInput(DecisionTriggerInput.CompareInvoked());
        Assert.Null(rt.ProcessTriggerInput(DecisionTriggerInput.DwellThresholdMet("b")));

        var updated = events.Last(e => e.Kind == DecisionGuidanceEventKind.BehaviorUpdated);
        Assert.NotNull(updated.BehaviorSnapshot);
        Assert.Equal("dwell", updated.BehaviorSnapshot!.Signal);
        Assert.NotNull(updated.PrimaryOutput);
        Assert.Equal("busy-copy", updated.PrimaryOutput!.Single!.WhatThisGivesYou);
    }

    [Fact]
    public void Snapshot_ExposesBehaviorKeyAndPhrase_AlongsideSentence()
    {
        var events = new List<DecisionGuidanceEvent>();
        var rt = new DecisionGuidanceRuntime(FastConfig(), _ => new DecisionOutputContent
        {
            WhatThisGivesYou = "g",
            WhatYouTradeOff = "t"
        }, events.Add);

        rt.NotifyProductFocusChanged("solo");
        rt.ProcessTriggerInput(DecisionTriggerInput.DwellThresholdMet("solo"));

        var enqueued = events.Single(e => e.Kind == DecisionGuidanceEventKind.OutputEnqueued);
        var snap = enqueued.BehaviorSnapshot;
        Assert.NotNull(snap);
        Assert.False(string.IsNullOrWhiteSpace(snap!.BehaviorKey));
        Assert.False(string.IsNullOrWhiteSpace(snap.BehaviorPhrase));
        Assert.False(string.IsNullOrWhiteSpace(snap.WhyThisMattersNow));
        Assert.Equal(DecisionBehaviorRationaleKeys.SingleWeak, snap.BehaviorKey);
    }

    [Fact]
    public void Runtime_Emits_Hesitation_Snapshot_AfterRepeatedFocusWithoutSelection()
    {
        var events = new List<DecisionGuidanceEvent>();
        var rt = new DecisionGuidanceRuntime(FastConfig(), _ => new DecisionOutputContent
        {
            WhatThisGivesYou = "g",
            WhatYouTradeOff = "t"
        }, events.Add);

        // Build hesitation pattern on "a": focus a → b → a → b → a
        // → focus count a = 3, revisit count a = 2, no selection.
        rt.NotifyProductFocusChanged("a");
        rt.NotifyProductFocusChanged("b");
        rt.NotifyProductFocusChanged("a");
        rt.NotifyProductFocusChanged("b");
        rt.NotifyProductFocusChanged("a");

        // A subsequent host trigger (dwell on "a") should be re-routed to Hesitation
        // by the runtime, because hesitation conditions on "a" are met.
        var emitted = rt.ProcessTriggerInput(DecisionTriggerInput.DwellThresholdMet("a"));

        Assert.NotNull(emitted);
        Assert.Equal(DecisionTriggerKind.Hesitation, emitted!.Value.Kind);

        var enqueued = events.Last(e => e.Kind == DecisionGuidanceEventKind.OutputEnqueued);
        Assert.NotNull(enqueued.BehaviorSnapshot);
        Assert.Equal("hesitation", enqueued.BehaviorSnapshot!.Signal);
        Assert.Equal(DecisionBehaviorRationaleKeys.HesitationLowConfidence,
            enqueued.BehaviorSnapshot.BehaviorKey);
        Assert.Equal("a", enqueued.BehaviorSnapshot.ProductId);
        Assert.Null(enqueued.BehaviorSnapshot.ProductIdHigh);
    }

    [Fact]
    public void Runtime_DoesNotEmit_Hesitation_WhenCompareActive()
    {
        var events = new List<DecisionGuidanceEvent>();
        var rt = new DecisionGuidanceRuntime(FastConfig(), _ => new DecisionOutputContent
        {
            WhatThisGivesYou = "g",
            WhatYouTradeOff = "t"
        }, events.Add);

        rt.NotifyProductFocusChanged("a");
        rt.NotifyProductFocusChanged("b");
        rt.NotifyProductFocusChanged("a");
        rt.NotifyProductFocusChanged("b");
        rt.NotifyProductFocusChanged("a");

        // Enter compare; subsequent dwell should NOT be re-routed to hesitation.
        rt.NotifyCompareEntered();
        var emitted = rt.ProcessTriggerInput(DecisionTriggerInput.DwellThresholdMet("a"));

        Assert.NotNull(emitted);
        Assert.NotEqual(DecisionTriggerKind.Hesitation, emitted!.Value.Kind);
    }

    [Fact]
    public void Snapshot_BehaviorPhrase_IsShorterThanFullSentence()
    {
        var events = new List<DecisionGuidanceEvent>();
        var rt = new DecisionGuidanceRuntime(FastConfig(), _ => new DecisionOutputContent
        {
            WhatThisGivesYou = "g",
            WhatYouTradeOff = "t"
        }, events.Add);

        rt.NotifyProductFocusChanged("a");
        rt.NotifyProductFocusChanged("b");
        rt.ProcessTriggerInput(DecisionTriggerInput.CompareInvoked());
        rt.ProcessTriggerInput(DecisionTriggerInput.CompareInvoked());

        var enqueued = events.Last(e => e.Kind == DecisionGuidanceEventKind.OutputEnqueued);
        var snap = enqueued.BehaviorSnapshot!;
        Assert.True(snap.BehaviorPhrase.Length < snap.WhyThisMattersNow.Length);
    }
}
