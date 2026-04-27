using System.Collections.Generic;
using Newtonsoft.Json;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Additive P7 context layer for behavior-aware decision outputs.
/// Optional in JSON to preserve backward compatibility with P6-only payloads.
/// </summary>
public sealed class DecisionBehaviorContext
{
    [JsonProperty("schema_version", Order = 1, Required = Required.Always)]
    public string SchemaVersion { get; set; } = DecisionGuidanceSchema.BehaviorContextVersion;

    [JsonProperty("session_scope", Order = 2, Required = Required.Always)]
    public string SessionScope { get; set; } = "session";

    [JsonProperty("behavior_signal_usage", Order = 3, Required = Required.Always)]
    public BehaviorSignalUsage BehaviorSignalUsage { get; set; } = new BehaviorSignalUsage();

    [JsonProperty("preference_indication", Order = 4, Required = Required.Always)]
    public PreferenceIndication PreferenceIndication { get; set; } = new PreferenceIndication();

    [JsonProperty("decision_convergence", Order = 5, Required = Required.Always)]
    public DecisionConvergenceSignal DecisionConvergence { get; set; } = new DecisionConvergenceSignal();

    [JsonProperty("guidance_disposition", Order = 6, Required = Required.Always)]
    public GuidanceDisposition GuidanceDisposition { get; set; } = GuidanceDisposition.Neutral;

    [JsonProperty("guidance_disposition_basis", Order = 7, Required = Required.Always)]
    public string GuidanceDispositionBasis { get; set; } = "p7_disposition_v1(confidence,convergence,ambiguity)";

    [JsonProperty("why_this_matters_now", Order = 8, Required = Required.Always)]
    public string WhyThisMattersNow { get; set; } = "";

    [JsonProperty("rationale_policy", Order = 9, Required = Required.Always)]
    public string RationalePolicy { get; set; } = "repeat_guard_aligned_v1";
}

public sealed class BehaviorSignalUsage
{
    [JsonProperty("selection_count", Order = 1, Required = Required.Always)]
    public int SelectionCount { get; set; }

    [JsonProperty("dwell_count", Order = 2, Required = Required.Always)]
    public int DwellCount { get; set; }

    [JsonProperty("swipe_count", Order = 3, Required = Required.Always)]
    public int SwipeCount { get; set; }

    [JsonProperty("compare_count", Order = 4, Required = Required.Always)]
    public int CompareCount { get; set; }

    [JsonProperty("revisit_count", Order = 5, Required = Required.Always)]
    public int RevisitCount { get; set; }

    [JsonProperty("normalized_signal_strength", Order = 6, Required = Required.Always)]
    public double NormalizedSignalStrength { get; set; }
}

public sealed class PreferenceIndication
{
    [JsonProperty("leaning", Order = 1, Required = Required.Always)]
    public PreferenceLean Leaning { get; set; } = PreferenceLean.None;

    [JsonProperty("is_ambiguous", Order = 2, Required = Required.Always)]
    public bool IsAmbiguous { get; set; }

    [JsonProperty("confidence", Order = 3, Required = Required.Always)]
    public double Confidence { get; set; }

    [JsonProperty("basis", Order = 4, Required = Required.Always)]
    public string Basis { get; set; } = "";
}

public enum PreferenceLean
{
    None,
    ProductA,
    ProductB
}

public sealed class DecisionConvergenceSignal
{
    [JsonProperty("score", Order = 1, Required = Required.Always)]
    public double Score { get; set; }

    [JsonProperty("level", Order = 2, Required = Required.Always)]
    public DecisionConvergenceLevel Level { get; set; } = DecisionConvergenceLevel.Low;

    [JsonProperty("trend", Order = 3, Required = Required.Always)]
    public DecisionConvergenceTrend Trend { get; set; } = DecisionConvergenceTrend.Flat;

    [JsonProperty("basis", Order = 4, Required = Required.Always)]
    public string Basis { get; set; } = "convergence_v1(selection,compare,dwell,swipe,revisit)";
}

public enum DecisionConvergenceLevel
{
    Low,
    Medium,
    High
}

public enum DecisionConvergenceTrend
{
    Improving,
    Flat,
    Declining
}

public enum GuidanceDisposition
{
    Neutral,
    Light,
    Strong
}
