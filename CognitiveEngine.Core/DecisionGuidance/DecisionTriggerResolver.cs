using System;
using System.Collections.Generic;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Deterministic P6 trigger resolution: Compare &gt; Revisit &gt; Dwell, at most one emission per
/// <see cref="AdvanceFrame"/> / <see cref="Advance"/> call, comparison limited to the latest two
/// distinct interacted products, and suppression of repeated identical emissions until MRU pair
/// changes (for compare) or focus product changes (for single-product triggers).
/// </summary>
public sealed class DecisionTriggerResolver
{
    private readonly List<string> _mruDistinct = new();

    private string? _currentFocus;

    private readonly HashSet<string> _leftProducts = new(StringComparer.Ordinal);

    private string? _lastEmittedSignature;

    private string? _lastEmittedComparePairKey;

    public void Reset()
    {
        _mruDistinct.Clear();
        _currentFocus = null;
        _leftProducts.Clear();
        _lastEmittedSignature = null;
        _lastEmittedComparePairKey = null;
    }

    /// <summary>
    /// Clears duplicate-emission suppression so the last resolver emission can be re-offered when primary
    /// presentation refused it (busy pipeline, panel open, or panel cooldown — not repeat guard).
    /// </summary>
    public void ClearEmitSuppression()
    {
        _lastEmittedSignature = null;
        _lastEmittedComparePairKey = null;
    }

    /// <summary>Single-signal convenience; same as <see cref="AdvanceFrame"/> with one field set.</summary>
    public ResolvedDecisionTrigger? Advance(in DecisionTriggerInput input) =>
        AdvanceFrame(DecisionTriggerFrame.FromInput(in input));

    /// <summary>
    /// Applies optional focus update first (MRU + revisit eligibility), then evaluates Compare,
    /// then Revisit, then Dwell. At most one trigger is returned.
    /// </summary>
    public ResolvedDecisionTrigger? AdvanceFrame(in DecisionTriggerFrame frame)
    {
        ResolvedDecisionTrigger? revisitPending = null;

        if (!string.IsNullOrWhiteSpace(frame.FocusProductIfChanged))
        {
            UpdateFocusStateOnly(frame.FocusProductIfChanged);
            revisitPending = TryBuildRevisitCandidate(frame.FocusProductIfChanged);
        }

        if (frame.CompareInvoked)
        {
            var compare = BuildCompareCandidate();
            if (compare is { } cmp && !IsDuplicate(cmp))
            {
                RememberEmitted(cmp);
                if (revisitPending != null && !string.IsNullOrWhiteSpace(frame.FocusProductIfChanged))
                    CommitRevisitEligibilityConsumed(frame.FocusProductIfChanged);
                return cmp;
            }
        }

        if (revisitPending is { } rev && !IsDuplicate(rev))
        {
            RememberEmitted(rev);
            if (!string.IsNullOrWhiteSpace(frame.FocusProductIfChanged))
                CommitRevisitEligibilityConsumed(frame.FocusProductIfChanged);
            return rev;
        }

        if (!string.IsNullOrWhiteSpace(frame.DwellProductIfThreshold))
        {
            var dwell = BuildDwellCandidate(frame.DwellProductIfThreshold);
            if (dwell is { } dw && !IsDuplicate(dw))
            {
                RememberEmitted(dw);
                return dw;
            }
        }

        return null;
    }

    private void UpdateFocusStateOnly(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("productId is required.", nameof(productId));

        if (string.Equals(_currentFocus, productId, StringComparison.Ordinal))
            return;

        if (_currentFocus != null && !string.Equals(_currentFocus, productId, StringComparison.Ordinal))
            _leftProducts.Add(_currentFocus);

        TouchMru(productId);
        _currentFocus = productId;
        InvalidateCompareDedupeIfMruPairChanged();
        ClearSingleProductSignatureOnFocusSwitch();
    }

    private void ClearSingleProductSignatureOnFocusSwitch()
    {
        if (_lastEmittedSignature == null)
            return;
        if (_lastEmittedSignature.StartsWith("dwell:", StringComparison.Ordinal) ||
            _lastEmittedSignature.StartsWith("revisit:", StringComparison.Ordinal))
            _lastEmittedSignature = null;
    }

    private ResolvedDecisionTrigger? TryBuildRevisitCandidate(string productId)
    {
        if (!_leftProducts.Contains(productId))
            return null;
        return ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Revisit, productId);
    }

    private void CommitRevisitEligibilityConsumed(string productId)
    {
        _leftProducts.Remove(productId);
    }

    private ResolvedDecisionTrigger? BuildCompareCandidate()
    {
        if (_mruDistinct.Count < 2)
            return null;
        return ResolvedDecisionTrigger.ForCompare(_mruDistinct[0], _mruDistinct[1]);
    }

    private ResolvedDecisionTrigger? BuildDwellCandidate(string productId)
    {
        if (_currentFocus == null || !string.Equals(_currentFocus, productId, StringComparison.Ordinal))
            return null;
        return ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, productId);
    }

    private void TouchMru(string productId)
    {
        _mruDistinct.Remove(productId);
        _mruDistinct.Insert(0, productId);
        while (_mruDistinct.Count > 2)
            _mruDistinct.RemoveAt(_mruDistinct.Count - 1);
    }

    private void InvalidateCompareDedupeIfMruPairChanged()
    {
        var key = CurrentMruComparePairKey();
        if (_lastEmittedComparePairKey == null)
            return;
        if (key == _lastEmittedComparePairKey)
            return;
        if (_lastEmittedSignature != null &&
            _lastEmittedSignature.StartsWith("compare:", StringComparison.Ordinal))
            _lastEmittedSignature = null;
    }

    private string? CurrentMruComparePairKey()
    {
        if (_mruDistinct.Count < 2)
            return null;
        var x = ResolvedDecisionTrigger.ForCompare(_mruDistinct[0], _mruDistinct[1]);
        return $"{x.ProductIdLow}|{x.ProductIdHigh}";
    }

    private bool IsDuplicate(ResolvedDecisionTrigger candidate)
    {
        var sig = ResolvedDecisionTrigger.Signature(candidate);
        return sig == _lastEmittedSignature;
    }

    private void RememberEmitted(ResolvedDecisionTrigger t)
    {
        _lastEmittedSignature = ResolvedDecisionTrigger.Signature(t);
        if (t.Kind == DecisionTriggerKind.Compare)
            _lastEmittedComparePairKey = $"{t.ProductIdLow}|{t.ProductIdHigh}";
        else
            _lastEmittedComparePairKey = null;
    }
}
