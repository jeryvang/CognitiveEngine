namespace CognitiveEngine.Core.TrialIntelligence;

public static class Schema
{
    public const string CurrentVersion = "1.2.0";

    public const string UtcTimestampFormatDescription =
        "ISO 8601 UTC (use DateTime.UtcNow.ToString(\"o\") or equivalent; trailing Z required for Zulu).";

    public const string SignalIdFormatDescription = "Guid string format N (32 hex, no braces), lowercase.";
}
