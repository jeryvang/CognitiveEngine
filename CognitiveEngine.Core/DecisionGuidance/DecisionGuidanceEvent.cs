namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Lightweight measurement event for P6 testing. The host can compute time-to-decision,
/// compare behavior, and hesitation patterns from these events.
/// </summary>
public readonly struct DecisionGuidanceEvent
{
    public DecisionGuidanceEvent(DecisionGuidanceEventKind kind, long logicalNowMs, string? productId = null, string? triggerSignature = null)
    {
        Kind = kind;
        LogicalNowMs = logicalNowMs;
        ProductId = productId;
        TriggerSignature = triggerSignature;
    }

    public DecisionGuidanceEventKind Kind { get; }

    public long LogicalNowMs { get; }

    public string? ProductId { get; }

    public string? TriggerSignature { get; }
}

public enum DecisionGuidanceEventKind
{
    FocusChanged,
    CompareInvoked,
    CompareEntered,
    CompareExited,
    DwellThresholdMet,
    TriggerResolved,
    OutputEnqueued,
    OutputBecameVisible,
    SelectNotified
}

