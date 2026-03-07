# Step 9: Your testing guide (Steps 1–8, except Step 7)

Follow this **in order**. You run the tests (repo), then add the results to the **QA section** of your Google Doc.

---

## Before you start

- You need: the repo on your machine, terminal (or VS Code terminal), and your Google Doc open.
- Step 7 (stress testing) is done on the **Unity side** by Elián — you skip it.
- Steps 1, 2, 3, 4, 5, 6, and 8 are covered by the **engine’s xUnit tests**. You will run them once and then record results per step.

---

## Step-by-step

### Step 0 — Run the full test suite once

1. Open a terminal in the repo root: `D:\#Project\AIEngine` (or your path).
2. Run:
   ```bash
   dotnet test
   ```
3. Check that all tests pass (e.g. "Passed: 110" or whatever the current count is). If any fail, fix or note them before continuing.
4. Leave the test output visible (or copy it) so you can refer to it when filling the Google Doc.

---

### Step 1 — Signal freezing / snapshot immutability

**What it verifies:** Logs and the snapshot used for evaluation are frozen at tick time. New signals injected after a tick do not change past logs or the snapshot that was already used.

**Tests that cover it:**
- `RuntimeMemoryContextTests`: `Signals_AreIsolatedFromSourceDictionary`, `Engine_TickContextDoesNotLeakBetweenTicks`
- `EvaluationSnapshotTests`: `Engine_UsesSnapshot_ForEvaluation`
- `EngineCoreTests`: (evaluation uses snapshot each tick)

**What you do:**
1. You already ran `dotnet test` in Step 0. These tests are included.
2. In your Google Doc, in the **QA / Tests for steps 1–8** section, add a row for Step 1:

   | Step | What is verified | Test class(es) | Result | Date |
   |------|------------------|----------------|--------|------|
   | 1 | Signal freezing; logs/snapshot immutable at tick time | RuntimeMemoryContextTests, EvaluationSnapshotTests, EngineCoreTests | Pass | (today’s date) |

   (If you already have a table, add this as the first data row.)

---

### Step 2 — Session memory isolation (no cross-product leakage)

**What it verifies:** Each product has its own session. Sessions do not share signal windows, signals, logs, or current state. Reset clears only the active session.

**Tests that cover it:**
- `SessionMemoryContextTests`: `TwoSessions_DoNotShareSignalWindows`, `TwoSessions_DoNotShareSignals`, `TwoSessions_DoNotShareLogs`, `TwoSessions_DoNotShareCurrentState`, `Reset_ClearsAllInternalState`

**What you do:**
1. Same run as Step 0; no extra command.
2. Add a row for Step 2:

   | Step | What is verified | Test class(es) | Result | Date |
   |------|------------------|----------------|--------|------|
   | 2 | Session isolation; no cross-product memory leakage | SessionMemoryContextTests | Pass | (date) |

---

### Step 3 — Rule evaluation and priority

**What it verifies:** Rules run in a fixed order; same inputs give same state; confirm overrides dwell, discrete events (ProductFocus, ComparisonAction, SwipeVelocity, ContextChange) and dwell rules behave as specified.

**Tests that cover it:**
- `EvaluationSnapshotTests`: all rule tests (ConfirmIntent, ProductFocus, ComparisonAction, SwipeVelocity, ContextChange, Dwell, NoSignals, DefaultExploration)
- `EngineCoreTests`: `ConfirmIntent_OverridesDwell_HighPriorityRule`, `Neutral_ReturnedWhenNoSignalsInWindow`, etc.

**What you do:**
1. Same run as Step 0.
2. Add a row for Step 3:

   | Step | What is verified | Test class(es) | Result | Date |
   |------|------------------|----------------|--------|------|
   | 3 | Rule evaluation and priority; deterministic same-input → same state | EvaluationSnapshotTests, EngineCoreTests | Pass | (date) |

---

### Step 4 — Product context switch

