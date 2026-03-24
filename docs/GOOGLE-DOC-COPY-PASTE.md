# Copy-paste content for your Google Doc

Use the sections below in your **single Google Doc**. Paste **Section 1** first (Architecture & guarantees), then **Section 2** (Integration & QA — fill in the bullets with your team).

---

# Section 1: Architecture & guarantees (Milestone 2)

## Engine state summary

### Public API

| Member | Description |
|--------|-------------|
| CognitiveEngine() / CognitiveEngine(EngineOptions?) | Construct; optional options. |
| InjectSignal(InputSignal signal) | Inject into active session (no productId). |
| InjectSignal(InputSignal signal, string productId) | Inject with productId; throws if productId ≠ ActiveProductId. |
| Update(float deltaTime, float currentTime) | Tick loop for active session. |
| Update(float deltaTime, float currentTime, string productId) | Tick with productId; throws if mismatch. |
| ResetSession() | Reset active session (window, signals, logs, state, tick index). |
| HandleProductContextSwitch(string newProductId) | Switch active product; resets incoming session, logs switch. |
| ActiveProductId | Current product context. |
| Logs | Per-tick log for active session (TickLog: tick index, timestamp, signals, rule, state, confidence). |
| SwitchLog | Context switch history (from/to productId). |
| BoundaryViolationLog | Rejected Inject/Update calls (wrong productId). |
| DeterminismViolationLog | Rule priority, undefined transition, or rapid flip-flop. |
| AuditLog | Ordered trace: ContextSwitch, SessionReset, Tick (productId, rule, state). |
| OnStateUpdated | Fires when resolved state changes. |
| OnBoundaryViolation | Fires when productId mismatch on Inject/Update. |
| OnDeterminismViolation | Fires when validator reports a violation. |
| OnAuditEntry | Fires for each audit entry (switch, reset, tick). |

### EngineOptions (defaults)

| Option | Default | Description |
|--------|---------|-------------|
| DecayFactor | 0.9 | Per-tick signal decay after aggregation. |
| WindowDuration | 2.0 | Sliding window seconds; older signals dropped. |
| FixedStep | 0.1 | Tick interval seconds (10 ticks/s). |
| MaxLogs | 1000 | Cap for active session TickLog. |
| MaxSignalWindow | 4096 | Cap for queued signals per session. |
| MaxSwitchLog | 100 | Cap for context switch history. |
| MaxBoundaryViolationLog | 100 | Cap for boundary violation records. |
| MaxDeterminismViolationLog | 100 | Cap for determinism violation records. |
| MaxAuditLog | 1000 | Cap for audit trace. |

### StateType

Neutral → Exploration → Comparison → Hesitation → ReadyToConfirm.

Rule priority: ConfirmIntent > ContextChange (Neutral) > ProductFocus (Exploration) > ComparisonAction (Comparison) > SwipeVelocity (Exploration) > HighDwell (Hesitation) > MediumDwell (Comparison) > NoSignals (Neutral) > DefaultExploration.

### SignalType

ProductFocus, DwellTime, SwipeVelocity, ComparisonAction, ConfirmIntent, ContextChange.

---

## Memory boundaries

- **One session per product.** Each productId has an isolated SessionMemoryContext (signal window, aggregated signals, logs, current state, tick index). Product A’s memory never affects Product B.
- **Context switch.** When Unity calls HandleProductContextSwitch(newProductId), the outgoing session remains stored; the incoming session is **reset** (window cleared, state to Neutral, tick index 0). No cognitive drift across products. Each switch is appended to SwitchLog and AuditLog.
- **Boundary enforcement.** InjectSignal(signal, productId) and Update(deltaTime, currentTime, productId) throw if productId != ActiveProductId. Violations are logged to BoundaryViolationLog and OnBoundaryViolation. Use these overloads when using multiple products so wrong-context calls are rejected at the API.

---

## Deterministic guarantees

- **No randomness.** No Random, GUID, or environment-dependent seeding in evaluation or scheduling.
- **Fixed-step scheduler.** Ticks run at FixedStep intervals; each tick gets a deterministic timestamp. Multiple ticks per Update() call are ordered and reproducible.
- **Frozen snapshot per tick.** Before rule evaluation, runtime signals are copied into RuntimeMemoryContext (immutable for the tick). Rules run on EvaluationSnapshot over that snapshot; no mid-cycle mutation.
- **Ordered rule cascade.** EvaluationSnapshot.Evaluate() uses a fixed if/else order: ConfirmIntent > ContextChange > ProductFocus > ComparisonAction > SwipeVelocity > Dwell thresholds > NoSignals > DefaultExploration. Same inputs → same state and rule.
- **Integrity validator.** Each tick, DeterministicIntegrityValidator checks: rule name matches resolved state; transition is allowed; no rapid flip-flop (A→B→A in three ticks). Violations go to DeterminismViolationLog and OnDeterminismViolation.

---

## Stress testing

