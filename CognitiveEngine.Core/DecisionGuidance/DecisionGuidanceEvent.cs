namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Lightweight measurement event for P6/P7 testing and host UI binding.
/// <see cref="BehaviorSnapshot"/> and <see cref="PrimaryOutput"/> are populated on
/// <see cref="DecisionGuidanceEventKind.OutputEnqueued"/>,
/// <see cref="DecisionGuidanceEventKind.OutputBecameVisible"/>, and
/// <see cref="DecisionGuidanceEventKind.BehaviorUpdated"/> when P7 behavior context is attached.
/// </summary>
public readonly struct DecisionGuidanceEvent
{
    public DecisionGuidanceEvent(
        DecisionGuidanceEventKind kind,
        long logicalNowMs,
        string? productId = null,
        string? triggerSignature = null,
        GuidanceBehaviorSnapshot? behaviorSnapshot = null,
        ResolvedDecisionTrigger? trigger = null,
        DecisionOutputBuildResult? primaryOutput = null)
    {
        Kind = kind;
        LogicalNowMs = logicalNowMs;
        ProductId = productId;
        TriggerSignature = triggerSignature;
        BehaviorSnapshot = behaviorSnapshot;
        Trigger = trigger;
        PrimaryOutput = primaryOutput;
    }

    public DecisionGuidanceEventKind Kind { get; }

    public long LogicalNowMs { get; }

    public string? ProductId { get; }

    public string? TriggerSignature { get; }

    /// <summary>P7 coach-line payload for UI (rationale, confidence, disposition, signal).</summary>
    public GuidanceBehaviorSnapshot? BehaviorSnapshot { get; }

    public ResolvedDecisionTrigger? Trigger { get; }

    /// <summary>P6 structured copy plus P7 <c>behavior_context</c> when guidance is built.</summary>
    public DecisionOutputBuildResult? PrimaryOutput { get; }
}

public enum DecisionGuidanceEventKind
{
    FocusChanged,
    CompareInvoked,
    CompareEntered,
    CompareExited,
    DwellThresholdMet,
    TriggerResolved,
    /// <summary>Trigger resolved and output built, but primary presentation did not enqueue (busy, guard, suppression).</summary>
    BehaviorUpdated,
    OutputEnqueued,
    OutputBecameVisible,
    SelectNotified
}
