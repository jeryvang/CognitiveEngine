using System;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Deterministic P7 rationale extraction helper for concise, user-facing "why this matters now" context.
/// Copy is supplied via <see cref="DecisionBehaviorRationaleTemplates"/> (defaults or host overrides).
/// </summary>
public static class DecisionBehaviorRationaleBuilder
{
    public static string BuildSingleWhyThisMattersNow(
        DecisionBehaviorSessionContext session,
        DecisionPreferenceResult preference,
        string productId,
        DecisionBehaviorRationaleTemplates? templates = null,
        int minDwellCountForRationale = DecisionGuidanceConfig.DefaultMinDwellCountForRationale)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (preference == null) throw new ArgumentNullException(nameof(preference));
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("productId is required.", nameof(productId));
        if (minDwellCountForRationale < 1)
            throw new ArgumentOutOfRangeException(
                nameof(minDwellCountForRationale),
                minDwellCountForRationale,
                $"{nameof(minDwellCountForRationale)} must be >= 1.");

        var t = DecisionBehaviorRationaleTemplates.ResolveEffective(templates);

        if (session.IsWeakBehaviorSignal())
            return t.SingleWeak;

        if (preference.Kind == PreferenceResultKind.LeanSingleProduct &&
            string.Equals(preference.PreferredProductId, productId, StringComparison.Ordinal))
        {
            if (session.GetSelectionCount(productId) > 0 && session.GetRevisitCount(productId) > 0)
                return t.SingleRevisitAndSelectionLead;
            if (session.GetSelectionCount(productId) > 0)
                return t.SingleSelectionLead;
            if (session.GetDwellCount(productId) >= minDwellCountForRationale)
                return t.SingleDwellLead;
            if (session.IsRepeatedFocus(productId))
                return t.SingleRepeatedFocus;
        }

        if (preference.IsAmbiguous)
            return t.SingleAmbiguous;

        return t.SingleFallback;
    }

    public static string BuildCompareReturnWhyThisMattersNow(
        DecisionBehaviorSessionContext session,
        DecisionPreferenceResult preference,
        string focusedProductId,
        string comparisonPartnerProductId,
        DecisionBehaviorRationaleTemplates? templates = null,
        int minRevisitCountForLean = DecisionGuidanceConfig.DefaultMinRevisitCountForCompareReturnLean)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (preference == null) throw new ArgumentNullException(nameof(preference));
        if (string.IsNullOrWhiteSpace(focusedProductId))
            throw new ArgumentException("focusedProductId is required.", nameof(focusedProductId));
        if (string.IsNullOrWhiteSpace(comparisonPartnerProductId))
            throw new ArgumentException("comparisonPartnerProductId is required.", nameof(comparisonPartnerProductId));
        if (minRevisitCountForLean < 1)
            throw new ArgumentOutOfRangeException(
                nameof(minRevisitCountForLean),
                minRevisitCountForLean,
                $"{nameof(minRevisitCountForLean)} must be >= 1.");

        var t = DecisionBehaviorRationaleTemplates.ResolveEffective(templates);

        if (session.IsWeakBehaviorSignal())
            return t.CompareReturnWeak;

        if (preference.Kind == PreferenceResultKind.LeanSingleProduct &&
            string.Equals(preference.PreferredProductId, focusedProductId, StringComparison.Ordinal) &&
            session.GetRevisitCount(focusedProductId) >= minRevisitCountForLean)
            return t.CompareReturnLeanFocused;

        if (preference.IsAmbiguous)
            return t.CompareReturnAmbiguous;

        return t.CompareReturnFallback;
    }

    public static string BuildComparisonWhyThisMattersNow(
        DecisionBehaviorSessionContext session,
        DecisionPreferenceResult preference,
        string productIdA,
        string productIdB,
        DecisionBehaviorRationaleTemplates? templates = null)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (preference == null) throw new ArgumentNullException(nameof(preference));
        if (string.IsNullOrWhiteSpace(productIdA))
            throw new ArgumentException("productIdA is required.", nameof(productIdA));
        if (string.IsNullOrWhiteSpace(productIdB))
            throw new ArgumentException("productIdB is required.", nameof(productIdB));
        if (string.Equals(productIdA, productIdB, StringComparison.Ordinal))
            throw new ArgumentException("comparison product ids must be distinct.");

        var t = DecisionBehaviorRationaleTemplates.ResolveEffective(templates);

        if (session.IsWeakBehaviorSignal())
            return t.CompareWeak;

        var pairCount = session.GetComparePairCount(productIdA, productIdB);
        if (pairCount > 1)
            return t.CompareRepeatedPair;

        if (preference.Kind == PreferenceResultKind.LeanComparison && !preference.IsAmbiguous)
            return t.CompareLean;

        return t.CompareFallback;
    }
}
