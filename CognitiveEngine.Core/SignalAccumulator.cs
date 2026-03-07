using System;

namespace CognitiveEngine.Core;

public sealed class SignalAccumulator
{
    private readonly float _decayRate;
    private readonly float _gainRate;
    private readonly float _ceiling;

    private float _value;

    public float Value      => _value;
    public float Normalized => _ceiling > 0f ? _value / _ceiling : 0f;

    public SignalAccumulator(float decayRate, float gainRate, float ceiling = 1f)
    {
        _decayRate = decayRate;
        _gainRate  = gainRate;
        _ceiling   = ceiling;
    }

    public void Update(float deltaTime, float input)
    {
        if (deltaTime <= 0f) return;

        _value *= MathF.Exp(-_decayRate * deltaTime);

        if (input > 0f)
            _value = MathF.Min(_value + input * _gainRate * deltaTime, _ceiling);
    }

    public void Reset() => _value = 0f;
}
