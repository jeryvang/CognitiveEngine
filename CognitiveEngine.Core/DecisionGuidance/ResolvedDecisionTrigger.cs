using System;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// A single resolved P6 trigger after priority and duplicate suppression (Step 2).
/// Compare products are stored in strict lexical order for deterministic equality.
/// </summary>
public readonly struct ResolvedDecisionTrigger : IEquatable<ResolvedDecisionTrigger>
{
    public ResolvedDecisionTrigger(DecisionTriggerKind kind, string productIdLow, string productIdHigh)
    {
        Kind = kind;
        ProductIdLow = productIdLow ?? throw new ArgumentNullException(nameof(productIdLow));
        ProductIdHigh = productIdHigh ?? throw new ArgumentNullException(nameof(productIdHigh));
        if (kind is DecisionTriggerKind.Revisit or DecisionTriggerKind.Dwell && !string.IsNullOrEmpty(productIdHigh))
            throw new ArgumentException("productIdHigh must be empty for Revisit and Dwell.", nameof(productIdHigh));
        if (kind == DecisionTriggerKind.Compare && string.CompareOrdinal(productIdLow, productIdHigh) >= 0)
            throw new ArgumentException("For Compare, productIdLow must be strictly less than productIdHigh (lexical).", nameof(productIdLow));
        if (kind == DecisionTriggerKind.CompareReturn)
        {
            if (string.IsNullOrEmpty(productIdHigh))
                throw new ArgumentException("CompareReturn requires a comparison partner in productIdHigh.", nameof(productIdHigh));
            if (string.Equals(productIdLow, productIdHigh, StringComparison.Ordinal))
                throw new ArgumentException("CompareReturn requires two distinct product ids.", nameof(productIdHigh));
        }
    }

    public DecisionTriggerKind Kind { get; }

    /// <summary>
    /// For <see cref="DecisionTriggerKind.Dwell"/> and <see cref="DecisionTriggerKind.Revisit"/>, the active product; high is empty.
    /// For <see cref="DecisionTriggerKind.Compare"/>, the two product ids (low, high) in lexical order.
    /// For <see cref="DecisionTriggerKind.CompareReturn"/>, the focused product (low) and comparison partner (high).
    /// </summary>
    public string ProductIdLow { get; }

    public string ProductIdHigh { get; }

    public static ResolvedDecisionTrigger ForSingle(DecisionTriggerKind kind, string productId)
    {
        if (kind is DecisionTriggerKind.Compare or DecisionTriggerKind.CompareReturn)
            throw new ArgumentException("Use ForCompare or ForCompareReturn.", nameof(kind));
        return new ResolvedDecisionTrigger(kind, productId, "");
    }

    /// <summary>
    /// User exited comparison and returned to single view on <paramref name="focusedProductId"/>.
    /// Partner id is the other product from the last comparison pair (lexical low/high storage).
    /// </summary>
    public static ResolvedDecisionTrigger ForCompareReturn(string focusedProductId, string comparisonPartnerProductId)
    {
        if (string.IsNullOrWhiteSpace(focusedProductId))
            throw new ArgumentException("focusedProductId is required.", nameof(focusedProductId));
        if (string.IsNullOrWhiteSpace(comparisonPartnerProductId))
            throw new ArgumentException("comparisonPartnerProductId is required.", nameof(comparisonPartnerProductId));
        if (string.Equals(focusedProductId, comparisonPartnerProductId, StringComparison.Ordinal))
            throw new ArgumentException("CompareReturn requires two distinct product ids.");

        return new ResolvedDecisionTrigger(
            DecisionTriggerKind.CompareReturn,
            focusedProductId,
            comparisonPartnerProductId);
    }

    public static ResolvedDecisionTrigger ForCompare(string productIdA, string productIdB)
    {
        int c = string.CompareOrdinal(productIdA, productIdB);
        if (c == 0)
            throw new ArgumentException("Compare requires two distinct product ids.", nameof(productIdB));
        return c < 0
            ? new ResolvedDecisionTrigger(DecisionTriggerKind.Compare, productIdA, productIdB)
            : new ResolvedDecisionTrigger(DecisionTriggerKind.Compare, productIdB, productIdA);
    }

    public static string Signature(ResolvedDecisionTrigger t) =>
        t.Kind switch
        {
            DecisionTriggerKind.Compare => $"compare:{t.ProductIdLow}|{t.ProductIdHigh}",
            DecisionTriggerKind.CompareReturn => $"compare_return:{t.ProductIdLow}|{t.ProductIdHigh}",
            DecisionTriggerKind.Revisit => $"revisit:{t.ProductIdLow}",
            DecisionTriggerKind.Dwell => $"dwell:{t.ProductIdLow}",
            _ => throw new InvalidOperationException("Unexpected DecisionTriggerKind.")
        };

    public bool Equals(ResolvedDecisionTrigger other) =>
        Kind == other.Kind &&
        string.Equals(ProductIdLow, other.ProductIdLow, StringComparison.Ordinal) &&
        string.Equals(ProductIdHigh, other.ProductIdHigh, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is ResolvedDecisionTrigger other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Kind, ProductIdLow, ProductIdHigh);
}
