namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// High-level phase for the primary P6 decision surface (appearance delay, visible, soft fade).
/// </summary>
public enum DecisionPresentationPhase
{
    Idle,

    /// <summary>Output accepted but not yet shown (appearance delay).</summary>
    AppearancePending,

    /// <summary>Primary structured output is fully visible.</summary>
    PrimaryVisible,

    /// <summary>Soft fade / collapse window after full visibility.</summary>
    PrimaryFading
}
