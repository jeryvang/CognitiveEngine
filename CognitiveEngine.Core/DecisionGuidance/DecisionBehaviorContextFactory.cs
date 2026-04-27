using System;
using System.Linq;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Maps deterministic P7 session behavior and preference signals into output behavior_context JSON.
/// </summary>
public static class DecisionBehaviorContextFactory
{
    private const double WeakSignalMaxConfidence = 0.25;
    private const double WeakSignalMaxNormalizedStrength = 0.25;

    public static DecisionBehaviorContext Create(
        DecisionBehaviorSessionContext session,
        in ResolvedDecisionTrigger trigger,
        DecisionGuidanceConfig? config = null)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        var cfg = config ?? DecisionGuidanceConfig.CreateDefault();

        var preference = ResolvePreference(session, in trigger);
        var weakSignal = session.IsWeakBehaviorSignal();
        if (weakSignal)
        {
            preference = DecisionPreferenceResult.NoClearLean(
                Math.Min(preference.Confidence, WeakSignalMaxConfidence),
                "context_v1:weak_signal_fallback");
        }

        var rationale = ResolveRationale(session, preference, in trigger);
        var signalUsage = BuildSignalUsage(session, in trigger);
        if (weakSignal)
            signalUsage.NormalizedSignalStrength = Math.Min(signalUsage.NormalizedSignalStrength, WeakSignalMaxNormalizedStrength);
        var convergence = ComputeConvergence(session);
        var disposition = ResolveGuidanceDisposition(preference, convergence, cfg);

