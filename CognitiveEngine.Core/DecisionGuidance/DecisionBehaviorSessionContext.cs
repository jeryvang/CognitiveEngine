using System;
using System.Collections.Generic;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Lightweight P7 session-only continuity state.
/// Keeps deterministic, resettable context without cross-session identity.
/// </summary>
public sealed class DecisionBehaviorSessionContext
{
    private readonly SortedDictionary<string, int> _selectionCountByProduct =
        new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, int> _swipeTransitionCount =
        new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, int> _revisitCountByProduct =
        new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, int> _comparePairCount =
        new(StringComparer.Ordinal);
    private readonly HashSet<string> _visitedProducts = new(StringComparer.Ordinal);

    public int SessionGeneration { get; private set; } = 1;

    public long? LastSessionEndedLogicalMs { get; private set; }

    public long LogicalNowMs { get; private set; }

    public string? CurrentProductId { get; private set; }

    public string? PreviousProductId { get; private set; }

    public int FocusSwitchCount { get; private set; }

    public int SelectionCount { get; private set; }

    public int SwipeCount { get; private set; }

    public int RevisitCount { get; private set; }

    public int CompareCount { get; private set; }

    public bool IsCompareActive { get; private set; }

    public string? LastCompareProductIdA { get; private set; }

    public string? LastCompareProductIdB { get; private set; }

    public long? LastCompareEnteredLogicalMs { get; private set; }

    public long? LastCompareExitedLogicalMs { get; private set; }

    public string? LastSelectedProductId { get; private set; }

    public long? LastSelectedLogicalMs { get; private set; }

    public bool HasAnyBehaviorSignals =>
        FocusSwitchCount > 0 || SelectionCount > 0 || SwipeCount > 0 || RevisitCount > 0 || CompareCount > 0;

    public void ResetForNewSession()
    {
        LastSessionEndedLogicalMs = LogicalNowMs;
        SessionGeneration++;
        ClearMutableSessionState();
    }

    public void Reset() => ResetForNewSession();

    private void ClearMutableSessionState()
    {
        LogicalNowMs = 0;
        CurrentProductId = null;
        PreviousProductId = null;
        FocusSwitchCount = 0;
        SelectionCount = 0;
        SwipeCount = 0;
        RevisitCount = 0;
        CompareCount = 0;
        IsCompareActive = false;
        LastCompareProductIdA = null;
        LastCompareProductIdB = null;
        LastCompareEnteredLogicalMs = null;
        LastCompareExitedLogicalMs = null;
        LastSelectedProductId = null;
        LastSelectedLogicalMs = null;
        _selectionCountByProduct.Clear();
        _swipeTransitionCount.Clear();
        _revisitCountByProduct.Clear();
        _comparePairCount.Clear();
        _visitedProducts.Clear();
    }

    public void SetLogicalNow(long logicalNowMs)
    {
        if (logicalNowMs < 0)
            throw new ArgumentOutOfRangeException(nameof(logicalNowMs), logicalNowMs, "logicalNowMs must be non-negative.");

        LogicalNowMs = logicalNowMs;
    }

    public void RecordFocusChanged(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("productId is required.", nameof(productId));

        if (string.Equals(CurrentProductId, productId, StringComparison.Ordinal))
            return;

        if (_visitedProducts.Contains(productId))
        {
            RevisitCount++;
            _revisitCountByProduct.TryGetValue(productId, out var priorRevisitCount);
            _revisitCountByProduct[productId] = priorRevisitCount + 1;
        }

        if (CurrentProductId != null)
        {
            SwipeCount++;
            var key = CurrentProductId + "->" + productId;
            _swipeTransitionCount.TryGetValue(key, out var prior);
            _swipeTransitionCount[key] = prior + 1;
        }

        PreviousProductId = CurrentProductId;
        CurrentProductId = productId;
        _visitedProducts.Add(productId);
        FocusSwitchCount++;
    }

    public void RecordSelect(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("productId is required.", nameof(productId));

        LastSelectedProductId = productId;
        LastSelectedLogicalMs = LogicalNowMs;
        SelectionCount++;
        _selectionCountByProduct.TryGetValue(productId, out var prior);
        _selectionCountByProduct[productId] = prior + 1;
    }

    public void RecordCompareInvoked(string? productIdA, string? productIdB)
    {
        CompareCount++;
        if (!TryBuildPairKey(productIdA, productIdB, out var productLow, out var productHigh, out var key))
            return;

        LastCompareProductIdA = productLow;
        LastCompareProductIdB = productHigh;
        _comparePairCount.TryGetValue(key, out var prior);
        _comparePairCount[key] = prior + 1;
    }

    public void RecordCompareEntered()
    {
        IsCompareActive = true;
        LastCompareEnteredLogicalMs = LogicalNowMs;
    }

    public void RecordCompareExited()
    {
        IsCompareActive = false;
        LastCompareExitedLogicalMs = LogicalNowMs;
    }

    public int GetSelectionCount(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("productId is required.", nameof(productId));
        return _selectionCountByProduct.TryGetValue(productId, out var count) ? count : 0;
    }

    public IReadOnlyDictionary<string, int> GetSelectionCountsByProduct() => _selectionCountByProduct;

    public int GetSwipeTransitionCount(string fromProductId, string toProductId)
    {
        if (string.IsNullOrWhiteSpace(fromProductId))
            throw new ArgumentException("fromProductId is required.", nameof(fromProductId));
        if (string.IsNullOrWhiteSpace(toProductId))
            throw new ArgumentException("toProductId is required.", nameof(toProductId));
        var key = fromProductId + "->" + toProductId;
        return _swipeTransitionCount.TryGetValue(key, out var count) ? count : 0;
    }

    public IReadOnlyDictionary<string, int> GetSwipeTransitionCounts() => _swipeTransitionCount;

    public int GetRevisitCount(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("productId is required.", nameof(productId));
        return _revisitCountByProduct.TryGetValue(productId, out var count) ? count : 0;
    }

    public IReadOnlyDictionary<string, int> GetRevisitCountsByProduct() => _revisitCountByProduct;

    public int GetComparePairCount(string productIdA, string productIdB)
    {
        if (!TryBuildPairKey(productIdA, productIdB, out _, out _, out var key))
            throw new ArgumentException("Both compare product ids must be non-empty and distinct.");
        return _comparePairCount.TryGetValue(key, out var count) ? count : 0;
    }

    public IReadOnlyDictionary<string, int> GetComparePairCounts() => _comparePairCount;

    private static bool TryBuildPairKey(
        string? productIdA,
        string? productIdB,
        out string productLow,
        out string productHigh,
        out string key)
    {
        productLow = "";
        productHigh = "";
        key = "";

        if (string.IsNullOrWhiteSpace(productIdA) ||
            string.IsNullOrWhiteSpace(productIdB) ||
            string.Equals(productIdA, productIdB, StringComparison.Ordinal))
            return false;

        if (string.CompareOrdinal(productIdA, productIdB) <= 0)
        {
            productLow = productIdA;
            productHigh = productIdB;
        }
        else
        {
            productLow = productIdB;
            productHigh = productIdA;
        }

        key = productLow + "|" + productHigh;
        return true;
    }
}
