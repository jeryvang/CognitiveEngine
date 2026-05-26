using System;
using CognitiveEngine.Core.DecisionGuidance;
using CognitiveEngine.Core.TrialIntelligence;
using Newtonsoft.Json;
using Xunit;

namespace CognitiveEngine.Tests;

public class DecisionGuidanceContractTests
{
    // Mirrors TrialIntelligenceTests.FixedSessionJson — proves Trial contracts unchanged alongside P6 types.
    private const string TrialGoldenSessionJson =
        "{\"schema_version\":\"1.2.0\",\"session_id\":\"sess-001\",\"exported_at_utc\":\"2025-03-24T12:00:00.0000000Z\",\"interaction_signals\":[{\"signal_id\":\"a1b2c3d4e5f6478990a1b2c3d4e5f601\",\"occurred_at_utc\":\"2025-03-24T12:00:01.0000000Z\",\"event_type\":\"compare\",\"product_id\":\"p-a\",\"intensity\":0.5,\"duration_ms\":1200}],\"preference_signals\":[{\"signal_id\":\"b2c3d4e5f6478990a1b2c3d4e5f6012\",\"derived_at_utc\":\"2025-03-24T12:00:02.0000000Z\",\"product_id\":\"p-a\",\"preference_strength\":0.7,\"basis\":\"dwell_weighted\"}],\"leaning_indicators\":[{\"product_id\":\"p-a\",\"leaning_score\":0.8,\"confidence\":0.6,\"rank\":1},{\"product_id\":\"p-b\",\"leaning_score\":0.2,\"confidence\":0.6,\"rank\":2}],\"friction_episodes\":[],\"decision_readiness\":{\"readiness_score\":0.5,\"readiness_level\":\"medium\",\"is_ready_to_confirm\":false,\"dominant_product_id\":\"p-a\",\"basis\":\"v1\"},\"confidence_interpretation\":{\"stability_score\":0.6,\"trend\":\"improving\",\"interpretation\":\"confidence is forming but still variable\",\"basis\":\"leaning_friction_proxy_v1\"},\"struggle_decision_summary\":{\"journey_classification\":\"balanced\",\"struggle_score\":0.2,\"decision_signal_score\":0.5,\"summary_tag\":\"mixed-signals\"},\"derived_metrics\":{\"switch_count\":1,\"exploration_switch_count\":3,\"selection_events_count\":2,\"total_compare_time_ms\":3456,\"compare_time_basis\":\"inferred_compare_window_v1(start=compare,end=selection|confirmIntent|contextChange|session_end)\",\"final_selected_product_id\":\"p-a\",\"longest_dwell_product_id\":\"p-a\"}}";

    private const string GoldenP6ConfigJson =
        "{\"schema_version\":\"p6.config.v1\",\"appearance_delay_ms\":1250,\"primary_visible_ms\":7000,\"soft_fade_or_collapse_ms\":500,\"repeat_guard_ms\":45000,\"sustained_stay_after_output_ms\":4000,\"panel_close_cooldown_ms\":2000,\"expand_duplicate_tap_ignore_ms\":300,\"strong_guidance_min_confidence\":0.68,\"strong_guidance_min_convergence\":0.65,\"neutral_max_confidence\":0.4,\"neutral_max_convergence\":0.45}";

    [Fact]
    public void DecisionGuidanceConfig_Defaults_MatchSpecBands()
    {
        var c = DecisionGuidanceConfig.CreateDefault();
        Assert.Equal(DecisionGuidanceSchema.ConfigVersion, c.SchemaVersion);
        Assert.Equal(DecisionGuidanceMode.Test, c.Mode);
        Assert.Equal(1250, c.AppearanceDelayMs);
        Assert.Equal(7000, c.PrimaryVisibleMs);
        Assert.Equal(500, c.SoftFadeOrCollapseMs);
        Assert.Equal(45000, c.RepeatGuardMs);
        Assert.Equal(4000, c.SustainedStayAfterOutputMs);
        Assert.Equal(2000, c.PanelCloseCooldownMs);
        Assert.Equal(300, c.ExpandDuplicateTapIgnoreMs);
        Assert.Equal(0.68, c.StrongGuidanceMinConfidence);
        Assert.Equal(0.65, c.StrongGuidanceMinConvergence);
        Assert.Equal(0.4, c.NeutralMaxConfidence);
        Assert.Equal(0.45, c.NeutralMaxConvergence);
    }

