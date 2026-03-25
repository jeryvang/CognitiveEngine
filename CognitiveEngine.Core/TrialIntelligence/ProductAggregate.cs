using System.Collections.Generic;
using Newtonsoft.Json;

namespace CognitiveEngine.Core.TrialIntelligence;

public sealed class ProductAggregate
{
    [JsonProperty("schema_version", Order = 1, Required = Required.Always)]
    public string SchemaVersion { get; set; } = Schema.CurrentVersion;

    [JsonProperty("aggregate_id", Order = 2, Required = Required.Always)]
    public string AggregateId { get; set; } = "";

    [JsonProperty("exported_at_utc", Order = 3, Required = Required.Always)]
    public string ExportedAtUtc { get; set; } = "";

    [JsonProperty("product_id", Order = 4, Required = Required.Always)]
    public string ProductId { get; set; } = "";

    [JsonProperty("sessions_contributed", Order = 5, Required = Required.Always)]
    public int SessionsContributed { get; set; }

    [JsonProperty("attraction", Order = 6, Required = Required.Always)]
    public ProductAttraction Attraction { get; set; } = new ProductAttraction();

    [JsonProperty("engagement", Order = 7, Required = Required.Always)]
    public ProductEngagement Engagement { get; set; } = new ProductEngagement();

    [JsonProperty("comparison_patterns", Order = 8, Required = Required.Always)]
    public ProductComparison ComparisonPatterns { get; set; } = new ProductComparison();
}

public sealed class ProductAttraction
{
    [JsonProperty("aggregate_attraction_score", Order = 1, Required = Required.Always)]
    public double AggregateAttractionScore { get; set; }

    [JsonProperty("selection_count", Order = 2, Required = Required.Always)]
    public int SelectionCount { get; set; }

    [JsonProperty("first_touch_rank", Order = 3)]
    public int? FirstTouchRank { get; set; }
}

public sealed class ProductEngagement
{
    [JsonProperty("total_dwell_ms", Order = 1, Required = Required.Always)]
    public long TotalDwellMs { get; set; }

    [JsonProperty("focused_view_count", Order = 2, Required = Required.Always)]
    public int FocusedViewCount { get; set; }

    [JsonProperty("return_visit_count", Order = 3, Required = Required.Always)]
    public int ReturnVisitCount { get; set; }
}

public sealed class ProductComparison
{
    [JsonProperty("compare_events_count", Order = 1, Required = Required.Always)]
    public int CompareEventsCount { get; set; }

    [JsonProperty("unique_comparison_partner_product_ids", Order = 2, Required = Required.Always)]
    public List<string> UniqueComparisonPartnerProductIds { get; set; } = new List<string>();

    [JsonProperty("hesitation_aligned_event_count", Order = 3, Required = Required.Always)]
    public int HesitationAlignedEventCount { get; set; }
}
