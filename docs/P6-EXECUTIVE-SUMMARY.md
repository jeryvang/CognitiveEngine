# Phase 6 (P6) — Executive Summary

Phase 6 adds a **decision-oriented AI response layer** for **testing**. It surfaces **direct, structured guidance on screen** from system triggers. The user does **not** need to tap to reveal the core answer. The behavior is deterministic and testable.

---

## What the user sees

- A **primary decision output** appears automatically after a short delay.
- The output is **short and structured**, not conversational.
- An optional detail panel can exist, but it is **secondary**.
- The panel can suppress new outputs while it is open.
- The system shows **one output at a time**. It does not stack outputs.
- The primary output does not hard-disappear. It uses a soft fade/collapse.

---

## When it shows (strict priority)

Only one output can be active at a time. If multiple triggers are eligible, P6 always chooses:

1. **Compare**
2. **Revisit**
3. **Dwell**

Comparison is limited to the **latest two products** the user actively interacted with.
This scope is strict and deterministic.

---

## Output formats (structured only)

### Single product

- **What this gives you**
- **What you trade off**

### Comparison

- **Key difference**
- **Which to choose if…**

---

## Timing rules (default configuration)

- **Appearance delay**: ~1.0–1.5s (default 1.25s)
- **Visible time**: ~6–8s (default 7.0s)
- **Soft fade/collapse**: used instead of a hard disappearance (default 0.5s)
- **Repeat guard**: ~30–60s (default 45s) to avoid spam
- **Panel close cooldown**: a short cooldown before the next output (default 2.0s)

---

## Stop nudges when a decision is detected

In P6 **Test mode**, nudges stay consistent for testing. P6 records selection signals for measurement but does not change output eligibility based on “decision detected”.

In P6 **Full mode**, P6 can stop nudges when:

- The user taps **Select**, or
- The user stays on the product for a sustained period **after the output appears**

Nudges resume only when the decision context changes.
For example, the user moves to a different product.

---

## Reliability and edge-case behavior

- Fast product switching cancels pending outputs and resets timers.
- Repeated taps do not create duplicate outputs.
- Overlapping triggers never stack outputs.
- Panel-open state suppresses new outputs.
- The same trigger does not re-fire during the repeat guard window.
- Behavior is deterministic under identical inputs.

---

## Measurement (P6 testing goal)

P6 is a testing phase. The goal is to measure whether structured AI guidance helps users decide faster.

### Definitions (aligned for evaluation)

- **Time to decision**: from **AI output becomes visible** → user taps **Select** or **exits compare**.
- **Hesitation**: repeated switching between products, or long dwell before/after compare.
- **Compare behavior**: number of compares, time spent in compare, switching frequency during compare.

### Events to collect (simple and deterministic)

P6 emits lightweight measurement events on a logical millisecond clock.

- `OutputBecameVisible` (AI output becomes visible)
- `SelectNotified` (user taps Select; measurement-only in Test mode)
- `CompareInvoked` (user presses compare action)
- `CompareEntered` / `CompareExited` (host-defined UI compare state; measurement-only)
- `FocusChanged` (product focus changed)
- `DwellThresholdMet` (dwell threshold met for a product)
- `TriggerResolved` and `OutputEnqueued` (internal checkpoints; optional for debugging)

### Example metric formulas (host-side)

- **TTD_select** = `SelectNotified.time - OutputBecameVisible.time`
- **TTD_exit_compare** = `CompareExited.time - OutputBecameVisible.time`
- **compare_count** = count of `CompareEntered`
- **compare_time_total** = sum over `(CompareExited.time - CompareEntered.time)`
- **switch_count_in_compare** = number of `FocusChanged` events while compare is active
- **switch_frequency_in_compare** = `switch_count_in_compare / compare_time_total_seconds`
- **hesitation_switching** = A↔B oscillations (derived from `FocusChanged` sequence)
- **hesitation_dwell** = time between `FocusChanged` events (derived from timestamps)

---

## What shipped in this repo

- A tested P6 core in `CognitiveEngine.Core/DecisionGuidance/`.
- Unit and integration tests in `CognitiveEngine.Tests/`.
- A P6-only Unity sample host in `Samples/CognitiveEngineDriver.cs`.
- The work is additive. It does not refactor unrelated systems.

For the technical appendix, see `docs/P6-IMPLEMENTATION-SUMMARY.md`.

