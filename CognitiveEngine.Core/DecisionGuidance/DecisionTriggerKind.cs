namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// P6 system triggers. Strict runtime priority (highest wins): Compare, CompareReturn, Hesitation, Revisit, Dwell.
/// Serialized as camelCase strings via <see cref="TrialIntelligence.ExportJson"/> settings.
/// </summary>
public enum DecisionTriggerKind
{
    Compare,
    CompareReturn,
    Hesitation,
    Revisit,
    Dwell
}
