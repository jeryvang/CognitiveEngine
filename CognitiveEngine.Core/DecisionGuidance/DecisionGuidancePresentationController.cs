using System;
using System.Collections.Generic;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Deterministic presentation timing: appearance delay, visible window, soft fade, repeat guard,
/// suppression while primary output is in-flight or optional panel is open, and post-close cooldown.
/// Uses a logical millisecond clock advanced via <see cref="AdvanceMs"/> (host maps from Unity time).
/// </summary>
public sealed class DecisionGuidancePresentationController
{
    private readonly DecisionGuidanceConfig _cfg;

    private long _nowMs;

    private bool _panelOpen;

    private long _panelCooldownUntilMs;

    private readonly Dictionary<string, long> _repeatGuardUntilMs = new(StringComparer.Ordinal);

    private DecisionPresentationPhase _phase = DecisionPresentationPhase.Idle;

    private long _offerStartMs;

    private long _visibleAtMs;

    private long _fadeStartAtMs;

    private long _idleAtMs;

    private string? _activeSignature;

    private DecisionOutputBuildResult? _activeBuild;

    public DecisionGuidancePresentationController(DecisionGuidanceConfig? config = null)
    {
        _cfg = config ?? DecisionGuidanceConfig.CreateDefault();
        _cfg.ValidateOrThrow();
    }

    public long LogicalNowMs => _nowMs;

    public DecisionPresentationPhase Phase => _phase;

    public bool IsPanelOpen => _panelOpen;

    /// <summary>True while primary output is waiting, visible, or fading (blocks stacking a second full output).</summary>
    public bool IsPrimaryPipelineActive =>
        _phase is DecisionPresentationPhase.AppearancePending
            or DecisionPresentationPhase.PrimaryVisible
            or DecisionPresentationPhase.PrimaryFading;

    /// <summary>
    /// Non-null during <see cref="DecisionPresentationPhase.PrimaryVisible"/> and
    /// <see cref="DecisionPresentationPhase.PrimaryFading"/> only (direct path: nothing to tap before this appears).
    /// </summary>
    public DecisionOutputBuildResult? CurrentPrimaryOutput =>
        _activeBuild != null &&
        _phase is DecisionPresentationPhase.PrimaryVisible or DecisionPresentationPhase.PrimaryFading
            ? _activeBuild
            : null;

    /// <summary>0 during appearance pending, 1 while fully visible, linear 1→0 during soft fade, 0 when idle.</summary>
    public float GetPrimaryPresentationAlpha()
    {
        if (_activeBuild == null)
            return 0f;
        if (_nowMs < _visibleAtMs)
            return 0f;
        if (_nowMs < _fadeStartAtMs)
            return 1f;
        if (_nowMs < _idleAtMs)
        {
            double span = _cfg.SoftFadeOrCollapseMs <= 0 ? 1.0 : _cfg.SoftFadeOrCollapseMs;
            double u = (_nowMs - _fadeStartAtMs) / span;
            u = Math.Clamp(u, 0.0, 1.0);
            return (float)(1.0 - u);
        }

        return 0f;
    }

    public void Reset()
    {
        _nowMs = 0;
        _panelOpen = false;
        _panelCooldownUntilMs = 0;
        _repeatGuardUntilMs.Clear();
        ClearActivePipeline();
    }

    /// <summary>Host calls when optional detail panel opens or closes.</summary>
    public void SetPanelOpen(bool isOpen)
    {
        if (isOpen)
        {
            if (!_panelOpen)
            {
                _panelOpen = true;
                if (_phase != DecisionPresentationPhase.Idle)
                    AbortPipelineWithoutRepeatGuard();
            }

            return;
        }

        if (_panelOpen)
        {
            _panelOpen = false;
            _panelCooldownUntilMs = _nowMs + _cfg.PanelCloseCooldownMs;
        }
    }

    /// <summary>
    /// Cancels appearance/visible/fade without applying repeat guard (fast product switch, explicit select, session reset).
    /// </summary>
    public void CancelPrimaryPipeline() => AbortPipelineWithoutRepeatGuard();

    /// <summary>
    /// Attempts to start the primary output timeline. Returns false if suppressed (panel, pipeline busy,
    /// panel-close cooldown, or repeat guard for the same trigger signature).
    /// </summary>
    public bool TryEnqueuePrimaryOutput(ResolvedDecisionTrigger trigger, DecisionOutputBuildResult buildResult)
    {
        if (buildResult == null)
            throw new ArgumentNullException(nameof(buildResult));

        PruneExpiredRepeatGuards();

        if (_panelOpen)
            return false;
        if (IsPrimaryPipelineActive)
            return false;
        if (_nowMs < _panelCooldownUntilMs)
            return false;

        var sig = ResolvedDecisionTrigger.Signature(trigger);
        if (_repeatGuardUntilMs.TryGetValue(sig, out var until) && _nowMs < until)
            return false;

        _activeSignature = sig;
        _activeBuild = buildResult;
        _offerStartMs = _nowMs;
        _visibleAtMs = _offerStartMs + _cfg.AppearanceDelayMs;
        _fadeStartAtMs = _visibleAtMs + _cfg.PrimaryVisibleMs;
        _idleAtMs = _fadeStartAtMs + _cfg.SoftFadeOrCollapseMs;
        _phase = DecisionPresentationPhase.AppearancePending;
        CatchUpTransitions();
        return true;
    }

    public void AdvanceMs(long deltaMs)
    {
        if (deltaMs <= 0)
            return;
        _nowMs += deltaMs;
        CatchUpTransitions();
    }

    private void PruneExpiredRepeatGuards()
    {
        if (_repeatGuardUntilMs.Count == 0)
            return;
        var stale = new List<string>();
        foreach (var kv in _repeatGuardUntilMs)
        {
            if (_nowMs >= kv.Value)
                stale.Add(kv.Key);
        }

        foreach (var k in stale)
            _repeatGuardUntilMs.Remove(k);
    }

    private void CatchUpTransitions()
    {
        while (_activeBuild != null)
        {
            if (_nowMs < _visibleAtMs)
            {
                _phase = DecisionPresentationPhase.AppearancePending;
                return;
            }

            if (_nowMs < _fadeStartAtMs)
            {
                _phase = DecisionPresentationPhase.PrimaryVisible;
                return;
            }

            if (_nowMs < _idleAtMs)
            {
                _phase = DecisionPresentationPhase.PrimaryFading;
                return;
            }

            CompleteWithRepeatGuard();
        }
    }

    private void CompleteWithRepeatGuard()
    {
        if (_activeSignature != null)
            _repeatGuardUntilMs[_activeSignature] = _nowMs + _cfg.RepeatGuardMs;
        ClearActivePipeline();
    }

    private void AbortPipelineWithoutRepeatGuard()
    {
        ClearActivePipeline();
    }

    private void ClearActivePipeline()
    {
        _phase = DecisionPresentationPhase.Idle;
        _activeSignature = null;
        _activeBuild = null;
    }
}
