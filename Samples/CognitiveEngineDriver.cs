using System;
using UnityEngine;
using CognitiveEngine.Core;

namespace CognitiveEngine.Samples
{
    public enum CognitiveEngineInputMode
    {
        Discrete,
        Streaming
    }

    public class CognitiveEngineDriver : MonoBehaviour
    {
        [Header("Input mode")]
        [SerializeField] private CognitiveEngineInputMode inputMode = CognitiveEngineInputMode.Discrete;

        [Header("Streaming inputs (used when Input mode = Streaming)")]
        [SerializeField] private float streamingDwellInput;
        [SerializeField] private float streamingConfirmInput;

        private CognitiveEngine.Core.CognitiveEngine _engine;
        private StreamingCognitiveEngine _streamingEngine;
        private string _currentProductId = "default";

        [Header("Product context (M2, Discrete only)")]
        [SerializeField] private bool useProductContext = false;
        [SerializeField] private string productIdForUpdate = "default";

        [Header("Logging")]
        [SerializeField] private bool logStateChanges = true;
        [SerializeField] private bool logContextSwitch = true;
        [SerializeField] private bool logBoundaryViolations = true;
        [SerializeField] private bool logDeterminismViolations = true;
        [SerializeField] private bool logAuditEntries = true;
        [SerializeField] private bool logAuditTickEntries = false;

        void Start()
        {
            if (inputMode == CognitiveEngineInputMode.Discrete)
            {
                _engine = new CognitiveEngine.Core.CognitiveEngine();
                _engine.OnStateUpdated += OnStateChanged;
                if (logBoundaryViolations)
                    _engine.OnBoundaryViolation += OnBoundaryViolation;
                if (logDeterminismViolations)
                    _engine.OnDeterminismViolation += OnDeterminismViolation;
                if (logAuditEntries)
                    _engine.OnAuditEntry += OnAuditEntry;
            }
            else
            {
                _streamingEngine = new StreamingCognitiveEngine();
                _streamingEngine.OnStateUpdated += OnStateChanged;
            }
        }

        void Update()
        {
            if (inputMode == CognitiveEngineInputMode.Discrete && _engine != null)
            {
                try
                {
                    if (useProductContext)
                        _engine.Update(Time.deltaTime, Time.time, productIdForUpdate);
                    else
                        _engine.Update(Time.deltaTime, Time.time);
                }
                catch (InvalidOperationException e)
                {
                    if (logBoundaryViolations)
                        Debug.LogWarning($"[CognitiveEngine] Boundary: {e.Message}");
                }
            }
            else if (inputMode == CognitiveEngineInputMode.Streaming && _streamingEngine != null)
            {
                float dwell = Application.isEditor && streamingDwellInput == 0f && streamingConfirmInput == 0f
                    ? 0f
                    : streamingDwellInput;
                float confirm = Application.isEditor && streamingDwellInput == 0f && streamingConfirmInput == 0f
                    ? (Input.GetMouseButton(0) ? 1f : 0f)
                    : streamingConfirmInput;
                _streamingEngine.Update(Time.deltaTime, dwell, confirm);
            }
        }

        public void SwitchProduct(string newProductId)
        {
            if (inputMode != CognitiveEngineInputMode.Discrete || _engine == null || string.IsNullOrEmpty(newProductId)) return;
            _engine.HandleProductContextSwitch(newProductId);
            _currentProductId = _engine.ActiveProductId;
            productIdForUpdate = _currentProductId;
            if (logContextSwitch)
                Debug.Log($"[CognitiveEngine] Switched to product: {_currentProductId}");
        }

        public void OnProductFocused()
        {
            Inject(new InputSignal(SignalType.ProductFocus, 1.0f, Time.time));
        }

        public void OnUserDwelling(float strength)
        {
            Inject(new InputSignal(SignalType.DwellTime, strength, Time.time));
        }

        public void OnSwipe(float velocity)
        {
            Inject(new InputSignal(SignalType.SwipeVelocity, velocity, Time.time));
        }

        public void OnComparisonAction()
        {
            Inject(new InputSignal(SignalType.ComparisonAction, 1.0f, Time.time));
        }

        public void OnConfirmIntent()
        {
            Inject(new InputSignal(SignalType.ConfirmIntent, 1.0f, Time.time));
        }

        public void OnContextChanged()
        {
            Inject(new InputSignal(SignalType.ContextChange, 1.0f, Time.time));
        }

        public void SetStreamingInputs(float dwell, float confirm)
        {
            streamingDwellInput = dwell;
            streamingConfirmInput = confirm;
        }

        private void Inject(InputSignal signal)
        {
            if (inputMode != CognitiveEngineInputMode.Discrete || _engine == null) return;
            try
            {
                if (useProductContext)
                    _engine.InjectSignal(signal, _currentProductId);
                else
                    _engine.InjectSignal(signal);
            }
            catch (InvalidOperationException e)
            {
                if (logBoundaryViolations)
                    Debug.LogWarning($"[CognitiveEngine] Boundary: {e.Message}");
            }
        }

        private void OnStateChanged(CognitiveState state)
        {
            if (logStateChanges)
                Debug.Log($"[CognitiveEngine] {state.State} | {state.ReasoningTag} | {state.Confidence:F2}");
        }

        private void OnBoundaryViolation(BoundaryViolationRecord r)
        {
            Debug.Log($"[CognitiveEngine] Boundary {r.Kind} expected={r.ExpectedProductId} actual={r.ActualProductId}");
        }

        private void OnDeterminismViolation(DeterminismViolationRecord r)
        {
            Debug.Log($"[CognitiveEngine] Determinism {r.Kind} tick={r.TickIndex} {r.FromState}→{r.ToState} rule={r.RuleName}");
        }

        private void OnAuditEntry(AuditEntry entry)
        {
            if (entry.Kind == AuditEntryKind.Tick && !logAuditTickEntries) return;
            switch (entry.Kind)
            {
                case AuditEntryKind.ContextSwitch:
                    Debug.Log($"[CognitiveEngine] Audit ContextSwitch productId={entry.ProductId} from={entry.FromProductId} to={entry.ToProductId}");
                    break;
                case AuditEntryKind.SessionReset:
                    Debug.Log($"[CognitiveEngine] Audit SessionReset productId={entry.ProductId}");
                    break;
                case AuditEntryKind.Tick:
                    Debug.Log($"[CognitiveEngine] Audit Tick productId={entry.ProductId} tick={entry.TickIndex} t={entry.Timestamp:F2} rule={entry.RuleName} state={entry.State}");
                    break;
            }
        }

        void OnDestroy()
        {
            if (_engine != null)
            {
                _engine.OnStateUpdated -= OnStateChanged;
                _engine.OnBoundaryViolation -= OnBoundaryViolation;
                _engine.OnDeterminismViolation -= OnDeterminismViolation;
                _engine.OnAuditEntry -= OnAuditEntry;
                if (!useProductContext)
                    _engine.ResetSession();
            }
            if (_streamingEngine != null)
            {
                _streamingEngine.OnStateUpdated -= OnStateChanged;
                _streamingEngine.Reset();
            }
        }

        public CognitiveEngine.Core.CognitiveEngine GetEngine() => _engine;
        public StreamingCognitiveEngine GetStreamingEngine() => _streamingEngine;
        public CognitiveEngineInputMode InputMode => inputMode;
        public CognitiveState CurrentState => inputMode == CognitiveEngineInputMode.Streaming && _streamingEngine != null
            ? _streamingEngine.Current
            : default;
        public string ActiveProductId => _engine != null ? _engine.ActiveProductId : "";
    }
}
