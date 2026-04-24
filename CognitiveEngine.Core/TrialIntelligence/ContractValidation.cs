using System;
using System.Globalization;

namespace CognitiveEngine.Core.TrialIntelligence;

public static class ContractValidation
{
    public static void ValidateSessionOrThrow(SessionContract contract)
    {
        if (contract == null)
            throw new ArgumentNullException(nameof(contract));
        if (string.IsNullOrWhiteSpace(contract.SchemaVersion))
            throw new ArgumentException("schema_version is required.", nameof(contract));
        if (string.IsNullOrWhiteSpace(contract.SessionId))
            throw new ArgumentException("session_id is required.", nameof(contract));
        ThrowIfUtcMissing(nameof(contract.ExportedAtUtc), contract.ExportedAtUtc);
        if (contract.InteractionSignals == null)
            throw new ArgumentException("interaction_signals is required.", nameof(contract));
        if (contract.PreferenceSignals == null)
            throw new ArgumentException("preference_signals is required.", nameof(contract));
        if (contract.LeaningIndicators == null)
            throw new ArgumentException("leaning_indicators is required.", nameof(contract));
        if (contract.FrictionEpisodes == null)
            throw new ArgumentException("friction_episodes is required.", nameof(contract));
        if (contract.DecisionReadiness == null)
            throw new ArgumentException("decision_readiness is required.", nameof(contract));
        if (contract.ConfidenceInterpretation == null)
            throw new ArgumentException("confidence_interpretation is required.", nameof(contract));
        if (contract.StruggleDecisionSummary == null)
            throw new ArgumentException("struggle_decision_summary is required.", nameof(contract));
        if (contract.DerivedMetrics == null)
            throw new ArgumentException("derived_metrics is required.", nameof(contract));
        if (contract.DerivedMetrics.SwitchCount < 0)
            throw new ArgumentException("derived_metrics.switch_count cannot be negative.", nameof(contract));
        if (contract.DerivedMetrics.ExplorationSwitchCount < 0)
            throw new ArgumentException("derived_metrics.exploration_switch_count cannot be negative.", nameof(contract));
        if (contract.DerivedMetrics.SelectionEventsCount < 0)
            throw new ArgumentException("derived_metrics.selection_events_count cannot be negative.", nameof(contract));
        if (contract.DerivedMetrics.TotalCompareTimeMs < 0)
            throw new ArgumentException("derived_metrics.total_compare_time_ms cannot be negative.", nameof(contract));
        if (string.IsNullOrWhiteSpace(contract.DerivedMetrics.CompareTimeBasis))
            throw new ArgumentException("derived_metrics.compare_time_basis is required.", nameof(contract));
        if (contract.DerivedMetrics.DecisionConvergenceScore.HasValue)
        {
            var score = contract.DerivedMetrics.DecisionConvergenceScore.Value;
            if (score < 0.0 || score > 1.0)
                throw new ArgumentException("derived_metrics.decision_convergence_score must be within [0,1].", nameof(contract));
        }
        if (contract.DerivedMetrics.DecisionConvergenceLevel.HasValue
            && string.IsNullOrWhiteSpace(contract.DerivedMetrics.DecisionConvergenceBasis))
        {
            throw new ArgumentException("derived_metrics.decision_convergence_basis is required when decision_convergence_level is present.", nameof(contract));
        }
    }

    public static void ValidateAggregateOrThrow(ProductAggregate contract)
    {
        if (contract == null)
            throw new ArgumentNullException(nameof(contract));
        if (string.IsNullOrWhiteSpace(contract.SchemaVersion))
            throw new ArgumentException("schema_version is required.", nameof(contract));
        if (string.IsNullOrWhiteSpace(contract.AggregateId))
            throw new ArgumentException("aggregate_id is required.", nameof(contract));
        if (string.IsNullOrWhiteSpace(contract.ProductId))
            throw new ArgumentException("product_id is required.", nameof(contract));
        ThrowIfUtcMissing(nameof(contract.ExportedAtUtc), contract.ExportedAtUtc);
        if (contract.Attraction == null)
            throw new ArgumentException("attraction is required.", nameof(contract));
        if (contract.Engagement == null)
            throw new ArgumentException("engagement is required.", nameof(contract));
        if (contract.ComparisonPatterns == null)
            throw new ArgumentException("comparison_patterns is required.", nameof(contract));
        if (contract.ComparisonPatterns.UniqueComparisonPartnerProductIds == null)
            throw new ArgumentException(
                "comparison_patterns.unique_comparison_partner_product_ids is required.", nameof(contract));

        if (contract.FrictionHotspots == null)
            throw new ArgumentException("friction_hotspots is required.", nameof(contract));
        if (contract.FrictionHotspots.Hotspots == null)
            throw new ArgumentException("friction_hotspots.hotspots is required.", nameof(contract));

        if (contract.StruggleTrends == null)
            throw new ArgumentException("struggle_trends is required.", nameof(contract));
        if (contract.StruggleTrends.JourneyClassificationCounts == null)
            throw new ArgumentException(
                "struggle_trends.journey_classification_counts is required.", nameof(contract));

        if (contract.ReadinessTrends == null)
            throw new ArgumentException("readiness_trends is required.", nameof(contract));
        if (contract.ReadinessTrends.ReadinessLevelCountsWhenDominant == null)
            throw new ArgumentException(
                "readiness_trends.readiness_level_counts_when_dominant is required.", nameof(contract));
    }

    private static void ThrowIfUtcMissing(string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{fieldName} is required.", fieldName);
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            throw new ArgumentException($"{fieldName} must be ISO 8601 parseable ({Schema.UtcTimestampFormatDescription}).", fieldName);
        if (dt.Kind != DateTimeKind.Utc)
            throw new ArgumentException($"{fieldName} must be UTC (Kind=Utc or Z offset).", fieldName);
    }
}
