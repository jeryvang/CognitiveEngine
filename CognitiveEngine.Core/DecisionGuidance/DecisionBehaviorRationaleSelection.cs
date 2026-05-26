using System;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Result of resolving a P7 rationale branch: stable key, short behavior phrase, and full sentence.
/// </summary>
/// <remarks>
/// Hosts compose the final AI Insights line as <c>Phrase + " " + catalog_framing</c> when catalog
/// framing is available, falling back to <c>Sentence</c> otherwise. <c>Key</c> is used for
/// localization tables, LLM input, or analytics — it never changes for a given branch.
/// </remarks>
public sealed class DecisionBehaviorRationaleSelection
{
    public string Key { get; }

    public string Phrase { get; }

    public string Sentence { get; }

    public DecisionBehaviorRationaleSelection(string key, string phrase, string sentence)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("key is required.", nameof(key));
        if (string.IsNullOrWhiteSpace(phrase))
            throw new ArgumentException("phrase is required.", nameof(phrase));
        if (string.IsNullOrWhiteSpace(sentence))
            throw new ArgumentException("sentence is required.", nameof(sentence));

        Key = key;
        Phrase = phrase;
        Sentence = sentence;
    }
}
