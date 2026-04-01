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
