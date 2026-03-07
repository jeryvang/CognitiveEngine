using System;
using System.Collections.Generic;
using System.Text.Json;
using CognitiveEngine.Core;

class Program
{
    static void Main()
    {
        var engine = new CognitiveEngine.Core.CognitiveEngine();

        engine.OnStateUpdated += state =>
        {
            Console.WriteLine(
                $"STATE → {state.State} | Rule: {state.ReasoningTag}");
        };

        float time = 0f;

        Console.WriteLine("Phase 1: Build Dwell (should reach Hesitation)");
        Console.WriteLine("----------------------------------------------");

        // Build dwell gradually
        for (int i = 0; i < 30; i++)
        {
            float deltaTime = 0.1f;
            time += deltaTime;

            engine.InjectSignal(
                new InputSignal(SignalType.DwellTime, 0.1f, time));

            engine.Update(deltaTime, time);
        }

        Console.WriteLine("\nPhase 2: Inject High ConfirmIntent (should override to READY_TO_CONFIRM)");
        Console.WriteLine("-----------------------------------------------------------------------");

        // Sudden high confirm spike
        for (int i = 0; i < 10; i++)
        {
            float deltaTime = 0.1f;
            time += deltaTime;

            engine.InjectSignal(
                new InputSignal(SignalType.ConfirmIntent, 1.0f, time));

            engine.Update(deltaTime, time);
        }

        Console.WriteLine("\nDone.");

        // Generate acceptance log (JSON)
        GenerateAcceptanceLog(engine);
    }

    static void GenerateAcceptanceLog(CognitiveEngine.Core.CognitiveEngine engine)
    {
        var logEntries = new List<object>();

        foreach (var log in engine.Logs)
        {
            var entry = new
            {
                tickIndex = log.TickIndex,
                timestamp = log.Timestamp,
                triggeredRule = log.TriggeredRule,
                finalState = log.FinalState.ToString(),
                signals = new Dictionary<string, float>()
            };

            foreach (var sig in log.Signals)
            {
                ((Dictionary<string, float>)entry.signals)[sig.Key.ToString()] = sig.Value;
            }

            logEntries.Add(entry);
        }

        var acceptanceReport = new
        {
            timestamp = DateTime.UtcNow.ToString("O"),
            phase = "Milestone 1 - Acceptance Test",
            totalTicks = logEntries.Count,
            logs = logEntries
        };

        var json = JsonSerializer.Serialize(acceptanceReport, new JsonSerializerOptions { WriteIndented = true });
        string filename = $"acceptance_log_{DateTime.Now:yyyyMMdd_HHmmss}.json";

        System.IO.File.WriteAllText(filename, json);
        Console.WriteLine($"\n✓ Acceptance log saved to: {filename}");
    }
}