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
    private GuidanceBehaviorSnapshot? _lastEnqueuedBehaviorSnapshot;
    private ResolvedDecisionTrigger? _lastEnqueuedTrigger;
    private DecisionOutputBuildResult? _lastEnqueuedBuild;

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
            _onEvent?.Invoke(CreateOutputLifecycleEvent(
                DecisionGuidanceEventKind.OutputBecameVisible,
                _session.Presentation.LogicalNowMs));
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
    public void NotifyCompareEntered()
    {
        _session.NotifyCompareEntered();
        _onEvent?.Invoke(new DecisionGuidanceEvent(DecisionGuidanceEventKind.CompareEntered, _session.Presentation.LogicalNowMs));
    }

    /// <summary>
    /// Host-defined compare UI state: call when the user exits compare mode. Used for measurement only.
    /// </summary>
    public void NotifyCompareExited()
    {
        _session.NotifyCompareExited();
        _onEvent?.Invoke(new DecisionGuidanceEvent(DecisionGuidanceEventKind.CompareExited, _session.Presentation.LogicalNowMs));
    }

    public void SetPanelOpen(bool isOpen) => _session.SetPanelOpen(isOpen);

    public void ResetSession()
    {
        _session.ResetSession();
        _lastExpandLogicalMs = -1;
        _lastPhase = _session.Presentation.Phase;
        _lastEnqueuedBehaviorSnapshot = null;
        _lastEnqueuedTrigger = null;
        _lastEnqueuedBuild = null;
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
        {
            _session.NotifyCompareInvoked();
            _onEvent?.Invoke(new DecisionGuidanceEvent(DecisionGuidanceEventKind.CompareInvoked, _session.Presentation.LogicalNowMs));
        }
        if (!string.IsNullOrWhiteSpace(frame.DwellProductIfThreshold))
        {
            _session.NotifyDwellThresholdMet(frame.DwellProductIfThreshold);
            _onEvent?.Invoke(new DecisionGuidanceEvent(DecisionGuidanceEventKind.DwellThresholdMet, _session.Presentation.LogicalNowMs, frame.DwellProductIfThreshold));
        }

        var resolvedFrame = AttachHesitationCandidateIfAny(in frame);
        var trigger = _session.Resolver.AdvanceFrame(in resolvedFrame);
        if (trigger == null)
            return null;

        _onEvent?.Invoke(new DecisionGuidanceEvent(DecisionGuidanceEventKind.TriggerResolved, _session.Presentation.LogicalNowMs, trigger.Value.ProductIdLow, ResolvedDecisionTrigger.Signature(trigger.Value)));

        var resolvedTrigger = trigger.Value;
        var build = DecisionOutputBuilder.Build(resolvedTrigger, _content(resolvedTrigger));
        TryAttachBehaviorContext(build, in resolvedTrigger);
        var behaviorSnapshot = GuidanceBehaviorSnapshot.TryCreate(build, in resolvedTrigger);

        if (!_session.TryEnqueuePrimaryOutput(trigger.Value, build))
        {
            PublishGuidancePayload(
                DecisionGuidanceEventKind.BehaviorUpdated,
                _session.Presentation.LogicalNowMs,
                in resolvedTrigger,
                build,
                behaviorSnapshot);

            if (_session.IsNudgeSuppressedForCurrentFocus)
                return null;

            var p = _session.Presentation;
            if (p.WouldRepeatGuardBlock(trigger.Value))
                return null;

            if (p.IsPrimaryPipelineActive || p.IsPanelOpen || p.IsPanelCooldownActive)
                _session.Resolver.ClearEmitSuppression();
            return null;
        }

        RememberEnqueuedBehavior(build, in resolvedTrigger, behaviorSnapshot);
        PublishGuidancePayload(
            DecisionGuidanceEventKind.OutputEnqueued,
            _session.Presentation.LogicalNowMs,
            in resolvedTrigger,
            build,
            behaviorSnapshot);
        return trigger;
    }

    public ResolvedDecisionTrigger? ProcessTriggerInput(in DecisionTriggerInput input) =>
        ProcessTriggerFrame(DecisionTriggerFrame.FromInput(in input));

    private void RememberEnqueuedBehavior(
        DecisionOutputBuildResult build,
        in ResolvedDecisionTrigger trigger,
        GuidanceBehaviorSnapshot? behaviorSnapshot)
    {
        _lastEnqueuedTrigger = trigger;
        _lastEnqueuedBehaviorSnapshot = behaviorSnapshot;
        _lastEnqueuedBuild = build;
    }

    private void PublishGuidancePayload(
        DecisionGuidanceEventKind kind,
        long logicalNowMs,
        in ResolvedDecisionTrigger trigger,
        DecisionOutputBuildResult build,
        GuidanceBehaviorSnapshot? behaviorSnapshot)
    {
        if (behaviorSnapshot == null)
            return;

        _onEvent?.Invoke(new DecisionGuidanceEvent(
            kind,
            logicalNowMs,
            trigger.ProductIdLow,
            ResolvedDecisionTrigger.Signature(trigger),
            behaviorSnapshot,
            trigger,
            build));
    }

    private DecisionGuidanceEvent CreateOutputLifecycleEvent(DecisionGuidanceEventKind kind, long logicalNowMs)
    {
        var t = _lastEnqueuedTrigger;
        var productId = t?.ProductIdLow;
        var signature = t != null ? ResolvedDecisionTrigger.Signature(t.Value) : null;
        return new DecisionGuidanceEvent(
            kind,
            logicalNowMs,
            productId,
            signature,
            _lastEnqueuedBehaviorSnapshot,
            t,
            _lastEnqueuedBuild);
    }

    /// <summary>
    /// Evaluates whether the focused product is in a hesitation pattern (repeated focus + repeated revisit
    /// + no selection) and, if so, attaches it to the frame so the resolver can emit a
    /// <see cref="DecisionTriggerKind.Hesitation"/> trigger between CompareReturn and Revisit.
    /// </summary>
    private DecisionTriggerFrame AttachHesitationCandidateIfAny(in DecisionTriggerFrame frame)
    {
        if (frame.HesitationCandidateProductId != null)
            return frame;

        if (_session.BehaviorSession.IsCompareActive)
            return frame;

        var candidate = frame.FocusProductIfChanged
                        ?? frame.DwellProductIfThreshold
                        ?? _session.BehaviorSession.CurrentProductId;
        if (string.IsNullOrWhiteSpace(candidate))
            return frame;

        var cfg = _session.Config;
        if (!_session.BehaviorSession.IsHesitating(
                candidate,
                cfg.MinFocusCountForHesitation,
                cfg.MinRevisitCountForHesitation))
            return frame;

        return frame.WithHesitationCandidate(candidate);
    }

    private void TryAttachBehaviorContext(DecisionOutputBuildResult build, in ResolvedDecisionTrigger trigger)
    {
        try
        {
            var behaviorContext = DecisionBehaviorContextFactory.Create(_session.BehaviorSession, in trigger, _session.Config);
            if (build.Shape == DecisionOutputKind.SingleProduct && build.Single != null)
                build.Single.BehaviorContext = behaviorContext;
            else if (build.Shape == DecisionOutputKind.Comparison && build.Comparison != null)
                build.Comparison.BehaviorContext = behaviorContext;
        }
        catch (Exception)
        {
            // P7 context is additive; never block core P6 output flow.
        }
    }
}
