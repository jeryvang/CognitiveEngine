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

    public long LogicalNowMs { get; private set; }

    public string? CurrentProductId { get; private set; }

    public string? PreviousProductId { get; private set; }

    public int FocusSwitchCount { get; private set; }

    public int SelectionCount { get; private set; }

    public int SwipeCount { get; private set; }

    public string? LastSelectedProductId { get; private set; }

    public long? LastSelectedLogicalMs { get; private set; }

    public void Reset()
    {
        LogicalNowMs = 0;
        CurrentProductId = null;
        PreviousProductId = null;
        FocusSwitchCount = 0;
        SelectionCount = 0;
        SwipeCount = 0;
        LastSelectedProductId = null;
        LastSelectedLogicalMs = null;
        _selectionCountByProduct.Clear();
        _swipeTransitionCount.Clear();
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

        if (CurrentProductId != null)
        {
            SwipeCount++;
            var key = CurrentProductId + "->" + productId;
            _swipeTransitionCount.TryGetValue(key, out var prior);
            _swipeTransitionCount[key] = prior + 1;
        }

        PreviousProductId = CurrentProductId;
        CurrentProductId = productId;
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
}
