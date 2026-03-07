namespace CognitiveEngine.Core;

public enum DeterminismViolationKind
{
    RulePriorityViolation,
    UndefinedTransition,
    RapidFlipFlop
}

public struct DeterminismViolationRecord
{
    public DeterminismViolationKind Kind { get; }
    public int TickIndex { get; }
    public StateType FromState { get; }
    public StateType ToState { get; }
    public string RuleName { get; }

    public DeterminismViolationRecord(
        DeterminismViolationKind kind,
        int tickIndex,
        StateType fromState,
        StateType toState,
        string ruleName)
    {
        Kind = kind;
        TickIndex = tickIndex;
        FromState = fromState;
        ToState = toState;
        RuleName = ruleName ?? "";
    }
}
