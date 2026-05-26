using System;
using System.Collections.Generic;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Deterministic P6 trigger resolution: Compare &gt; CompareReturn &gt; Revisit &gt; Dwell, at most one emission per
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

    private bool _compareReturnArmed;

    private string? _compareReturnPairLow;

    private string? _compareReturnPairHigh;

    public void Reset()
    {
        _mruDistinct.Clear();
        _currentFocus = null;
        _leftProducts.Clear();
        _lastEmittedSignature = null;
        _lastEmittedComparePairKey = null;
        DisarmCompareReturn();
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

    /// <summary>
    /// Arms compare-return eligibility after the host reports compare UI exit. The next focus on either
    /// product in the pair may emit <see cref="DecisionTriggerKind.CompareReturn"/>.
    /// </summary>
    public void ArmCompareReturn(string productIdA, string productIdB)
    {
        if (string.IsNullOrWhiteSpace(productIdA))
            throw new ArgumentException("productIdA is required.", nameof(productIdA));
        if (string.IsNullOrWhiteSpace(productIdB))
            throw new ArgumentException("productIdB is required.", nameof(productIdB));
        if (string.Equals(productIdA, productIdB, StringComparison.Ordinal))
            throw new ArgumentException("CompareReturn pair requires two distinct product ids.");

        if (string.CompareOrdinal(productIdA, productIdB) < 0)
        {
            _compareReturnPairLow = productIdA;
            _compareReturnPairHigh = productIdB;
        }
        else
        {
            _compareReturnPairLow = productIdB;
            _compareReturnPairHigh = productIdA;
        }

        _compareReturnArmed = true;
    }

    /// <summary>Uses the latest two MRU products when an explicit compare pair was not recorded.</summary>
    public bool TryArmCompareReturnFromMru()
    {
        if (_mruDistinct.Count < 2)
            return false;

        ArmCompareReturn(_mruDistinct[0], _mruDistinct[1]);
        return true;
    }

    public void DisarmCompareReturn()
    {
        _compareReturnArmed = false;
        _compareReturnPairLow = null;
        _compareReturnPairHigh = null;
    }

    /// <summary>Single-signal convenience; same as <see cref="AdvanceFrame"/> with one field set.</summary>
    public ResolvedDecisionTrigger? Advance(in DecisionTriggerInput input) =>
        AdvanceFrame(DecisionTriggerFrame.FromInput(in input));

    /// <summary>
    /// Applies optional focus update first (MRU + revisit/compare-return eligibility), then evaluates Compare,
    /// then CompareReturn, then Hesitation, then Revisit, then Dwell. At most one trigger is returned.
    /// </summary>
    public ResolvedDecisionTrigger? AdvanceFrame(in DecisionTriggerFrame frame)
    {
        ResolvedDecisionTrigger? revisitPending = null;
        ResolvedDecisionTrigger? compareReturnPending = null;

        if (!string.IsNullOrWhiteSpace(frame.FocusProductIfChanged))
        {
            UpdateFocusStateOnly(frame.FocusProductIfChanged);
            compareReturnPending = TryBuildCompareReturnCandidate(frame.FocusProductIfChanged);
            revisitPending = TryBuildRevisitCandidate(frame.FocusProductIfChanged);
        }

        if (frame.CompareInvoked)
        {
            DisarmCompareReturn();
            var compare = BuildCompareCandidate();
            if (compare is { } cmp && !IsDuplicate(cmp))
            {
                RememberEmitted(cmp);
                if (!string.IsNullOrWhiteSpace(frame.FocusProductIfChanged))
                    CommitFocusEligibilityConsumed(frame.FocusProductIfChanged);
                return cmp;
            }
        }

        if (compareReturnPending is { } cr && !IsDuplicate(cr))
        {
            RememberEmitted(cr);
            if (!string.IsNullOrWhiteSpace(frame.FocusProductIfChanged))
                CommitFocusEligibilityConsumed(frame.FocusProductIfChanged);
            return cr;
        }

        if (!string.IsNullOrWhiteSpace(frame.HesitationCandidateProductId))
        {
            var hesitation = ResolvedDecisionTrigger.ForSingle(
                DecisionTriggerKind.Hesitation, frame.HesitationCandidateProductId);
            if (!IsDuplicate(hesitation))
            {
                RememberEmitted(hesitation);
                if (!string.IsNullOrWhiteSpace(frame.FocusProductIfChanged))
                    CommitFocusEligibilityConsumed(frame.FocusProductIfChanged);
                return hesitation;
            }
        }

        if (revisitPending is { } rev && !IsDuplicate(rev))
        {
            RememberEmitted(rev);
            if (!string.IsNullOrWhiteSpace(frame.FocusProductIfChanged))
                CommitFocusEligibilityConsumed(frame.FocusProductIfChanged);
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
            _lastEmittedSignature.StartsWith("revisit:", StringComparison.Ordinal) ||
            _lastEmittedSignature.StartsWith("compare_return:", StringComparison.Ordinal) ||
            _lastEmittedSignature.StartsWith("hesitation:", StringComparison.Ordinal))
            _lastEmittedSignature = null;
    }

    private ResolvedDecisionTrigger? TryBuildCompareReturnCandidate(string focusedProductId)
    {
        if (!_compareReturnArmed || !IsInCompareReturnPair(focusedProductId))
            return null;

        var partner = string.Equals(focusedProductId, _compareReturnPairLow, StringComparison.Ordinal)
            ? _compareReturnPairHigh
            : _compareReturnPairLow;
        return ResolvedDecisionTrigger.ForCompareReturn(focusedProductId, partner!);
    }

    private bool IsInCompareReturnPair(string productId) =>
        string.Equals(productId, _compareReturnPairLow, StringComparison.Ordinal) ||
        string.Equals(productId, _compareReturnPairHigh, StringComparison.Ordinal);

    private ResolvedDecisionTrigger? TryBuildRevisitCandidate(string productId)
    {
        if (!_leftProducts.Contains(productId))
            return null;
        return ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Revisit, productId);
    }

    private void CommitFocusEligibilityConsumed(string productId)
    {
        _leftProducts.Remove(productId);
        DisarmCompareReturn();
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
