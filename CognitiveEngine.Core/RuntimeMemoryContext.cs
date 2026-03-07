using System.Collections.Generic;
using System.Linq;

namespace CognitiveEngine.Core;

public sealed class RuntimeMemoryContext
{
    public int TickIndex { get; }
    public float Timestamp { get; }
    public IReadOnlyDictionary<SignalType, float> Signals { get; }
    public bool HasSignals => Signals.Count > 0;

    public RuntimeMemoryContext(int tickIndex, float timestamp, IDictionary<SignalType, float> signals)
    {
        TickIndex = tickIndex;
        Timestamp = timestamp;
        var sorted = new SortedDictionary<SignalType, float>(signals);
        Signals = sorted.ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public float GetSignal(SignalType type) =>
        Signals.TryGetValue(type, out var value) ? value : 0f;
}
