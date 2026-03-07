# CognitiveEngine

A deterministic, frame-rate-independent cognitive state machine for processing behavioral input signals and resolving user intent states in real time. Designed for Unity integration via a fixed-step accumulator scheduler.

---

## Build

```bash
dotnet build
```

Requires .NET SDK 10.0 or later.

---

## Run Tests

```bash
dotnet test --verbosity normal
```

The test suite covers:

- Sliding window expiry (signals older than `WindowDuration` are removed)
- Decay model (per-tick signal attenuation)
- Rule priority (ConfirmIntent overrides DwellTime regardless of value)
- Event emission (`OnStateUpdated` fires on state change)
- Neutral state recovery (state returns to `Neutral` when signal window is empty)
- Burst tick determinism (each sub-tick in a burst receives a distinct, ordered timestamp)
- ProductFocus and ComparisonAction rules (discrete events drive Exploration and Comparison)
- `SignalAccumulator` build, decay, ceiling, and reset behaviour
- `StreamingCognitiveEngine` dwell/confirm state progression and smooth decay

---

## Acceptance Demo

```bash
dotnet run --project CognitiveEngine.Test
```

Produces a timestamped acceptance log (`acceptance_log_<yyyyMMdd_HHmmss>.json`) in the working directory.

**Expected state sequence:**

| Phase | Injected Signal          | Expected State Sequence                    |
|-------|--------------------------|--------------------------------------------|
| 1     | DwellTime (gradual build)| Exploration → Comparison → Hesitation      |
| 2     | ConfirmIntent spike      | ReadyToConfirm (overrides Hesitation)      |

---

## Configuration

Pass an `EngineOptions` instance to the constructor to override defaults:

```csharp
var engine = new CognitiveEngine(new CognitiveEngine.EngineOptions
{
    DecayFactor     = 0.9f,
    WindowDuration  = 2.0f,
    FixedStep       = 0.1f,
    MaxLogs         = 1000,
    MaxSignalWindow = 4096
});
```

| Option            | Default | Description                                                  |
|-------------------|---------|--------------------------------------------------------------|
| `DecayFactor`     | `0.9f`  | Per-tick signal decay multiplier (applied after aggregation) |
| `WindowDuration`  | `2.0f`  | Sliding window duration in seconds                           |
| `FixedStep`       | `0.1f`  | Tick interval in seconds (10 ticks/second by default)        |
| `MaxLogs`         | `1000`  | Maximum retained `TickLog` entries                           |
| `MaxSignalWindow` | `4096`  | Maximum queued signals; excess injections are dropped        |

---

## Unity Integration

Two drivers are provided depending on input type:

| Driver | Use when |
|---|---|
| [`Samples/CognitiveEngineDriver.cs`](Samples/CognitiveEngineDriver.cs) | **Discrete:** event-based inject (dwell, confirm, swipe, etc.); **Streaming:** per-frame dwell/confirm floats. Optional product context (M2) and audit logging in Discrete mode. |

See **[Samples/README.md](Samples/README.md)** for setup, **Step 7** (stress testing), and **Step 8** (audit logging).  
For architecture, memory boundaries, and determinism guarantees, see **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)**.

### Event-based (CognitiveEngine)

Signals that drive state (rule priority order): **ConfirmIntent** → **ProductFocus** → **ComparisonAction** → DwellTime (Hesitation / Comparison) → NoSignals → DefaultExploration. Wire driver methods (e.g. `OnProductFocused()`, `OnComparisonAction()`, `OnUserDwelling()`, `OnConfirmIntent()`) to your input layer; you can send dwell at intervals instead of every frame.

```csharp
// Call from pointer/UI event handlers — not every frame:
engine.InjectSignal(new InputSignal(SignalType.DwellTime, strength, Time.time));

// Tick every frame regardless of whether signals were injected:
engine.Update(Time.deltaTime, Time.time);

// Subscribe once in Start():
engine.OnStateUpdated += state =>
{
    // state.State        — resolved StateType
    // state.ReasoningTag — name of the winning rule
    // state.Confidence   — normalized signal strength (0–1)
};
```

The scheduler is frame-rate-independent: ticks fire at `FixedStep` intervals regardless of Unity frame duration. Multiple ticks may fire in a single `Update()` call during frame spikes; each receives a distinct reconstructed timestamp.

### Streaming (StreamingCognitiveEngine)

```csharp
// Pass raw 0–1 float inputs each frame:
engine.Update(Time.deltaTime, dwellInput, confirmInput);

// Poll every frame for UI — confidence updates even when State hasn't changed:
UpdateConfidenceBar(engine.Current.Confidence);
UpdateStateLabel(engine.Current.State);

// Subscribe for one-shot transition events only:
engine.OnStateUpdated += state =>
{
    // fires only when StateType changes
    PlayTransitionAnimation(state.State);
};
```

---

## Determinism Guarantees

| Guarantee                        | Mechanism                                                                         |
|----------------------------------|-----------------------------------------------------------------------------------|
| No randomness                    | No `System.Random`, GUID, or environment-dependent seeding in any code path       |
| Ordered signal aggregation       | `_signalWindow` is `List<T>`; aggregation iterates in strict insertion order       |
| Ordered decay                    | `ApplyDecay()` sorts dictionary keys via `OrderBy` before iteration               |
| Deterministic rule cascade       | `EvaluateRules()` is a pure, ordered `if/else` with no hidden state               |
| Frame-rate-independent scheduler | Fixed-step accumulator; each burst sub-tick receives a reconstructed timestamp    |
| Bounded memory                   | `_logs` capped at `MaxLogs`; `_signalWindow` capped at `MaxSignalWindow`          |
| Immutable audit trail            | `TickLog.Signals` exposed as `IReadOnlyDictionary` — external mutation prevented  |

---

## Acceptance Checklist (Milestone 1)

- [x] `EngineOptions` implemented and documented.
- [x] Automated tests passing locally.
- [x] Demo reproduces contract scenarios and output attached.
- [x] `README.md` and acceptance doc added.
- [x] CI runs build and tests on push.
