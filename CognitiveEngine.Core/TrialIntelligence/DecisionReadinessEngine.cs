using System;
using System.Collections.Generic;
using System.Linq;
using CognitiveEngine.Core;

namespace CognitiveEngine.Core.TrialIntelligence;

public static class DecisionReadinessEngine
{
    /// <summary>
    /// Optional per-tick confidence trace: one entry per engine tick with UTC time, smoothed confidence, and resolved state.
    /// When null or fewer than 2 points, confidence interpretation falls back to leaning plus friction proxy.
    /// </summary>
    public static (
        DecisionReadinessAssessment readiness,
        ConfidenceInterpretation confidence,
        StruggleDecisionSummary summary)
        Evaluate(
            IReadOnlyList<InteractionSignal> signals,
            IReadOnlyList<LeaningIndicator> leaning,
            IReadOnlyList<FrictionEpisode> frictionEpisodes,
            IReadOnlyList<(string occurredAtUtc, float confidence, StateType state)>? confidenceTrace = null)
    {
        var readiness = BuildReadiness(signals, leaning, frictionEpisodes);
        var confidence = BuildConfidence(leaning, frictionEpisodes, confidenceTrace);
        var summary = BuildSummary(readiness, confidence, frictionEpisodes);
        return (readiness, confidence, summary);
    }

    private static DecisionReadinessAssessment BuildReadiness(
        IReadOnlyList<InteractionSignal> signals,
        IReadOnlyList<LeaningIndicator> leaning,
        IReadOnlyList<FrictionEpisode> frictionEpisodes)
    {
        int confirmCount = signals.Count(s => s.EventType == InteractionEventKind.ConfirmIntent);
        int selectionCount = signals.Count(s => s.EventType == InteractionEventKind.Selection);
        int compareCount = signals.Count(s => s.EventType == InteractionEventKind.Compare);
        bool hasReadyBacktrack = frictionEpisodes.Any(f => f.FrictionKind == FrictionKind.PostReadyBacktrack);
        double topLeaning = leaning.Count == 0 ? 0.0 : leaning.Max(x => x.LeaningScore);
        double gap = 0.0;
        if (leaning.Count >= 2)
        {
            var ordered = leaning.OrderByDescending(x => x.LeaningScore).ThenBy(x => x.ProductId, StringComparer.Ordinal).ToList();
            gap = Math.Max(0.0, ordered[0].LeaningScore - ordered[1].LeaningScore);
        }

        double frictionSeverity = frictionEpisodes.Sum(FrictionSeverityWeight);
        double frictionPenalty = 0.16 * Math.Min(1.0, frictionSeverity / 3.0);

        bool healthyCompareFlow =
            compareCount > 0 &&
            selectionCount > 0 &&
            topLeaning >= 0.65 &&
            gap >= 0.12 &&
            !hasReadyBacktrack;
        double healthyFlowBoost = healthyCompareFlow ? 0.16 : 0.0;

        double score = Clamp01(
            0.45 * (confirmCount > 0 ? 1.0 : 0.0) +
            0.35 * topLeaning +
            0.20 * gap -
            frictionPenalty +
            healthyFlowBoost);
        if (hasReadyBacktrack)
            score = Clamp01(score - 0.10);

        var level = score >= 0.75 ? DecisionReadinessLevel.High :
            score >= 0.45 ? DecisionReadinessLevel.Medium : DecisionReadinessLevel.Low;

        return new DecisionReadinessAssessment
        {
            ReadinessScore = Round4(score),
            ReadinessLevel = level,
            IsReadyToConfirm = level == DecisionReadinessLevel.High && !hasReadyBacktrack,
            DominantProductId = leaning.OrderBy(x => x.Rank).Select(x => x.ProductId).FirstOrDefault(),
            Basis = "v2(confirm,leaning,gap,weighted_friction_penalty,healthy_compare_boost)"
        };
    }

    private static ConfidenceInterpretation BuildConfidence(
        IReadOnlyList<LeaningIndicator> leaning,
        IReadOnlyList<FrictionEpisode> frictionEpisodes,
        IReadOnlyList<(string occurredAtUtc, float confidence, StateType state)>? confidenceTrace)
    {
        var fromTrace = TryBuildConfidenceFromTrace(confidenceTrace, frictionEpisodes);
        if (fromTrace != null)
            return fromTrace;

        return BuildConfidenceFromLeaningAndFriction(leaning, frictionEpisodes);
    }

