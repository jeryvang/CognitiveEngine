# P5 Checkpoint 2 Completion Summary

Checkpoint 2 for Phase 5 is complete in this codebase. The work builds on Checkpoint 1 and adds decision friction: hesitation and friction with bottleneck tags, confidence interpretation, decision readiness, and richer product rollups so you can review struggle versus decision per session and across the cohort—not only preference and leaning. It is implemented and unit-tested in this repository; Unity integration is not validated on this build yet (hookup and play-mode checks are separate). Nothing here depends on you reading code. The goal remains trial-ready structured outputs, not log dumps.

---

## Completed scope

- Hesitation and friction detection derived from interaction events (selection, dwell, compare, confirm intent, and related fields) and engine state where supplied; episodes include bottleneck tags for review and analysis.
- Decision readiness signals and confidence stabilization interpretation at session level.
- Session-level struggle-versus-decision summary: classification and scores alongside the existing preference and leaning outputs from Checkpoint 1.
- Product-level aggregation extended with friction hotspots, struggle trends, and readiness trends so the product summary reflects combined sessions and shows struggle versus decision, not one session at a time and not only attraction and engagement.
- Structured JSON outputs: one record per student session and one per product per aggregation run, with schema version, UTC timestamps, and stable field names for use after the host application is wired up.

---

## Agreed scope vs fields in this repo


| Agreement (Checkpoint 2)              | Engine JSON                                                                       |
| ------------------------------------- | --------------------------------------------------------------------------------- |
| Hesitation / friction                 | Session `friction_episodes`; product `friction_hotspots`                          |
| Bottleneck tagging                    | `bottleneck_tag` on episodes and hotspots                                         |
| Confidence interpretation             | `confidence_interpretation`                                                       |
| Decision readiness                    | Session `decision_readiness`; product `readiness_trends`                          |
| Enhanced product summaries + friction | `comparison_patterns`, `friction_hotspots`, `struggle_trends`, `readiness_trends` |
| Struggle vs decision at product level | `struggle_trends`, `readiness_trends` (from session `struggle_decision_summary`)  |


---

## Trial use (once the app feeds events)


| Need                                | Answer in JSON                                                                     |
| ----------------------------------- | ---------------------------------------------------------------------------------- |
| Compare products over many sessions | Product record per `product_id` with counts and rollups.                           |
| One student’s journey               | Session record: events, preference, friction, readiness, struggle summary.         |
| Where people get stuck              | `friction_episodes` / `friction_hotspots`.                                         |
| Struggle vs ready to decide         | `struggle_decision_summary` plus product `struggle_trends` and `readiness_trends`. |


Inputs: **interaction events** (e.g. selection, compare, dwell) and optional **prior states** (exploration, comparison, hesitation). Same inputs and engine version give the same JSON.

---

## Field map (quick reference)

**Conventions:** `schema_version` (e.g. `"1.0.0"`); times UTC with `Z`; IDs often 32-char hex strings.

**Session (`SessionContract`):**


| Block                                                             | Role                                                           |
| ----------------------------------------------------------------- | -------------------------------------------------------------- |
| `interaction_signals`, `preference_signals`, `leaning_indicators` | What happened and who led.                                     |
| `friction_episodes`                                               | Stuck / hesitant spells with type and `bottleneck_tag`.        |
| `decision_readiness`, `confidence_interpretation`                 | How close to deciding; how stable confidence reads.            |
| `struggle_decision_summary`                                       | One-line struggle vs decision (scores + classification + tag). |


**Product (`ProductAggregate`):**


| Block                                             | Role                                                              |
| ------------------------------------------------- | ----------------------------------------------------------------- |
| `attraction`, `engagement`, `comparison_patterns` | Pull, time, compares (Checkpoint 1 style).                        |
| `friction_hotspots`                               | Cohort friction totals and top patterns.                          |
| `struggle_trends`, `readiness_trends`             | Struggle and readiness across sessions when this product matters. |


