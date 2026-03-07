using System;
using System.Collections.Generic;

namespace CognitiveEngine.Core;

public static class DeterministicIntegrityValidator
{
    private static readonly HashSet<(StateType, StateType)> AllowedTransitions = BuildAllowedTransitions();

    private static HashSet<(StateType, StateType)> BuildAllowedTransitions()
    {
        var set = new HashSet<(StateType, StateType)>();
        foreach (StateType from in Enum.GetValues(typeof(StateType)))
        foreach (StateType to in Enum.GetValues(typeof(StateType)))
            set.Add((from, to));
        return set;
    }

    private static readonly Dictionary<string, StateType> RuleToState = new Dictionary<string, StateType>(StringComparer.Ordinal)
    {
        { "ConfirmIntentHigh", StateType.ReadyToConfirm },
        { "ContextChange", StateType.Neutral },
        { "ProductFocus", StateType.Exploration },
        { "ComparisonAction", StateType.Comparison },
        { "SwipeVelocity", StateType.Exploration },
        { "HighDwell", StateType.Hesitation },
        { "MediumDwell", StateType.Comparison },
        { "NoSignals", StateType.Neutral },
        { "DefaultExploration", StateType.Exploration }
    };

    public static IReadOnlyList<DeterminismViolationRecord> Validate(
        int tickIndex,
        StateType previousState,
        StateType resolvedState,
        string ruleName,
        IReadOnlyList<StateType> recentStates)
    {
        var violations = new List<DeterminismViolationRecord>();

        if (RuleToState.TryGetValue(ruleName ?? "", out var expectedState) && expectedState != resolvedState)
            violations.Add(new DeterminismViolationRecord(
                DeterminismViolationKind.RulePriorityViolation, tickIndex, previousState, resolvedState, ruleName ?? ""));

        if (!AllowedTransitions.Contains((previousState, resolvedState)))
            violations.Add(new DeterminismViolationRecord(
                DeterminismViolationKind.UndefinedTransition, tickIndex, previousState, resolvedState, ruleName ?? ""));

        // Only report RapidFlipFlop when we actually flipped: A → B → A with B ≠ A (not when staying in same state)
        if (recentStates != null && recentStates.Count >= 2
            && resolvedState == recentStates[recentStates.Count - 2]
            && resolvedState != recentStates[recentStates.Count - 1])
            violations.Add(new DeterminismViolationRecord(
                DeterminismViolationKind.RapidFlipFlop, tickIndex, previousState, resolvedState, ruleName ?? ""));

        return violations;
    }
}
