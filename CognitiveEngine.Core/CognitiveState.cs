namespace CognitiveEngine.Core;

public struct CognitiveState
{
    public StateType State { get; }
    public float Confidence { get; }
    public string ReasoningTag { get; }
    public string Explanation { get; }

    public CognitiveState(StateType state, float confidence, string reasoningTag)
    {
        State = state;
        Confidence = confidence;
        ReasoningTag = reasoningTag;
        Explanation = ExplainabilityLayer.GetExplanation(reasoningTag);
    }
}