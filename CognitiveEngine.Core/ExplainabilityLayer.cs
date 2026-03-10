using System.Collections.Generic;

namespace CognitiveEngine.Core;

public static class ExplainabilityLayer
{
    private static readonly Dictionary<string, string> Map = new Dictionary<string, string>
    {
        ["ConfirmIntentHigh"] = "User confirmed intent with high confidence",
        ["ContextChange"] = "User changed context or navigated away",
        ["ProductFocus"] = "User is focusing on a product",
        ["ComparisonAction"] = "User is comparing options before deciding",
        ["SwipeVelocity"] = "Swipe or scroll indicates exploration",
        ["HighDwell"] = "Prolonged dwell suggests hesitation",
        ["MediumDwell"] = "Moderate dwell suggests comparison",
        ["NoSignals"] = "No significant signals in the window",
        ["DefaultExploration"] = "Default exploration state from dominant signal"
    };

    public static string GetExplanation(string ruleName)
    {
        if (string.IsNullOrEmpty(ruleName))
            return "";
        return Map.TryGetValue(ruleName, out var text) ? text : ruleName;
    }
}
