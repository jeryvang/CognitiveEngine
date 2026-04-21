using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CognitiveEngine.Core;

namespace CognitiveEngine.Core.TrialIntelligence;

public static class FrictionDetectionEngine
{
    private const int MinCompareEventsForLoop = 3;
    private const int MinHesitationDwellMsForBurst = 3000;

    public static List<FrictionEpisode> DetectFrictionEpisodes(
        string sessionId,
        IEnumerable<InteractionSignal> interactionSignals,
        IEnumerable<(string occurredAtUtc, StateType state)> stateTimeline)
    {
        if (sessionId == null) throw new ArgumentNullException(nameof(sessionId));
        if (interactionSignals == null) throw new ArgumentNullException(nameof(interactionSignals));
        if (stateTimeline == null) throw new ArgumentNullException(nameof(stateTimeline));

        var signals = interactionSignals
            .Where(s => s != null)
            .OrderBy(s => s.OccurredAtUtc, StringComparer.Ordinal)
            .ThenBy(s => s.ProductId, StringComparer.Ordinal)
            .ThenBy(s => s.SignalId, StringComparer.Ordinal)
            .ToList();

        var states = stateTimeline
            .OrderBy(s => s.occurredAtUtc, StringComparer.Ordinal)
            .ThenBy(s => s.state)
            .ToList();

        var episodes = new List<FrictionEpisode>();

        DetectComparisonLoops(sessionId, signals, states, episodes);
        DetectHesitationBursts(sessionId, signals, states, episodes);
        DetectPostReadyBacktrack(sessionId, signals, states, episodes);

        return episodes
            .OrderBy(e => e.StartedAtUtc, StringComparer.Ordinal)
            .ThenBy(e => e.ProductId, StringComparer.Ordinal)
            .ThenBy(e => e.EpisodeId, StringComparer.Ordinal)
            .ToList();
    }

    private static void DetectComparisonLoops(
        string sessionId,
        List<InteractionSignal> signals,
        List<(string occurredAtUtc, StateType state)> states,
        List<FrictionEpisode> output)
    {
        if (signals.Count == 0 || states.Count == 0)
            return;

        var compareSignals = signals
            .Where(s => s.EventType == InteractionEventKind.Compare)
            .ToList();
        if (compareSignals.Count < MinCompareEventsForLoop)
            return;

        var byProduct = compareSignals
            .GroupBy(s => s.ProductId, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        foreach (var g in byProduct)
        {
            var ordered = g
                .OrderBy(s => s.OccurredAtUtc, StringComparer.Ordinal)
                .ThenBy(s => s.SignalId, StringComparer.Ordinal)
                .ToList();

            var first = ordered.First();
            var last = ordered.Last();

            var windowStates = states
                .Where(s => string.CompareOrdinal(s.occurredAtUtc, first.OccurredAtUtc) >= 0 &&
                            string.CompareOrdinal(s.occurredAtUtc, last.OccurredAtUtc) <= 0)
                .Select(s => s.state)
                .ToList();

            if (!windowStates.Contains(StateType.Comparison) || windowStates.Contains(StateType.ReadyToConfirm))
                continue;

            var partners = new HashSet<string>(StringComparer.Ordinal);
            foreach (var s in ordered)
            {
                if (!string.IsNullOrWhiteSpace(s.ComparisonPartnerProductId))
                    partners.Add(s.ComparisonPartnerProductId);
                if (s.ComparisonPartnerProductIds != null)
                {
                    foreach (var id in s.ComparisonPartnerProductIds)
                    {
                        if (!string.IsNullOrWhiteSpace(id) && !string.Equals(id, g.Key, StringComparison.Ordinal))
                            partners.Add(id);
                    }
                }
            }

            if (partners.Count == 0)
                continue;

            int dwellMs = signals
                .Where(s => s.EventType == InteractionEventKind.Dwell &&
                            string.Equals(s.ProductId, g.Key, StringComparison.Ordinal) &&
                            string.CompareOrdinal(s.OccurredAtUtc, first.OccurredAtUtc) >= 0 &&
                            string.CompareOrdinal(s.OccurredAtUtc, last.OccurredAtUtc) <= 0)
                .Select(s => s.DurationMs ?? 0)
                .Sum();

            var episode = new FrictionEpisode
            {
                EpisodeId = DeterministicEpisodeId(sessionId, g.Key, first.OccurredAtUtc, last.OccurredAtUtc, "comparison_loop"),
                ProductId = g.Key,
                StartedAtUtc = first.OccurredAtUtc,
                EndedAtUtc = last.OccurredAtUtc,
                FrictionKind = FrictionKind.ComparisonLoop,
                BottleneckTag = "comparison-loop",
                EventCount = ordered.Count,
                TotalDwellMs = dwellMs
            };

            output.Add(episode);
        }
    }

    private static void DetectHesitationBursts(
        string sessionId,
        List<InteractionSignal> signals,
        List<(string occurredAtUtc, StateType state)> states,
        List<FrictionEpisode> output)
    {
        if (signals.Count == 0 || states.Count == 0)
            return;

        var hesitationSpans = states
            .Where(s => s.state == StateType.Hesitation)
            .Select(s => s.occurredAtUtc)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        if (hesitationSpans.Count == 0)
            return;

        string start = hesitationSpans.First();
        string end = hesitationSpans.Last();

        var dwellByProduct = signals
            .Where(s => s.EventType == InteractionEventKind.Dwell &&
                        string.CompareOrdinal(s.OccurredAtUtc, start) >= 0 &&
                        string.CompareOrdinal(s.OccurredAtUtc, end) <= 0)
            .GroupBy(s => s.ProductId, StringComparer.Ordinal)
            .Select(g => new
            {
                ProductId = g.Key,
                Total = g.Select(x => x.DurationMs ?? 0).Sum()
            })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.ProductId, StringComparer.Ordinal)
            .ToList();

        if (dwellByProduct.Count == 0)
            return;

        var top = dwellByProduct[0];
        if (top.Total < MinHesitationDwellMsForBurst)
            return;

        var episode = new FrictionEpisode
        {
            EpisodeId = DeterministicEpisodeId(sessionId, top.ProductId, start, end, "hesitation_burst"),
            ProductId = top.ProductId,
            StartedAtUtc = start,
            EndedAtUtc = end,
            FrictionKind = FrictionKind.HesitationBurst,
            BottleneckTag = "hesitation-on-shortlist",
            EventCount = signals.Count(s =>
                s.EventType == InteractionEventKind.Dwell &&
                string.Equals(s.ProductId, top.ProductId, StringComparison.Ordinal) &&
                string.CompareOrdinal(s.OccurredAtUtc, start) >= 0 &&
                string.CompareOrdinal(s.OccurredAtUtc, end) <= 0),
            TotalDwellMs = top.Total
        };

        output.Add(episode);
    }

