using System;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Deterministic trigger ordering for P6: Compare &gt; CompareReturn &gt; Hesitation &gt; Revisit &gt; Dwell
/// (only one active at a time).
/// </summary>
public static class DecisionTriggerPriority
{
    /// <summary>
    /// Highest priority first. Use this for stable iteration and documentation parity with product spec.
    /// </summary>
    public static readonly DecisionTriggerKind[] StrictDescendingOrder =
    {
        DecisionTriggerKind.Compare,
        DecisionTriggerKind.CompareReturn,
        DecisionTriggerKind.Hesitation,
        DecisionTriggerKind.Revisit,
        DecisionTriggerKind.Dwell
    };

    /// <summary>
    /// Lower rank value means higher priority (0 = Compare).
    /// </summary>
    public static int Rank(DecisionTriggerKind kind) =>
        kind switch
        {
            DecisionTriggerKind.Compare => 0,
            DecisionTriggerKind.CompareReturn => 1,
            DecisionTriggerKind.Hesitation => 2,
            DecisionTriggerKind.Revisit => 3,
            DecisionTriggerKind.Dwell => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
}
