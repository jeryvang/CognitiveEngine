# Phase 4 – Configurable Parameters, Reasoning Explanations, and Session JSON

**Phase 4 in context.** Phase 4 delivers the **structural foundation only**: confidence refinement, explainability, structured signal export, an extendable JSON schema, signal tagging, and a confidence-trace format. It does **not** implement full Phase 5 intelligence. It is the base for validation with users and for later phases to build on.

---

## 1. Configurable parameters

Engine behavior can be adjusted via JSON configuration (e.g. loaded with `EngineConfigLoader.LoadFromFile`, `LoadFromStream`, or `LoadFromJson`). Omitted properties use the defaults below.


| Parameter                  | Type  | Default | Meaning                                                       |
| -------------------------- | ----- | ------- | ------------------------------------------------------------- |
| DecayFactor                | float | 0.9     | Multiplier applied to aggregated signals each tick (0–1).     |
| WindowDuration             | float | 2.0     | Time window in seconds; signals older than this are dropped.  |
| FixedStep                  | float | 0.1     | Simulation step duration in seconds.                          |
| MaxLogs                    | int   | 1000    | Maximum tick logs kept per session.                           |
| MaxSignalWindow            | int   | 4096    | Maximum raw signals kept in the sliding window.               |
| MaxSwitchLog               | int   | 100     | Maximum context-switch records kept.                          |
| MaxBoundaryViolationLog    | int   | 100     | Maximum boundary violation records.                           |
| MaxDeterminismViolationLog | int   | 100     | Maximum determinism violation records.                        |
| MaxAuditLog                | int   | 1000    | Maximum audit log entries.                                    |
| ConfidenceSmoothingAlpha   | float | 1.0     | Smoothing factor for confidence (0–1). 1 = no smoothing.      |
| ConfidenceDecayRate        | float | 0       | Decay rate per second when signals are absent (0 = no decay). |
| ConfidenceMinChange        | float | 0       | Minimum change in confidence to apply an update (stability).  |


Example JSON (PascalCase property names):

```json
{
  "DecayFactor": 0.9,
  "WindowDuration": 2.0,
  "FixedStep": 0.1,
  "ConfidenceSmoothingAlpha": 0.5,
  "ConfidenceDecayRate": 1.0,
  "ConfidenceMinChange": 0.05
}
```

Usage: load with `EngineConfigLoader.LoadFromJson(json)` or `LoadFromFile(path)`, then pass the returned `EngineOptions` into the `CognitiveEngine` constructor.

---

## 2. Reasoning explanation mapping

Internal rule names are mapped to short, human-readable explanations via `ExplainabilityLayer.GetExplanation(ruleName)` and are exposed on `CognitiveState.Explanation` and in session JSON per tick.


| Rule name          | Explanation                                    |
| ------------------ | ---------------------------------------------- |
| ConfirmIntentHigh  | User confirmed intent with high confidence     |
| ContextChange      | User changed context or navigated away         |
| ProductFocus       | User is focusing on a product                  |
| ComparisonAction   | User is comparing options before deciding      |
| SwipeVelocity      | Swipe or scroll indicates exploration          |
| HighDwell          | Prolonged dwell suggests hesitation            |
| MediumDwell        | Moderate dwell suggests comparison             |
| NoSignals          | No significant signals in the window           |
| DefaultExploration | Default exploration state from dominant signal |


Unknown rule names are returned unchanged.

---

## 3. Session JSON structure

Session export is produced by `CognitiveEngine.ExportSession()` (DTO) or `ExportSessionToJson()` (JSON string). Schema:

### Root object


| Field     | Type   | Description                         |
| --------- | ------ | ----------------------------------- |
| ProductId | string | Session/product context identifier. |
| Ticks     | array  | List of tick entries (see below).   |


### Tick entry (each element of `Ticks`)


| Field            | Type   | Description                                                                     |
| ---------------- | ------ | ------------------------------------------------------------------------------- |
| TickIndex        | int    | Tick index for this step.                                                       |
| Timestamp        | float  | Simulation time for this tick.                                                  |
| Signals          | object | Map of signal type name (string) to value (float).                              |
| Rule             | string | Internal rule that fired (e.g. HighDwell, ConfirmIntentHigh).                   |
| State            | string | Cognitive state (Neutral, Exploration, Comparison, Hesitation, ReadyToConfirm). |
| Confidence       | float  | Smoothed confidence for this tick (0–1).                                        |
| Explanation      | string | Human-readable explanation for why this rule/state was chosen.                  |
| ReasoningSignals | array  | List of signals that contributed to this tick.                                  |


Serialization uses Newtonsoft.Json. The export reflects the **active session only** (e.g. after a context switch, only the current product’s ticks are included).

---

## 4. Phase 5 direction (lean brand trial plan -> implementation clarity)

### Phase 4 validation (before Phase 5)

Phase 4 is validated first with around **15 university students**. After that, Phase 5 starts (small iteration is allowed if needed).

### Phase 5 – lean brand trial (validation-focused)

Phase 5 is a **lean validation-focused brand trial**:

