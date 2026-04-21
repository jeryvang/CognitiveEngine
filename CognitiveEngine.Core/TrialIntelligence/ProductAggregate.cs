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

    // Compatibility note: additive fields for Phase 5 trial-analysis export (Unity 2022).
    [JsonProperty("friction_hotspots", Order = 9, Required = Required.Always)]
    public ProductFrictionHotspots FrictionHotspots { get; set; } = new ProductFrictionHotspots();

    [JsonProperty("struggle_trends", Order = 10, Required = Required.Always)]
    public ProductStruggleTrends StruggleTrends { get; set; } = new ProductStruggleTrends();

    [JsonProperty("readiness_trends", Order = 11, Required = Required.Always)]
    public ProductReadinessTrends ReadinessTrends { get; set; } = new ProductReadinessTrends();
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

public sealed class ProductFrictionHotspots
{
    [JsonProperty("total_friction_episodes_count", Order = 1, Required = Required.Always)]
    public int TotalFrictionEpisodesCount { get; set; }

    [JsonProperty("total_friction_event_count", Order = 2, Required = Required.Always)]
    public int TotalFrictionEventCount { get; set; }

    [JsonProperty("total_friction_dwell_ms", Order = 3, Required = Required.Always)]
    public long TotalFrictionDwellMs { get; set; }

    [JsonProperty("hotspots", Order = 4, Required = Required.Always)]
    public List<ProductFrictionHotspot> Hotspots { get; set; } = new List<ProductFrictionHotspot>();
}

public sealed class ProductFrictionHotspot
{
    [JsonProperty("friction_kind", Order = 1, Required = Required.Always)]
    public FrictionKind FrictionKind { get; set; }

    [JsonProperty("bottleneck_tag", Order = 2, Required = Required.Always)]
    public string BottleneckTag { get; set; } = "";

    [JsonProperty("episodes_count", Order = 3, Required = Required.Always)]
    public int EpisodesCount { get; set; }

    [JsonProperty("total_event_count", Order = 4, Required = Required.Always)]
    public int TotalEventCount { get; set; }

    [JsonProperty("total_dwell_ms", Order = 5, Required = Required.Always)]
    public long TotalDwellMs { get; set; }
}

public enum ProductTrendDirection
{
    Worsening,
    Stable,
    Improving
}

public sealed class ProductStruggleTrends
{
    [JsonProperty("average_product_struggle_score", Order = 1, Required = Required.Always)]
    public double AverageProductStruggleScore { get; set; }

    [JsonProperty("struggle_trend_direction", Order = 2, Required = Required.Always)]
    public ProductTrendDirection StruggleTrendDirection { get; set; }

    [JsonProperty("journey_classification_counts", Order = 3, Required = Required.Always)]
    public ProductJourneyClassificationCounts JourneyClassificationCounts { get; set; } = new ProductJourneyClassificationCounts();
}

public sealed class ProductJourneyClassificationCounts
{
    [JsonProperty("indecisive_count", Order = 1, Required = Required.Always)]
    public int IndecisiveCount { get; set; }

    [JsonProperty("struggling_count", Order = 2, Required = Required.Always)]
    public int StrugglingCount { get; set; }

    [JsonProperty("balanced_count", Order = 3, Required = Required.Always)]
    public int BalancedCount { get; set; }

    [JsonProperty("decisive_count", Order = 4, Required = Required.Always)]
    public int DecisiveCount { get; set; }
}

public sealed class ProductReadinessTrends
{
    [JsonProperty("dominant_product_sessions_contributing", Order = 1, Required = Required.Always)]
    public int DominantProductSessionsContributing { get; set; }

    [JsonProperty("average_readiness_score_when_dominant", Order = 2, Required = Required.Always)]
    public double AverageReadinessScoreWhenDominant { get; set; }

    [JsonProperty("ready_to_confirm_sessions_count_when_dominant", Order = 3, Required = Required.Always)]
    public int ReadyToConfirmSessionsCountWhenDominant { get; set; }

    [JsonProperty("readiness_level_counts_when_dominant", Order = 4, Required = Required.Always)]
    public ProductReadinessLevelCounts ReadinessLevelCountsWhenDominant { get; set; } = new ProductReadinessLevelCounts();

    [JsonProperty("readiness_trend_direction", Order = 5, Required = Required.Always)]
    public ProductTrendDirection ReadinessTrendDirection { get; set; }
}

public sealed class ProductReadinessLevelCounts
{
    [JsonProperty("low_count", Order = 1, Required = Required.Always)]
    public int LowCount { get; set; }

    [JsonProperty("medium_count", Order = 2, Required = Required.Always)]
    public int MediumCount { get; set; }

    [JsonProperty("high_count", Order = 3, Required = Required.Always)]
    public int HighCount { get; set; }
}
