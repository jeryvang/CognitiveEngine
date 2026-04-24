using System.Collections.Generic;
using Newtonsoft.Json;

namespace CognitiveEngine.Core.TrialIntelligence;

public sealed class SessionContract
{
    [JsonProperty("schema_version", Order = 1, Required = Required.Always)]
    public string SchemaVersion { get; set; } = Schema.CurrentVersion;

    [JsonProperty("session_id", Order = 2, Required = Required.Always)]
    public string SessionId { get; set; } = "";

    [JsonProperty("exported_at_utc", Order = 3, Required = Required.Always)]
    public string ExportedAtUtc { get; set; } = "";

    [JsonProperty("interaction_signals", Order = 4, Required = Required.Always)]
    public List<InteractionSignal> InteractionSignals { get; set; } = new List<InteractionSignal>();

    [JsonProperty("preference_signals", Order = 5, Required = Required.Always)]
    public List<PreferenceSignal> PreferenceSignals { get; set; } = new List<PreferenceSignal>();

    [JsonProperty("leaning_indicators", Order = 6, Required = Required.Always)]
    public List<LeaningIndicator> LeaningIndicators { get; set; } = new List<LeaningIndicator>();

    [JsonProperty("friction_episodes", Order = 7, Required = Required.Always)]
    public List<FrictionEpisode> FrictionEpisodes { get; set; } = new List<FrictionEpisode>();

    [JsonProperty("decision_readiness", Order = 8, Required = Required.Always)]
    public DecisionReadinessAssessment DecisionReadiness { get; set; } = new DecisionReadinessAssessment();

    [JsonProperty("confidence_interpretation", Order = 9, Required = Required.Always)]
    public ConfidenceInterpretation ConfidenceInterpretation { get; set; } = new ConfidenceInterpretation();

    [JsonProperty("struggle_decision_summary", Order = 10, Required = Required.Always)]
    public StruggleDecisionSummary StruggleDecisionSummary { get; set; } = new StruggleDecisionSummary();

    [JsonProperty("derived_metrics", Order = 11, Required = Required.Always)]
    public SessionDerivedMetrics DerivedMetrics { get; set; } = new SessionDerivedMetrics();
}

public sealed class InteractionSignal
{
    [JsonProperty("signal_id", Order = 1, Required = Required.Always)]
    public string SignalId { get; set; } = "";

    [JsonProperty("occurred_at_utc", Order = 2, Required = Required.Always)]
    public string OccurredAtUtc { get; set; } = "";

    [JsonProperty("event_type", Order = 3, Required = Required.Always)]
    public InteractionEventKind EventType { get; set; }

    [JsonProperty("product_id", Order = 4, Required = Required.Always)]
    public string ProductId { get; set; } = "";

    [JsonProperty("intensity", Order = 5)]
    public double? Intensity { get; set; }

    [JsonProperty("duration_ms", Order = 6)]
    public int? DurationMs { get; set; }

    // Optional partner for compare-type interactions; allows product-level compare-network analysis.
    [JsonProperty("comparison_partner_product_id", Order = 7)]
    public string? ComparisonPartnerProductId { get; set; }

    // Optional multi-partner representation for compare-type interactions that involve >1 candidates.
    [JsonProperty("comparison_partner_product_ids", Order = 8)]
    public List<string>? ComparisonPartnerProductIds { get; set; }
}

public sealed class PreferenceSignal
{
    [JsonProperty("signal_id", Order = 1, Required = Required.Always)]
    public string SignalId { get; set; } = "";

    [JsonProperty("derived_at_utc", Order = 2, Required = Required.Always)]
    public string DerivedAtUtc { get; set; } = "";

    [JsonProperty("product_id", Order = 3, Required = Required.Always)]
    public string ProductId { get; set; } = "";

    [JsonProperty("preference_strength", Order = 4, Required = Required.Always)]
    public double PreferenceStrength { get; set; }

    [JsonProperty("basis", Order = 5, Required = Required.Always)]
    public string Basis { get; set; } = "";
}

public sealed class LeaningIndicator
{
    [JsonProperty("product_id", Order = 1, Required = Required.Always)]
    public string ProductId { get; set; } = "";

    [JsonProperty("leaning_score", Order = 2, Required = Required.Always)]
    public double LeaningScore { get; set; }

    [JsonProperty("confidence", Order = 3, Required = Required.Always)]
    public double Confidence { get; set; }

    [JsonProperty("rank", Order = 4, Required = Required.Always)]
    public int Rank { get; set; }
}

public sealed class FrictionEpisode
{
    [JsonProperty("episode_id", Order = 1, Required = Required.Always)]
    public string EpisodeId { get; set; } = "";

