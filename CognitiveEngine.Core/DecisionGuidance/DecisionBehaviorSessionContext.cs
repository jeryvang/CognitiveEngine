using System;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Lightweight P7 session-only continuity state.
/// Keeps deterministic, resettable context without cross-session identity.
/// </summary>
public sealed class DecisionBehaviorSessionContext
{
    public long LogicalNowMs { get; private set; }

    public string? CurrentProductId { get; private set; }

    public string? PreviousProductId { get; private set; }

    public int FocusSwitchCount { get; private set; }

    public int SelectionCount { get; private set; }

    public void Reset()
    {
        LogicalNowMs = 0;
        CurrentProductId = null;
        PreviousProductId = null;
        FocusSwitchCount = 0;
        SelectionCount = 0;
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

        PreviousProductId = CurrentProductId;
        CurrentProductId = productId;
        FocusSwitchCount++;
    }

    public void RecordSelect(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("productId is required.", nameof(productId));
        SelectionCount++;
    }
}
