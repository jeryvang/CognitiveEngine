using System;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Deterministic P7 rationale resolver. Picks a branch from session behavior + preference, then
/// returns the stable key, short behavior phrase, and full sentence variants.
/// Copy is supplied via <see cref="DecisionBehaviorRationaleTemplates"/> (defaults or host overrides).
/// </summary>
public static class DecisionBehaviorRationaleBuilder
{
    public static DecisionBehaviorRationaleSelection BuildSingleRationale(
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
            return new DecisionBehaviorRationaleSelection(
                DecisionBehaviorRationaleKeys.SingleWeak, t.SingleWeakPhrase, t.SingleWeak);

        if (preference.Kind == PreferenceResultKind.LeanSingleProduct &&
            string.Equals(preference.PreferredProductId, productId, StringComparison.Ordinal))
        {
            if (session.GetSelectionCount(productId) > 0 && session.GetRevisitCount(productId) > 0)
                return new DecisionBehaviorRationaleSelection(
                    DecisionBehaviorRationaleKeys.SingleRevisitAndSelectionLead,
                    t.SingleRevisitAndSelectionLeadPhrase,
                    t.SingleRevisitAndSelectionLead);
            if (session.GetSelectionCount(productId) > 0)
                return new DecisionBehaviorRationaleSelection(
                    DecisionBehaviorRationaleKeys.SingleSelectionLead,
                    t.SingleSelectionLeadPhrase,
                    t.SingleSelectionLead);
            if (session.GetDwellCount(productId) >= minDwellCountForRationale)
                return new DecisionBehaviorRationaleSelection(
                    DecisionBehaviorRationaleKeys.SingleDwellLead,
                    t.SingleDwellLeadPhrase,
                    t.SingleDwellLead);
            if (session.IsRepeatedFocus(productId))
                return new DecisionBehaviorRationaleSelection(
                    DecisionBehaviorRationaleKeys.SingleRepeatedFocus,
                    t.SingleRepeatedFocusPhrase,
                    t.SingleRepeatedFocus);
        }

        if (preference.IsAmbiguous)
            return new DecisionBehaviorRationaleSelection(
                DecisionBehaviorRationaleKeys.SingleAmbiguous, t.SingleAmbiguousPhrase, t.SingleAmbiguous);

        return new DecisionBehaviorRationaleSelection(
            DecisionBehaviorRationaleKeys.SingleFallback, t.SingleFallbackPhrase, t.SingleFallback);
    }

    public static DecisionBehaviorRationaleSelection BuildCompareReturnRationale(
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
            return new DecisionBehaviorRationaleSelection(
                DecisionBehaviorRationaleKeys.CompareReturnWeak, t.CompareReturnWeakPhrase, t.CompareReturnWeak);

        if (preference.Kind == PreferenceResultKind.LeanSingleProduct &&
            string.Equals(preference.PreferredProductId, focusedProductId, StringComparison.Ordinal) &&
            session.GetRevisitCount(focusedProductId) >= minRevisitCountForLean)
            return new DecisionBehaviorRationaleSelection(
                DecisionBehaviorRationaleKeys.CompareReturnLeanFocused,
                t.CompareReturnLeanFocusedPhrase,
                t.CompareReturnLeanFocused);

        if (preference.IsAmbiguous)
            return new DecisionBehaviorRationaleSelection(
                DecisionBehaviorRationaleKeys.CompareReturnAmbiguous,
                t.CompareReturnAmbiguousPhrase,
                t.CompareReturnAmbiguous);

        return new DecisionBehaviorRationaleSelection(
            DecisionBehaviorRationaleKeys.CompareReturnFallback,
            t.CompareReturnFallbackPhrase,
            t.CompareReturnFallback);
    }

    public static DecisionBehaviorRationaleSelection BuildComparisonRationale(
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
            return new DecisionBehaviorRationaleSelection(
                DecisionBehaviorRationaleKeys.CompareWeak, t.CompareWeakPhrase, t.CompareWeak);

        var pairCount = session.GetComparePairCount(productIdA, productIdB);
        if (pairCount > 1)
            return new DecisionBehaviorRationaleSelection(
                DecisionBehaviorRationaleKeys.CompareRepeatedPair, t.CompareRepeatedPairPhrase, t.CompareRepeatedPair);

        if (preference.Kind == PreferenceResultKind.LeanComparison && !preference.IsAmbiguous)
            return new DecisionBehaviorRationaleSelection(
                DecisionBehaviorRationaleKeys.CompareLean, t.CompareLeanPhrase, t.CompareLean);

        return new DecisionBehaviorRationaleSelection(
            DecisionBehaviorRationaleKeys.CompareFallback, t.CompareFallbackPhrase, t.CompareFallback);
    }

    // ---------------------------------------------------------------------
    // Backward-compatible string accessors. Existing call sites continue to
    // receive the full sentence (why_this_matters_now) variant.
    // ---------------------------------------------------------------------

    public static string BuildSingleWhyThisMattersNow(
        DecisionBehaviorSessionContext session,
        DecisionPreferenceResult preference,
        string productId,
        DecisionBehaviorRationaleTemplates? templates = null,
        int minDwellCountForRationale = DecisionGuidanceConfig.DefaultMinDwellCountForRationale) =>
        BuildSingleRationale(session, preference, productId, templates, minDwellCountForRationale).Sentence;

    public static string BuildCompareReturnWhyThisMattersNow(
        DecisionBehaviorSessionContext session,
        DecisionPreferenceResult preference,
        string focusedProductId,
        string comparisonPartnerProductId,
        DecisionBehaviorRationaleTemplates? templates = null,
        int minRevisitCountForLean = DecisionGuidanceConfig.DefaultMinRevisitCountForCompareReturnLean) =>
        BuildCompareReturnRationale(
            session, preference, focusedProductId, comparisonPartnerProductId, templates, minRevisitCountForLean).Sentence;

    public static string BuildComparisonWhyThisMattersNow(
        DecisionBehaviorSessionContext session,
        DecisionPreferenceResult preference,
        string productIdA,
        string productIdB,
        DecisionBehaviorRationaleTemplates? templates = null) =>
        BuildComparisonRationale(session, preference, productIdA, productIdB, templates).Sentence;
}
