## Unity drivers for the CognitiveEngine. Attach one to a GameObject and wire your input events to its public methods.

| File | Use when |
|------|----------|
| [CognitiveEngineDriver.cs](CognitiveEngineDriver.cs) | **Input mode = Discrete:** event-based inject (OnUserDwelling, OnConfirmIntent, swipe, etc.), optional product context (M2), audit logging. **Input mode = Streaming:** per-frame dwell/confirm floats via **Streaming Dwell Input** / **Streaming Confirm Input** or `SetStreamingInputs(dwell, confirm)`; uses `StreamingCognitiveEngine`. |

---

## Step 7: Stress testing on the Unity side

These checks make sure the engine behaves correctly under heavy or tricky input. Run them in a test scene. Use the same engine API.

### 1. Conflicting signals (only one state active)

When several signals are strong at once (e.g. user dwelling and also pressing confirm), the engine must pick a single state and respect rule priority: confirm intent overrides dwell. This test injects high dwell, confirm, and comparison every frame and verifies the final state is ReadyToConfirm.

**Goal:** High dwell + high confirm → only one state; confirm wins (ReadyToConfirm).

```csharp
using UnityEngine;
using CognitiveEngine.Core;

public class StressTestConflictingSignals : MonoBehaviour
{
    private CognitiveEngine.Core.CognitiveEngine _engine;
    private int _tickCount;
    private bool _passed;

    void Start()
    {
        _engine = new CognitiveEngine.Core.CognitiveEngine();
        _engine.OnStateUpdated += OnStateChanged;
        _tickCount = 0;
        _passed = true;
    }

    void Update()
    {
        if (_tickCount >= 50) return;
        float t = Time.time;
        _engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.9f, t));
        _engine.InjectSignal(new InputSignal(SignalType.ConfirmIntent, 0.9f, t));
        _engine.InjectSignal(new InputSignal(SignalType.ComparisonAction, 0.5f, t));
        _engine.Update(Time.deltaTime, Time.time);
        _tickCount++;
    }

    private void OnStateChanged(CognitiveState state)
    {
        if (state.State != StateType.ReadyToConfirm && _tickCount > 5)
            _passed = false;
    }

    void OnDestroy()
    {
        if (_engine != null)
        {
            _engine.OnStateUpdated -= OnStateChanged;
            if (_engine.Logs.Count > 0)
            {
                var last = _engine.Logs[_engine.Logs.Count - 1];
                _passed = _passed && last.FinalState == StateType.ReadyToConfirm;
            }
            Debug.Log($"[Stress] Conflicting signals: {(_passed ? "PASS" : "FAIL")} (last state: {(_engine.Logs.Count > 0 ? _engine.Logs[_engine.Logs.Count - 1].FinalState.ToString() : "n/a")})");
        }
    }
}
```

*What it does:* Runs 50 ticks, each with three signals injected and one `Update`. Pass means the last state in `Logs` is ReadyToConfirm; fail means the engine did not give priority to confirm.

### 2. Rapid event burst (no memory growth, no frame blocking)

Under heavy use (many signals per frame, many updates), the engine must not grow memory without bound and must not block the main thread. This test runs 500 frames with 20 injects per frame, measures total allocated memory before/after and max frame time, and logs whether both stayed within acceptable limits.

**Goal:** Many injects + updates; memory bounded, no long frame spikes.

```csharp
using UnityEngine;
using CognitiveEngine.Core;
using System.Diagnostics;

public class StressTestRapidBurst : MonoBehaviour
{
    private CognitiveEngine.Core.CognitiveEngine _engine;
    private Stopwatch _sw;
    private long _memBefore;
    private int _frameCount;
    private float _maxFrameTime;
    private const int TargetFrames = 500;
    private const float MaxAcceptableFrameMs = 33f;

    void Start()
    {
        _engine = new CognitiveEngine.Core.CognitiveEngine(new CognitiveEngine.Core.CognitiveEngine.EngineOptions
        {
            MaxLogs = 500,
            MaxSignalWindow = 2048,
            FixedStep = 0.1f
        });
        _sw = Stopwatch.StartNew();
        _memBefore = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        _frameCount = 0;
        _maxFrameTime = 0f;
    }

    void Update()
    {
        if (_frameCount >= TargetFrames) return;
        float frameStart = Time.realtimeSinceStartup;
        float t = Time.time;
        for (int i = 0; i < 20; i++)
            _engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.2f, t + i * 0.01f));
        _engine.Update(Time.deltaTime, Time.time);
        float elapsed = (Time.realtimeSinceStartup - frameStart) * 1000f;
        if (elapsed > _maxFrameTime) _maxFrameTime = elapsed;
        _frameCount++;
    }

    void OnDestroy()
    {
        if (_engine == null) return;
        _sw.Stop();
        long memAfter = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        bool noBlock = _maxFrameTime < MaxAcceptableFrameMs;
        bool memOk = (memAfter - _memBefore) < 50 * 1024 * 1024;
        Debug.Log($"[Stress] Rapid burst: frames={_frameCount} maxFrameMs={_maxFrameTime:F1} noBlock={noBlock} memDeltaMB={((memAfter - _memBefore) / (1024.0 * 1024.0)):F2} {(noBlock && memOk ? "PASS" : "CHECK")}");
    }
}
```

