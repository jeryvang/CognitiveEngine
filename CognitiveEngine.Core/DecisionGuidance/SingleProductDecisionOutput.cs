using Newtonsoft.Json;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Structured single-product decision copy (P6 primary path).
/// </summary>
public sealed class SingleProductDecisionOutput
{
    [JsonProperty("schema_version", Order = 1, Required = Required.Always)]
    public string SchemaVersion { get; set; } = DecisionGuidanceSchema.StructuredOutputVersion;

    [JsonProperty("output_kind", Order = 2, Required = Required.Always)]
    public DecisionOutputKind OutputKind { get; set; } = DecisionOutputKind.SingleProduct;

    [JsonProperty("product_id", Order = 3, Required = Required.Always)]
    public string ProductId { get; set; } = "";

    [JsonProperty("what_this_gives_you", Order = 4, Required = Required.Always)]
    public string WhatThisGivesYou { get; set; } = "";

    [JsonProperty("what_you_trade_off", Order = 5, Required = Required.Always)]
    public string WhatYouTradeOff { get; set; } = "";

    [JsonProperty("behavior_context", Order = 6, DefaultValueHandling = DefaultValueHandling.Ignore)]
    public DecisionBehaviorContext? BehaviorContext { get; set; }
}