**What it verifies:** `HandleProductContextSwitch` changes active product, creates/reuses sessions, resets the incoming session (no cognitive drift), logs the switch, and respects SwitchLog cap.

**Tests that cover it:**
- `ProductContextSwitchTests`: all tests in that class

**What you do:**
1. Same run as Step 0.
2. Add a row for Step 4:

   | Step | What is verified | Test class(es) | Result | Date |
   |------|------------------|----------------|--------|------|
   | 4 | Product context switch; session reset on switch; SwitchLog | ProductContextSwitchTests | Pass | (date) |

---

### Step 5 — Boundary enforcement (productId guard)

**What it verifies:** `InjectSignal(signal, productId)` and `Update(..., productId)` reject wrong productId (throw and log); matching productId is accepted; BoundaryViolationLog and OnBoundaryViolation work; log is capped.

**Tests that cover it:**
- `MemoryBoundaryEnforcementTests`: all tests in that class

**What you do:**
1. Same run as Step 0.
2. Add a row for Step 5:

   | Step | What is verified | Test class(es) | Result | Date |
   |------|------------------|----------------|--------|------|
   | 5 | Boundary enforcement; wrong productId rejected and logged | MemoryBoundaryEnforcementTests | Pass | (date) |

---

### Step 6 — Determinism and integrity validator

**What it verifies:** Rule↔state match, allowed transitions, rapid flip-flop detection (A→B→A with B≠A only); DeterminismViolationLog and OnDeterminismViolation; no false positive when state stays Neutral.

**Tests that cover it:**
- `DeterministicIntegrityValidatorTests`: all tests in that class

**What you do:**
1. Same run as Step 0.
2. Add a row for Step 6:

   | Step | What is verified | Test class(es) | Result | Date |
   |------|------------------|----------------|--------|------|
   | 6 | Determinism validator; rule/state match; flip-flop detection | DeterministicIntegrityValidatorTests | Pass | (date) |

---

### Step 7 — Stress testing (Unity)

**What it verifies:** Conflicting signals, rapid burst, near-threshold oscillation (and optionally performance) on the Unity side.

**What you do:** Skip. This is Elián’s work (Unity scene + stress test scripts). You can add a row that says “Step 7: Stress testing (Unity) — run by Unity team; see Integration & QA / runbook.”

---

### Step 8 — Audit log (memory isolation / trace)

**What it verifies:** AuditLog gets ContextSwitch, SessionReset, and Tick entries; order is correct; OnAuditEntry fires; log is capped; tick entry has timestamp.

**Tests that cover it:**
- `MemoryIsolationAuditLogTests`: all tests in that class

**What you do:**
1. Same run as Step 0.
2. Add a row for Step 8:

   | Step | What is verified | Test class(es) | Result | Date |
   |------|------------------|----------------|--------|------|
   | 8 | Audit log; ContextSwitch/SessionReset/Tick; OnAuditEntry; cap | MemoryIsolationAuditLogTests | Pass | (date) |

---

## After you finish all steps

1. **Check:** Your Google Doc has a table (or list) for Steps 1–8 with Result = Pass (and Step 7 noted as Unity-side).
2. **Optional:** Under the table, add one line: “Engine steps 1–6 and 8 verified by xUnit test run on [date]. Step 7 verified on Unity (see runbook).”
3. **Sign-off:** When you and Elián are satisfied, add your QA sign-off line (e.g. “M2 steps 1–8 (except 7 run on Unity) accepted. [Your name], [date].”).

---

## Quick reference — one table to paste into the Google Doc

You can paste this into the QA section and fill the Result and Date columns:

