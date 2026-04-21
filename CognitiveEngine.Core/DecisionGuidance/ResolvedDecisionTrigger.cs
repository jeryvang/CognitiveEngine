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
        if (kind != DecisionTriggerKind.Compare && !string.IsNullOrEmpty(productIdHigh))
            throw new ArgumentException("productIdHigh must be empty unless Kind is Compare.", nameof(productIdHigh));
        if (kind == DecisionTriggerKind.Compare && string.CompareOrdinal(productIdLow, productIdHigh) >= 0)
            throw new ArgumentException("For Compare, productIdLow must be strictly less than productIdHigh (lexical).", nameof(productIdLow));
    }

    public DecisionTriggerKind Kind { get; }

    /// <summary>
    /// For <see cref="DecisionTriggerKind.Dwell"/> and <see cref="DecisionTriggerKind.Revisit"/>, the active product; high is empty.
    /// For <see cref="DecisionTriggerKind.Compare"/>, the two product ids (low, high).
    /// </summary>
    public string ProductIdLow { get; }

    public string ProductIdHigh { get; }

    public static ResolvedDecisionTrigger ForSingle(DecisionTriggerKind kind, string productId)
    {
        if (kind == DecisionTriggerKind.Compare)
            throw new ArgumentException("Use ForCompare for Compare.", nameof(kind));
        return new ResolvedDecisionTrigger(kind, productId, "");
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