*What it does:* Records memory at Start, runs 500 frames with burst inject + update, then in OnDestroy compares memory and checks max frame time &lt; 33 ms. PASS = no blocking and no large memory jump; CHECK = review the logged numbers.

### 3. Near-threshold oscillation (no rapid flip-flopping)

When input hovers near a rule threshold (e.g. dwell around 0.7 for Hesitation), the state can sometimes flip back and forth every tick (flip-flop). The engine detects that and reports it. This test drives dwell near the threshold for 30 ticks and either sees stable behavior or sees flip-flop violations in the log—so you know the engine is traceable.

**Goal:** Dwell near 0.7; state stable or flip-flop reported in DeterminismViolationLog.

```csharp
using UnityEngine;
using CognitiveEngine.Core;
using System.Linq;

public class StressTestNearThreshold : MonoBehaviour
{
    private CognitiveEngine.Core.CognitiveEngine _engine;
    private int _tickCount;
    private const int Ticks = 30;

    void Start()
    {
        _engine = new CognitiveEngine.Core.CognitiveEngine(new CognitiveEngine.Core.CognitiveEngine.EngineOptions
        {
            WindowDuration = 0.5f,
            FixedStep = 0.1f
        });
        _engine.OnDeterminismViolation += OnViolation;
        _tickCount = 0;
    }

    void Update()
    {
        if (_tickCount >= Ticks) return;
        float t = Time.time;
        float dwell = 0.68f + (_tickCount % 3) * 0.02f;
        _engine.InjectSignal(new InputSignal(SignalType.DwellTime, dwell, t));
        _engine.Update(Time.deltaTime, Time.time);
        _tickCount++;
    }

    private void OnViolation(DeterminismViolationRecord r)
    {
        if (r.Kind == DeterminismViolationKind.RapidFlipFlop)
            Debug.Log($"[Stress] Flip-flop detected at tick {r.TickIndex}: {r.FromState} -> {r.ToState}");
    }

    void OnDestroy()
    {
        if (_engine != null)
        {
            _engine.OnDeterminismViolation -= OnViolation;
            int flipFlops = _engine.DeterminismViolationLog.Count(v => v.Kind == DeterminismViolationKind.RapidFlipFlop);
            Debug.Log($"[Stress] Near-threshold: ticks={_engine.Logs.Count} flipFlopViolations={flipFlops} (detected = traceable)");
        }
    }
}
```

*What it does:* Injects dwell that oscillates around 0.68–0.72 for 30 ticks. Subscribes to `OnDeterminismViolation`; if the engine detects RapidFlipFlop it logs the tick and transition. OnDestroy logs how many flip-flop violations were recorded (detected = traceable for debugging).

### Optional: product context (M2)

If your game has multiple “products” (e.g. different scenes or panels), use **CognitiveEngineDriver** with **Use Product Context** enabled. Call `SwitchProduct(newProductId)` when the user switches; the driver uses the productId overloads so the engine rejects wrong-context calls and keeps memory isolated.

---

## Step 8: Memory isolation audit logging

**CognitiveEngineDriver** includes audit logging. In the Inspector: **Log Audit Entries** (default on) logs context switches and session resets; **Log Audit Tick Entries** (default off) adds a per-tick trace (productId, rule, state). Use `driver.GetEngine().AuditLog` or subscribe to `OnAuditEntry` if you want to forward entries to your own logger or analytics.

---

**In production** use the same engine instance and read from `engine.Logs`, `OnStateUpdated`, and (for flip-flop and other violations) `DeterminismViolationLog` or `OnDeterminismViolation` so behavior stays consistent and traceable.
