using System;
using System.IO;
using Newtonsoft.Json;

namespace CognitiveEngine.Core;

public sealed class EngineOptionsConfig
{
    public float? DecayFactor { get; set; }
    public float? WindowDuration { get; set; }
    public float? FixedStep { get; set; }
    public int? MaxLogs { get; set; }
    public int? MaxSignalWindow { get; set; }
    public int? MaxSwitchLog { get; set; }
    public int? MaxBoundaryViolationLog { get; set; }
    public int? MaxDeterminismViolationLog { get; set; }
    public int? MaxAuditLog { get; set; }
    public float? ConfidenceSmoothingAlpha { get; set; }
    public float? ConfidenceDecayRate { get; set; }
    public float? ConfidenceMinChange { get; set; }
}

public static class EngineConfigLoader
{
    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    public static CognitiveEngine.EngineOptions LoadFromStream(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return LoadFromJson(json);
    }

    public static CognitiveEngine.EngineOptions LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        using var stream = File.OpenRead(filePath);
        return LoadFromStream(stream);
    }

    public static CognitiveEngine.EngineOptions LoadFromJson(string json)
    {
        if (json == null)
            throw new ArgumentNullException(nameof(json));

        var config = JsonConvert.DeserializeObject<EngineOptionsConfig>(json, JsonSettings)
            ?? new EngineOptionsConfig();
        return ToEngineOptions(config);
    }

    public static CognitiveEngine.EngineOptions ToEngineOptions(EngineOptionsConfig config)
    {
        if (config == null)
            config = new EngineOptionsConfig();

        var defaults = new CognitiveEngine.EngineOptions();

        return new CognitiveEngine.EngineOptions
        {
            DecayFactor = config.DecayFactor ?? defaults.DecayFactor,
            WindowDuration = config.WindowDuration ?? defaults.WindowDuration,
            FixedStep = config.FixedStep ?? defaults.FixedStep,
            MaxLogs = config.MaxLogs ?? defaults.MaxLogs,
            MaxSignalWindow = config.MaxSignalWindow ?? defaults.MaxSignalWindow,
            MaxSwitchLog = config.MaxSwitchLog ?? defaults.MaxSwitchLog,
            MaxBoundaryViolationLog = config.MaxBoundaryViolationLog ?? defaults.MaxBoundaryViolationLog,
            MaxDeterminismViolationLog = config.MaxDeterminismViolationLog ?? defaults.MaxDeterminismViolationLog,
            MaxAuditLog = config.MaxAuditLog ?? defaults.MaxAuditLog,
            ConfidenceSmoothingAlpha = config.ConfidenceSmoothingAlpha ?? defaults.ConfidenceSmoothingAlpha,
            ConfidenceDecayRate = config.ConfidenceDecayRate ?? defaults.ConfidenceDecayRate,
            ConfidenceMinChange = config.ConfidenceMinChange ?? defaults.ConfidenceMinChange
        };
    }
}