---

## Example JSON (shape only; values are fake)

**Session**

```json
{
  "schema_version": "1.0.0",
  "session_id": "sess-101",
  "exported_at_utc": "2025-03-24T12:00:00.0000000Z",
  "interaction_signals": [ { "signal_id": "11111111111111111111111111111111", "occurred_at_utc": "2025-03-24T12:00:01.0000000Z", "event_type": "selection", "product_id": "p-a" } ],
  "preference_signals": [ { "signal_id": "pref-a", "derived_at_utc": "2025-03-24T12:00:03.0000000Z", "product_id": "p-a", "preference_strength": 0.78, "basis": "weighted_norm_v1(attraction,engagement,comparison)" } ],
  "leaning_indicators": [ { "product_id": "p-a", "leaning_score": 0.78, "confidence": 0.9, "rank": 1 } ],
  "friction_episodes": [ { "episode_id": "ep-1", "product_id": "p-b", "started_at_utc": "2025-03-24T12:00:02.0000000Z", "ended_at_utc": "2025-03-24T12:00:03.0000000Z", "friction_kind": "hesitationBurst", "bottleneck_tag": "hesitation-on-shortlist", "event_count": 2, "total_dwell_ms": 6000 } ],
  "decision_readiness": { "readiness_score": 0.82, "readiness_level": "high", "is_ready_to_confirm": true, "dominant_product_id": "p-a", "basis": "v1(confirm,leaning,gap,friction_penalty)" },
  "confidence_interpretation": { "stability_score": 0.76, "trend": "stabilized", "interpretation": "confidence has stabilized with low volatility", "basis": "leaning_friction_proxy_v1" },
  "struggle_decision_summary": { "journey_classification": "decisive", "struggle_score": 0.18, "decision_signal_score": 0.82, "summary_tag": "decision-led" }
}
```

**Product**

```json
{
  "schema_version": "1.0.0",
  "aggregate_id": "agg-trial-uni-1",
  "exported_at_utc": "2025-03-24T12:00:00.0000000Z",
  "product_id": "p-a",
  "sessions_contributed": 12,
  "attraction": { "aggregate_attraction_score": 0.46, "selection_count": 9, "first_touch_rank": 2 },
  "engagement": { "total_dwell_ms": 32000, "focused_view_count": 11, "return_visit_count": 4 },
  "comparison_patterns": { "compare_events_count": 7, "unique_comparison_partner_product_ids": ["p-b", "p-c"], "hesitation_aligned_event_count": 3 },
  "friction_hotspots": { "total_friction_episodes_count": 5, "total_friction_event_count": 14, "total_friction_dwell_ms": 12000, "hotspots": [ { "friction_kind": "comparisonLoop", "bottleneck_tag": "comparison-loop", "episodes_count": 3, "total_event_count": 9, "total_dwell_ms": 7000 } ] },
  "struggle_trends": { "average_product_struggle_score": 0.41, "struggle_trend_direction": "improving", "journey_classification_counts": { "indecisive_count": 2, "struggling_count": 3, "balanced_count": 3, "decisive_count": 4 } },
  "readiness_trends": { "dominant_product_sessions_contributing": 10, "average_readiness_score_when_dominant": 0.69, "ready_to_confirm_sessions_count_when_dominant": 6, "readiness_level_counts_when_dominant": { "low_count": 1, "medium_count": 3, "high_count": 6 }, "readiness_trend_direction": "improving" }
}
```

---

## How to read results

Start with **attraction**, **engagement**, and **comparison** on each product; then **friction_hotspots** with **struggle_trends** and **readiness_trends**. High attraction + low struggle + improving readiness usually looks healthy; high attraction + high struggle + worsening readiness often looks like interest without commitment.

Use **session** JSON for one student; **product** JSON for the cohort.

---

*Pair with the Checkpoint 1 summary for full Phase 5 in the library. Not a substitute for Unity integration testing or app sign-off.*