    private static void DetectPostReadyBacktrack(
        string sessionId,
        List<InteractionSignal> signals,
        List<(string occurredAtUtc, StateType state)> states,
        List<FrictionEpisode> output)
    {
        int readyIndex = states.FindIndex(s => s.state == StateType.ReadyToConfirm);
        if (readyIndex < 0 || readyIndex >= states.Count - 1)
            return;

        var backtrackState = states
            .Skip(readyIndex + 1)
            .FirstOrDefault(s => s.state == StateType.Comparison || s.state == StateType.Hesitation);

        if (string.IsNullOrWhiteSpace(backtrackState.occurredAtUtc))
            return;

        var tailSignals = signals
            .Where(s => string.CompareOrdinal(s.OccurredAtUtc, backtrackState.occurredAtUtc) >= 0)
            .ToList();
        if (tailSignals.Count == 0)
            return;

        var evidenceSignals = tailSignals
            .Where(s => s.EventType == InteractionEventKind.Compare || s.EventType == InteractionEventKind.Dwell)
            .ToList();
        if (evidenceSignals.Count == 0)
            return;

        var product = evidenceSignals
            .GroupBy(s => s.ProductId, StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => g.Key)
            .FirstOrDefault() ?? "";

        int dwellMs = evidenceSignals
            .Where(s => s.EventType == InteractionEventKind.Dwell)
            .Select(s => s.DurationMs ?? 0)
            .Sum();

        output.Add(new FrictionEpisode
        {
            EpisodeId = DeterministicEpisodeId(
                sessionId,
                product,
                backtrackState.occurredAtUtc,
                evidenceSignals[^1].OccurredAtUtc,
                "post_ready_backtrack"),
            ProductId = product,
            StartedAtUtc = backtrackState.occurredAtUtc,
            EndedAtUtc = evidenceSignals[^1].OccurredAtUtc,
            FrictionKind = FrictionKind.PostReadyBacktrack,
            BottleneckTag = "post-ready-backtrack",
            EventCount = evidenceSignals.Count,
            TotalDwellMs = dwellMs
        });
    }

    private static string DeterministicEpisodeId(
        string sessionId,
        string productId,
        string startUtc,
        string endUtc,
        string kindKey)
    {
        var input = string.Format(
            CultureInfo.InvariantCulture,
            "{0}|{1}|{2}|{3}|{4}|{5}",
            sessionId,
            productId,
            startUtc,
            endUtc,
            kindKey,
            Schema.CurrentVersion);

        using var md5 = System.Security.Cryptography.MD5.Create();
        byte[] bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(bytes).ToString("N").ToLowerInvariant();
    }
}

