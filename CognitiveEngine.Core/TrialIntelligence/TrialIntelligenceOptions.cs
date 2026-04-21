using System;

namespace CognitiveEngine.Core.TrialIntelligence;

public sealed class TrialIntelligenceOptions
{
    public int FocusedViewThresholdMs { get; set; } = 2000;
    public int HesitationAlignedDwellThresholdMs { get; set; } = 5000;

    public static TrialIntelligenceOptions Default => new TrialIntelligenceOptions();

    public void ValidateOrThrow()
    {
        if (FocusedViewThresholdMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(FocusedViewThresholdMs), "must be > 0.");
        if (HesitationAlignedDwellThresholdMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(HesitationAlignedDwellThresholdMs), "must be > 0.");
    }
}

