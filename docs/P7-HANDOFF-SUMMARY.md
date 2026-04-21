# Phase 7 (P7) — AIEngine Handoff Summary

This document is the final P7 handoff summary for copy/paste into Google Docs.

---

## P7 Scope Confirmation

P7 implementation is complete in AIEngine production code and is additive to shipped P6 behavior.

Implemented within scope:

- Deterministic, rule-based preference evaluation (no ML/probabilistic inference)
- Reuse of existing interaction signals: selection, dwell, swipe, compare, revisit
- Session-level continuity tracking only
- Context-aware output enrichment based on current session behavior patterns
- JSON output enrichment for:
  - behavior signal usage
  - preference indications
  - context-aware rationale ("why this matters now")
- Stability hardening and deterministic output shaping

Not added (by design):

- No login/account/profile system
- No cross-session user identity or memory
- No external AI service calls
- No Unity-side presentation logic implementation in this phase

---

## What Was Added (Production Runtime)

Primary P7 additions under `CognitiveEngine.Core/DecisionGuidance/`:

- `DecisionBehaviorSessionContext`
  - Session-scoped behavior signal tracking and normalization helpers
  - Tracks selection, dwell, swipe transitions, compare activity/pairs, revisit, repeated focus
- `DecisionPreferenceResult`
  - Deterministic preference outcome contract
- `DecisionPreferenceModel`
  - Deterministic leaning rules for single-product and comparison contexts
- `DecisionBehaviorRationaleBuilder`
  - Concise context rationale generation ("why this matters now")
- `DecisionBehaviorContextFactory`
  - JSON context mapping/export layer (signals + preference + rationale)
- `DecisionBehaviorContext` (and related DTO members)
  - Additive behavior context contract attached to existing outputs

Wiring updates:

- `DecisionGuidanceRuntime` now attaches `behavior_context` during output build
- Context attachment is fail-safe (best-effort): P7 enrichment cannot block core P6 output flow

---

## Preference Model Logic (Deterministic Rules)

P7 preference logic is deterministic and explainable.

### Single-product preference evaluation

Inputs (normalized where applicable):

- Selection strength
- Dwell strength
- Revisit strength
- Focus repetition strength

Weighted score determines lean confidence.

Low-signal gate:

- If total evidence is weak, output is forced to no-clear-lean fallback

Repeated-focus adjustment:

- Repeated focus can increase confidence for a single-product lean

### Comparison preference evaluation

Inputs:

- Per-product weighted scores
- Score delta between compared products
- Compare pair strength (including pair-repeat reinforcement)

Decision outcomes:

- Lean A / Lean B when winner and separation thresholds are met
- No-clear-lean when tie/ambiguous or insufficient separation

### Ambiguous and weak-signal handling

- Explicit ambiguous tie threshold
- Explicit weak-signal fallback path
- Fallback caps for confidence and normalized signal strength to avoid over-personalization

### Determinism hardening

- Consistent lexical pair ordering for compare keys
- Stable map/set ordering for per-product/per-pair counts
- Numeric rounding for exported confidence and normalized signal strength (4 decimals)

---

## Behavior Signal Utilization and Normalization

P7 reuses existing interaction events and normalizes them for deterministic runtime usage.

Signals consumed:

- Selection
- Dwell threshold events
- Swipe/navigation (focus transitions)
- Compare invocation/entered/exited
- Revisit (leave and return)

Normalized helpers include:

- Per-product normalized strengths for selection, dwell, revisit
- Pair-normalized compare strength
- Repeated-focus helpers
- Weak-signal helpers

This ensures similar behavior patterns produce consistent preference/context output across separate sessions.

---

## Session Tracking Model

Session tracking is lightweight and session-level only.

Characteristics:

- No identity model, no cross-session memory
- Session reset semantics with generation tracking
- Session-bound counters and evidence maps only
- Session boundary clears mutable evidence deterministically

---

## Context-Aware Output Behavior

P7 enriches existing P6 output payloads with behavior-aware context while preserving P6 structure.

Output personalization behavior:

- Uses current session behavior evidence to determine lean/no-clear-lean
- Adds concise rationale text for why guidance matters now
- Falls back to non-personalized/low-confidence context when evidence is weak

P6 behavior remains intact:

- Trigger priority remains `Compare > Revisit > Dwell`
- No overlap behavior changes
- Repeat/cooldown behavior unchanged
- Confirmation/suppression and reset behavior unchanged