Stress tests are run **on the Unity side** (see repo Samples/README.md, Step 7). They validate:

1. **Conflicting signals** — High dwell + high confirm → single state; confirm wins (ReadyToConfirm).
2. **Rapid event burst** — Many injects + updates; logs and signal window stay capped; no long frame blocks.
3. **Near-threshold oscillation** — Dwell near thresholds; either stable or flip-flop reported in DeterminismViolationLog.

Runbook and outcomes are maintained in the Integration & QA section below.

---

## Milestone 2 checklist (reference)

- No cross-product memory leakage (sessions isolated; boundary enforcement).
- No undetected oscillating states under stress (flip-flop detection; stress tests in Unity).
- Deterministic behavior reproducible (fixed step, frozen snapshot, ordered rules).
- State machine invariants (validator: one state, rule priority, allowed transitions).
- Unity performance (stress test: no blocking; memory bounded).
- Reset behavior consistent (ResetSession; HandleProductContextSwitch resets incoming).

---

# Section 2: Integration & QA

## When and where Unity calls the engine

- **Engine construction:** (e.g. scene load, product load — fill in)
- **Update(deltaTime, time) or Update(..., productId):** (e.g. every frame, fixed update — fill in)
- **Signal injection:** Which events call OnUserDwelling, OnConfirmIntent, OnContextChanged, swipe, ProductFocus, ComparisonAction, etc. (fill in)
- **HandleProductContextSwitch(newProductId):** (e.g. on product/screen change — fill in)

## HandleProductContextSwitch timing

- **Exact trigger:** (e.g. when user opens a different product card — fill in)
- **Order:** Called before or after the first Inject/Update for the new product? (fill in)
- **Edge cases:** (e.g. quick back-and-forth between products — fill in)

## Stress test runbook

- **Where to run:** (scene, components — fill in)
- **Steps:** Conflicting signals, Rapid event burst, Near-threshold oscillation (see repo Samples/README.md Step 7 for code).
- **How to interpret:** Check Logs last state, DeterminismViolationLog, memory/frame time (fill in as needed).

## Measured performance impact (with documented results)

Run the stress test (blank scene, UI result) on each device. Capture a screenshot of the **Test Finished** screen, then add the screenshot and the summary below to this doc.

**Device 1:** [Device name, e.g. Samsung Galaxy A23]

[Screenshot: Test Finished — Device 1]

| Metric | Value |
|--------|--------|
| Test duration | (e.g. 10.01 s) |
| Total frames | (e.g. 906) |
| Average FPS | (e.g. 90.53) |
| Average engine time | (e.g. 0.1080 ms) |
| Max engine time | (e.g. 2.8790 ms) |
| ≥60 FPS | (e.g. 100% — 906 frames) |
| ≤30 FPS | (e.g. 0% — 0 frames) |
| Total signals | (e.g. 90600) |
| Product switches | (e.g. 4530) |

**Device 2:** [Device name, e.g. Samsung Galaxy A52]

[Screenshot: Test Finished — Device 2]

| Metric | Value |
|--------|--------|
| Test duration | |
| Total frames | |
| Average FPS | |
| Average engine time | |
| Max engine time | |
| ≥60 FPS | |
| ≤30 FPS | |
| Total signals | |
| Product switches | |

**Conclusion:** According to the tests, [e.g. there are no abrupt frame rate drops / engine time remains under X ms / all frames ≥60 FPS on tested devices].

---

## QA outcomes

- **Runs:** (table or log: date, test name, pass/fail, notes — fill in)
- **Sign-off:** M2 stress tests and integration behavior accepted (fill in when done).

---

# Section 2 — What to do exactly (detailed guide)

*This part is for you and Elián; it does not get pasted into the Google Doc. It explains what to write in each bullet of Section 2.*

---

## 1. When and where Unity calls the engine

**Goal:** Document the exact moments in the app when the engine is used, so anyone can reproduce or audit integration.

| Bullet | What to do | Example of what to write |
|--------|------------|---------------------------|
| **Engine construction** | Decide when the `CognitiveEngine` instance is created. Then write one short sentence. | e.g. "Created once when the app/scene loads (or when the product viewer is opened)." |
| **Update(deltaTime, time)** | Decide where `Update` is called (every frame in Update(), or in FixedUpdate(), etc.). Write when and how often. | e.g. "Called every frame in MonoBehaviour.Update(), using Time.deltaTime and Time.time." Or "Called from FixedUpdate every 0.1s." |
| **Signal injection** | List each kind of input and which engine call it triggers. Use the driver method names if you use CognitiveEngineDriver. | e.g. "OnUserDwelling(strength) from coroutine every 0.1s while user has touch on product. OnConfirmIntent() on confirm button. OnContextChanged() when user switches tab (Details/Reviews). Swipe/rotate sends SignalType.SwipeVelocity. ProductFocus when product is in focus; ComparisonAction when user opens comparison." |
| **HandleProductContextSwitch(newProductId)** | Decide when the user "changes product" (e.g. opens another product card). Write that trigger. | e.g. "Called when user selects a different product from the list (or navigates to another product page)." |

