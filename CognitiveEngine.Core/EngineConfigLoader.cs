using System;
using System.IO;
using System.Text.Json;

namespace CognitiveEngine.Core;

/// <summary>
/// JSON-serializable configuration model for engine parameters.
/// Omitted properties are filled from <see cref="CognitiveEngine.EngineOptions"/> defaults when loading.
/// </summary>
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
}

/// <summary>
/// Loads <see cref="CognitiveEngine.EngineOptions"/> from external configuration (e.g. JSON file or stream).
/// </summary>
public static class EngineConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Load engine options from a JSON stream.
    /// Missing properties use default values from <see cref="CognitiveEngine.EngineOptions"/>.
    /// </summary>
    public static CognitiveEngine.EngineOptions LoadFromStream(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        var config = JsonSerializer.Deserialize<EngineOptionsConfig>(stream, JsonOptions)
            ?? new EngineOptionsConfig();
        return ToEngineOptions(config);
    }

    /// <summary>
    /// Load engine options from a JSON file at the given path.
    /// </summary>
    public static CognitiveEngine.EngineOptions LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        using var stream = File.OpenRead(filePath);
        return LoadFromStream(stream);
    }

    /// <summary>
    /// Load engine options from a JSON string (e.g. for tests or in-memory config).
    /// </summary>
    public static CognitiveEngine.EngineOptions LoadFromJson(string json)
    {
        if (json == null)
            throw new ArgumentNullException(nameof(json));

        var config = JsonSerializer.Deserialize<EngineOptionsConfig>(json, JsonOptions)
            ?? new EngineOptionsConfig();
        return ToEngineOptions(config);
    }

    /// <summary>
    /// Build <see cref="CognitiveEngine.EngineOptions"/> from config, applying defaults for any null property.
    /// </summary>
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
            MaxAuditLog = config.MaxAuditLog ?? defaults.MaxAuditLog
        };
    }
}
