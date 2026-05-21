using System;
using Newtonsoft.Json;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Host-configurable copy for P7 <c>why_this_matters_now</c> rationale branches.
/// Omitted JSON fields fall back to built-in defaults; logic keys are stable across versions.
/// </summary>
public sealed class DecisionBehaviorRationaleTemplates
{
    [JsonProperty("single_weak")]
    public string SingleWeak { get; set; } = "";

    [JsonProperty("single_revisit_and_selection_lead")]
    public string SingleRevisitAndSelectionLead { get; set; } = "";

    [JsonProperty("single_selection_lead")]
    public string SingleSelectionLead { get; set; } = "";

    [JsonProperty("single_dwell_lead")]
    public string SingleDwellLead { get; set; } = "";

    [JsonProperty("single_repeated_focus")]
    public string SingleRepeatedFocus { get; set; } = "";

    [JsonProperty("single_ambiguous")]
    public string SingleAmbiguous { get; set; } = "";

    [JsonProperty("single_fallback")]
    public string SingleFallback { get; set; } = "";

    [JsonProperty("compare_return_weak")]
    public string CompareReturnWeak { get; set; } = "";

    [JsonProperty("compare_return_lean_focused")]
    public string CompareReturnLeanFocused { get; set; } = "";

    [JsonProperty("compare_return_ambiguous")]
    public string CompareReturnAmbiguous { get; set; } = "";

    [JsonProperty("compare_return_fallback")]
    public string CompareReturnFallback { get; set; } = "";

    [JsonProperty("compare_weak")]
    public string CompareWeak { get; set; } = "";

    [JsonProperty("compare_repeated_pair")]
    public string CompareRepeatedPair { get; set; } = "";

    [JsonProperty("compare_lean")]
    public string CompareLean { get; set; } = "";

    [JsonProperty("compare_fallback")]
    public string CompareFallback { get; set; } = "";

    public static DecisionBehaviorRationaleTemplates CreateDefault() =>
        new()
        {
            SingleWeak = BuiltIn.SingleWeak,
            SingleRevisitAndSelectionLead = BuiltIn.SingleRevisitAndSelectionLead,
            SingleSelectionLead = BuiltIn.SingleSelectionLead,
            SingleDwellLead = BuiltIn.SingleDwellLead,
            SingleRepeatedFocus = BuiltIn.SingleRepeatedFocus,
            SingleAmbiguous = BuiltIn.SingleAmbiguous,
            SingleFallback = BuiltIn.SingleFallback,
            CompareReturnWeak = BuiltIn.CompareReturnWeak,
            CompareReturnLeanFocused = BuiltIn.CompareReturnLeanFocused,
            CompareReturnAmbiguous = BuiltIn.CompareReturnAmbiguous,
            CompareReturnFallback = BuiltIn.CompareReturnFallback,
            CompareWeak = BuiltIn.CompareWeak,
            CompareRepeatedPair = BuiltIn.CompareRepeatedPair,
            CompareLean = BuiltIn.CompareLean,
            CompareFallback = BuiltIn.CompareFallback
        };

