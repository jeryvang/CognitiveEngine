using Newtonsoft.Json;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Structured comparison decision copy for the latest two actively interacted products (P6).
/// </summary>
public sealed class ComparisonDecisionOutput
{
    [JsonProperty("schema_version", Order = 1, Required = Required.Always)]
    public string SchemaVersion { get; set; } = DecisionGuidanceSchema.StructuredOutputVersion;

    [JsonProperty("output_kind", Order = 2, Required = Required.Always)]
    public DecisionOutputKind OutputKind { get; set; } = DecisionOutputKind.Comparison;

    [JsonProperty("product_id_a", Order = 3, Required = Required.Always)]
    public string ProductIdA { get; set; } = "";

    [JsonProperty("product_id_b", Order = 4, Required = Required.Always)]
    public string ProductIdB { get; set; } = "";

    [JsonProperty("key_difference", Order = 5, Required = Required.Always)]
    public string KeyDifference { get; set; } = "";

    [JsonProperty("which_to_choose_if", Order = 6, Required = Required.Always)]
    public string WhichToChooseIf { get; set; } = "";
}
