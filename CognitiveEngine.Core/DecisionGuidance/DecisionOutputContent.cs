namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Host- or catalog-supplied copy for P6 structured outputs. The <see cref="DecisionOutputBuilder"/>
/// maps fields by resolved trigger: single-product triggers use the first two strings; Compare uses
/// the comparison strings. Null or whitespace-only values become empty strings on the DTOs.
/// </summary>
public sealed class DecisionOutputContent
{
    public string? WhatThisGivesYou { get; set; }

    public string? WhatYouTradeOff { get; set; }

    public string? KeyDifference { get; set; }

    public string? WhichToChooseIf { get; set; }
}
