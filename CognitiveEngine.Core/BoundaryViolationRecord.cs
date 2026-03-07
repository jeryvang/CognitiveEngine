namespace CognitiveEngine.Core;

public enum BoundaryViolationKind
{
    InjectSignal,
    Update
}

public struct BoundaryViolationRecord
{
    public BoundaryViolationKind Kind { get; }
    public string ExpectedProductId { get; }
    public string ActualProductId { get; }

    public BoundaryViolationRecord(BoundaryViolationKind kind, string expectedProductId, string actualProductId)
    {
        Kind = kind;
        ExpectedProductId = expectedProductId;
        ActualProductId = actualProductId;
    }
}
