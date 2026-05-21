using CognitiveEngine.Core.DecisionGuidance;
using Xunit;

namespace CognitiveEngine.Tests;

public class DecisionBehaviorContextFactoryTests
{
    [Fact]
    public void Create_IncludesConvergenceAndDispositionFields()
    {
        var session = new DecisionBehaviorSessionContext();
        session.SetLogicalNow(100);
        session.RecordFocusChanged("a");
        session.RecordDwellThresholdMet("a");
        session.RecordSelect("a");
        session.RecordFocusChanged("b");
        session.RecordCompareInvoked("a", "b");

        var trigger = ResolvedDecisionTrigger.ForCompare("a", "b");
        var ctx = DecisionBehaviorContextFactory.Create(session, in trigger, DecisionGuidanceConfig.CreateDefault());

        Assert.Equal("p7.behavior_context.v2", ctx.SchemaVersion);
        Assert.InRange(ctx.DecisionConvergence.Score, 0.0, 1.0);
        Assert.Equal("convergence_v1(selection,compare,dwell,swipe,revisit)", ctx.DecisionConvergence.Basis);
        Assert.Equal("p7_disposition_v1(confidence,convergence,ambiguity)", ctx.GuidanceDispositionBasis);
        Assert.Equal("repeat_guard_aligned_v1", ctx.RationalePolicy);
    }

    [Fact]
    public void Create_CompareAndDwellSignalsOutweighSwipeHeavyPattern()
    {
        var swipeHeavy = new DecisionBehaviorSessionContext();
        swipeHeavy.RecordFocusChanged("a");
        swipeHeavy.RecordFocusChanged("b");
        swipeHeavy.RecordFocusChanged("a");
        swipeHeavy.RecordFocusChanged("b");
        swipeHeavy.RecordFocusChanged("a");
        swipeHeavy.RecordCompareInvoked("a", "b");

        var compareDwellHeavy = new DecisionBehaviorSessionContext();
        compareDwellHeavy.RecordFocusChanged("a");
        compareDwellHeavy.RecordFocusChanged("b");
        compareDwellHeavy.RecordCompareInvoked("a", "b");
        compareDwellHeavy.RecordCompareInvoked("a", "b");
        compareDwellHeavy.RecordDwellThresholdMet("a");
        compareDwellHeavy.RecordDwellThresholdMet("b");
        compareDwellHeavy.RecordDwellThresholdMet("a");
        compareDwellHeavy.RecordSelect("a");

        var trigger = ResolvedDecisionTrigger.ForCompare("a", "b");
        var cfg = DecisionGuidanceConfig.CreateDefault();
        var swipeCtx = DecisionBehaviorContextFactory.Create(swipeHeavy, in trigger, cfg);
        var compareCtx = DecisionBehaviorContextFactory.Create(compareDwellHeavy, in trigger, cfg);

        Assert.True(
            compareCtx.BehaviorSignalUsage.NormalizedSignalStrength >
            swipeCtx.BehaviorSignalUsage.NormalizedSignalStrength);
    }

    [Fact]
    public void Create_RespectsNeutralAndStrongDispositionThresholds()
    {
        var cfg = DecisionGuidanceConfig.CreateDefault();
        cfg.StrongGuidanceMinConfidence = 0.20;
        cfg.StrongGuidanceMinConvergence = 0.20;
        cfg.NeutralMaxConfidence = 0.10;
        cfg.NeutralMaxConvergence = 0.10;
        cfg.ValidateOrThrow();

        var weak = new DecisionBehaviorSessionContext();
        weak.RecordFocusChanged("x");
        var weakTrigger = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "x");
        var weakCtx = DecisionBehaviorContextFactory.Create(weak, in weakTrigger, cfg);
        Assert.Equal(GuidanceDisposition.Neutral, weakCtx.GuidanceDisposition);

        var strong = new DecisionBehaviorSessionContext();
        strong.RecordFocusChanged("a");
        strong.RecordDwellThresholdMet("a");
        strong.RecordSelect("a");
        strong.RecordSelect("a");
        strong.RecordDwellThresholdMet("a");
        strong.RecordFocusChanged("b");
        strong.RecordCompareInvoked("a", "b");
        strong.RecordFocusChanged("a");
        var strongTrigger = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Revisit, "a");
        var strongCtx = DecisionBehaviorContextFactory.Create(strong, in strongTrigger, cfg);
        Assert.Equal(GuidanceDisposition.Strong, strongCtx.GuidanceDisposition);
    }

    [Fact]
    public void Rationale_IsSpecificActionOrientedAndNonChatty()
    {
        var session = new DecisionBehaviorSessionContext();
        session.RecordFocusChanged("Nike");
        session.RecordFocusChanged("Crocs");
        session.RecordCompareInvoked("Nike", "Crocs");
        session.RecordCompareInvoked("Nike", "Crocs");

        var preference = DecisionPreferenceResult.LeanComparison(
            PreferenceLean.ProductA,
            "Crocs",
            "Nike",
            0.75,
            "test");

        var rationale = DecisionBehaviorRationaleBuilder.BuildComparisonWhyThisMattersNow(
            session,
            preference,
            "Crocs",
            "Nike");

        Assert.Contains("compared", rationale.ToLowerInvariant());
        Assert.DoesNotContain("Crocs", rationale);
        Assert.DoesNotContain("Nike", rationale);
        Assert.DoesNotContain("!", rationale);
        Assert.DoesNotContain("awesome", rationale.ToLowerInvariant());
        Assert.DoesNotContain("behavior", rationale.ToLowerInvariant());
    }

    [Fact]
    public void Create_UsesConfigurableRationaleTemplatesFromConfig()
    {
        const string customCompareRepeated = "Custom: you keep comparing these two.";
        var cfg = DecisionGuidanceConfig.CreateDefault();
        cfg.BehaviorRationaleTemplates = new DecisionBehaviorRationaleTemplates
        {
            CompareRepeatedPair = customCompareRepeated
        };
        cfg.ValidateOrThrow();

        var session = new DecisionBehaviorSessionContext();
        session.RecordFocusChanged("a");
        session.RecordFocusChanged("b");
        session.RecordCompareInvoked("a", "b");
        session.RecordCompareInvoked("a", "b");
        session.RecordDwellThresholdMet("a");

        var trigger = ResolvedDecisionTrigger.ForCompare("a", "b");
        var ctx = DecisionBehaviorContextFactory.Create(session, in trigger, cfg);

        Assert.Equal(customCompareRepeated, ctx.WhyThisMattersNow);
    }

    [Fact]
    public void RationaleBuilder_AppliesPartialTemplateOverrides()
    {
        var session = new DecisionBehaviorSessionContext();
        session.RecordFocusChanged("x");

        var rationale = DecisionBehaviorRationaleBuilder.BuildSingleWhyThisMattersNow(
            session,
            DecisionPreferenceResult.NoClearLean(0.5, "test"),
            "x",
            new DecisionBehaviorRationaleTemplates { SingleWeak = "Host: keep browsing." });

        Assert.Equal("Host: keep browsing.", rationale);
    }
}