- **Brands:** 2–3 small brands
- **Products:** ~30 total
- **Users:** 50–80 students

Goal: add lightweight intelligence modules to observe real product discovery and comparison behavior, and to capture decision signals that help explain “why” users may prefer one product over another.

Expected outputs from Phase 5 include lightweight brand insights such as:

- product attention (what users looked at)
- hesitation points (where users got stuck)
- confidence evolution during comparison (how confidence rises/falls before a decision)

### Phase 5 scope boundary (out of scope for trial)

Phase 5 is intentionally lean. The following are **not** in scope for the trial:

- No dashboard
- No admin layer
- No extra predictive logic
- No broad rewrite of existing systems

These will come in Phase 6–7 and will require additional budget when implemented.

---

### Phase 5 input event list (simple, explicit)

P5 consumes these events from Unity. Logic is grounded on this list; no other events are required for the trial.


| Event                 | Meaning                                | Key fields                        |
| --------------------- | -------------------------------------- | --------------------------------- |
| `product_view`        | User focuses on a product              | `productId`, optional `variantId` |
| `rotate`              | User explores (inspect/rotate product) | `productId`, `delta` (magnitude)  |
| `compare_enter`       | User starts comparing products         | `productId` (anchor)              |
| `compare_switch`      | User switches product during compare   | `fromProductId`, `toProductId`    |
| `variant_select`      | User selects a variant (decision step) | `productId`, `variantId`          |
| `dwell_or_inactivity` | User stays or becomes inactive         | `productId`, `durationSeconds`    |
| `session_end`         | User ends trial session                | `t` (timestamp)                   |


Event mapping (Unity → engine signals that drive P5 metrics):

- `product_view` -> `SignalType.ProductFocus` (strength `1.0`)
- `rotate` -> `SignalType.SwipeVelocity` (strength derived from `delta`)
- `compare_enter` -> `SignalType.ComparisonAction` (strength `1.0`)
- `compare_switch` -> switch engine product context to `toProductId`, then inject `SignalType.ComparisonAction` (strength `1.0`)
- `variant_select` -> `SignalType.ConfirmIntent` (strength `1.0`)
- `dwell_or_inactivity` -> `SignalType.DwellTime` (strength derived from `durationSeconds`)
- `session_end` -> export the current session JSON and finalize aggregation for this user trial

### Phase 5 event → state mapping (light definition)

Phase 5 builds on Phase 4 engine states. This is a lightweight mapping from trial events to the engine states used for metrics in session JSON.

Priority (simplified): `ReadyToConfirm` overrides other signals, then `Neutral` (context/no signals), then `Exploration`, then `Comparison`, then `Hesitation`.

- `Exploration`
  - Typical triggers: `product_view` (focus) and/or `rotate` (swipe/inspect)
  - Trial meaning: user is browsing and attention is not committed yet

- `Comparison`
  - Typical triggers: `compare_enter` / `compare_switch` (comparison action), and/or moderate `dwell_or_inactivity`
  - Trial meaning: user is weighing options in compare mode

- `Hesitation`
  - Typical triggers: high/long `dwell_or_inactivity` that dominates over other cues
  - Trial meaning: user is slowing down or getting stuck before selecting

- `ReadyToConfirm`
  - Typical triggers: `variant_select` (confirm intent)
  - Trial meaning: decision-ready confidence has been reached

- `Neutral`
  - Typical triggers: no active signals in the window, or context change recovery
  - Trial meaning: no stable intent signal is present yet

The exact confidence thresholds and timing are still handled by the Phase 4 engine; this mapping is for trial consistency and alignment.

Strength derivation uses simple normalization against a trial reference scale (example: dwell seconds and rotation delta are mapped into a 0–1 range). If Unity uses different scales, Phase 5 only needs small constant tweaks.

### Phase 5 metric definitions (simple, derived-from note)

Phase 5 metrics are per `productId` per `session`, then aggregated across sessions. Lightweight interpretation notes:


| Metric                        | Meaning                                               | Derived from                                                              |
| ----------------------------- | ----------------------------------------------------- | ------------------------------------------------------------------------- |
| **Attraction score**          | How much attention went to this product               | Product view + dwell time (time spent focusing before selecting)          |
| **Engagement depth**          | How deep the user explored and compared               | Rotate/swipe events, compare behavior, variant selections                 |
| **Friction score**            | How much the user hesitated before deciding           | Time in hesitation state, low-confidence ticks, repeated compare switches |
| **Confidence thresholds**     | Bands for interpreting engine confidence (0–1)        | Candidate ≥ 0.45, Ready ≥ 0.75, Uncertain ≤ 0.25                          |
| **Decision readiness signal** | Whether user reached a stable “ready to decide” state | Confidence hit Ready at least once, then stabilized, with low friction    |


Metric interpretation (how to read results during trial):

- Attraction and engagement reflect the user’s attention and exploration phases (`Exploration` + `Comparison`).
- Friction reflects hesitation behavior (`Hesitation`) and low-confidence ticks during attention.
- Decision readiness interprets the engine confidence trace into a clear yes/no signal.