    private static ConfidenceInterpretation? TryBuildConfidenceFromTrace(
        IReadOnlyList<(string occurredAtUtc, float confidence, StateType state)>? trace,
        IReadOnlyList<FrictionEpisode> frictionEpisodes)
    {
        if (trace == null || trace.Count < 2)
            return null;

        var ordered = trace
            .OrderBy(t => t.occurredAtUtc, StringComparer.Ordinal)
            .ThenBy(t => t.state)
            .ToList();

        var values = ordered.Select(t => Clamp01((double)t.confidence)).ToList();
        double mean = values.Average();
        double variance = values.Select(v => (v - mean) * (v - mean)).Average();

        int n = values.Count;
        int k = n / 2;
        double firstMean = k == 0 ? mean : values.Take(k).Average();
        double secondMean = n - k == 0 ? mean : values.Skip(k).Average();
        double delta = secondMean - firstMean;

        double frictionPenalty = Math.Min(0.25, frictionEpisodes.Count * 0.06);
        double stability = Clamp01(mean * (1.0 - Math.Min(1.0, variance * 6.0)) - frictionPenalty);

        ConfidenceTrend trend;
        string interpretation;

        if (variance > 0.02)
        {
            trend = ConfidenceTrend.Unstable;
            interpretation = "Confidence trace shows high volatility across ticks";
        }
        else if (delta > 0.06 && secondMean > firstMean)
        {
            trend = ConfidenceTrend.Improving;
            interpretation = "Confidence trace trends upward in the second half of the session";
        }
        else if (mean >= 0.62 && variance <= 0.012)
        {
            trend = ConfidenceTrend.Stabilized;
            interpretation = "Confidence trace is high and steady";
        }
        else if (mean >= 0.45)
        {
            trend = ConfidenceTrend.Improving;
            interpretation = "Confidence trace is forming with moderate stability";
        }
        else
        {
            trend = ConfidenceTrend.Unstable;
            interpretation = "Confidence trace remains low or erratic";
        }

        return new ConfidenceInterpretation
        {
            StabilityScore = Round4(stability),
            Trend = trend,
            Interpretation = interpretation,
            Basis = "confidence_trace_v1(mean,variance,half_delta,friction_penalty)"
        };
    }

    private static ConfidenceInterpretation BuildConfidenceFromLeaningAndFriction(
        IReadOnlyList<LeaningIndicator> leaning,
        IReadOnlyList<FrictionEpisode> frictionEpisodes)
    {
        double baseConfidence = leaning.Count == 0 ? 0.0 : leaning.Average(x => x.Confidence);
        double frictionPenalty = Math.Min(0.5, frictionEpisodes.Count * 0.12);
        double stability = Clamp01(baseConfidence - frictionPenalty);

        var trend = stability >= 0.70 ? ConfidenceTrend.Stabilized :
            stability >= 0.40 ? ConfidenceTrend.Improving : ConfidenceTrend.Unstable;

        string interpretation = trend switch
        {
            ConfidenceTrend.Stabilized => "confidence has stabilized with low volatility",
            ConfidenceTrend.Improving => "confidence is forming but still variable",
            _ => "confidence is unstable and likely conflict-driven"
        };

        return new ConfidenceInterpretation
        {
            StabilityScore = Round4(stability),
            Trend = trend,
            Interpretation = interpretation,
            Basis = "leaning_friction_proxy_v1"
        };
    }

    private static StruggleDecisionSummary BuildSummary(
        DecisionReadinessAssessment readiness,
        ConfidenceInterpretation confidence,
        IReadOnlyList<FrictionEpisode> frictionEpisodes)
    {
        double struggleScore = Clamp01((frictionEpisodes.Count / 3.0) + (confidence.Trend == ConfidenceTrend.Unstable ? 0.3 : 0.0));
        double decisionSignalScore = readiness.ReadinessScore;

        JourneyClassification cls;
        string tag;
        if (decisionSignalScore >= 0.75 && struggleScore < 0.35)
        {
            cls = JourneyClassification.Decisive;
            tag = "decision-led";
        }
        else if (struggleScore >= 0.70 && decisionSignalScore < 0.45)
        {
            cls = JourneyClassification.Indecisive;
            tag = "struggle-led";
        }
        else if (struggleScore > decisionSignalScore)
        {
            cls = JourneyClassification.Struggling;
            tag = "struggle-dominant";
        }
        else
        {
            cls = JourneyClassification.Balanced;
            tag = "mixed-signals";
        }

        return new StruggleDecisionSummary
        {
            JourneyClassification = cls,
            StruggleScore = Round4(struggleScore),
            DecisionSignalScore = Round4(decisionSignalScore),
            SummaryTag = tag
        };
    }

    private static double Clamp01(double v)
    {
        if (v < 0.0) return 0.0;
        if (v > 1.0) return 1.0;
        return v;
    }

    private static double Round4(double v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);

    private static double FrictionSeverityWeight(FrictionEpisode episode)
    {
        return episode.FrictionKind switch
        {
            FrictionKind.PostReadyBacktrack => 1.0,
            FrictionKind.HesitationBurst => 0.75,
            FrictionKind.ComparisonLoop => 0.50,
            _ => 0.60
        };
    }
}
