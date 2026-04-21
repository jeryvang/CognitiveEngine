using System;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Deterministic P7 rationale extraction helper for concise "why this matters now" context.
/// </summary>
public static class DecisionBehaviorRationaleBuilder
{
    public static string BuildSingleWhyThisMattersNow(
        DecisionBehaviorSessionContext session,
        DecisionPreferenceResult preference,
        string productId)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (preference == null) throw new ArgumentNullException(nameof(preference));
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("productId is required.", nameof(productId));

        if (session.IsWeakBehaviorSignal())
            return "Signal is still limited, so this keeps the guidance simple for now.";

        if (preference.Kind == PreferenceResultKind.LeanSingleProduct &&
            string.Equals(preference.PreferredProductId, productId, StringComparison.Ordinal))
        {
            if (session.GetSelectionCount(productId) > 0 && session.GetRevisitCount(productId) > 0)
                return "You keep returning to this option and selecting it, which signals a practical fit.";
            if (session.GetSelectionCount(productId) > 0)
                return "Your recent selections suggest this option matches what matters most right now.";
            if (session.GetDwellCount(productId) > 0)
                return "You spent more attention here, so this guidance focuses on your active consideration.";
            if (session.IsRepeatedFocus(productId))
                return "Your repeated focus on this option suggests it is becoming the leading choice.";
        }

        if (preference.IsAmbiguous)
            return "Your behavior is mixed so far, so this guidance helps clarify the main tradeoff.";

        return "This guidance reflects your recent interaction pattern in the current session.";
    }

    public static string BuildComparisonWhyThisMattersNow(
        DecisionBehaviorSessionContext session,
        DecisionPreferenceResult preference,
        string productIdA,
        string productIdB)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (preference == null) throw new ArgumentNullException(nameof(preference));
        if (string.IsNullOrWhiteSpace(productIdA))
            throw new ArgumentException("productIdA is required.", nameof(productIdA));
        if (string.IsNullOrWhiteSpace(productIdB))
            throw new ArgumentException("productIdB is required.", nameof(productIdB));
        if (string.Equals(productIdA, productIdB, StringComparison.Ordinal))
            throw new ArgumentException("comparison product ids must be distinct.");

        if (session.IsWeakBehaviorSignal())
            return "There is not enough signal yet, so this comparison highlights the key difference only.";

        var pairCount = session.GetComparePairCount(productIdA, productIdB);
        if (pairCount > 1)
            return "You revisited this comparison multiple times, so resolving the core tradeoff matters now.";

        if (preference.Kind == PreferenceResultKind.LeanComparison && !preference.IsAmbiguous)
            return "Your current compare and focus behavior shows a clearer direction between these options.";

        return "Current interaction signals are close, so this comparison keeps attention on the deciding difference.";
    }
}
