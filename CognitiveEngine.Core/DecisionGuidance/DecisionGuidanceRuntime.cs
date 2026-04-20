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
    private readonly Action<DecisionGuidanceEvent>? _onEvent;

    private long _lastExpandLogicalMs = -1;
    private DecisionPresentationPhase _lastPhase;

    public DecisionGuidanceRuntime(
        DecisionGuidanceConfig? config = null,
        Func<ResolvedDecisionTrigger, DecisionOutputContent>? content = null,
        Action<DecisionGuidanceEvent>? onEvent = null)
    {
        var cfg = config ?? DecisionGuidanceConfig.CreateDefault();
        _session = new DecisionGuidanceSessionCoordinator(cfg);
        _content = content ?? (_ => new DecisionOutputContent());
        _onEvent = onEvent;
        _lastPhase = _session.Presentation.Phase;
    }

    public DecisionGuidanceSessionCoordinator Session => _session;

    public void Tick(long deltaMs)
    {
        _session.AdvanceMs(deltaMs);

        var phase = _session.Presentation.Phase;
        if (_lastPhase != DecisionPresentationPhase.PrimaryVisible &&
            phase == DecisionPresentationPhase.PrimaryVisible)
        {
            _onEvent?.Invoke(new DecisionGuidanceEvent(DecisionGuidanceEventKind.OutputBecameVisible, _session.Presentation.LogicalNowMs));
        }
        _lastPhase = phase;
    }

    public void NotifyProductFocusChanged(string productId)
    {
        _onEvent?.Invoke(new DecisionGuidanceEvent(DecisionGuidanceEventKind.FocusChanged, _session.Presentation.LogicalNowMs, productId));
        _session.NotifyProductFocusChanged(productId);
    }

    public void NotifySelect(string productId)
    {
        _onEvent?.Invoke(new DecisionGuidanceEvent(DecisionGuidanceEventKind.SelectNotified, _session.Presentation.LogicalNowMs, productId));
        _session.NotifySelect(productId);
    }

    /// <summary>
    /// Host-defined compare UI state: call when the user enters compare mode. Used for measurement only.
    /// </summary>
    public void NotifyCompareEntered() =>
        _onEvent?.Invoke(new DecisionGuidanceEvent(DecisionGuidanceEventKind.CompareEntered, _session.Presentation.LogicalNowMs));

    /// <summary>
    /// Host-defined compare UI state: call when the user exits compare mode. Used for measurement only.
    /// </summary>
    public void NotifyCompareExited() =>
        _onEvent?.Invoke(new DecisionGuidanceEvent(DecisionGuidanceEventKind.CompareExited, _session.Presentation.LogicalNowMs));

    public void SetPanelOpen(bool isOpen) => _session.SetPanelOpen(isOpen);

    public void ResetSession()
    {
        _session.ResetSession();
        _lastExpandLogicalMs = -1;
        _lastPhase = _session.Presentation.Phase;
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

        if (!string.IsNullOrWhiteSpace(frame.FocusProductIfChanged))
            _onEvent?.Invoke(new DecisionGuidanceEvent(DecisionGuidanceEventKind.FocusChanged, _session.Presentation.LogicalNowMs, frame.FocusProductIfChanged));
        if (frame.CompareInvoked)
            _onEvent?.Invoke(new DecisionGuidanceEvent(DecisionGuidanceEventKind.CompareInvoked, _session.Presentation.LogicalNowMs));
        if (!string.IsNullOrWhiteSpace(frame.DwellProductIfThreshold))
            _onEvent?.Invoke(new DecisionGuidanceEvent(DecisionGuidanceEventKind.DwellThresholdMet, _session.Presentation.LogicalNowMs, frame.DwellProductIfThreshold));

        var trigger = _session.Resolver.AdvanceFrame(in frame);
        if (trigger == null)
            return null;

        _onEvent?.Invoke(new DecisionGuidanceEvent(DecisionGuidanceEventKind.TriggerResolved, _session.Presentation.LogicalNowMs, trigger.Value.ProductIdLow, ResolvedDecisionTrigger.Signature(trigger.Value)));

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

        _onEvent?.Invoke(new DecisionGuidanceEvent(DecisionGuidanceEventKind.OutputEnqueued, _session.Presentation.LogicalNowMs, trigger.Value.ProductIdLow, ResolvedDecisionTrigger.Signature(trigger.Value)));
        return trigger;
    }

    public ResolvedDecisionTrigger? ProcessTriggerInput(in DecisionTriggerInput input) =>
        ProcessTriggerFrame(DecisionTriggerFrame.FromInput(in input));
}
