using System;

namespace CognitiveEngine.Core;

public sealed class StreamingCognitiveEngine
{
    public sealed class Options
    {
        public float DwellDecayRate   { get; set; } = 1.5f;
        public float DwellGainRate    { get; set; } = 2.0f;

        public float ConfirmDecayRate { get; set; } = 3.0f;
        public float ConfirmGainRate  { get; set; } = 4.0f;
    }

    private const float ThresholdComparison     = 0.3f;
    private const float ThresholdHesitation     = 0.7f;
    private const float ThresholdReadyToConfirm = 0.8f;
    private const float Epsilon                 = 0.01f;

    private readonly SignalAccumulator _dwell;
    private readonly SignalAccumulator _confirm;

    private StateType _currentState = StateType.Neutral;

    public event Action<CognitiveState>? OnStateUpdated;

    public CognitiveState Current { get; private set; } =
        new CognitiveState(StateType.Neutral, 0f, "NoSignals");

    public StreamingCognitiveEngine() : this(new Options()) { }

    public StreamingCognitiveEngine(Options options)
    {
        _dwell   = new SignalAccumulator(options.DwellDecayRate,   options.DwellGainRate);
        _confirm = new SignalAccumulator(options.ConfirmDecayRate,  options.ConfirmGainRate);
    }

    public void Update(float deltaTime, float dwellInput, float confirmInput)
    {
        if (deltaTime <= 0f) return;

        _dwell.Update(deltaTime, dwellInput);
        _confirm.Update(deltaTime, confirmInput);

        var (state, rule, confidence) = EvaluateRules();

        Current = new CognitiveState(state, confidence, rule);

        if (state != _currentState)
        {
            _currentState = state;
            OnStateUpdated?.Invoke(Current);
        }
    }

    public void Reset()
    {
        _dwell.Reset();
        _confirm.Reset();
        _currentState = StateType.Neutral;
        Current       = new CognitiveState(StateType.Neutral, 0f, "NoSignals");
    }

    private (StateType state, string rule, float confidence) EvaluateRules()
    {
        float dwell   = _dwell.Value;
        float confirm = _confirm.Value;

        if (confirm > ThresholdReadyToConfirm)
            return (StateType.ReadyToConfirm, "ConfirmIntentHigh", _confirm.Normalized);

        if (dwell > ThresholdHesitation)
            return (StateType.Hesitation, "HighDwell", _dwell.Normalized);

        if (dwell > ThresholdComparison)
            return (StateType.Comparison, "MediumDwell", _dwell.Normalized);

        if (dwell < Epsilon && confirm < Epsilon)
            return (StateType.Neutral, "NoSignals", 0f);

        return (StateType.Exploration, "DefaultExploration", _dwell.Normalized);
    }
}
