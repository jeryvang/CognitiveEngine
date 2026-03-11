# Phase 4. Checkpoint 1: Core Intelligence Stability

## **Delivery summary**

### 1. Config-driven parameter system

- Engine behavior can be changed **without editing code** by using an external configuration file (JSON).
- Configurable parameters include:
  - **DecayFactor**: how quickly signal strength fades
  - **WindowDuration**: time window for signals
  - **FixedStep**: simulation step size
  - **MaxLogs**, **MaxSignalWindow**, **MaxSwitchLog**, **MaxBoundaryViolationLog**, **MaxDeterminismViolationLog**, **MaxAuditLog**: capacity and logging limits
  - **ConfidenceSmoothingAlpha**: how quickly confidence follows raw signals (smoothing)
  - **ConfidenceDecayRate**: how quickly confidence drops when signals are weak
  - **ConfidenceMinChange**: minimum change before confidence updates (stability)
- Configuration can be loaded from:
  - A JSON file path
  - A stream
  - A JSON string (e.g. for tests or in-memory config)
- Omitted parameters use built-in defaults.

### 2. Confidence model refinement

- **Accumulation**: confidence is smoothed over time so it does not jump on every tick.
- **Decay**: when signals weaken or disappear, confidence decreases over time instead of staying high.
- **Stability**: small changes are filtered so confidence does not fluctuate erratically.
- Result: more predictable and reliable confidence during user interaction sessions.

---

## What is in the codebase

- **CognitiveEngine.Core**
  - `EngineOptionsConfig`: configuration model for JSON.
  - `EngineConfigLoader`: loads config from file, stream, or JSON string and returns `EngineOptions`.
  - `ConfidenceModel`: applies smoothing, decay, and minimum-change logic to raw confidence.
  - `CognitiveEngine.EngineOptions`: extended with `ConfidenceSmoothingAlpha`, `ConfidenceDecayRate`, `ConfidenceMinChange`.
  - Engine uses config-loaded options and the refined confidence model; existing constructors and behavior are preserved.
- **[For xUnit Test] CognitiveEngine.Tests (ConfigDrivenParameterTests.cs, ConfidenceModelRefinementTests.cs)**
  - Tests for config-driven behavior (e.g. loading from JSON/file, partial config, defaults).
  - Tests for confidence (decay when signals drop, smoothing, response to signal strength, default behavior unchanged).

---

## Example config (JSON)

```json
{
  "DecayFactor": 0.9,
  "WindowDuration": 2.0,
  "FixedStep": 0.1,
  "MaxLogs": 1000,
  "ConfidenceSmoothingAlpha": 0.5,
  "ConfidenceDecayRate": 1.0,
  "ConfidenceMinChange": 0.05
}
```

Any of these can be omitted; omitted values use engine defaults.