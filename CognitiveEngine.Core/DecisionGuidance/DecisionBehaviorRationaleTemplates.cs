using System;
using Newtonsoft.Json;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Host-configurable copy for P7 <c>why_this_matters_now</c> rationale branches.
/// Each branch has two variants:
/// <list type="bullet">
///   <item><description><c>{Branch}</c> — full sentence (used as <c>why_this_matters_now</c> fallback display)</description></item>
///   <item><description><c>{Branch}Phrase</c> — short behavior prefix (composed by the host with catalog framing)</description></item>
/// </list>
/// Omitted JSON fields fall back to built-in defaults; logic keys are stable across versions.
/// </summary>
public sealed class DecisionBehaviorRationaleTemplates
{
    [JsonProperty("single_weak")]
    public string SingleWeak { get; set; } = "";

    [JsonProperty("single_weak_phrase")]
    public string SingleWeakPhrase { get; set; } = "";

    [JsonProperty("single_revisit_and_selection_lead")]
    public string SingleRevisitAndSelectionLead { get; set; } = "";

    [JsonProperty("single_revisit_and_selection_lead_phrase")]
    public string SingleRevisitAndSelectionLeadPhrase { get; set; } = "";

    [JsonProperty("single_selection_lead")]
    public string SingleSelectionLead { get; set; } = "";

    [JsonProperty("single_selection_lead_phrase")]
    public string SingleSelectionLeadPhrase { get; set; } = "";

    [JsonProperty("single_dwell_lead")]
    public string SingleDwellLead { get; set; } = "";

    [JsonProperty("single_dwell_lead_phrase")]
    public string SingleDwellLeadPhrase { get; set; } = "";

    [JsonProperty("single_repeated_focus")]
    public string SingleRepeatedFocus { get; set; } = "";

    [JsonProperty("single_repeated_focus_phrase")]
    public string SingleRepeatedFocusPhrase { get; set; } = "";

    [JsonProperty("single_ambiguous")]
    public string SingleAmbiguous { get; set; } = "";

    [JsonProperty("single_ambiguous_phrase")]
    public string SingleAmbiguousPhrase { get; set; } = "";

    [JsonProperty("single_fallback")]
    public string SingleFallback { get; set; } = "";

    [JsonProperty("single_fallback_phrase")]
    public string SingleFallbackPhrase { get; set; } = "";

    [JsonProperty("compare_return_weak")]
    public string CompareReturnWeak { get; set; } = "";

    [JsonProperty("compare_return_weak_phrase")]
    public string CompareReturnWeakPhrase { get; set; } = "";

    [JsonProperty("compare_return_lean_focused")]
    public string CompareReturnLeanFocused { get; set; } = "";

    [JsonProperty("compare_return_lean_focused_phrase")]
    public string CompareReturnLeanFocusedPhrase { get; set; } = "";

    [JsonProperty("compare_return_ambiguous")]
    public string CompareReturnAmbiguous { get; set; } = "";

    [JsonProperty("compare_return_ambiguous_phrase")]
    public string CompareReturnAmbiguousPhrase { get; set; } = "";

    [JsonProperty("compare_return_fallback")]
    public string CompareReturnFallback { get; set; } = "";

    [JsonProperty("compare_return_fallback_phrase")]
    public string CompareReturnFallbackPhrase { get; set; } = "";

    [JsonProperty("compare_weak")]
    public string CompareWeak { get; set; } = "";

    [JsonProperty("compare_weak_phrase")]
    public string CompareWeakPhrase { get; set; } = "";

    [JsonProperty("compare_repeated_pair")]
    public string CompareRepeatedPair { get; set; } = "";

    [JsonProperty("compare_repeated_pair_phrase")]
    public string CompareRepeatedPairPhrase { get; set; } = "";

    [JsonProperty("compare_lean")]
    public string CompareLean { get; set; } = "";

    [JsonProperty("compare_lean_phrase")]
    public string CompareLeanPhrase { get; set; } = "";

    [JsonProperty("compare_fallback")]
    public string CompareFallback { get; set; } = "";

    [JsonProperty("compare_fallback_phrase")]
    public string CompareFallbackPhrase { get; set; } = "";

