# P5 Checkpoint 1 Completion Summary

Checkpoint 1 for Phase 5 is complete. The work turns raw trial interactions into structured outputs you can use for review and later analysis: first per student session, then rolled up per product across all sessions in a run. Nothing here depends on you reading code. The goal was trial-ready signals, not log dumps.

---

## Completed scope

- Preference signals derived from interaction events (selection, dwell, compare, confirm intent, and related fields).
- Product leaning within a session: which products the session favors and in what order.
- Session-level export: one bundle per session with normalized signals plus derived preference and leaning.
- Product-level aggregation: one summary per product, combining all sessions that touched that product.
- Attraction, engagement, and comparison-style patterns included in the product summary so you see combined behavior, not one session at a time.

---

## Output structure

**Session-level output**  
One record per session. It includes the interaction timeline (cleaned and ordered), derived preference strength per product, and a ranked leaning list (who is ahead in that session and how strong the overall read is).

**Product-level aggregated output**  
One record per product in the trial. It rolls up every session that included that product. You get counts and scores that reflect the whole cohort, not a single student.

---

## How to read the main metrics (plain English)

- **Attraction**  
  How strongly the product shows up as a preferred choice in the aggregated data: selections, confirmations, and the derived score we use to summarize pull toward that product.

- **Engagement**  
  How much time and attention the product got: total dwell, focused views (longer looks), and return visits when students come back to the same product within a session.

- **Comparison behavior**  
  How often the product was used in compare-style flows, who it was compared against, and how often long dwells line up with hesitation-style thresholds (useful for “stuck comparing” style reads).

- **Leaning / preference**  
  At session level, leaning is the ranked “who is winning” view with a simple confidence-style read for how strong the session-level preference picture is. At product level, you rely more on the aggregated attraction and engagement blocks for cohort-level conclusions.

---

## Example outputs (minimal)

**Session (conceptual)**  
Session `S-202` at export time `2025-03-24T12:00:00Z`:  
Interactions: e.g. dwell on A, compare A vs B, selection on A.  
Preference: A stronger than B.  
Leaning: rank 1 A, rank 2 B, with a single session confidence on that ranking.

**Session (tiny JSON shape)**  
```json
{
  "session_id": "S-202",
  "interaction_signals": [ "ordered events with product_id and times" ],
  "preference_signals": [ { "product_id": "A", "preference_strength": 0.72 } ],
  "leaning_indicators": [ { "product_id": "A", "rank": 1, "leaning_score": 0.72 } ]
}
```

**Product (conceptual)**  
Product `A` across 12 sessions:  
Sessions contributed: 8.  
Attraction: higher aggregate score, several selections.  
Engagement: solid total dwell, a few return visits.  
Comparison: repeated compares vs B and C, partners listed.

**Product (tiny JSON shape)**  
```json
{
  "product_id": "A",
  "sessions_contributed": 8,
  "attraction": { "aggregate_attraction_score": 0.41, "selection_count": 5 },
  "engagement": { "total_dwell_ms": 9000, "focused_view_count": 4, "return_visit_count": 1 },
  "comparison_patterns": { "compare_events_count": 2, "unique_comparison_partner_product_ids": ["B", "C"] }
}
```

Real exports use the same fields with a version tag and full timestamps so you can file them and re-run analysis later.

---

## Current status

Checkpoint 1 is ready for your alignment pass and for hookup testing on the Unity side. If you want to adjust labels or add one field for your reporting workflow, we can do that as a small follow-up before you lock the trial export format.
