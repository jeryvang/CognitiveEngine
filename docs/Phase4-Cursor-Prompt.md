# Phase 4 – Optimized Cursor Prompt (Step-by-Step)

Use this as the **Cursor prompt** for each step. Follow the workflow strictly.

---

## Workflow (Every Step)

1. **Implement** the step in the codebase (CognitiveEngine.Core; tests in CognitiveEngine.Tests).
2. **Test:** run `dotnet test CognitiveEngine.Tests/CognitiveEngine.Tests.csproj`. If any test fails, fix and re-run until all pass.
3. **If all tests pass:** output a **git commit command** with a **short, clear commit message** for this step only. Do **not** run git yourself; only provide the command and message.
4. **Do not advance** to the next step until the user explicitly says they allow it (e.g. “next step”, “proceed”, “done”).

---

## Step 1 – Config-driven parameter system (Checkpoint 1)

**Goal:** Engine parameters must be adjustable from external configuration, without changing core code.

**Tasks:**

- Define a configuration model (e.g. JSON or key-value) that maps to `CognitiveEngine.EngineOptions` (DecayFactor, WindowDuration, FixedStep, MaxLogs, MaxSignalWindow, etc.).
- Add a way to load this config (e.g. from file path or stream) and build `EngineOptions` from it.
- Ensure `CognitiveEngine` can be constructed with options from this config.
- Keep existing constructors working (no breaking changes).
- Add or extend tests that verify engine behaviour when options are loaded from config (e.g. different DecayFactor/WindowDuration produce expected behaviour).

**Reference:** `CognitiveEngine.cs` (`EngineOptions`, constructors), existing tests in `CognitiveEngine.Tests`.

After implementation: run tests → if pass, provide git command + commit message. Wait for user approval before Step 2.

---

## Step 2 – Confidence model refinement (Checkpoint 1)

**Goal:** More predictable, reliable confidence: refinement of accumulation logic, confidence decay behaviour, and stability tuning to reduce erratic fluctuations.

**Tasks:**

- Refine how confidence is computed in the evaluation path (e.g. in `EvaluationSnapshot` or a dedicated confidence model):
  - **Accumulation:** define how confidence accumulates over time from signals (e.g. smoothing, running average, or use of `SignalAccumulator` where appropriate).
  - **Decay:** apply confidence decay when signals weaken or time passes, so confidence does not stay high indefinitely.
  - **Stability:** reduce sudden jumps (e.g. hysteresis, min change threshold, or temporal smoothing).
- Expose any new tuning parameters via `EngineOptions` (and thus via config from Step 1).
- Add or update tests that assert:
  - Confidence responds sensibly to signal strength and time.
  - Confidence decays when signals drop.
  - Confidence does not fluctuate erratically (e.g. within a short sequence of similar inputs).

After implementation: run tests → if pass, provide git command + commit message. Wait for user approval before Step 3.

---

## Step 3 – Explainability layer (Checkpoint 2)

**Goal:** Human-readable explanations for why the engine produced a given reasoning outcome.

**Tasks:**

- Create an explainability layer that maps **internal rule names** (e.g. `ConfirmIntentHigh`, `HighDwell`, `ComparisonAction`) to **short, human-readable explanations** (e.g. “User confirmed intent with high confidence”, “Prolonged dwell suggests hesitation”).
- Integrate this so that whenever a rule is chosen (e.g. in `CognitiveEngine` / `EvaluationSnapshot` path), a human-readable explanation is available (e.g. on `CognitiveState`, or a separate method/API that returns explanation for the last or current outcome).
- Do not change the existing rule logic or state machine; only add the mapping and exposure of explanations.
- Add tests that verify at least a few rules return the expected human-readable text.

After implementation: run tests → if pass, provide git command + commit message. Wait for user approval before Step 4.

---

## Step 4 – Session JSON export (Checkpoint 2)

**Goal:** Lightweight session export as structured JSON for analysis.

**Tasks:**

- Implement a **session export** that produces **JSON** containing:
  - Session identifier / product context if applicable.
  - Key session signals and a series of “ticks” or “steps” (e.g. timestamp, signals, state, rule, confidence).
  - Enough structure for post-session analysis (e.g. array of tick objects with consistent schema).
- Prefer a single, well-defined JSON schema (e.g. one root object with session metadata + array of tick entries). Use standard serialization (e.g. `System.Text.Json`) and keep the format documented.
- Add tests that export a short session and assert the JSON structure and key fields (e.g. presence of ticks, states, timestamps).

After implementation: run tests → if pass, provide git command + commit message. Wait for user approval before Step 5.

---

## Step 5 – Confidence signal tracking in JSON (Checkpoint 2)

**Goal:** Session JSON includes which primary signals and rule triggers influenced the confidence score.

**Tasks:**

- Extend the session JSON export so each tick (or each state change) includes a **lightweight record of primary signals** that influenced confidence, e.g.:
  - Which rule fired.
  - Which signals contributed (e.g. comparison behaviour, rule triggers, attribute alignment) and optionally their contribution or strength.
- Include **cognitive state data** needed for post-session evaluation (e.g. state, confidence, rule, and the new “confidence signals” or “reasoning signals” section).
- Keep the format lightweight (no heavy duplication); document the new fields in the session JSON structure.
- Add or update tests that assert the exported JSON contains these confidence/reasoning signal fields for sample sessions.

After implementation: run tests → if pass, provide git command + commit message. Wait for user approval before Step 6.

---

## Step 6 – Documentation (Deliverable 6)

**Goal:** Basic documentation for configurable parameters, reasoning explanation mapping, and session JSON structure.

**Tasks:**

- Add or update docs (e.g. in `docs/`) to cover:
  - **Configurable parameters:** list of all parameters that can be set via config, their meaning, and example values.
  - **Reasoning explanation mapping:** table or list of rule names and their human-readable explanations.
  - **Session JSON structure:** description of the export format (root object, arrays, key fields, and the confidence signal tracking fields).
- No code changes required unless you fix typos or add doc comments that reference these docs. No new features in this step.
- Run tests to ensure nothing is broken. If pass, provide git command + commit message.

After implementation: run tests → if pass, provide git command + commit message. Phase 4 implementation prompt complete.

---

## Quick reference – Commands

- **Build:** `dotnet build`
- **Test (all):** `dotnet test`
- **Test (this project only):** `dotnet test CognitiveEngine.Tests/CognitiveEngine.Tests.csproj`
- **Test (verbose):** `dotnet test CognitiveEngine.Tests/CognitiveEngine.Tests.csproj --logger "console;verbosity=detailed"`

---

## Important

- **One step at a time:** only implement the current step until the user allows moving on.
- **Tests must pass** before suggesting a commit; if they fail, loop on “Implement → Test” until green.
- **Git:** only output the exact `git add` / `git commit` command and message; do not execute git unless the user asks you to.