    [JsonProperty("product_id", Order = 2, Required = Required.Always)]
    public string ProductId { get; set; } = "";

    [JsonProperty("started_at_utc", Order = 3, Required = Required.Always)]
    public string StartedAtUtc { get; set; } = "";

    [JsonProperty("ended_at_utc", Order = 4, Required = Required.Always)]
    public string EndedAtUtc { get; set; } = "";

    [JsonProperty("friction_kind", Order = 5, Required = Required.Always)]
    public FrictionKind FrictionKind { get; set; }

    [JsonProperty("bottleneck_tag", Order = 6, Required = Required.Always)]
    public string BottleneckTag { get; set; } = "";

    [JsonProperty("event_count", Order = 7, Required = Required.Always)]
    public int EventCount { get; set; }

    [JsonProperty("total_dwell_ms", Order = 8, Required = Required.Always)]
    public int TotalDwellMs { get; set; }
}

public enum FrictionKind
{
    ComparisonLoop,
    HesitationBurst,
    PostReadyBacktrack
}

public sealed class DecisionReadinessAssessment
{
    [JsonProperty("readiness_score", Order = 1, Required = Required.Always)]
    public double ReadinessScore { get; set; }

    [JsonProperty("readiness_level", Order = 2, Required = Required.Always)]
    public DecisionReadinessLevel ReadinessLevel { get; set; }

    [JsonProperty("is_ready_to_confirm", Order = 3, Required = Required.Always)]
    public bool IsReadyToConfirm { get; set; }

    [JsonProperty("dominant_product_id", Order = 4)]
    public string? DominantProductId { get; set; }

    [JsonProperty("basis", Order = 5, Required = Required.Always)]
    public string Basis { get; set; } = "";
}

public enum DecisionReadinessLevel
{
    Low,
    Medium,
    High
}

public sealed class ConfidenceInterpretation
{
    [JsonProperty("stability_score", Order = 1, Required = Required.Always)]
    public double StabilityScore { get; set; }

    [JsonProperty("trend", Order = 2, Required = Required.Always)]
    public ConfidenceTrend Trend { get; set; }

    [JsonProperty("interpretation", Order = 3, Required = Required.Always)]
    public string Interpretation { get; set; } = "";

    [JsonProperty("basis", Order = 4)]
    public string Basis { get; set; } = "";
}

public enum ConfidenceTrend
{
    Unstable,
    Improving,
    Stabilized
}

public sealed class StruggleDecisionSummary
{
    [JsonProperty("journey_classification", Order = 1, Required = Required.Always)]
    public JourneyClassification JourneyClassification { get; set; }

    [JsonProperty("struggle_score", Order = 2, Required = Required.Always)]
    public double StruggleScore { get; set; }

    [JsonProperty("decision_signal_score", Order = 3, Required = Required.Always)]
    public double DecisionSignalScore { get; set; }

    [JsonProperty("summary_tag", Order = 4, Required = Required.Always)]
    public string SummaryTag { get; set; } = "";
}

public enum JourneyClassification
{
    Indecisive,
    Struggling,
    Balanced,
    Decisive
}

public sealed class SessionDerivedMetrics
{
    [JsonProperty("switch_count", Order = 1, Required = Required.Always)]
    public int SwitchCount { get; set; }

    [JsonProperty("exploration_switch_count", Order = 2, Required = Required.Always)]
    public int ExplorationSwitchCount { get; set; }

    [JsonProperty("selection_events_count", Order = 3, Required = Required.Always)]
    public int SelectionEventsCount { get; set; }

    [JsonProperty("total_compare_time_ms", Order = 4, Required = Required.Always)]
    public long TotalCompareTimeMs { get; set; }

    [JsonProperty("compare_time_basis", Order = 5, Required = Required.Always)]
    public string CompareTimeBasis { get; set; } = "unspecified";

    [JsonProperty("final_selected_product_id", Order = 6)]
    public string? FinalSelectedProductId { get; set; }

    [JsonProperty("longest_dwell_product_id", Order = 7)]
    public string? LongestDwellProductId { get; set; }

    [JsonProperty("decision_convergence_score", Order = 8)]
    public double? DecisionConvergenceScore { get; set; }

    [JsonProperty("decision_convergence_level", Order = 9)]
    public DecisionConvergenceLevel? DecisionConvergenceLevel { get; set; }

    [JsonProperty("decision_convergence_basis", Order = 10)]
    public string? DecisionConvergenceBasis { get; set; }
}

public enum DecisionConvergenceLevel
{
    Low,
    Medium,
    High
}