    public static DecisionBehaviorRationaleTemplates CreateDefault() =>
        new()
        {
            SingleWeak = BuiltIn.SingleWeak,
            SingleWeakPhrase = BuiltIn.SingleWeakPhrase,
            SingleRevisitAndSelectionLead = BuiltIn.SingleRevisitAndSelectionLead,
            SingleRevisitAndSelectionLeadPhrase = BuiltIn.SingleRevisitAndSelectionLeadPhrase,
            SingleSelectionLead = BuiltIn.SingleSelectionLead,
            SingleSelectionLeadPhrase = BuiltIn.SingleSelectionLeadPhrase,
            SingleDwellLead = BuiltIn.SingleDwellLead,
            SingleDwellLeadPhrase = BuiltIn.SingleDwellLeadPhrase,
            SingleRepeatedFocus = BuiltIn.SingleRepeatedFocus,
            SingleRepeatedFocusPhrase = BuiltIn.SingleRepeatedFocusPhrase,
            SingleAmbiguous = BuiltIn.SingleAmbiguous,
            SingleAmbiguousPhrase = BuiltIn.SingleAmbiguousPhrase,
            SingleFallback = BuiltIn.SingleFallback,
            SingleFallbackPhrase = BuiltIn.SingleFallbackPhrase,
            CompareReturnWeak = BuiltIn.CompareReturnWeak,
            CompareReturnWeakPhrase = BuiltIn.CompareReturnWeakPhrase,
            CompareReturnLeanFocused = BuiltIn.CompareReturnLeanFocused,
            CompareReturnLeanFocusedPhrase = BuiltIn.CompareReturnLeanFocusedPhrase,
            CompareReturnAmbiguous = BuiltIn.CompareReturnAmbiguous,
            CompareReturnAmbiguousPhrase = BuiltIn.CompareReturnAmbiguousPhrase,
            CompareReturnFallback = BuiltIn.CompareReturnFallback,
            CompareReturnFallbackPhrase = BuiltIn.CompareReturnFallbackPhrase,
            CompareWeak = BuiltIn.CompareWeak,
            CompareWeakPhrase = BuiltIn.CompareWeakPhrase,
            CompareRepeatedPair = BuiltIn.CompareRepeatedPair,
            CompareRepeatedPairPhrase = BuiltIn.CompareRepeatedPairPhrase,
            CompareLean = BuiltIn.CompareLean,
            CompareLeanPhrase = BuiltIn.CompareLeanPhrase,
            CompareFallback = BuiltIn.CompareFallback,
            CompareFallbackPhrase = BuiltIn.CompareFallbackPhrase
        };

