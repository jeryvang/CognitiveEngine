namespace CognitiveEngine.Core;

public struct InputSignal
{
    public SignalType Type { get; }
    public float Value { get; }
    public float Timestamp { get; }

    public InputSignal(SignalType type, float value, float timestamp)
    {
        Type = type;
        Value = value;
        Timestamp = timestamp;
    }
}