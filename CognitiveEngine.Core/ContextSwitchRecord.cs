namespace CognitiveEngine.Core;

public struct ContextSwitchRecord
{
    public string FromProductId { get; }
    public string ToProductId { get; }

    public ContextSwitchRecord(string fromProductId, string toProductId)
    {
        FromProductId = fromProductId;
        ToProductId = toProductId;
    }
}
