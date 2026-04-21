namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// P6 system triggers. Strict runtime priority (highest wins): Compare, then Revisit, then Dwell.
/// Serialized as camelCase strings via <see cref="TrialIntelligence.ExportJson"/> settings.
/// </summary>
public enum DecisionTriggerKind
{
    Compare,
    Revisit,
    Dwell
}