    [Fact]
    public void DecisionGuidanceConfig_ValidateOrThrow_AcceptsDefaults()
    {
        var c = DecisionGuidanceConfig.CreateDefault();
        c.ValidateOrThrow();
    }

    [Fact]
    public void DecisionGuidanceConfig_ValidateOrThrow_RejectsNegative()
    {
        var c = DecisionGuidanceConfig.CreateDefault();
        c.AppearanceDelayMs = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => c.ValidateOrThrow());
    }

    [Fact]
    public void DecisionGuidanceConfig_ValidateOrThrow_RejectsBlankSchema()
    {
        var c = DecisionGuidanceConfig.CreateDefault();
        c.SchemaVersion = "";
        Assert.Throws<ArgumentException>(() => c.ValidateOrThrow());
    }

    [Fact]
    public void DecisionGuidanceConfig_RoundTrip_PreservesValues()
    {
        var original = new DecisionGuidanceConfig
        {
            SchemaVersion = DecisionGuidanceSchema.ConfigVersion,
            AppearanceDelayMs = 1000,
            PrimaryVisibleMs = 8000,
            SoftFadeOrCollapseMs = 600,
            RepeatGuardMs = 60000,
            SustainedStayAfterOutputMs = 5000,
            PanelCloseCooldownMs = 3500,
            ExpandDuplicateTapIgnoreMs = 250,
            StrongGuidanceMinConfidence = 0.7,
            StrongGuidanceMinConvergence = 0.66,
            NeutralMaxConfidence = 0.35,
            NeutralMaxConvergence = 0.30
        };

        var json = ExportJson.Serialize(original);
        var back = ExportJson.Deserialize<DecisionGuidanceConfig>(json);

        Assert.Equal(original.SchemaVersion, back.SchemaVersion);
        Assert.Equal(1000, back.AppearanceDelayMs);
        Assert.Equal(8000, back.PrimaryVisibleMs);
        Assert.Equal(600, back.SoftFadeOrCollapseMs);
        Assert.Equal(60000, back.RepeatGuardMs);
        Assert.Equal(5000, back.SustainedStayAfterOutputMs);
        Assert.Equal(3500, back.PanelCloseCooldownMs);
        Assert.Equal(250, back.ExpandDuplicateTapIgnoreMs);
        Assert.Equal(0.7, back.StrongGuidanceMinConfidence);
        Assert.Equal(0.66, back.StrongGuidanceMinConvergence);
        Assert.Equal(0.35, back.NeutralMaxConfidence);
        Assert.Equal(0.30, back.NeutralMaxConvergence);
    }

    [Fact]
    public void DecisionGuidanceConfig_GoldenJson_MatchesExpectedShape()
    {
        var parsed = ExportJson.Deserialize<DecisionGuidanceConfig>(GoldenP6ConfigJson);
        parsed.ValidateOrThrow();
        var again = ExportJson.Serialize(parsed);
        Assert.Equal(GoldenP6ConfigJson, again);
    }

    [Fact]
    public void DecisionGuidanceConfig_RoundTrip_PreservesPartialRationaleTemplateOverrides()
    {
        var original = DecisionGuidanceConfig.CreateDefault();
        original.BehaviorRationaleTemplates = new DecisionBehaviorRationaleTemplates
        {
            SingleAmbiguous = "Still deciding? Pick one priority.",
            CompareLean = "Lean is emerging from your compare."
        };

        var json = ExportJson.Serialize(original);
        var back = ExportJson.Deserialize<DecisionGuidanceConfig>(json);

        Assert.NotNull(back.BehaviorRationaleTemplates);
        Assert.Equal("Still deciding? Pick one priority.", back.BehaviorRationaleTemplates!.SingleAmbiguous);
        Assert.Equal("Lean is emerging from your compare.", back.BehaviorRationaleTemplates.CompareLean);
        Assert.True(string.IsNullOrEmpty(back.BehaviorRationaleTemplates.SingleWeak));

        var effective = DecisionBehaviorRationaleTemplates.ResolveEffective(back.BehaviorRationaleTemplates);
        Assert.Equal("Still deciding? Pick one priority.", effective.SingleAmbiguous);
        Assert.Equal(
            DecisionBehaviorRationaleTemplates.CreateDefault().SingleWeak,
            effective.SingleWeak);
    }

    [Fact]
    public void DecisionGuidanceConfig_ValidateOrThrow_RejectsBlankRationaleOverride()
    {
        var c = DecisionGuidanceConfig.CreateDefault();
        c.BehaviorRationaleTemplates = new DecisionBehaviorRationaleTemplates { CompareWeak = "   " };
        Assert.Throws<ArgumentException>(() => c.ValidateOrThrow());
    }

    [Fact]
    public void SingleProductDecisionOutput_RoundTrip_PreservesValues()
    {
        var original = new SingleProductDecisionOutput
        {
            ProductId = "prod-x",
            WhatThisGivesYou = "Faster checkout",
            WhatYouTradeOff = "Higher monthly cost"
        };

        var json = ExportJson.Serialize(original);
        var back = ExportJson.Deserialize<SingleProductDecisionOutput>(json);

        Assert.Equal(DecisionGuidanceSchema.StructuredOutputVersion, back.SchemaVersion);
        Assert.Equal(DecisionOutputKind.SingleProduct, back.OutputKind);
        Assert.Equal("prod-x", back.ProductId);
        Assert.Equal("Faster checkout", back.WhatThisGivesYou);
        Assert.Equal("Higher monthly cost", back.WhatYouTradeOff);
    }

    [Fact]
    public void ComparisonDecisionOutput_RoundTrip_PreservesValues()
    {
        var original = new ComparisonDecisionOutput
        {
            ProductIdA = "a",
            ProductIdB = "b",
            KeyDifference = "A emphasizes speed; B emphasizes cost",
            WhichToChooseIf = "If you need lowest TCO, lean B; if speed matters most, lean A."
        };

        var json = ExportJson.Serialize(original);
        var back = ExportJson.Deserialize<ComparisonDecisionOutput>(json);

        Assert.Equal(DecisionOutputKind.Comparison, back.OutputKind);
        Assert.Equal("a", back.ProductIdA);
        Assert.Equal("b", back.ProductIdB);
        Assert.Equal(original.KeyDifference, back.KeyDifference);
        Assert.Equal(original.WhichToChooseIf, back.WhichToChooseIf);
    }

    [Fact]
    public void DecisionTriggerPriority_Order_IsCompareCompareReturnHesitationRevisitDwell()
    {
        Assert.Equal(5, DecisionTriggerPriority.StrictDescendingOrder.Length);
        Assert.Equal(DecisionTriggerKind.Compare, DecisionTriggerPriority.StrictDescendingOrder[0]);
        Assert.Equal(DecisionTriggerKind.CompareReturn, DecisionTriggerPriority.StrictDescendingOrder[1]);
        Assert.Equal(DecisionTriggerKind.Hesitation, DecisionTriggerPriority.StrictDescendingOrder[2]);
        Assert.Equal(DecisionTriggerKind.Revisit, DecisionTriggerPriority.StrictDescendingOrder[3]);
        Assert.Equal(DecisionTriggerKind.Dwell, DecisionTriggerPriority.StrictDescendingOrder[4]);
        Assert.True(DecisionTriggerPriority.Rank(DecisionTriggerKind.Compare) <
                    DecisionTriggerPriority.Rank(DecisionTriggerKind.CompareReturn));
        Assert.True(DecisionTriggerPriority.Rank(DecisionTriggerKind.CompareReturn) <
                    DecisionTriggerPriority.Rank(DecisionTriggerKind.Hesitation));
        Assert.True(DecisionTriggerPriority.Rank(DecisionTriggerKind.Hesitation) <
                    DecisionTriggerPriority.Rank(DecisionTriggerKind.Revisit));
        Assert.True(DecisionTriggerPriority.Rank(DecisionTriggerKind.Revisit) <
                    DecisionTriggerPriority.Rank(DecisionTriggerKind.Dwell));
    }

    [Fact]
    public void TrialIntelligence_SessionContract_GoldenJson_UnchangedWithP6Present()
    {
        var parsed = ExportJson.Deserialize<SessionContract>(TrialGoldenSessionJson);
        ContractValidation.ValidateSessionOrThrow(parsed);
        var again = ExportJson.Serialize(parsed);
        Assert.Equal(TrialGoldenSessionJson, again);
    }
}
