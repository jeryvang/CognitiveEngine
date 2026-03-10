using System;

namespace CognitiveEngine.Core;

public static class ConfidenceModel
{
    private const float NoSignalThreshold = 0.01f;

    public static float Update(
        float previousSmoothed,
        float rawConfidence,
        float deltaTime,
        float smoothingAlpha,
        float decayRate,
        float minChange)
    {
        if (deltaTime <= 0f)
            return previousSmoothed;

        float candidate;
        if (rawConfidence < NoSignalThreshold && decayRate > 0f)
            candidate = previousSmoothed * MathF.Exp(-decayRate * deltaTime);
        else
            candidate = smoothingAlpha * rawConfidence + (1f - smoothingAlpha) * previousSmoothed;

        candidate = Math.Clamp(candidate, 0f, 1f);
        if (minChange > 0f && Math.Abs(candidate - previousSmoothed) < minChange)
            return previousSmoothed;
        return candidate;
    }
}
