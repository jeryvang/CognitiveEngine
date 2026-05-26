using System;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Optional bundle of host signals for one deterministic resolution step. Compare is evaluated with
/// priority over revisit and dwell when multiple flags are set.
/// </summary>
public readonly struct DecisionTriggerFrame
{
    public DecisionTriggerFrame(
        string? focusProductIfChanged,
        bool compareInvoked,
        string? dwellProductIfThreshold,
        string? hesitationCandidateProductId = null)
    {
        FocusProductIfChanged = focusProductIfChanged;
        CompareInvoked = compareInvoked;
        DwellProductIfThreshold = dwellProductIfThreshold;
        HesitationCandidateProductId = hesitationCandidateProductId;
    }

    public string? FocusProductIfChanged { get; }

    public bool CompareInvoked { get; }

    public string? DwellProductIfThreshold { get; }

    /// <summary>
    /// Optional: a product the host (or coordinator) determined is in a hesitation pattern.
    /// Set by the runtime after session-level evaluation; resolver emits
    /// <see cref="DecisionTriggerKind.Hesitation"/> when set and not duplicated.
    /// </summary>
    public string? HesitationCandidateProductId { get; }

    public DecisionTriggerFrame WithHesitationCandidate(string? productId) =>
        new(FocusProductIfChanged, CompareInvoked, DwellProductIfThreshold, productId);

    public static DecisionTriggerFrame FromInput(in DecisionTriggerInput input) =>
        input.Kind switch
        {
            DecisionTriggerInputKind.FocusChanged => new DecisionTriggerFrame(input.ProductId, false, null),
            DecisionTriggerInputKind.CompareInvoked => new DecisionTriggerFrame(null, true, null),
            DecisionTriggerInputKind.DwellThresholdMet => new DecisionTriggerFrame(null, false, input.ProductId),
            _ => throw new ArgumentOutOfRangeException(nameof(input), input.Kind, null)
        };
}
