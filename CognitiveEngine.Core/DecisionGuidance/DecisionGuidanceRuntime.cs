using System;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// End-to-end P6 wiring: host discrete signals → <see cref="DecisionTriggerResolver"/> →
/// <see cref="DecisionOutputBuilder"/> → <see cref="DecisionGuidanceSessionCoordinator.TryEnqueuePrimaryOutput"/>.
/// Handles overlapping emissions (presentation refusal rolls back resolver dedupe when appropriate),
/// optional expand first-tap-wins debounce, and shares the session logical clock for ticks.
/// </summary>
public sealed class DecisionGuidanceRuntime
{
    private readonly DecisionGuidanceSessionCoordinator _session;
    private readonly Func<ResolvedDecisionTrigger, DecisionOutputContent> _content;

    private long _lastExpandLogicalMs = -1;

    public DecisionGuidanceRuntime(
        DecisionGuidanceConfig? config = null,
        Func<ResolvedDecisionTrigger, DecisionOutputContent>? content = null)
    {
        var cfg = config ?? DecisionGuidanceConfig.CreateDefault();
        _session = new DecisionGuidanceSessionCoordinator(cfg);
        _content = content ?? (_ => new DecisionOutputContent());
    }

    public DecisionGuidanceSessionCoordinator Session => _session;

    public void Tick(long deltaMs) => _session.AdvanceMs(deltaMs);

    public void NotifyProductFocusChanged(string productId) => _session.NotifyProductFocusChanged(productId);

    public void NotifySelect(string productId) => _session.NotifySelect(productId);

    public void SetPanelOpen(bool isOpen) => _session.SetPanelOpen(isOpen);

    public void ResetSession()
    {
        _session.ResetSession();
        _lastExpandLogicalMs = -1;
    }

    /// <summary>
    /// First tap wins for optional expand/panel open: returns false for duplicate taps within
    /// <see cref="DecisionGuidanceConfig.ExpandDuplicateTapIgnoreMs"/> on the presentation logical clock.
    /// </summary>
    public bool TryConsumeExpandTap()
    {
        var now = _session.Presentation.LogicalNowMs;
        if (_lastExpandLogicalMs >= 0 &&
            now - _lastExpandLogicalMs < _session.Config.ExpandDuplicateTapIgnoreMs)
            return false;

        _lastExpandLogicalMs = now;
        return true;
    }

    /// <summary>
    /// Applies one resolver frame, builds structured output, and enqueues primary presentation when allowed.
    /// </summary>
    public ResolvedDecisionTrigger? ProcessTriggerFrame(in DecisionTriggerFrame frame)
    {
        if (_session.Presentation.IsPanelOpen)
            return null;

        var trigger = _session.Resolver.AdvanceFrame(in frame);
        if (trigger == null)
            return null;

        var build = DecisionOutputBuilder.Build(trigger.Value, _content(trigger.Value));
        if (!_session.TryEnqueuePrimaryOutput(trigger.Value, build))
        {
            if (_session.IsNudgeSuppressedForCurrentFocus)
                return null;

            var p = _session.Presentation;
            if (p.WouldRepeatGuardBlock(trigger.Value))
                return null;

            if (p.IsPrimaryPipelineActive || p.IsPanelOpen || p.IsPanelCooldownActive)
                _session.Resolver.ClearEmitSuppression();
            return null;
        }

        return trigger;
    }

    public ResolvedDecisionTrigger? ProcessTriggerInput(in DecisionTriggerInput input) =>
        ProcessTriggerFrame(DecisionTriggerFrame.FromInput(in input));
}