    /// <summary>Merges optional host overrides onto built-in defaults.</summary>
    public static DecisionBehaviorRationaleTemplates ResolveEffective(DecisionBehaviorRationaleTemplates? overrides)
    {
        if (overrides == null)
            return CreateDefault();

        return new DecisionBehaviorRationaleTemplates
        {
            SingleWeak = Pick(overrides.SingleWeak, BuiltIn.SingleWeak),
            SingleWeakPhrase = Pick(overrides.SingleWeakPhrase, BuiltIn.SingleWeakPhrase),
            SingleRevisitAndSelectionLead = Pick(overrides.SingleRevisitAndSelectionLead, BuiltIn.SingleRevisitAndSelectionLead),
            SingleRevisitAndSelectionLeadPhrase = Pick(overrides.SingleRevisitAndSelectionLeadPhrase, BuiltIn.SingleRevisitAndSelectionLeadPhrase),
            SingleSelectionLead = Pick(overrides.SingleSelectionLead, BuiltIn.SingleSelectionLead),
            SingleSelectionLeadPhrase = Pick(overrides.SingleSelectionLeadPhrase, BuiltIn.SingleSelectionLeadPhrase),
            SingleDwellLead = Pick(overrides.SingleDwellLead, BuiltIn.SingleDwellLead),
            SingleDwellLeadPhrase = Pick(overrides.SingleDwellLeadPhrase, BuiltIn.SingleDwellLeadPhrase),
            SingleRepeatedFocus = Pick(overrides.SingleRepeatedFocus, BuiltIn.SingleRepeatedFocus),
            SingleRepeatedFocusPhrase = Pick(overrides.SingleRepeatedFocusPhrase, BuiltIn.SingleRepeatedFocusPhrase),
            SingleAmbiguous = Pick(overrides.SingleAmbiguous, BuiltIn.SingleAmbiguous),
            SingleAmbiguousPhrase = Pick(overrides.SingleAmbiguousPhrase, BuiltIn.SingleAmbiguousPhrase),
            SingleFallback = Pick(overrides.SingleFallback, BuiltIn.SingleFallback),
            SingleFallbackPhrase = Pick(overrides.SingleFallbackPhrase, BuiltIn.SingleFallbackPhrase),
            CompareReturnWeak = Pick(overrides.CompareReturnWeak, BuiltIn.CompareReturnWeak),
            CompareReturnWeakPhrase = Pick(overrides.CompareReturnWeakPhrase, BuiltIn.CompareReturnWeakPhrase),
            CompareReturnLeanFocused = Pick(overrides.CompareReturnLeanFocused, BuiltIn.CompareReturnLeanFocused),
            CompareReturnLeanFocusedPhrase = Pick(overrides.CompareReturnLeanFocusedPhrase, BuiltIn.CompareReturnLeanFocusedPhrase),
            CompareReturnAmbiguous = Pick(overrides.CompareReturnAmbiguous, BuiltIn.CompareReturnAmbiguous),
            CompareReturnAmbiguousPhrase = Pick(overrides.CompareReturnAmbiguousPhrase, BuiltIn.CompareReturnAmbiguousPhrase),
            CompareReturnFallback = Pick(overrides.CompareReturnFallback, BuiltIn.CompareReturnFallback),
            CompareReturnFallbackPhrase = Pick(overrides.CompareReturnFallbackPhrase, BuiltIn.CompareReturnFallbackPhrase),
            CompareWeak = Pick(overrides.CompareWeak, BuiltIn.CompareWeak),
            CompareWeakPhrase = Pick(overrides.CompareWeakPhrase, BuiltIn.CompareWeakPhrase),
            CompareRepeatedPair = Pick(overrides.CompareRepeatedPair, BuiltIn.CompareRepeatedPair),
            CompareRepeatedPairPhrase = Pick(overrides.CompareRepeatedPairPhrase, BuiltIn.CompareRepeatedPairPhrase),
            CompareLean = Pick(overrides.CompareLean, BuiltIn.CompareLean),
            CompareLeanPhrase = Pick(overrides.CompareLeanPhrase, BuiltIn.CompareLeanPhrase),
            CompareFallback = Pick(overrides.CompareFallback, BuiltIn.CompareFallback),
            CompareFallbackPhrase = Pick(overrides.CompareFallbackPhrase, BuiltIn.CompareFallbackPhrase)
        };
    }

    private static class BuiltIn
    {
        public const string SingleWeak = "You're still exploring. Take your time before deciding.";
        public const string SingleWeakPhrase = "You're still exploring.";

        public const string SingleRevisitAndSelectionLead = "You keep coming back to this option—it may be your best fit.";
        public const string SingleRevisitAndSelectionLeadPhrase = "You keep coming back to this option.";

        public const string SingleSelectionLead = "You've shown strong interest in this option. It could be your leading choice.";
        public const string SingleSelectionLeadPhrase = "You've shown strong interest in this option.";

        public const string SingleDwellLead = "You've spent more time on this option. A closer look may help you decide.";
        public const string SingleDwellLeadPhrase = "You've spent more time on this option.";

        public const string SingleRepeatedFocus = "You've returned to this option several times. Compare it with one clear alternative.";
        public const string SingleRepeatedFocusPhrase = "You've returned to this option several times.";

        public const string SingleAmbiguous = "You're still weighing a few directions. Focus on one tradeoff that matters most.";
        public const string SingleAmbiguousPhrase = "You're still weighing a few directions.";

        public const string SingleFallback = "Nothing is clear yet. Keep it simple and decide one next step.";
        public const string SingleFallbackPhrase = "Nothing is clear yet.";

        public const string CompareReturnWeak = "You just compared two options. Take a moment with this one before you decide.";
        public const string CompareReturnWeakPhrase = "You just compared two options.";

