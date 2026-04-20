using CognitiveEngine.Core.DecisionGuidance;
using CognitiveEngine.Core.TrialIntelligence;
using Xunit;

namespace CognitiveEngine.Tests;

public class DecisionGuidanceIntegrationTests
{
    private static DecisionGuidanceConfig FastRuntimeConfig()
    {
        var c = DecisionGuidanceConfig.CreateDefault();
        c.Mode = DecisionGuidanceMode.Full;
        c.AppearanceDelayMs = 20;
        c.PrimaryVisibleMs = 100;
        c.SoftFadeOrCollapseMs = 30;
        c.RepeatGuardMs = 400;
        c.PanelCloseCooldownMs = 5;
        c.SustainedStayAfterOutputMs = 60;
        c.ExpandDuplicateTapIgnoreMs = 50;
        c.ValidateOrThrow();
        return c;
    }

    private static DecisionOutputContent ContentFor(in ResolvedDecisionTrigger t) =>
        t.Kind switch
        {
            DecisionTriggerKind.Compare => new DecisionOutputContent
            {
                KeyDifference = "diff",
                WhichToChooseIf = "if"
            },
            _ => new DecisionOutputContent
            {
                WhatThisGivesYou = "gives",
                WhatYouTradeOff = "trade"
            }
        };

    [Fact]
    public void E2E_Compare_From_Focus_To_Visible_Output()
    {
        var rt = new DecisionGuidanceRuntime(FastRuntimeConfig(), t => ContentFor(in t));
        rt.NotifyProductFocusChanged("alpha");
        rt.NotifyProductFocusChanged("beta");

        var emitted = rt.ProcessTriggerInput(DecisionTriggerInput.CompareInvoked());
        Assert.NotNull(emitted);
        Assert.Equal(DecisionTriggerKind.Compare, emitted.Value.Kind);
        Assert.Null(rt.Session.Presentation.CurrentPrimaryOutput);

        rt.Tick(20);
        Assert.NotNull(rt.Session.Presentation.CurrentPrimaryOutput);
        Assert.Equal(DecisionOutputKind.Comparison, rt.Session.Presentation.CurrentPrimaryOutput!.Comparison!.OutputKind);
    }

    [Fact]
    public void Resolver_Rollback_When_Presentation_Rejects_Allows_Reemit_While_Pipeline_Still_Busy()
    {
        var rt = new DecisionGuidanceRuntime(FastRuntimeConfig(), t => ContentFor(in t));
        rt.NotifyProductFocusChanged("a");
        rt.NotifyProductFocusChanged("b");
        Assert.NotNull(rt.ProcessTriggerInput(DecisionTriggerInput.CompareInvoked()));
        rt.Tick(5);
        Assert.Equal(DecisionPresentationPhase.AppearancePending, rt.Session.Presentation.Phase);

        Assert.Null(rt.ProcessTriggerInput(DecisionTriggerInput.DwellThresholdMet("b")));
        Assert.NotNull(rt.Session.Resolver.Advance(DecisionTriggerInput.DwellThresholdMet("b")));
    }

    [Fact]
    public void ExpandTap_FirstWins_Within_Debounce_Window()
    {
        var rt = new DecisionGuidanceRuntime(FastRuntimeConfig(), t => ContentFor(in t));
        Assert.True(rt.TryConsumeExpandTap());
        Assert.False(rt.TryConsumeExpandTap());
    }

    [Fact]
    public void ExpandTap_After_Ignore_Window_Allows_Again()
    {
        var rt = new DecisionGuidanceRuntime(FastRuntimeConfig(), t => ContentFor(in t));
        Assert.True(rt.TryConsumeExpandTap());
        rt.Tick(60);
        Assert.True(rt.TryConsumeExpandTap());
    }

    [Fact]
    public void PanelOpen_ShortCircuits_Process_Without_Resolver_Advance()
    {
        var rt = new DecisionGuidanceRuntime(FastRuntimeConfig(), t => ContentFor(in t));
        rt.NotifyProductFocusChanged("x");
        rt.SetPanelOpen(true);
        Assert.Null(rt.ProcessTriggerInput(DecisionTriggerInput.DwellThresholdMet("x")));

        rt.SetPanelOpen(false);
        rt.Tick(10);
        Assert.NotNull(rt.ProcessTriggerInput(DecisionTriggerInput.DwellThresholdMet("x")));
    }

    [Fact]
    public void TrialIntelligence_SessionContract_GoldenJson_StillStable()
    {
        const string golden =
            "{\"schema_version\":\"1.0.0\",\"session_id\":\"sess-001\",\"exported_at_utc\":\"2025-03-24T12:00:00.0000000Z\",\"interaction_signals\":[{\"signal_id\":\"a1b2c3d4e5f6478990a1b2c3d4e5f601\",\"occurred_at_utc\":\"2025-03-24T12:00:01.0000000Z\",\"event_type\":\"compare\",\"product_id\":\"p-a\",\"intensity\":0.5,\"duration_ms\":1200}],\"preference_signals\":[{\"signal_id\":\"b2c3d4e5f6478990a1b2c3d4e5f6012\",\"derived_at_utc\":\"2025-03-24T12:00:02.0000000Z\",\"product_id\":\"p-a\",\"preference_strength\":0.7,\"basis\":\"dwell_weighted\"}],\"leaning_indicators\":[{\"product_id\":\"p-a\",\"leaning_score\":0.8,\"confidence\":0.6,\"rank\":1},{\"product_id\":\"p-b\",\"leaning_score\":0.2,\"confidence\":0.6,\"rank\":2}],\"friction_episodes\":[],\"decision_readiness\":{\"readiness_score\":0.5,\"readiness_level\":\"medium\",\"is_ready_to_confirm\":false,\"dominant_product_id\":\"p-a\",\"basis\":\"v1\"},\"confidence_interpretation\":{\"stability_score\":0.6,\"trend\":\"improving\",\"interpretation\":\"confidence is forming but still variable\",\"basis\":\"leaning_friction_proxy_v1\"},\"struggle_decision_summary\":{\"journey_classification\":\"balanced\",\"struggle_score\":0.2,\"decision_signal_score\":0.5,\"summary_tag\":\"mixed-signals\"}}";
        var parsed = ExportJson.Deserialize<SessionContract>(golden);
        ContractValidation.ValidateSessionOrThrow(parsed);
        Assert.Equal(golden, ExportJson.Serialize(parsed));
    }

    [Fact]
    public void ResetSession_Runtime_Clears_Expand_Debounce_State()
    {
        var rt = new DecisionGuidanceRuntime(FastRuntimeConfig(), t => ContentFor(in t));
        Assert.True(rt.TryConsumeExpandTap());
        rt.ResetSession();
        Assert.True(rt.TryConsumeExpandTap());
    }
}
