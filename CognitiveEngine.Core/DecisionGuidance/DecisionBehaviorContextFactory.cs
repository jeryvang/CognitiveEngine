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
            0.25 * selectionStrength +
            0.25 * dwellStrength +
            0.20 * revisitStrength +
            0.30 * focusStrength);
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
        var normalizedSignalStrength = Clamp01(
            0.25 * selectionStrength +
            0.25 * dwellStrength +
            0.20 * revisitStrength +
            0.30 * compareStrength);
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
            NormalizedSignalStrength = normalizedSignalStrength
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
