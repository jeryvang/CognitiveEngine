using System.Collections.Generic;
using CognitiveEngine.Core.DecisionGuidance;
using Xunit;

namespace CognitiveEngine.Tests;

public class DecisionGuidanceTestModeGuardrailTests
{
    private static DecisionGuidanceConfig TestModeConfig()
    {
        var c = DecisionGuidanceConfig.CreateDefault();
        c.Mode = DecisionGuidanceMode.Test;
        c.AppearanceDelayMs = 10;
        c.PrimaryVisibleMs = 50;
        c.SoftFadeOrCollapseMs = 10;
        c.RepeatGuardMs = 100;
        c.PanelCloseCooldownMs = 1000;
        c.SustainedStayAfterOutputMs = 20;
        c.ValidateOrThrow();
        return c;
    }

    [Fact]
    public void TestMode_PanelSignals_DoNotSuppressOrAbort()
    {
        var cfg = TestModeConfig();
        var rt = new DecisionGuidanceRuntime(cfg, _ => new DecisionOutputContent());

        rt.SetPanelOpen(true);
        rt.NotifyProductFocusChanged("p1");
        Assert.NotNull(rt.ProcessTriggerInput(DecisionTriggerInput.DwellThresholdMet("p1")));

        rt.Tick(10);
        Assert.Equal(DecisionPresentationPhase.PrimaryVisible, rt.Session.Presentation.Phase);

        rt.SetPanelOpen(true);
        Assert.Equal(DecisionPresentationPhase.PrimaryVisible, rt.Session.Presentation.Phase);
    }

    [Fact]
    public void TestMode_Select_DoesNotStopNudges()
    {
        var cfg = TestModeConfig();
        var rt = new DecisionGuidanceRuntime(cfg, _ => new DecisionOutputContent());

        rt.NotifyProductFocusChanged("p1");
        Assert.NotNull(rt.ProcessTriggerInput(DecisionTriggerInput.DwellThresholdMet("p1")));
        rt.Tick(10);

        rt.NotifySelect("p1");
        Assert.False(rt.Session.IsNudgeSuppressedForCurrentFocus);
    }

    [Fact]
    public void TestMode_SustainedStay_DoesNotStopNudges()
    {
        var cfg = TestModeConfig();
        var rt = new DecisionGuidanceRuntime(cfg, _ => new DecisionOutputContent());

        rt.NotifyProductFocusChanged("p1");
        Assert.NotNull(rt.ProcessTriggerInput(DecisionTriggerInput.DwellThresholdMet("p1")));
        rt.Tick(10);
        rt.Tick(50);

        Assert.False(rt.Session.IsNudgeSuppressedForCurrentFocus);
        Assert.Null(rt.Session.ConfirmedProductId);
    }

    [Fact]
    public void Telemetry_EmitsCoreEvents()
    {
        var cfg = TestModeConfig();
        var events = new List<DecisionGuidanceEvent>();
        var rt = new DecisionGuidanceRuntime(cfg, _ => new DecisionOutputContent(), events.Add);

        rt.NotifyProductFocusChanged("p1");
        rt.NotifyCompareEntered();
        rt.ProcessTriggerInput(DecisionTriggerInput.DwellThresholdMet("p1"));
        rt.Tick(10);
        rt.NotifyCompareExited();

        Assert.Contains(events, e => e.Kind == DecisionGuidanceEventKind.FocusChanged);
        Assert.Contains(events, e => e.Kind == DecisionGuidanceEventKind.CompareEntered);
        Assert.Contains(events, e => e.Kind == DecisionGuidanceEventKind.CompareExited);
        Assert.Contains(events, e => e.Kind == DecisionGuidanceEventKind.DwellThresholdMet);
        Assert.Contains(events, e => e.Kind == DecisionGuidanceEventKind.TriggerResolved);
        Assert.Contains(events, e => e.Kind == DecisionGuidanceEventKind.OutputEnqueued);
        Assert.Contains(events, e => e.Kind == DecisionGuidanceEventKind.OutputBecameVisible);
    }
}

