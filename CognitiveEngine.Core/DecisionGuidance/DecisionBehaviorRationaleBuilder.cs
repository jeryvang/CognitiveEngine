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
            return "Signals are still light. Keep exploring before committing.";

        if (preference.Kind == PreferenceResultKind.LeanSingleProduct &&
            string.Equals(preference.PreferredProductId, productId, StringComparison.Ordinal))
        {
            if (session.GetSelectionCount(productId) > 0 && session.GetRevisitCount(productId) > 0)
                return $"You keep returning to {productId}. Prioritize it if this pattern continues.";
            if (session.GetSelectionCount(productId) > 0)
                return $"You selected {productId} repeatedly. Use it as the current lead option.";
            if (session.GetDwellCount(productId) > 0)
                return $"You spent more time on {productId}. Validate this option first.";
            if (session.IsRepeatedFocus(productId))
                return $"You revisited {productId} several times. Compare it against one clear alternative.";
        }

        if (preference.IsAmbiguous)
            return "Your behavior is split. Decide based on one main tradeoff.";

        return "Current behavior is still mixed. Keep the next step simple and measurable.";
    }

    public static string BuildCompareReturnWhyThisMattersNow(
        DecisionBehaviorSessionContext session,
        DecisionPreferenceResult preference,
        string focusedProductId,
        string comparisonPartnerProductId)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (preference == null) throw new ArgumentNullException(nameof(preference));
        if (string.IsNullOrWhiteSpace(focusedProductId))
            throw new ArgumentException("focusedProductId is required.", nameof(focusedProductId));
        if (string.IsNullOrWhiteSpace(comparisonPartnerProductId))
            throw new ArgumentException("comparisonPartnerProductId is required.", nameof(comparisonPartnerProductId));

        if (session.IsWeakBehaviorSignal())
            return "You just compared options. Take a moment to validate this one before deciding.";

        if (preference.Kind == PreferenceResultKind.LeanSingleProduct &&
            string.Equals(preference.PreferredProductId, focusedProductId, StringComparison.Ordinal))
            return $"You returned to {focusedProductId} after comparing with {comparisonPartnerProductId}. This looks like your stronger fit.";

        if (preference.IsAmbiguous)
            return $"You returned to {focusedProductId} after comparing with {comparisonPartnerProductId}. Decide using one main tradeoff.";

        return $"You exited compare and focused {focusedProductId}. Confirm this choice against {comparisonPartnerProductId}.";
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
            return "Signals are still weak. Use this compare to isolate one deciding factor.";

        var pairCount = session.GetComparePairCount(productIdA, productIdB);
        if (pairCount > 1)
            return $"You compared {productIdA} and {productIdB} multiple times. Choose based on the strongest tradeoff.";

        if (preference.Kind == PreferenceResultKind.LeanComparison && !preference.IsAmbiguous)
            return $"Your compare behavior now leans one way. Move forward with the stronger option.";

        return "These options are still close. Decide with one concrete priority.";
    }
}