    /// <summary>Merges optional host overrides onto built-in defaults.</summary>
    public static DecisionBehaviorRationaleTemplates ResolveEffective(DecisionBehaviorRationaleTemplates? overrides)
    {
        if (overrides == null)
            return CreateDefault();

        return new DecisionBehaviorRationaleTemplates
        {
            SingleWeak = Pick(overrides.SingleWeak, BuiltIn.SingleWeak),
            SingleRevisitAndSelectionLead = Pick(overrides.SingleRevisitAndSelectionLead, BuiltIn.SingleRevisitAndSelectionLead),
            SingleSelectionLead = Pick(overrides.SingleSelectionLead, BuiltIn.SingleSelectionLead),
            SingleDwellLead = Pick(overrides.SingleDwellLead, BuiltIn.SingleDwellLead),
            SingleRepeatedFocus = Pick(overrides.SingleRepeatedFocus, BuiltIn.SingleRepeatedFocus),
            SingleAmbiguous = Pick(overrides.SingleAmbiguous, BuiltIn.SingleAmbiguous),
            SingleFallback = Pick(overrides.SingleFallback, BuiltIn.SingleFallback),
            CompareReturnWeak = Pick(overrides.CompareReturnWeak, BuiltIn.CompareReturnWeak),
            CompareReturnLeanFocused = Pick(overrides.CompareReturnLeanFocused, BuiltIn.CompareReturnLeanFocused),
            CompareReturnAmbiguous = Pick(overrides.CompareReturnAmbiguous, BuiltIn.CompareReturnAmbiguous),
            CompareReturnFallback = Pick(overrides.CompareReturnFallback, BuiltIn.CompareReturnFallback),
            CompareWeak = Pick(overrides.CompareWeak, BuiltIn.CompareWeak),
            CompareRepeatedPair = Pick(overrides.CompareRepeatedPair, BuiltIn.CompareRepeatedPair),
            CompareLean = Pick(overrides.CompareLean, BuiltIn.CompareLean),
            CompareFallback = Pick(overrides.CompareFallback, BuiltIn.CompareFallback)
        };
    }

    private static class BuiltIn
    {
        public const string SingleWeak = "You're still exploring. Take your time before deciding.";
        public const string SingleRevisitAndSelectionLead = "You keep coming back to this option—it may be your best fit.";
        public const string SingleSelectionLead = "You've shown strong interest in this option. It could be your leading choice.";
        public const string SingleDwellLead = "You've spent more time on this option. A closer look may help you decide.";
        public const string SingleRepeatedFocus = "You've returned to this option several times. Compare it with one clear alternative.";
        public const string SingleAmbiguous = "You're still weighing a few directions. Focus on one tradeoff that matters most.";
        public const string SingleFallback = "Nothing is clear yet. Keep it simple and decide one next step.";
        public const string CompareReturnWeak = "You just compared two options. Take a moment with this one before you decide.";
        public const string CompareReturnLeanFocused = "After comparing, you came back to this option—it may be the stronger fit.";
        public const string CompareReturnAmbiguous = "You compared these options and came back here. Pick the tradeoff that matters most to you.";
        public const string CompareReturnFallback = "You're back on this option after comparing. See if it still feels like the right choice.";
        public const string CompareWeak = "Use this comparison to spot the one difference that matters most.";
        public const string CompareRepeatedPair = "You've compared these options more than once. Choose based on the tradeoff that matters most.";
        public const string CompareLean = "Your comparison points to a lean. The stronger option may be clearer now.";
        public const string CompareFallback = "These options are still close. Decide using one priority that matters to you.";
    }

    public void ValidateOrThrow()
    {
        ValidateField(nameof(SingleWeak), SingleWeak);
        ValidateField(nameof(SingleRevisitAndSelectionLead), SingleRevisitAndSelectionLead);
        ValidateField(nameof(SingleSelectionLead), SingleSelectionLead);
        ValidateField(nameof(SingleDwellLead), SingleDwellLead);
        ValidateField(nameof(SingleRepeatedFocus), SingleRepeatedFocus);
        ValidateField(nameof(SingleAmbiguous), SingleAmbiguous);
        ValidateField(nameof(SingleFallback), SingleFallback);
        ValidateField(nameof(CompareReturnWeak), CompareReturnWeak);
        ValidateField(nameof(CompareReturnLeanFocused), CompareReturnLeanFocused);
        ValidateField(nameof(CompareReturnAmbiguous), CompareReturnAmbiguous);
        ValidateField(nameof(CompareReturnFallback), CompareReturnFallback);
        ValidateField(nameof(CompareWeak), CompareWeak);
        ValidateField(nameof(CompareRepeatedPair), CompareRepeatedPair);
        ValidateField(nameof(CompareLean), CompareLean);
        ValidateField(nameof(CompareFallback), CompareFallback);
    }

    private static string Pick(string? custom, string fallback) =>
        string.IsNullOrWhiteSpace(custom) ? fallback : custom.Trim();

    private static void ValidateField(string name, string value)
    {
        if (!string.IsNullOrEmpty(value) && string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} cannot be blank when provided.", name);
    }
}