---

## Updated / New JSON Outputs (P7 Additive Layer)

P7 does not remove or rename shipped P6 fields.
P7 adds `behavior_context` to existing output objects.

### New additive object: `behavior_context`

```json
{
  "schema_version": "p7.behavior_context.v1",
  "session_scope": "session",
  "behavior_signal_usage": {
    "selection_count": 0,
    "dwell_count": 0,
    "swipe_count": 0,
    "compare_count": 0,
    "revisit_count": 0,
    "normalized_signal_strength": 0.0
  },
  "preference_indication": {
    "leaning": "None",
    "is_ambiguous": true,
    "confidence": 0.0,
    "basis": "context_v1:weak_signal_fallback"
  },
  "why_this_matters_now": ""
}
```

### Single-product output (P6 + P7 additive)

```json
{
  "schema_version": "p6.output.v1",
  "output_kind": "SingleProduct",
  "product_id": "product_a",
  "what_this_gives_you": "Fast setup and lower upfront effort.",
  "what_you_trade_off": "Less advanced customization.",
  "behavior_context": {
    "schema_version": "p7.behavior_context.v1",
    "session_scope": "session",
    "behavior_signal_usage": {
      "selection_count": 3,
      "dwell_count": 2,
      "swipe_count": 5,
      "compare_count": 1,
      "revisit_count": 2,
      "normalized_signal_strength": 0.7425
    },
    "preference_indication": {
      "leaning": "ProductA",
      "is_ambiguous": false,
      "confidence": 0.81,
      "basis": "single_rule_v1:weighted(selection,dwell,revisit,focus)+repeated_focus_boost"
    },
    "why_this_matters_now": "You keep returning to this option and selecting it, which signals a practical fit."
  }
}
```

### Comparison output (P6 + P7 additive)

```json
{
  "schema_version": "p6.output.v1",
  "output_kind": "Comparison",
  "product_id_a": "product_a",
  "product_id_b": "product_b",
  "key_difference": "A is easier to start, B offers deeper control.",
  "which_to_choose_if": "Choose A for speed, B for configurability.",
  "behavior_context": {
    "schema_version": "p7.behavior_context.v1",
    "session_scope": "session",
    "behavior_signal_usage": {
      "selection_count": 2,
      "dwell_count": 3,
      "swipe_count": 6,
      "compare_count": 4,
      "revisit_count": 2,
      "normalized_signal_strength": 0.6883
    },
    "preference_indication": {
      "leaning": "ProductB",
      "is_ambiguous": false,
      "confidence": 0.63,
      "basis": "compare_rule_v1:weighted_delta_with_pair_strength"
    },
    "why_this_matters_now": "You revisited this comparison multiple times, so resolving the core tradeoff matters now."
  }
}
```

### Low-signal fallback example

```json
{
  "behavior_context": {
    "behavior_signal_usage": {
      "normalized_signal_strength": 0.25
    },
    "preference_indication": {
      "leaning": "None",
      "is_ambiguous": true,
      "confidence": 0.25,
      "basis": "context_v1:weak_signal_fallback"
    },
    "why_this_matters_now": "There is not enough signal yet, so this comparison highlights the key difference only."
  }
}
```

---

## Compatibility Notes (P6 Preservation)

Compatibility choice applied:

- When P7 wording is broader than shipped P6 behavior, shipped P6 runtime contract behavior is preserved.
- P7 context layer is additive only.
- Existing output contracts keep original fields and semantics.

Explicit hardening:

- P7 behavior context attachment in runtime is fail-safe and cannot block core P6 output emission.

---

## Validation Status

Validation runs succeeded with full test suite:

- `dotnet test CognitiveEngine.Tests/CognitiveEngine.Tests.csproj`
- Result: all tests passing

Focused decision-guidance validations were repeatedly executed during implementation and remained passing.

---

## Deliverables Checklist (P7 Contract)

- [x] Deterministic rule-based preference layer
- [x] Reuse of selection/dwell/swipe/compare/revisit signals
- [x] Lightweight session tracking only
- [x] Context-aware personalized output layer (session-scoped)
- [x] Brief/clear rationale ("why this matters now")
- [x] JSON output enrichment for behavior usage + preference + context
- [x] P6 compatibility preserved
- [x] Stable, lightweight, testable implementation

