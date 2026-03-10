using System;
using System.Collections.Generic;

namespace CognitiveEngine.Core;

public sealed class SessionMemoryContext
{
    public string ProductId { get; }

    internal List<InputSignal>              SignalWindow  { get; } = new List<InputSignal>();
    internal Dictionary<SignalType, float>  Signals       { get; } = new Dictionary<SignalType, float>();
    internal List<TickLog>                  Logs          { get; } = new List<TickLog>();
    internal List<StateType>                RecentStates  { get; } = new List<StateType>();
    internal StateType                      CurrentState  { get; set; } = StateType.Neutral;
    internal int                            TickIndex     { get; set; } = 0;
    internal float                          SmoothedConfidence { get; set; } = 0f;

    public IReadOnlyList<TickLog> ReadOnlyLogs => Logs;

    public SessionMemoryContext(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("ProductId cannot be null or empty.", nameof(productId));

        ProductId = productId;
    }

    internal void Reset()
    {
        SignalWindow.Clear();
        Signals.Clear();
        Logs.Clear();
        RecentStates.Clear();
        CurrentState = StateType.Neutral;
        TickIndex    = 0;
        SmoothedConfidence = 0f;
    }
}
