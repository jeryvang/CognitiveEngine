# Phase 4 – Document Analysis (Agreement Snap Ad Co Ltd – Jerry Vang)

**Source:** Agreement for Artificial Intelligence Phase 4 Development – Cognitive Engine Refinement (signed 7 March 2026)

---

## 1. Objective

Improve **reliability**, **interpretability**, and **analytical visibility** of the cognitive engine: stabilize the intelligence layer and enable clearer reasoning outputs and structured session analysis.

---

## 2. Scope (Two Checkpoints)

### Checkpoint 1 – Core Intelligence Stability

| Item | Description |
|------|-------------|
| **Config-driven parameter system** | Engine parameters adjustable externally via configuration; no changes to core code for tuning. |
| **Confidence model refinement** | Refine internal confidence model: **accumulation logic**, **confidence decay behaviour**, **stability tuning** to reduce erratic confidence fluctuations. Goal: more predictable, reliable confidence during user sessions. |

### Checkpoint 2 – Transparency & Observability

| Item | Description |
|------|-------------|
| **Explainability layer** | Map internal rules to **human-readable explanations** of why the engine produced a given reasoning outcome. |
| **Session JSON export** | Lightweight export producing **structured JSON** with key session signals for later analysis. |
| **Confidence signal tracking** | JSON must include a **lightweight record of primary signals** influencing confidence (e.g. comparison behaviour, rule triggers, attribute alignment). Include **cognitive state data** needed for post-session evaluation and testing. |

---

## 3. Deliverables (Summary)

1. Updated cognitive engine with refined confidence model  
2. Config-driven parameter architecture  
3. Explainability layer (human-readable reasoning)  
4. Lightweight session JSON export (reasoning + confidence signal tracking)  
5. Code integrated with existing engine architecture  
6. Basic documentation: configurable parameters, reasoning explanation mapping, session JSON structure  

---

## 4. Acceptance Criteria

- Confidence model refinements are **operational** in the engine  
- Parameters are **adjustable through the configuration system**  
- Reasoning explanations are **generated in human-readable form**  
- Session data is **exportable as JSON** for analysis  
- System **compiles and integrates** with the existing project  

---

## 5. Out of Scope (Phase 4)

- New cognitive modules  
- UI implementation  
- Unity-side integration work  
- Additional AI capabilities beyond the defined scope  

---

## 6. Current Codebase Context (Relevant to Phase 4)

- **Parameters:** `CognitiveEngine.EngineOptions` holds `DecayFactor`, `WindowDuration`, `FixedStep`, `MaxLogs`, etc.; currently passed in code, not from config.  
- **Confidence:** In `EvaluationSnapshot`, confidence is `Clamp(signalValue / WindowDuration, 0, 1)`; no accumulation over time, no explicit decay, no stability tuning.  
- **Rules:** Rule names (e.g. `ConfirmIntentHigh`, `HighDwell`) are internal tags; no human-readable explanation layer.  
- **Session / export:** `TickLog` has tick, timestamp, signals, rule, state, confidence; no JSON export or confidence-signal tracking structure.  
- **Tests:** `CognitiveEngine.Tests` (xUnit); run with `dotnet test CognitiveEngine.Tests/CognitiveEngine.Tests.csproj`.  

---

## 7. Suggested Implementation Order

1. **Config-driven parameters** (load `EngineOptions` or equivalent from config).  
2. **Confidence model** (accumulation, decay, stability).  
3. **Explainability layer** (rule id → human-readable text).  
4. **Session JSON export** (session + ticks + signals + cognitive state).  
5. **Confidence signal tracking** (record which signals/rules influenced confidence in export).  
6. **Documentation** (parameters, explanation mapping, JSON structure).  

Each step: **Implement → Test (if fail, fix and re-test) → If pass, provide git commit command and message.** Proceed to the next step only when the user explicitly allows it.
