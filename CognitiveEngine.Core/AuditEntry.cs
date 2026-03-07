namespace CognitiveEngine.Core;

public enum AuditEntryKind
{
    ContextSwitch,
    SessionReset,
    Tick
}

public struct AuditEntry
{
    public AuditEntryKind Kind { get; }
    public float Timestamp { get; }
    public string ProductId { get; }
    public string FromProductId { get; }
    public string ToProductId { get; }
    public int TickIndex { get; }
    public string RuleName { get; }
    public StateType State { get; }

    public static AuditEntry ContextSwitch(string fromProductId, string toProductId)
    {
        return new AuditEntry(AuditEntryKind.ContextSwitch, 0f, toProductId ?? "", fromProductId ?? "", toProductId ?? "", 0, "", StateType.Neutral);
    }

    public static AuditEntry SessionReset(string productId)
    {
        return new AuditEntry(AuditEntryKind.SessionReset, 0f, productId ?? "", "", "", 0, "", StateType.Neutral);
    }

    public static AuditEntry Tick(float timestamp, string productId, int tickIndex, string ruleName, StateType state)
    {
        return new AuditEntry(AuditEntryKind.Tick, timestamp, productId ?? "", "", "", tickIndex, ruleName ?? "", state);
    }

    private AuditEntry(AuditEntryKind kind, float timestamp, string productId, string fromProductId, string toProductId, int tickIndex, string ruleName, StateType state)
    {
        Kind = kind;
        Timestamp = timestamp;
        ProductId = productId;
        FromProductId = fromProductId;
        ToProductId = toProductId;
        TickIndex = tickIndex;
        RuleName = ruleName;
        State = state;
    }
}
