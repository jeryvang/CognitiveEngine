using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace CognitiveEngine.Core;

public sealed class SessionExportDto
{
    public string ProductId { get; set; }
    public List<TickEntryDto> Ticks { get; set; }

    public SessionExportDto()
    {
        ProductId = "";
        Ticks = new List<TickEntryDto>();
    }

    public SessionExportDto(string productId, List<TickEntryDto> ticks)
    {
        ProductId = productId ?? "";
        Ticks = ticks ?? new List<TickEntryDto>();
    }
}

public sealed class ReasoningSignalDto
{
    public string SignalType { get; set; } = "";
    public float Value { get; set; }
}

public sealed class TickEntryDto
{
    public int TickIndex { get; set; }
    public float Timestamp { get; set; }
    public Dictionary<string, float> Signals { get; set; }
    public string Rule { get; set; }
    public string State { get; set; }
    public float Confidence { get; set; }
    public string Explanation { get; set; }
    public List<ReasoningSignalDto> ReasoningSignals { get; set; }

    public TickEntryDto()
    {
        Rule = "";
        State = "";
        Explanation = "";
        Signals = new Dictionary<string, float>();
        ReasoningSignals = new List<ReasoningSignalDto>();
    }
}

public static class SessionExporter
{
    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.None,
        NullValueHandling = NullValueHandling.Ignore
    };

    public static SessionExportDto Export(string productId, IReadOnlyList<TickLog> logs)
    {
        var ticks = new List<TickEntryDto>();
        if (logs != null)
        {
            foreach (var log in logs)
            {
                var signals = new Dictionary<string, float>();
                var reasoningSignals = new List<ReasoningSignalDto>();
                if (log.Signals != null)
                {
                    foreach (var kv in log.Signals)
                    {
                        var name = kv.Key.ToString();
                        signals[name] = kv.Value;
                        reasoningSignals.Add(new ReasoningSignalDto { SignalType = name, Value = kv.Value });
                    }
                }
                ticks.Add(new TickEntryDto
                {
                    TickIndex = log.TickIndex,
                    Timestamp = log.Timestamp,
                    Signals = signals,
                    Rule = log.TriggeredRule ?? "",
                    State = log.FinalState.ToString(),
                    Confidence = log.Confidence,
                    Explanation = ExplainabilityLayer.GetExplanation(log.TriggeredRule ?? ""),
                    ReasoningSignals = reasoningSignals
                });
            }
        }
        return new SessionExportDto(productId ?? "", ticks);
    }

    public static string ToJson(SessionExportDto dto)
    {
        if (dto == null)
            return "{}";
        return JsonConvert.SerializeObject(dto, JsonSettings);
    }
}
