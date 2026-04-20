using System;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Wires trigger resolution, presentation timing, and session-level rules: decision confirmation
/// (Select or sustained stay after primary output), clearing suppression when focus leaves the
/// confirmed product, cancelling the primary pipeline on fast product switches, and full reset on
/// session restart.
/// </summary>
public sealed class DecisionGuidanceSessionCoordinator
{
    private readonly DecisionGuidanceConfig _cfg;
    private readonly DecisionGuidanceMode _mode;
    private readonly DecisionTriggerResolver _resolver = new();
    private readonly DecisionGuidancePresentationController _presentation;
    private readonly DecisionBehaviorSessionContext _behaviorSession = new();

    private string? _lastFocusedProductId;

    private string? _confirmedProductId;

    private long? _sustainedStayStartMs;

    private string? _sustainedStayProductId;

    public DecisionGuidanceSessionCoordinator(DecisionGuidanceConfig? config = null)
    {
        _cfg = config ?? DecisionGuidanceConfig.CreateDefault();
        _cfg.ValidateOrThrow();
        _mode = _cfg.Mode;
        _presentation = new DecisionGuidancePresentationController(_cfg);
    }

    public DecisionGuidanceConfig Config => _cfg;

    public DecisionTriggerResolver Resolver => _resolver;

    public DecisionGuidancePresentationController Presentation => _presentation;

    public DecisionBehaviorSessionContext BehaviorSession => _behaviorSession;

    public string? CurrentFocusedProductId => _lastFocusedProductId;

    public string? ConfirmedProductId => _confirmedProductId;

    /// <summary>True when nudges are suppressed for the current focused product after Select or sustained stay.</summary>
    public bool IsNudgeSuppressedForCurrentFocus =>
        _mode == DecisionGuidanceMode.Full &&
        _confirmedProductId != null &&
        _lastFocusedProductId != null &&
        string.Equals(_lastFocusedProductId, _confirmedProductId, StringComparison.Ordinal);

    public void ResetSession()
    {
        _resolver.Reset();
        _presentation.Reset();
        _behaviorSession.ResetForNewSession();
        _lastFocusedProductId = null;
        _confirmedProductId = null;
        _sustainedStayStartMs = null;
        _sustainedStayProductId = null;
    }

    public void SetPanelOpen(bool isOpen) => _presentation.SetPanelOpen(isOpen);

    public void NotifyCompareInvoked() =>
        _behaviorSession.RecordCompareInvoked(_behaviorSession.CurrentProductId, _behaviorSession.PreviousProductId);

    public void NotifyCompareEntered() => _behaviorSession.RecordCompareEntered();

    public void NotifyCompareExited() => _behaviorSession.RecordCompareExited();

    /// <summary>
    /// Call when the user focuses a different product. Cancels an in-flight primary pipeline (pending timers),
    /// clears sustained-stay tracking, advances resolver revisit/MRU state, and clears decision suppression
    /// when navigating away from the previously confirmed product.
    /// </summary>
    public void NotifyProductFocusChanged(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("productId is required.", nameof(productId));

        if (string.Equals(_lastFocusedProductId, productId, StringComparison.Ordinal))
            return;

        if (_confirmedProductId != null &&
            !string.Equals(productId, _confirmedProductId, StringComparison.Ordinal))
            _confirmedProductId = null;

        if (_presentation.IsPrimaryPipelineActive)
            _presentation.CancelPrimaryPipeline();

        _sustainedStayStartMs = null;
        _sustainedStayProductId = null;

        _resolver.Advance(DecisionTriggerInput.FocusChanged(productId));
        _behaviorSession.RecordFocusChanged(productId);
        _lastFocusedProductId = productId;
    }

    /// <summary>Call when the user confirms choice for a product; stops nudges while focus remains on that product.</summary>
    public void NotifySelect(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("productId is required.", nameof(productId));

        _behaviorSession.RecordSelect(productId);

        if (_mode == DecisionGuidanceMode.Test)
            return;

        _confirmedProductId = productId;
        _presentation.CancelPrimaryPipeline();
        _sustainedStayStartMs = null;
        _sustainedStayProductId = null;
    }

    /// <summary>
    /// Enqueues primary output if presentation allows and session rules do not suppress nudges.
    /// </summary>
    public bool TryEnqueuePrimaryOutput(ResolvedDecisionTrigger trigger, DecisionOutputBuildResult buildResult)
    {
        if (buildResult == null)
            throw new ArgumentNullException(nameof(buildResult));
        if (IsNudgeSuppressedForCurrentFocus)
            return false;
        return _presentation.TryEnqueuePrimaryOutput(trigger, buildResult);
    }

    public void AdvanceMs(long deltaMs)
    {
        var phaseBefore = _presentation.Phase;
        _presentation.AdvanceMs(deltaMs);
        _behaviorSession.SetLogicalNow(_presentation.LogicalNowMs);
        var phaseAfter = _presentation.Phase;

        if (phaseBefore != DecisionPresentationPhase.PrimaryVisible &&
            phaseAfter == DecisionPresentationPhase.PrimaryVisible)
        {
            _sustainedStayStartMs = _presentation.LogicalNowMs;
            _sustainedStayProductId = _lastFocusedProductId;
        }

        if (phaseAfter == DecisionPresentationPhase.Idle &&
            phaseBefore != DecisionPresentationPhase.Idle &&
            _confirmedProductId == null)
        {
            _sustainedStayStartMs = null;
            _sustainedStayProductId = null;
        }

        if (!IsNudgeSuppressedForCurrentFocus &&
            _sustainedStayStartMs != null &&
            _sustainedStayProductId != null &&
            _lastFocusedProductId != null &&
            string.Equals(_lastFocusedProductId, _sustainedStayProductId, StringComparison.Ordinal) &&
            (phaseAfter == DecisionPresentationPhase.PrimaryVisible ||
             phaseAfter == DecisionPresentationPhase.PrimaryFading))
        {
            if (_mode == DecisionGuidanceMode.Test)
                return;

            if (_presentation.LogicalNowMs - _sustainedStayStartMs.Value >= _cfg.SustainedStayAfterOutputMs)
            {
                _confirmedProductId = _sustainedStayProductId;
                _sustainedStayStartMs = null;
                _sustainedStayProductId = null;
            }
        }
    }
}
