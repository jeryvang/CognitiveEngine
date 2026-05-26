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

    [Fact]
    public void SingleRationale_DoesNotUseDwellLead_OnSingleDwellEvent_AtDefaultThreshold()
    {
        var session = BuildSingleLeanSessionWithDwells("p", dwellCount: 1);
        var lean = DecisionPreferenceResult.LeanSingle("p", 0.7, "test_lean");

        var rationale = DecisionBehaviorRationaleBuilder.BuildSingleWhyThisMattersNow(
            session,
            lean,
            "p");

        var defaults = DecisionBehaviorRationaleTemplates.CreateDefault();
        Assert.NotEqual(defaults.SingleDwellLead, rationale);
    }

    [Fact]
    public void SingleRationale_UsesDwellLead_WhenDwellCountMeetsThreshold()
    {
        var session = BuildSingleLeanSessionWithDwells("p", dwellCount: 2);
        var lean = DecisionPreferenceResult.LeanSingle("p", 0.7, "test_lean");

        var rationale = DecisionBehaviorRationaleBuilder.BuildSingleWhyThisMattersNow(
            session,
            lean,
            "p");

        var defaults = DecisionBehaviorRationaleTemplates.CreateDefault();
        Assert.Equal(defaults.SingleDwellLead, rationale);
    }

    [Fact]
    public void SingleRationale_RespectsConfigMinDwellCountOverride()
    {
        var session = BuildSingleLeanSessionWithDwells("p", dwellCount: 2);
        var lean = DecisionPreferenceResult.LeanSingle("p", 0.7, "test_lean");

        var rationale = DecisionBehaviorRationaleBuilder.BuildSingleWhyThisMattersNow(
            session,
            lean,
            "p",
            templates: null,
            minDwellCountForRationale: 3);

        var defaults = DecisionBehaviorRationaleTemplates.CreateDefault();
        Assert.NotEqual(defaults.SingleDwellLead, rationale);
    }

    [Fact]
    public void Factory_RoutesMinDwellCountFromConfig()
    {
        var session = BuildSingleLeanSessionWithDwells("p", dwellCount: 2);
        var trigger = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "p");
        var cfg = DecisionGuidanceConfig.CreateDefault();
        cfg.MinDwellCountForRationale = 5;

        var ctx = DecisionBehaviorContextFactory.Create(session, in trigger, cfg);

        var defaults = DecisionBehaviorRationaleTemplates.CreateDefault();
        Assert.NotEqual(defaults.SingleDwellLead, ctx.WhyThisMattersNow);
    }

    private static DecisionBehaviorSessionContext BuildSingleLeanSessionWithDwells(string productId, int dwellCount)
    {
        var session = new DecisionBehaviorSessionContext();
        session.RecordFocusChanged(productId);
        for (int i = 0; i < dwellCount; i++)
            session.RecordDwellThresholdMet(productId);
        session.RecordFocusChanged("other");
        session.RecordFocusChanged(productId);
        return session;
    }

    [Fact]
    public void Factory_DuringActiveCompare_RoutesSingleTriggerToCompareRationale()
    {
        var session = new DecisionBehaviorSessionContext();
        session.RecordFocusChanged("a");
        session.RecordFocusChanged("b");
        session.RecordCompareInvoked("a", "b");
        session.RecordCompareEntered();
        session.RecordDwellThresholdMet("a");
        session.RecordDwellThresholdMet("a");

        var trigger = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "a");
        var ctx = DecisionBehaviorContextFactory.Create(session, in trigger, DecisionGuidanceConfig.CreateDefault());

        var defaults = DecisionBehaviorRationaleTemplates.CreateDefault();
        Assert.NotEqual(defaults.SingleDwellLead, ctx.WhyThisMattersNow);
        Assert.Contains(
            ctx.WhyThisMattersNow,
            new[] { defaults.CompareWeak, defaults.CompareRepeatedPair, defaults.CompareLean, defaults.CompareFallback });
    }

    [Fact]
    public void Factory_DuringActiveCompare_WithRepeatedPair_UsesCompareRepeatedTemplate()
    {
        var session = new DecisionBehaviorSessionContext();
        session.RecordFocusChanged("a");
        session.RecordFocusChanged("b");
        session.RecordCompareInvoked("a", "b");
        session.RecordCompareInvoked("a", "b");
        session.RecordCompareEntered();
        session.RecordDwellThresholdMet("a");
        session.RecordDwellThresholdMet("a");

        var trigger = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "a");
        var ctx = DecisionBehaviorContextFactory.Create(session, in trigger, DecisionGuidanceConfig.CreateDefault());

        var defaults = DecisionBehaviorRationaleTemplates.CreateDefault();
        Assert.Equal(defaults.CompareRepeatedPair, ctx.WhyThisMattersNow);
    }

    [Fact]
    public void Factory_CompareNotActive_StillUsesSingleRationale()
    {
        var session = new DecisionBehaviorSessionContext();
        session.RecordFocusChanged("a");
        session.RecordFocusChanged("b");
        session.RecordCompareInvoked("a", "b");
        session.RecordCompareEntered();
        session.RecordCompareExited();
        session.RecordDwellThresholdMet("a");
        session.RecordDwellThresholdMet("a");
        session.RecordFocusChanged("a");

        var trigger = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "a");
        var ctx = DecisionBehaviorContextFactory.Create(session, in trigger, DecisionGuidanceConfig.CreateDefault());

        var defaults = DecisionBehaviorRationaleTemplates.CreateDefault();
        Assert.NotEqual(defaults.CompareFallback, ctx.WhyThisMattersNow);
        Assert.NotEqual(defaults.CompareRepeatedPair, ctx.WhyThisMattersNow);
    }

    [Fact]
    public void CompareReturnRationale_DoesNotUseLeanFocused_OnPassiveDefaultLanding()
    {
        var session = new DecisionBehaviorSessionContext();
        session.RecordFocusChanged("Gucci");
        session.RecordDwellThresholdMet("Gucci");
        session.RecordSelect("Gucci");
        session.RecordFocusChanged("LV");
        session.RecordCompareInvoked("Gucci", "LV");
        session.RecordCompareEntered();
        session.RecordCompareExited();
        session.RecordFocusChanged("Gucci");

        Assert.Equal(1, session.GetRevisitCount("Gucci"));

        var leanFocused = DecisionPreferenceResult.LeanSingle("Gucci", 0.7, "test_lean");
        var rationale = DecisionBehaviorRationaleBuilder.BuildCompareReturnWhyThisMattersNow(
            session,
            leanFocused,
            "Gucci",
            "LV");

        var defaults = DecisionBehaviorRationaleTemplates.CreateDefault();
        Assert.NotEqual(defaults.CompareReturnLeanFocused, rationale);
        Assert.Equal(defaults.CompareReturnFallback, rationale);
    }

    [Fact]
    public void CompareReturnRationale_UsesLeanFocused_WhenRevisitEvidenceMeetsThreshold()
    {
        var session = new DecisionBehaviorSessionContext();
        session.RecordFocusChanged("Gucci");
        session.RecordDwellThresholdMet("Gucci");
        session.RecordSelect("Gucci");
        session.RecordFocusChanged("LV");
        session.RecordFocusChanged("Gucci");
        session.RecordFocusChanged("LV");
        session.RecordCompareInvoked("Gucci", "LV");
        session.RecordCompareEntered();
        session.RecordCompareExited();
        session.RecordFocusChanged("Gucci");

        Assert.True(session.GetRevisitCount("Gucci") >= 2);

        var leanFocused = DecisionPreferenceResult.LeanSingle("Gucci", 0.7, "test_lean");
        var rationale = DecisionBehaviorRationaleBuilder.BuildCompareReturnWhyThisMattersNow(
            session,
            leanFocused,
            "Gucci",
            "LV");

        var defaults = DecisionBehaviorRationaleTemplates.CreateDefault();
        Assert.Equal(defaults.CompareReturnLeanFocused, rationale);
    }

    [Fact]
    public void CompareReturnRationale_RespectsConfigMinRevisitOverride()
    {
        var session = new DecisionBehaviorSessionContext();
        session.RecordFocusChanged("Gucci");
        session.RecordDwellThresholdMet("Gucci");
        session.RecordSelect("Gucci");
        session.RecordFocusChanged("LV");
        session.RecordFocusChanged("Gucci");
        session.RecordFocusChanged("LV");
        session.RecordCompareInvoked("Gucci", "LV");
        session.RecordCompareEntered();
        session.RecordCompareExited();
        session.RecordFocusChanged("Gucci");

        var leanFocused = DecisionPreferenceResult.LeanSingle("Gucci", 0.7, "test_lean");
        var rationale = DecisionBehaviorRationaleBuilder.BuildCompareReturnWhyThisMattersNow(
            session,
            leanFocused,
            "Gucci",
            "LV",
            templates: null,
            minRevisitCountForLean: 5);

        var defaults = DecisionBehaviorRationaleTemplates.CreateDefault();
        Assert.NotEqual(defaults.CompareReturnLeanFocused, rationale);
    }

    [Fact]
    public void Factory_RoutesMinRevisitCountForCompareReturnLeanFromConfig()
    {
        var session = new DecisionBehaviorSessionContext();
        session.RecordFocusChanged("Gucci");
        session.RecordDwellThresholdMet("Gucci");
        session.RecordSelect("Gucci");
        session.RecordFocusChanged("LV");
        session.RecordFocusChanged("Gucci");
        session.RecordFocusChanged("LV");
        session.RecordCompareInvoked("Gucci", "LV");
        session.RecordCompareEntered();
        session.RecordCompareExited();
        session.RecordFocusChanged("Gucci");

        var cfg = DecisionGuidanceConfig.CreateDefault();
        cfg.MinRevisitCountForCompareReturnLean = 10;

        var trigger = ResolvedDecisionTrigger.ForCompareReturn("Gucci", "LV");
        var ctx = DecisionBehaviorContextFactory.Create(session, in trigger, cfg);

        var defaults = DecisionBehaviorRationaleTemplates.CreateDefault();
        Assert.NotEqual(defaults.CompareReturnLeanFocused, ctx.WhyThisMattersNow);
    }

    [Fact]
    public void CompareRationale_UsesCompareLean_WhenLeaningAndNotAmbiguous()
    {
        var session = new DecisionBehaviorSessionContext();
        session.RecordFocusChanged("a");
        session.RecordDwellThresholdMet("a");
        session.RecordSelect("a");
        session.RecordFocusChanged("b");
        session.RecordCompareInvoked("a", "b");

        var leanPreference = DecisionPreferenceResult.LeanComparison(
            PreferenceLean.ProductA,
            "a",
            "b",
            0.75,
            "test_lean");

        var rationale = DecisionBehaviorRationaleBuilder.BuildComparisonWhyThisMattersNow(
            session,
            leanPreference,
            "a",
            "b");

        var defaults = DecisionBehaviorRationaleTemplates.CreateDefault();
        Assert.Equal(defaults.CompareLean, rationale);
        Assert.NotEqual(defaults.CompareFallback, rationale);
    }
}
