namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Behavior mode for P6. Test mode keeps outputs clean and isolated for validation.
/// </summary>
public enum DecisionGuidanceMode
{
    /// <summary>
    /// P6 testing mode. Panel suppression and decision-detected nudge stopping are disabled.
    /// </summary>
    Test,

    /// <summary>
    /// Full behavior mode. Enables panel suppression and decision-detected nudge stopping.
    /// </summary>
    Full
}

