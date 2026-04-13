using System;
using Newtonsoft.Json;

namespace CognitiveEngine.Core.DecisionGuidance;

/// <summary>
/// Host-serializable timing and interaction parameters for the P6 decision-response layer.
/// Defaults sit in the recommended bands (appearance ~1.0–1.5s, visible ~6–8s, repeat guard ~30–60s).
/// </summary>
public sealed class DecisionGuidanceConfig
{
    public const int DefaultAppearanceDelayMs = 1250;

    public const int DefaultPrimaryVisibleMs = 7000;

    public const int DefaultSoftFadeOrCollapseMs = 500;

    public const int DefaultRepeatGuardMs = 45000;

    public const int DefaultSustainedStayAfterOutputMs = 4000;

    public const int DefaultPanelCloseCooldownMs = 2000;

    public const int DefaultExpandDuplicateTapIgnoreMs = 300;

    [JsonProperty("schema_version", Order = 1, Required = Required.Always)]
    public string SchemaVersion { get; set; } = DecisionGuidanceSchema.ConfigVersion;

    /// <summary>Delay after trigger conditions are met before showing primary output (~1.0–1.5s).</summary>
    [JsonProperty("appearance_delay_ms", Order = 2, Required = Required.Always)]
    public int AppearanceDelayMs { get; set; } = DefaultAppearanceDelayMs;

    /// <summary>How long primary output stays fully visible before soft fade/collapse (~6–8s).</summary>
    [JsonProperty("primary_visible_ms", Order = 3, Required = Required.Always)]
    public int PrimaryVisibleMs { get; set; } = DefaultPrimaryVisibleMs;

    /// <summary>Duration of soft fade or collapse (not an instant hide).</summary>
    [JsonProperty("soft_fade_or_collapse_ms", Order = 4, Required = Required.Always)]
    public int SoftFadeOrCollapseMs { get; set; } = DefaultSoftFadeOrCollapseMs;

    /// <summary>Same trigger type should not fire again inside this window (~30–60s).</summary>
    [JsonProperty("repeat_guard_ms", Order = 5, Required = Required.Always)]
    public int RepeatGuardMs { get; set; } = DefaultRepeatGuardMs;

    /// <summary>
    /// After output is shown, staying on the same product for this long counts as decision confirmation.
    /// </summary>
    [JsonProperty("sustained_stay_after_output_ms", Order = 6, Required = Required.Always)]
    public int SustainedStayAfterOutputMs { get; set; } = DefaultSustainedStayAfterOutputMs;

    /// <summary>Minimum time after optional panel closes before a new output may appear.</summary>
    [JsonProperty("panel_close_cooldown_ms", Order = 7, Required = Required.Always)]
    public int PanelCloseCooldownMs { get; set; } = DefaultPanelCloseCooldownMs;

    /// <summary>Ignore duplicate expand taps within this window (e.g. 300ms).</summary>
    [JsonProperty("expand_duplicate_tap_ignore_ms", Order = 8, Required = Required.Always)]
    public int ExpandDuplicateTapIgnoreMs { get; set; } = DefaultExpandDuplicateTapIgnoreMs;

    public static DecisionGuidanceConfig CreateDefault() => new();

    public void ValidateOrThrow()
    {
        void NonNegative(string name, int v)
        {
            if (v < 0)
                throw new ArgumentOutOfRangeException(name, v, $"{name} must be non-negative.");
        }

        NonNegative(nameof(AppearanceDelayMs), AppearanceDelayMs);
        NonNegative(nameof(PrimaryVisibleMs), PrimaryVisibleMs);
        NonNegative(nameof(SoftFadeOrCollapseMs), SoftFadeOrCollapseMs);
        NonNegative(nameof(RepeatGuardMs), RepeatGuardMs);
        NonNegative(nameof(SustainedStayAfterOutputMs), SustainedStayAfterOutputMs);
        NonNegative(nameof(PanelCloseCooldownMs), PanelCloseCooldownMs);
        NonNegative(nameof(ExpandDuplicateTapIgnoreMs), ExpandDuplicateTapIgnoreMs);

        if (string.IsNullOrWhiteSpace(SchemaVersion))
            throw new ArgumentException("schema_version is required.", nameof(SchemaVersion));
    }
}
