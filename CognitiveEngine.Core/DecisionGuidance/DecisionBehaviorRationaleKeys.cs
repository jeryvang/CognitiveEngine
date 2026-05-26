namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Stable, snake_case keys for each P7 <c>why_this_matters_now</c> branch.
/// Hosts use these for localization tables, LLM input prompts, analytics, or fallback maps.
/// Keys are versioned: once published, an existing key's semantics do not change.
/// </summary>
public static class DecisionBehaviorRationaleKeys
{
    public const string SingleWeak = "single_weak";
    public const string SingleRevisitAndSelectionLead = "single_revisit_and_selection_lead";
    public const string SingleSelectionLead = "single_selection_lead";
    public const string SingleDwellLead = "single_dwell_lead";
    public const string SingleRepeatedFocus = "single_repeated_focus";
    public const string SingleAmbiguous = "single_ambiguous";
    public const string SingleFallback = "single_fallback";

    public const string CompareReturnWeak = "compare_return_weak";
    public const string CompareReturnLeanFocused = "compare_return_lean_focused";
    public const string CompareReturnAmbiguous = "compare_return_ambiguous";
    public const string CompareReturnFallback = "compare_return_fallback";

    public const string CompareWeak = "compare_weak";
    public const string CompareRepeatedPair = "compare_repeated_pair";
    public const string CompareLean = "compare_lean";
    public const string CompareFallback = "compare_fallback";
}
