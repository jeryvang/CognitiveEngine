using System;
using System.Linq;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Deterministic P7 rule-based preference evaluation over normalized session behavior evidence.
/// </summary>
public static class DecisionPreferenceModel
{
    private const double MinSingleLeanScore = 0.35;
    private const double MinCompareWinnerScore = 0.30;
    private const double MinCompareDelta = 0.12;

    public static DecisionPreferenceResult EvaluateSingle(DecisionBehaviorSessionContext session, string productId)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("productId is required.", nameof(productId));

        var score = ComputeProductScore(session, productId);
        if (score < MinSingleLeanScore)
            return DecisionPreferenceResult.NoClearLean(score, "single_rule_v1:insufficient_single_score");

        return DecisionPreferenceResult.LeanSingle(productId, score, "single_rule_v1:weighted(selection,dwell,revisit,focus)");
    }

    public static DecisionPreferenceResult EvaluateComparison(
        DecisionBehaviorSessionContext session,
        string productIdA,
        string productIdB)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (string.IsNullOrWhiteSpace(productIdA))
            throw new ArgumentException("productIdA is required.", nameof(productIdA));
        if (string.IsNullOrWhiteSpace(productIdB))
            throw new ArgumentException("productIdB is required.", nameof(productIdB));
        if (string.Equals(productIdA, productIdB, StringComparison.Ordinal))
            throw new ArgumentException("product ids must be distinct.");

        var scoreA = ComputeProductScore(session, productIdA);
        var scoreB = ComputeProductScore(session, productIdB);
        var winner = Math.Max(scoreA, scoreB);
        var delta = Math.Abs(scoreA - scoreB);

        var pairStrength = session.GetCompareEvidenceNormalizedStrength(productIdA, productIdB);
        var confidence = Clamp01(0.65 * delta + 0.35 * pairStrength);

        if (winner < MinCompareWinnerScore || delta < MinCompareDelta)
            return DecisionPreferenceResult.NoClearLean(confidence, "compare_rule_v1:insufficient_separation");

        if (scoreA >= scoreB)
            return DecisionPreferenceResult.LeanComparison(
                PreferenceLean.ProductA,
                productIdA,
                productIdB,
                confidence,
                "compare_rule_v1:weighted_delta_with_pair_strength");

        return DecisionPreferenceResult.LeanComparison(
            PreferenceLean.ProductB,
            productIdB,
            productIdA,
            confidence,
            "compare_rule_v1:weighted_delta_with_pair_strength");
    }

    private static double ComputeProductScore(DecisionBehaviorSessionContext session, string productId)
    {
        var selection = session.GetSelectionEvidenceNormalizedStrength(productId);
        var dwell = session.GetDwellEvidenceNormalizedStrength(productId);
        var revisit = session.GetRevisitEvidenceNormalizedStrength(productId);

        var focusCounts = session.GetFocusCountsByProduct();
        var maxFocus = focusCounts.Count == 0 ? 0 : focusCounts.Values.Max();
        var focus = maxFocus <= 0 ? 0.0 : (double)session.GetFocusCount(productId) / maxFocus;

        return Clamp01(
            0.40 * selection +
            0.30 * dwell +
            0.20 * revisit +
            0.10 * focus);
    }

    private static double Clamp01(double v)
    {
        if (v < 0.0) return 0.0;
        if (v > 1.0) return 1.0;
        return v;
    }
}
