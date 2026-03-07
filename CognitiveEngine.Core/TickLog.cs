using System.Collections.Generic;

namespace CognitiveEngine.Core;

public struct TickLog
{
    public int TickIndex { get; }
    public float Timestamp { get; }
    public IReadOnlyDictionary<SignalType, float> Signals { get; }
    public string TriggeredRule { get; }
    public StateType FinalState { get; }
    public float Confidence { get; }

    public TickLog(
        int tickIndex,
        float timestamp,
        IReadOnlyDictionary<SignalType, float> signals,
        string triggeredRule,
        StateType finalState,
        float confidence)
    {
        TickIndex = tickIndex;
        Timestamp = timestamp;
        Signals = signals;
        TriggeredRule = triggeredRule;
        FinalState = finalState;
        Confidence = confidence;
    }
}
