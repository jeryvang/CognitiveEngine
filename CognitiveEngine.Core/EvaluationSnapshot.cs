using System;
using System.Linq;

namespace CognitiveEngine.Core;

public sealed class EvaluationSnapshot
{
    private const float ThresholdConfirm    = 0.8f;
    private const float ThresholdDiscrete   = 0.5f;
    private const float ThresholdHesitation = 0.7f;
    private const float ThresholdComparison = 0.3f;

    public RuntimeMemoryContext Context { get; }
    public StateType PreviousState { get; }
    public float WindowDuration { get; }

    public EvaluationSnapshot(RuntimeMemoryContext context, StateType previousState, float windowDuration)
    {
        Context        = context;
        PreviousState  = previousState;
        WindowDuration = windowDuration;
    }

    public (StateType state, string rule, float confidence) Evaluate()
    {
        float dwell         = Context.GetSignal(SignalType.DwellTime);
        float confirm       = Context.GetSignal(SignalType.ConfirmIntent);
        float contextChange = Context.GetSignal(SignalType.ContextChange);
        float productFocus  = Context.GetSignal(SignalType.ProductFocus);
        float comparison    = Context.GetSignal(SignalType.ComparisonAction);
        float swipe         = Context.GetSignal(SignalType.SwipeVelocity);

        if (confirm > ThresholdConfirm)        return (StateType.ReadyToConfirm, "ConfirmIntentHigh", Confidence(confirm));
        if (contextChange > ThresholdDiscrete) return (StateType.Neutral,       "ContextChange",     Confidence(contextChange));
        if (productFocus > ThresholdDiscrete)  return (StateType.Exploration,   "ProductFocus",      Confidence(productFocus));
        if (comparison > ThresholdDiscrete)    return (StateType.Comparison,     "ComparisonAction",  Confidence(comparison));
        if (Math.Abs(swipe) > ThresholdDiscrete) return (StateType.Exploration,  "SwipeVelocity",     Confidence(Math.Abs(swipe)));
        if (dwell > ThresholdHesitation)       return (StateType.Hesitation,      "HighDwell",         Confidence(dwell));
        if (dwell > ThresholdComparison)     return (StateType.Comparison,      "MediumDwell",       Confidence(dwell));
        if (!Context.HasSignals)              return (StateType.Neutral,         "NoSignals",         0f);

        float dominant = Context.Signals.Values.Max();
        return (StateType.Exploration, "DefaultExploration", Confidence(dominant));
    }

    private float Confidence(float signalValue) =>
        WindowDuration > 0f ? Math.Clamp(signalValue / WindowDuration, 0f, 1f) : 0f;
}
