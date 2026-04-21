using System;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Deterministic P7 preference outcome contract produced from normalized session behavior evidence.
/// This shape is additive and designed to be mapped into behavior_context JSON fields.
/// </summary>
public sealed class DecisionPreferenceResult
{
    public PreferenceResultKind Kind { get; }

    public PreferenceLean Leaning { get; }

    public string? PreferredProductId { get; }

    public string? SecondaryProductId { get; }

    public bool IsAmbiguous { get; }

    public double Confidence { get; }

    public string Basis { get; }

    private DecisionPreferenceResult(
        PreferenceResultKind kind,
        PreferenceLean leaning,
        string? preferredProductId,
        string? secondaryProductId,
        bool isAmbiguous,
        double confidence,
        string basis)
    {
        Kind = kind;
        Leaning = leaning;
        PreferredProductId = preferredProductId;
        SecondaryProductId = secondaryProductId;
        IsAmbiguous = isAmbiguous;
        Confidence = Clamp01(confidence);
        Basis = basis ?? "";
    }

    public static DecisionPreferenceResult NoClearLean(double confidence, string basis) =>
        new(PreferenceResultKind.NoClearLean, PreferenceLean.None, null, null, true, confidence, basis);

    public static DecisionPreferenceResult LeanSingle(string preferredProductId, double confidence, string basis)
    {
        if (string.IsNullOrWhiteSpace(preferredProductId))
            throw new ArgumentException("preferredProductId is required.", nameof(preferredProductId));
        return new DecisionPreferenceResult(
            PreferenceResultKind.LeanSingleProduct,
            PreferenceLean.ProductA,
            preferredProductId,
            null,
            false,
            confidence,
            basis);
    }

    public static DecisionPreferenceResult LeanComparison(
        PreferenceLean leaning,
        string preferredProductId,
        string secondaryProductId,
        double confidence,
        string basis)
    {
        if (leaning != PreferenceLean.ProductA && leaning != PreferenceLean.ProductB)
            throw new ArgumentException("leaning must be ProductA or ProductB.", nameof(leaning));
        if (string.IsNullOrWhiteSpace(preferredProductId))
            throw new ArgumentException("preferredProductId is required.", nameof(preferredProductId));
        if (string.IsNullOrWhiteSpace(secondaryProductId))
            throw new ArgumentException("secondaryProductId is required.", nameof(secondaryProductId));
        if (string.Equals(preferredProductId, secondaryProductId, StringComparison.Ordinal))
            throw new ArgumentException("preferredProductId and secondaryProductId must be distinct.");

        return new DecisionPreferenceResult(
            PreferenceResultKind.LeanComparison,
            leaning,
            preferredProductId,
            secondaryProductId,
            false,
            confidence,
            basis);
    }

    private static double Clamp01(double v)
    {
        if (v < 0.0) return 0.0;
        if (v > 1.0) return 1.0;
        return v;
    }
}

public enum PreferenceResultKind
{
    NoClearLean,
    LeanSingleProduct,
    LeanComparison
}