        return new DecisionBehaviorContext
        {
            SessionScope = "session",
            BehaviorSignalUsage = signalUsage,
            PreferenceIndication = new PreferenceIndication
            {
                Leaning = preference.Leaning,
                IsAmbiguous = preference.IsAmbiguous,
                Confidence = Round4(preference.Confidence),
                Basis = preference.Basis
            },
            DecisionConvergence = convergence,
            GuidanceDisposition = disposition,
            GuidanceDispositionBasis = "p7_disposition_v1(confidence,convergence,ambiguity)",
            WhyThisMattersNow = rationale,
            RationalePolicy = "repeat_guard_aligned_v1"
        };
    }

    private static BehaviorSignalUsage BuildSignalUsage(
        DecisionBehaviorSessionContext session,
        in ResolvedDecisionTrigger trigger) =>
        trigger.Kind == DecisionTriggerKind.Compare
            ? BuildCompareSignalUsage(session, in trigger)
            : BuildSingleSignalUsage(session, trigger.ProductIdLow);

    private static BehaviorSignalUsage BuildSingleSignalUsage(
        DecisionBehaviorSessionContext session,
        string productId)
    {
        var selectionStrength = session.GetSelectionEvidenceNormalizedStrength(productId);
        var dwellStrength = session.GetDwellEvidenceNormalizedStrength(productId);
        var revisitStrength = session.GetRevisitEvidenceNormalizedStrength(productId);
        var focusStrength = GetFocusNormalizedStrength(session, productId);
        var normalizedSignalStrength = Clamp01(
            0.28 * selectionStrength +
            0.32 * dwellStrength +
            0.20 * revisitStrength +
            0.20 * focusStrength);
        return BuildSignalUsageOutput(session, normalizedSignalStrength);
    }

    private static BehaviorSignalUsage BuildCompareSignalUsage(
        DecisionBehaviorSessionContext session,
        in ResolvedDecisionTrigger trigger)
    {
        var selectionStrength = AverageOrZero(
            session.GetSelectionEvidenceNormalizedStrength(trigger.ProductIdLow),
            session.GetSelectionEvidenceNormalizedStrength(trigger.ProductIdHigh));
        var dwellStrength = AverageOrZero(
            session.GetDwellEvidenceNormalizedStrength(trigger.ProductIdLow),
            session.GetDwellEvidenceNormalizedStrength(trigger.ProductIdHigh));
        var revisitStrength = AverageOrZero(
            session.GetRevisitEvidenceNormalizedStrength(trigger.ProductIdLow),
            session.GetRevisitEvidenceNormalizedStrength(trigger.ProductIdHigh));
        var compareStrength = session.GetCompareEvidenceNormalizedStrength(trigger.ProductIdLow, trigger.ProductIdHigh);
        var pairRepeatBoost = GetComparePairRepeatBoost(session, trigger.ProductIdLow, trigger.ProductIdHigh);
        var swipePressure = NormalizeSwipePressure(session);
        var normalizedSignalStrength = Clamp01(
            0.22 * selectionStrength +
            0.33 * dwellStrength +
            0.15 * revisitStrength +
            0.25 * compareStrength +
            0.10 * pairRepeatBoost -
            0.05 * swipePressure);
        return BuildSignalUsageOutput(session, normalizedSignalStrength);
    }

    private static BehaviorSignalUsage BuildSignalUsageOutput(
        DecisionBehaviorSessionContext session,
        double normalizedSignalStrength)
    {
        return new BehaviorSignalUsage
        {
            SelectionCount = session.SelectionCount,
            DwellCount = session.DwellCount,
            SwipeCount = session.SwipeCount,
            CompareCount = session.CompareCount,
            RevisitCount = session.RevisitCount,
            NormalizedSignalStrength = Round4(normalizedSignalStrength)
        };
    }

    private static double GetFocusNormalizedStrength(DecisionBehaviorSessionContext session, string productId)
    {
        var focusCounts = session.GetFocusCountsByProduct();
        var maxFocus = focusCounts.Count == 0 ? 0 : focusCounts.Values.Max();
        if (maxFocus <= 0)
            return 0.0;
        return (double)session.GetFocusCount(productId) / maxFocus;
    }

    private static double GetComparePairRepeatBoost(
        DecisionBehaviorSessionContext session,
        string productIdA,
        string productIdB)
    {
        var pairCount = session.GetComparePairCount(productIdA, productIdB);
        if (pairCount <= 1)
            return 0.0;
        return Clamp01((pairCount - 1) / 3.0);
    }

    private static double NormalizeSwipePressure(DecisionBehaviorSessionContext session)
    {
        if (session.FocusSwitchCount <= 0)
            return 0.0;
        return Clamp01((double)session.SwipeCount / session.FocusSwitchCount);
    }

    private static DecisionPreferenceResult ResolvePreference(
        DecisionBehaviorSessionContext session,
        in ResolvedDecisionTrigger trigger) =>
        trigger.Kind == DecisionTriggerKind.Compare
            ? DecisionPreferenceModel.EvaluateComparison(session, trigger.ProductIdLow, trigger.ProductIdHigh)
            : DecisionPreferenceModel.EvaluateSingle(session, trigger.ProductIdLow);

    private static string ResolveRationale(
        DecisionBehaviorSessionContext session,
        DecisionPreferenceResult preference,
        in ResolvedDecisionTrigger trigger) =>
        trigger.Kind == DecisionTriggerKind.Compare
            ? DecisionBehaviorRationaleBuilder.BuildComparisonWhyThisMattersNow(
                session,
                preference,
                trigger.ProductIdLow,
                trigger.ProductIdHigh)
            : DecisionBehaviorRationaleBuilder.BuildSingleWhyThisMattersNow(
                session,
                preference,
                trigger.ProductIdLow);

    private static DecisionConvergenceSignal ComputeConvergence(DecisionBehaviorSessionContext session)
    {
        double selectionSignal = Clamp01(session.SelectionCount / 3.0);
        double compareSignal = Clamp01(session.CompareCount / 3.0);
        double dwellSignal = Clamp01(session.DwellCount / 4.0);
        double revisitPenalty = Clamp01(session.RevisitCount / 3.0);
        double swipePenalty = Clamp01(session.SwipeCount / 6.0);
        double score = Clamp01(
            0.35 * selectionSignal +
            0.35 * compareSignal +
            0.20 * dwellSignal -
            0.05 * revisitPenalty -
            0.05 * swipePenalty);
        score = Round4(score);

        DecisionConvergenceLevel level = score >= 0.67
            ? DecisionConvergenceLevel.High
            : score >= 0.34
                ? DecisionConvergenceLevel.Medium
                : DecisionConvergenceLevel.Low;

        DecisionConvergenceTrend trend = session.SelectionCount > session.RevisitCount
            ? DecisionConvergenceTrend.Improving
            : session.RevisitCount > session.SelectionCount
                ? DecisionConvergenceTrend.Declining
                : DecisionConvergenceTrend.Flat;

        return new DecisionConvergenceSignal
        {
            Score = score,
            Level = level,
            Trend = trend,
            Basis = "convergence_v1(selection,compare,dwell,swipe,revisit)"
        };
    }

    private static GuidanceDisposition ResolveGuidanceDisposition(
        DecisionPreferenceResult preference,
        DecisionConvergenceSignal convergence,
        DecisionGuidanceConfig cfg)
    {
        if (preference.IsAmbiguous ||
            preference.Confidence <= cfg.NeutralMaxConfidence ||
            convergence.Score <= cfg.NeutralMaxConvergence)
        {
            return GuidanceDisposition.Neutral;
        }

        if (preference.Confidence >= cfg.StrongGuidanceMinConfidence &&
            convergence.Score >= cfg.StrongGuidanceMinConvergence)
        {
            return GuidanceDisposition.Strong;
        }

        return GuidanceDisposition.Light;
    }

    private static double AverageOrZero(double a, double? b)
    {
        if (b == null)
            return a;
        return (a + b.Value) * 0.5;
    }

    private static double Clamp01(double v)
    {
        if (v < 0.0) return 0.0;
        if (v > 1.0) return 1.0;
        return v;
    }

    private static double Round4(double v) =>
        Math.Round(v, 4, MidpointRounding.AwayFromZero);
}
