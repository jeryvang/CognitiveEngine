using System;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Builds P6 primary-path structured outputs from a resolved trigger and optional copy.
/// Does not depend on panel, whisper, or tap-to-expand; the returned DTOs are ready for direct UI binding.
/// </summary>
public static class DecisionOutputBuilder
{
    /// <summary>
    /// Produces a <see cref="SingleProductDecisionOutput"/> for <see cref="DecisionTriggerKind.Dwell"/>,
    /// <see cref="DecisionTriggerKind.Revisit"/>, <see cref="DecisionTriggerKind.Hesitation"/>, and
    /// <see cref="DecisionTriggerKind.CompareReturn"/>, or a <see cref="ComparisonDecisionOutput"/> for
    /// <see cref="DecisionTriggerKind.Compare"/>.
    /// </summary>
    public static DecisionOutputBuildResult Build(ResolvedDecisionTrigger trigger, DecisionOutputContent? content = null)
    {
        content ??= new DecisionOutputContent();

        return trigger.Kind switch
        {
            DecisionTriggerKind.Compare => DecisionOutputBuildResult.FromComparison(BuildComparison(in trigger, content)),
            DecisionTriggerKind.Revisit
                or DecisionTriggerKind.Dwell
                or DecisionTriggerKind.Hesitation
                or DecisionTriggerKind.CompareReturn =>
                DecisionOutputBuildResult.FromSingle(BuildSingle(in trigger, content)),
            _ => throw new InvalidOperationException("Unexpected DecisionTriggerKind.")
        };
    }

    private static SingleProductDecisionOutput BuildSingle(in ResolvedDecisionTrigger trigger, DecisionOutputContent content)
    {
        return new SingleProductDecisionOutput
        {
            ProductId = trigger.ProductIdLow,
            WhatThisGivesYou = Normalize(content.WhatThisGivesYou),
            WhatYouTradeOff = Normalize(content.WhatYouTradeOff)
        };
    }

    private static ComparisonDecisionOutput BuildComparison(in ResolvedDecisionTrigger trigger, DecisionOutputContent content)
    {
        return new ComparisonDecisionOutput
        {
            ProductIdA = trigger.ProductIdLow,
            ProductIdB = trigger.ProductIdHigh,
            KeyDifference = Normalize(content.KeyDifference),
            WhichToChooseIf = Normalize(content.WhichToChooseIf)
        };
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
}
