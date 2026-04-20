using System;
using System.Linq;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Maps deterministic P7 session behavior and preference signals into output behavior_context JSON.
/// </summary>
public static class DecisionBehaviorContextFactory
{
    public static DecisionBehaviorContext Create(
        DecisionBehaviorSessionContext session,
        in ResolvedDecisionTrigger trigger)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));

        var preference = ResolvePreference(session, in trigger);
        var rationale = ResolveRationale(session, preference, in trigger);

        return new DecisionBehaviorContext
        {
            SessionScope = "session",
            BehaviorSignalUsage = BuildSignalUsage(session, in trigger),
            PreferenceIndication = new PreferenceIndication
            {
                Leaning = preference.Leaning,
                IsAmbiguous = preference.IsAmbiguous,
                Confidence = preference.Confidence
            },
            WhyThisMattersNow = rationale
        };
    }

    private static BehaviorSignalUsage BuildSignalUsage(
        DecisionBehaviorSessionContext session,
        in ResolvedDecisionTrigger trigger)
    {
        var selectionStrength = AverageOrZero(
            session.GetSelectionEvidenceNormalizedStrength(trigger.ProductIdLow),
            trigger.Kind == DecisionTriggerKind.Compare ? session.GetSelectionEvidenceNormalizedStrength(trigger.ProductIdHigh) : (double?)null);

        var dwellStrength = AverageOrZero(
            session.GetDwellEvidenceNormalizedStrength(trigger.ProductIdLow),
            trigger.Kind == DecisionTriggerKind.Compare ? session.GetDwellEvidenceNormalizedStrength(trigger.ProductIdHigh) : (double?)null);

        var revisitStrength = AverageOrZero(
            session.GetRevisitEvidenceNormalizedStrength(trigger.ProductIdLow),
            trigger.Kind == DecisionTriggerKind.Compare ? session.GetRevisitEvidenceNormalizedStrength(trigger.ProductIdHigh) : (double?)null);

        var compareStrength = trigger.Kind == DecisionTriggerKind.Compare
            ? session.GetCompareEvidenceNormalizedStrength(trigger.ProductIdLow, trigger.ProductIdHigh)
            : 0.0;

        var swipeTransitionCounts = session.GetSwipeTransitionCounts();
        var maxSwipe = swipeTransitionCounts.Count == 0 ? 0 : swipeTransitionCounts.Values.Max();
        var swipeStrength = maxSwipe <= 0 ? 0.0 : (double)session.SwipeCount / maxSwipe;

        var normalizedSignalStrength = Clamp01(
            0.25 * selectionStrength +
            0.25 * dwellStrength +
            0.20 * revisitStrength +
            0.20 * compareStrength +
            0.10 * Clamp01(swipeStrength));

        return new BehaviorSignalUsage
        {
            SelectionCount = session.SelectionCount,
            DwellCount = session.DwellCount,
            SwipeCount = session.SwipeCount,
            CompareCount = session.CompareCount,
            RevisitCount = session.RevisitCount,
            NormalizedSignalStrength = normalizedSignalStrength
        };
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
}
