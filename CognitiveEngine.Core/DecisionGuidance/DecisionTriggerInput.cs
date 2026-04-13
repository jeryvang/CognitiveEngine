namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// One host-supplied event for deterministic trigger resolution.
/// </summary>
public readonly struct DecisionTriggerInput
{
    public DecisionTriggerInput(DecisionTriggerInputKind kind, string? productId = null)
    {
        Kind = kind;
        ProductId = productId;
    }

    public DecisionTriggerInputKind Kind { get; }

    public string? ProductId { get; }

    public static DecisionTriggerInput FocusChanged(string productId) =>
        new(DecisionTriggerInputKind.FocusChanged, productId);

    public static DecisionTriggerInput CompareInvoked() =>
        new(DecisionTriggerInputKind.CompareInvoked);

    public static DecisionTriggerInput DwellThresholdMet(string productId) =>
        new(DecisionTriggerInputKind.DwellThresholdMet, productId);
}

public enum DecisionTriggerInputKind
{
    FocusChanged,
    CompareInvoked,
    DwellThresholdMet
}