        public const string CompareReturnLeanFocused = "After comparing, you came back to this option—it may be the stronger fit.";
        public const string CompareReturnLeanFocusedPhrase = "After comparing, you came back to this option.";

        public const string CompareReturnAmbiguous = "You compared these options and came back here. Pick the tradeoff that matters most to you.";
        public const string CompareReturnAmbiguousPhrase = "You compared these options and came back here.";

        public const string CompareReturnFallback = "You're back on this option after comparing. See if it still feels like the right choice.";
        public const string CompareReturnFallbackPhrase = "You're back on this option after comparing.";

        public const string CompareWeak = "Use this comparison to spot the one difference that matters most.";
        public const string CompareWeakPhrase = "You just started comparing.";

        public const string CompareRepeatedPair = "You've compared these options more than once. Choose based on the tradeoff that matters most.";
        public const string CompareRepeatedPairPhrase = "You've compared these options more than once.";

        public const string CompareLean = "Your comparison points to a lean. The stronger option may be clearer now.";
        public const string CompareLeanPhrase = "Your comparison is pointing one way.";

        public const string CompareFallback = "These options are still close. Decide using one priority that matters to you.";
        public const string CompareFallbackPhrase = "These options are still close.";
    }

    public void ValidateOrThrow()
    {
        ValidateField(nameof(SingleWeak), SingleWeak);
        ValidateField(nameof(SingleWeakPhrase), SingleWeakPhrase);
        ValidateField(nameof(SingleRevisitAndSelectionLead), SingleRevisitAndSelectionLead);
        ValidateField(nameof(SingleRevisitAndSelectionLeadPhrase), SingleRevisitAndSelectionLeadPhrase);
        ValidateField(nameof(SingleSelectionLead), SingleSelectionLead);
        ValidateField(nameof(SingleSelectionLeadPhrase), SingleSelectionLeadPhrase);
        ValidateField(nameof(SingleDwellLead), SingleDwellLead);
        ValidateField(nameof(SingleDwellLeadPhrase), SingleDwellLeadPhrase);
        ValidateField(nameof(SingleRepeatedFocus), SingleRepeatedFocus);
        ValidateField(nameof(SingleRepeatedFocusPhrase), SingleRepeatedFocusPhrase);
        ValidateField(nameof(SingleAmbiguous), SingleAmbiguous);
        ValidateField(nameof(SingleAmbiguousPhrase), SingleAmbiguousPhrase);
        ValidateField(nameof(SingleFallback), SingleFallback);
        ValidateField(nameof(SingleFallbackPhrase), SingleFallbackPhrase);
        ValidateField(nameof(CompareReturnWeak), CompareReturnWeak);
        ValidateField(nameof(CompareReturnWeakPhrase), CompareReturnWeakPhrase);
        ValidateField(nameof(CompareReturnLeanFocused), CompareReturnLeanFocused);
        ValidateField(nameof(CompareReturnLeanFocusedPhrase), CompareReturnLeanFocusedPhrase);
        ValidateField(nameof(CompareReturnAmbiguous), CompareReturnAmbiguous);
        ValidateField(nameof(CompareReturnAmbiguousPhrase), CompareReturnAmbiguousPhrase);
        ValidateField(nameof(CompareReturnFallback), CompareReturnFallback);
        ValidateField(nameof(CompareReturnFallbackPhrase), CompareReturnFallbackPhrase);
        ValidateField(nameof(CompareWeak), CompareWeak);
        ValidateField(nameof(CompareWeakPhrase), CompareWeakPhrase);
        ValidateField(nameof(CompareRepeatedPair), CompareRepeatedPair);
        ValidateField(nameof(CompareRepeatedPairPhrase), CompareRepeatedPairPhrase);
        ValidateField(nameof(CompareLean), CompareLean);
        ValidateField(nameof(CompareLeanPhrase), CompareLeanPhrase);
        ValidateField(nameof(CompareFallback), CompareFallback);
        ValidateField(nameof(CompareFallbackPhrase), CompareFallbackPhrase);
    }

    private static string Pick(string? custom, string fallback) =>
        string.IsNullOrWhiteSpace(custom) ? fallback : custom.Trim();

    private static void ValidateField(string name, string value)
    {
        if (!string.IsNullOrEmpty(value) && string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} cannot be blank when provided.", name);
    }
}