| Step | What is verified | Test class(es) | Result | Date |
|------|------------------|----------------|--------|------|
| 1 | Signal freezing; logs/snapshot immutable at tick time | RuntimeMemoryContextTests, EvaluationSnapshotTests, EngineCoreTests | | |
| 2 | Session isolation; no cross-product memory leakage | SessionMemoryContextTests | | |
| 3 | Rule evaluation and priority; deterministic | EvaluationSnapshotTests, EngineCoreTests | | |
| 4 | Product context switch; session reset; SwitchLog | ProductContextSwitchTests | | |
| 5 | Boundary enforcement; wrong productId rejected and logged | MemoryBoundaryEnforcementTests | | |
| 6 | Determinism validator; rule/state; flip-flop detection | DeterministicIntegrityValidatorTests | | |
| 7 | Stress testing (Unity) | Run by Unity team — see runbook | (skip / N/A) | |
| 8 | Audit log; ContextSwitch/SessionReset/Tick; cap | MemoryIsolationAuditLogTests | | |

Then run `dotnet test` once, and if all pass, fill every row (except Step 7) with **Pass** and today’s date.

---

## Adding a screenshot per step in the QA section

Yes — having a **per-step section with a screenshot** for each step is a good way to document results. Use the structure below.

### How to structure the QA section

1. **Keep the summary table** at the top (Step, What is verified, Test class(es), Result, Date).
2. **Below the table**, add one subsection per step (Step 1, Step 2, … Step 8). In each subsection:
   - Heading: e.g. **Step 1. Signal freezing / snapshot immutability**
   - Screenshot (see below for what to capture)
   - One line: **Result:** Pass / Skip **Date:** (date)

### What screenshot to add for each step

**Steps 1, 2, 3, 4, 5, 6, 8 (engine xUnit tests)**

You have two options:

- **Option A — One test run, one screenshot (simplest)**  
  1. Run once: `dotnet test`  
  2. Take **one screenshot** of the terminal output showing “Passed: 110” (or your total).  
  3. Use that **same screenshot** in every step section (1, 2, 3, 4, 5, 6, 8), or paste it once at the top under the table with the note: “Full test run (all steps 1–6 and 8) — see below per step.”  
  4. Under each step heading, write: **Result: Pass**, **Date:** (date). You can skip a second screenshot per step if you already have the one full run above.

- **Option B — One screenshot per step (filtered run)**  
  1. For each step, run only the tests for that step (filter by test class), then take a screenshot of that run.  
  2. Use these commands (run in repo root), then capture the terminal output for each:

  | Step | Command to run |
  |------|-----------------|
  | 1 | `dotnet test --filter "FullyQualifiedName~RuntimeMemoryContextTests|FullyQualifiedName~EvaluationSnapshotTests|FullyQualifiedName~EngineCoreTests"` |
  | 2 | `dotnet test --filter "FullyQualifiedName~SessionMemoryContextTests"` |
  | 3 | `dotnet test --filter "FullyQualifiedName~EvaluationSnapshotTests|FullyQualifiedName~EngineCoreTests"` |
  | 4 | `dotnet test --filter "FullyQualifiedName~ProductContextSwitchTests"` |
  | 5 | `dotnet test --filter "FullyQualifiedName~MemoryBoundaryEnforcementTests"` |
  | 6 | `dotnet test --filter "FullyQualifiedName~DeterministicIntegrityValidatorTests"` |
  | 8 | `dotnet test --filter "FullyQualifiedName~MemoryIsolationAuditLogTests"` |

  3. Paste the screenshot for that step under the matching step heading in the Google Doc.

**Step 7 (Unity stress)**

- Screenshot = the **Unity / device** stress test result (e.g. “Test Finished” UI on device or in Editor).  
- Add that screenshot under **Step 7. Stress testing (Unity)** and write **Result:** Skip (run by Unity team) or **Pass** if you ran it and it passed.

### Checklist

- [ ] Summary table at top, with Result and Date filled for all steps.
- [ ] Subsection for each step (Step 1 … Step 8) with heading.
- [ ] For each step: screenshot (full run, or filtered run for that step, or Unity for Step 7) + **Result** and **Date**.
- [ ] Step 7 uses the Unity/device screenshot; others use `dotnet test` output (one shared or one per step).
