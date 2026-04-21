using System;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Result of formatting a <see cref="ResolvedDecisionTrigger"/> into one structured output shape.
/// Exactly one of <see cref="Single"/> or <see cref="Comparison"/> is non-null.
/// </summary>
public sealed class DecisionOutputBuildResult
{
    private DecisionOutputBuildResult(DecisionOutputKind shape, SingleProductDecisionOutput? single, ComparisonDecisionOutput? comparison)
    {
        Shape = shape;
        Single = single;
        Comparison = comparison;
    }

    public DecisionOutputKind Shape { get; }

    public SingleProductDecisionOutput? Single { get; }

    public ComparisonDecisionOutput? Comparison { get; }

    public static DecisionOutputBuildResult FromSingle(SingleProductDecisionOutput output)
    {
        if (output == null)
            throw new ArgumentNullException(nameof(output));
        return new DecisionOutputBuildResult(DecisionOutputKind.SingleProduct, output, null);
    }

    public static DecisionOutputBuildResult FromComparison(ComparisonDecisionOutput output)
    {
        if (output == null)
            throw new ArgumentNullException(nameof(output));
        return new DecisionOutputBuildResult(DecisionOutputKind.Comparison, null, output);
    }
}
