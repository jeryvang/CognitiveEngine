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

    [JsonProperty("why_this_matters_now", Order = 5, Required = Required.Always)]
    public string WhyThisMattersNow { get; set; } = "";
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
}

public enum PreferenceLean
{
    None,
    ProductA,
    ProductB
}
