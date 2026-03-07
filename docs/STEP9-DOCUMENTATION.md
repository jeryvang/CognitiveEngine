# Step 9: Documentation — One Google Doc (Architecture & guarantees + Integration & QA)

Step 9 uses a **single Google Doc** as the canonical M2 documentation. That doc contains **both** Architecture & guarantees **and** Integration & QA. The repo only keeps a pointer and link.

---

## What Step 9 Covers

| Content | Location |
|---------|----------|
| **Architecture & guarantees** | Google Doc (section 1) |
| **Integration & QA** | Google Doc (section 2) |
| **Pointer + link** | `docs/ARCHITECTURE.md` (repo) |

Everything below lives in the one Google Doc.

---

## Step-by-step completion

### Step 9.1 — Create the Google Doc and add “Architecture & guarantees”

Create one Google Doc. Add a first section **Architecture & guarantees** with:

1. **Engine state summary**
   - **Public API table** — All members: `CognitiveEngine`, `InjectSignal` (with/without productId), `Update` (with/without productId), `ResetSession`, `HandleProductContextSwitch`, `ActiveProductId`, `Logs`, `SwitchLog`, `BoundaryViolationLog`, `DeterminismViolationLog`, `AuditLog`, `OnStateUpdated`, `OnBoundaryViolation`, `OnDeterminismViolation`, `OnAuditEntry`.
   - **EngineOptions table** — DecayFactor, WindowDuration, FixedStep, MaxLogs, MaxSignalWindow, MaxSwitchLog, MaxBoundaryViolationLog, MaxDeterminismViolationLog, MaxAuditLog (default + short description).
   - **StateType** — Neutral → Exploration → Comparison → Hesitation → ReadyToConfirm. Rule priority: ConfirmIntent > ContextChange (Neutral) > ProductFocus (Exploration) > ComparisonAction (Comparison) > SwipeVelocity (Exploration) > HighDwell (Hesitation) > MediumDwell (Comparison) > NoSignals (Neutral) > DefaultExploration.
   - **SignalType** — ProductFocus, DwellTime, SwipeVelocity, ComparisonAction, ConfirmIntent, ContextChange.

2. **Memory boundaries**
   - One session per product; no cross-product leakage.
   - Context switch: `HandleProductContextSwitch` resets incoming session; SwitchLog and AuditLog.
   - Boundary enforcement: Inject/Update with productId throw on mismatch; BoundaryViolationLog and OnBoundaryViolation.

3. **Deterministic guarantees**
   - No randomness; fixed-step scheduler; frozen snapshot per tick; ordered rule cascade (ConfirmIntent > ContextChange > ProductFocus > ComparisonAction > SwipeVelocity > Dwell > NoSignals > DefaultExploration); integrity validator (rule↔state, transitions, flip-flop); DeterminismViolationLog and OnDeterminismViolation.

4. **Stress testing**
   - Unity-side tests; reference to `Samples/README.md` Step 7. Three tests: conflicting signals, rapid burst, near-threshold oscillation.

5. **Milestone 2 checklist (reference)**
   - No cross-product memory leakage; no undetected oscillating states; deterministic behavior; state machine invariants; Unity performance; reset behavior.

*You can copy the structure and text from the previous version of `docs/ARCHITECTURE.md` (in git history) into this section.*

---

### Step 9.2 — Add “Integration & QA” to the same Google Doc

In the **same** Google Doc, add a second section **Integration & QA** with:

1. **When and where Unity calls the engine**
   - When the engine is constructed.
   - When `Update(...)` is called.
   - When signals are injected (which events).
   - When `HandleProductContextSwitch(newProductId)` is called.

2. **HandleProductContextSwitch timing**
   - Exact trigger; before/after first Inject/Update for new product; edge cases.

3. **Stress test runbook**
   - Where to run (scene, components); steps per test; how to interpret results.

4. **QA outcomes**
   - Runs (date, test, pass/fail, notes); sign-off that M2 stress tests and integration are accepted.

---

### Step 9.3 — Set the link in the repo and share the doc

1. [ ] In `docs/ARCHITECTURE.md`, replace  
   `**[Link to your Google Doc — add here and share with Unity developer]**`  
   with the real URL of your Google Doc.
2. [ ] Share the Google Doc with the Unity developer (Elián) with at least view access (comment/edit if needed).

---

### Step 9.4 — Optional: pointer in Samples

In `Samples/README.md`, you can add one line (e.g. near the top or in a “Documentation” section):

- “M2 documentation (Architecture & guarantees + Integration & QA) is in the Google Doc linked from `docs/ARCHITECTURE.md`.”

---

## Summary checklist

| # | Task | Status |
|---|------|--------|
| 9.1 | Google Doc created; “Architecture & guarantees” section added | Your action |
| 9.2 | “Integration & QA” section added to same Google Doc | Your action |
| 9.3 | Link in `docs/ARCHITECTURE.md` set; doc shared with Unity developer | Your action |
| 9.4 | Optional: pointer in Samples/README.md | Optional |

When 9.1–9.3 are done, Step 9 is complete. The **single source of truth** is the Google Doc (Architecture & guarantees + Integration & QA). The repo keeps only the pointer in `docs/ARCHITECTURE.md`.
