using System;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Flat, host-friendly view of P7 behavior context for Unity/web UI binding
/// (rationale, confidence, disposition, trigger signal).
/// </summary>
public sealed class GuidanceBehaviorSnapshot
{
    public string Signal { get; set; } = "";

    public DecisionTriggerKind TriggerKind { get; set; }

    public string ProductId { get; set; } = "";

    public string? ProductIdHigh { get; set; }

    public double Confidence { get; set; }

    public bool IsAmbiguous { get; set; }

    public GuidanceDisposition Disposition { get; set; } = GuidanceDisposition.Neutral;

    public string WhyThisMattersNow { get; set; } = "";

    public static string SignalFromTriggerKind(DecisionTriggerKind kind) =>
        kind switch
        {
            DecisionTriggerKind.Compare => "compare",
            DecisionTriggerKind.CompareReturn => "compare_return",
            DecisionTriggerKind.Revisit => "revisit",
            DecisionTriggerKind.Dwell => "dwell",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    public static GuidanceBehaviorSnapshot? TryCreate(
        DecisionOutputBuildResult? build,
        in ResolvedDecisionTrigger trigger)
    {
        if (build == null)
            return null;

        var ctx = build.Single?.BehaviorContext ?? build.Comparison?.BehaviorContext;
        if (ctx == null)
            return null;

        return new GuidanceBehaviorSnapshot
        {
            Signal = SignalFromTriggerKind(trigger.Kind),
            TriggerKind = trigger.Kind,
            ProductId = trigger.ProductIdLow,
            ProductIdHigh = trigger.Kind is DecisionTriggerKind.Compare or DecisionTriggerKind.CompareReturn
                ? trigger.ProductIdHigh
                : null,
            Confidence = ctx.PreferenceIndication.Confidence,
            IsAmbiguous = ctx.PreferenceIndication.IsAmbiguous,
            Disposition = ctx.GuidanceDisposition,
            WhyThisMattersNow = ctx.WhyThisMattersNow
        };
    }
}