**Check:** After filling, a reader should know: when the engine is created, when it is ticked, when each signal is sent, and when product context switches.

---

## 2. HandleProductContextSwitch timing

**Goal:** Make the order of operations clear so there are no wrong-productId rejects (boundary violations) and no drift between UI and engine.

| Bullet | What to do | Example of what to write |
|--------|------------|---------------------------|
| **Exact trigger** | Name the user action or code path that calls HandleProductContextSwitch. | e.g. "When user taps a different product card." Or "When ProductListController sets currentProductId and calls HandleProductContextSwitch(newProductId)." |
| **Order** | Decide: do you call HandleProductContextSwitch **before** or **after** the first InjectSignal/Update for the new product? Then state it clearly. | e.g. "We call HandleProductContextSwitch(newProductId) first, then update UI and start injecting signals for the new product. So the first Inject/Update for the new product always uses the new ActiveProductId." (Recommended: switch first, then inject/update.) |
| **Edge cases** | Note any special cases you thought about. | e.g. "If user switches product very quickly, we still call HandleProductContextSwitch for each selection; old session is kept, new one is reset. We do not inject signals for a product after switching away until the user switches back." |

**Check:** No InjectSignal(signal, productA) or Update(..., productA) when ActiveProductId is productB; otherwise you get boundary violations.

---

## 3. Stress test runbook

**Goal:** Someone else (or you later) can run the same stress tests and get the same kind of results.

| Bullet | What to do | Example of what to write |
|--------|------------|---------------------------|
| **Where to run** | Name the scene and what’s in it. | e.g. "Scene: StressTest. Only CognitiveEngineDriver (or CognitiveEngineStressTest) and a UI Text for results. No other gameplay." |
| **Steps** | For each of the three tests, write 1–2 sentences: what to run and what to check. Code lives in repo Samples/README.md Step 7. | e.g. "**Conflicting signals:** Run the script that injects high dwell + confirm every frame for 50 ticks. Pass: last state in Logs is ReadyToConfirm. **Rapid burst:** Run N frames with many Inject + Update per frame. Pass: no long frame spikes; memory bounded. **Near-threshold:** Dwell near 0.7. Pass: either stable or flip-flop logged in DeterminismViolationLog." |
| **How to interpret** | Say what “pass” means and where to look. | e.g. "Check engine.Logs[last].FinalState for target state. Check DeterminismViolationLog count (0 or documented). For performance: note avg/max engine time (ms) and % frames ≥60 FPS from stress test UI or log." |

**Check:** A new person can open the scene, run each test, and know pass/fail and where to read numbers.

---

## 4. Measured performance impact — test and add result with screenshot

**Goal:** Have documented performance results (and screenshots) in the Google Doc, like the Samsung Galaxy A23 / A52 example.

**What to do (step-by-step):**

1. **Run the stress test** on the device (blank scene with the stress test UI). Let it run until **Test Finished** is shown (e.g. ~10 seconds).
2. **Take a screenshot** of the full **Test Finished** screen (duration, frames, FPS, engine time, ≥60 FPS / ≤30 FPS, signals, product switches).
3. **Open the Google Doc** → go to the **Measured performance impact** section (paste the template from this file if you haven’t yet).
4. **Add a device block:**  
   - Title: e.g. **Device 1: Samsung Galaxy A23**  
   - Insert the screenshot (Insert → Image → upload or paste).  
   - Under the image, paste or fill the **metric table** (Test duration, Total frames, Average FPS, Average engine time, Max engine time, ≥60 FPS, ≤30 FPS, Total signals, Product switches) from the UI.
5. **Repeat** for each device (Device 2: Samsung Galaxy A52, etc.).
6. **Fill the conclusion line** at the bottom, e.g. “According to the tests, there are no abrupt frame rate drops.”

**Check:** Each device has a screenshot + table; the conclusion sentence is filled; Anna’s “measured performance impact (with documented results)” is satisfied.

---

## 5. QA outcomes

**Goal:** Record that stress tests and integration were run and accepted (for Anna / stakeholders and for the Google Doc).

| Bullet | What to do | Example of what to write |
|--------|------------|---------------------------|
| **Runs** | Keep a small table or log of runs. Add a row when you run a test. | e.g. Table: Date \| Test name \| Pass/Fail \| Notes. "2025-02-24 \| Conflicting signals \| Pass \| ReadyToConfirm. 2025-02-24 \| Rapid burst \| Pass \| Max engine 0.2 ms." |
| **Sign-off** | When you’re satisfied, add one line. | e.g. "M2 stress tests and integration behavior accepted. [Name], [Date]." Or "Signed off by [Elián/Anna], [date]." |

**Check:** You can point to this section as “measured and documented” for M2 (including performance if you add stress test numbers here or in a “Measured performance impact” subsection).