Results stay explainable: outputs are tied to inputs (events) and engine conclusions (rule/state/confidence trace in session JSON).

### Phase 5 aggregation logic (included in Checkpoint 1 deliverable)

Phase 5 aggregates user sessions into product-level metrics for analysis.

Per-product aggregation includes:

- Viewed user count (how many users paid attention to the product)
- Decision-ready rate (how many users reached decision readiness for that product)
- Average Attraction score, Engagement depth, and Friction score across viewed sessions
- Comparison frequency per user (how often the user switched/compared this product)
- Highest confidence observed across users (best-case confidence trace)
- “Most interacted” and “Final viewed” rates

Most interacted and final viewed (per session) are selected by these simple rules:

- Most interacted: product with strongest engagement depth during the session (tie-break by higher attraction)
- Final viewed: product associated with the last `product_view` before `session_end`

Checkpoint 1 output requirement (explicit):

- Checkpoint 1 must produce a **clear, usable product-level summary output** for the trial (not session logs only).
- This summary must include per product:
  - Attraction / Engagement
  - Friction / Hesitation
  - Comparison behavior
  - Basic decision signals (including decision-ready rate)

### Sample export structures (alignment deliverables)

Phase 5 deliverables include one session-level output per trial and one aggregated product-level output per brand. These samples show the actual usable deliverable shape.

**1. Sample session-level output** (one user trial):

```json
{
  "SessionId": "S-000123",
  "BrandId": "BrandA",
  "UserId": "U-456",
  "Products": [
    {
      "ProductId": "P-101",
      "AttractionScore": 0.72,
      "EngagementDepth": 0.54,
      "FrictionScore": 0.18,
      "CandidateSeen": true,
      "ReadySeen": true,
      "ConfidenceStabilized": true,
      "HighestConfidence": 0.81,
      "DecisionReadinessValue": 0.74,
      "DecisionReady": true,
      "AttentionSeconds": 8.6,
      "RotateCount": 6,
      "CompareSwitchCount": 2,
      "HesitationSeconds": 0.4,
      "BottleneckTags": []
    },
    {
      "ProductId": "P-104",
      "AttractionScore": 0.63,
      "EngagementDepth": 0.61,
      "FrictionScore": 0.46,
      "CandidateSeen": true,
      "ReadySeen": false,
      "ConfidenceStabilized": false,
      "HighestConfidence": 0.52,
      "DecisionReadinessValue": 0.21,
      "DecisionReady": false,
      "AttentionSeconds": 7.1,
      "RotateCount": 7,
      "CompareSwitchCount": 5,
      "HesitationSeconds": 1.4,
      "BottleneckTags": ["ChoiceOverload"]
    }
  ],
  "SessionFinalization": {
    "MostInteractedProductId": "P-104",
    "FinalViewedProductId": "P-101"
  }
}
```

**2. Sample aggregated product-level output** (across many users):

```json
{
  "BrandId": "BrandA",
  "Products": [
    {
      "ProductId": "P-101",
      "ViewedUserCount": 26,
      "DecisionReadyRate": 0.31,
      "AvgAttractionScore": 0.68,
      "AvgEngagementDepth": 0.52,
      "AvgFrictionScore": 0.22,
      "ComparisonFrequencyPerUser": 1.6,
      "HighestConfidenceAcrossUsers": 0.90,
      "MostInteractedRate": 0.19,
      "FinalViewedRate": 0.23
    },
    {
      "ProductId": "P-104",
      "ViewedUserCount": 24,
      "DecisionReadyRate": 0.08,
      "AvgAttractionScore": 0.62,
      "AvgEngagementDepth": 0.60,
      "AvgFrictionScore": 0.44,
      "ComparisonFrequencyPerUser": 2.3,
      "HighestConfidenceAcrossUsers": 0.74,
      "MostInteractedRate": 0.26,
      "FinalViewedRate": 0.11
    }
  ]
}
```

### Phase 6 and Phase 7 direction (only context)

Phase 6 is the **growth stage** for deeper reasoning and stronger decision support (built on Phase 5 learnings and collected data).

Phase 7 is the **scale stage** for broader merchant intelligence and advanced AI commerce reasoning.

---

## 5. Phase 5 checkpoint packaging, estimate, and budget

Phase 5 is packaged into 2 checkpoints:

### Checkpoint 1 — Preference & Leaning Engine (+ product-level trial summary)

- Scope:
  - Product leaning detection
  - Preference signal extraction
  - Session-level output
  - Product-level aggregated trial summary output
- Estimated timeline: **7–10 working days**
- Budget: **USD $800**

### Checkpoint 2 — Decision Friction Modeling

- Scope:
  - Hesitation / friction detection
  - Bottleneck tagging
  - Confidence stabilization interpretation
  - Decision readiness components in exports
- Estimated timeline: **6–9 working days**
- Budget: **USD $1500**

Total Phase 5 estimate:

- Timeline: **13–19 working days**
- Budget: **USD $2300**