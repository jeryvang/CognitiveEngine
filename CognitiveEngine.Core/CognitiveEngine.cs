using System;
using System.Collections.Generic;
using System.Linq;

namespace CognitiveEngine.Core;

public class CognitiveEngine
{
    public class EngineOptions
    {
        public float DecayFactor { get; set; } = 0.9f;
        public float WindowDuration { get; set; } = 2.0f;
        public float FixedStep { get; set; } = 0.1f;
        public int MaxLogs { get; set; } = 1000;
        public int MaxSignalWindow { get; set; } = 4096;
        public int MaxSwitchLog { get; set; } = 100;
        public int MaxBoundaryViolationLog { get; set; } = 100;
        public int MaxDeterminismViolationLog { get; set; } = 100;
        public int MaxAuditLog { get; set; } = 1000;
    }

    private const int RecentStateHistorySize = 4;

    private readonly float _decayFactor;
    private readonly float _windowDuration;
    private readonly float _fixedStep;
    private readonly int _maxLogs;
    private readonly int _maxSignalWindow;
    private readonly int _maxSwitchLog;
    private readonly int _maxBoundaryViolationLog;
    private readonly int _maxDeterminismViolationLog;
    private readonly int _maxAuditLog;

    private readonly Dictionary<string, SessionMemoryContext> _sessions =
        new Dictionary<string, SessionMemoryContext>();

    private readonly List<ContextSwitchRecord> _switchLog = new List<ContextSwitchRecord>();
    private readonly List<BoundaryViolationRecord> _boundaryViolations = new List<BoundaryViolationRecord>();
    private readonly List<DeterminismViolationRecord> _determinismViolations = new List<DeterminismViolationRecord>();
    private readonly List<AuditEntry> _auditLog = new List<AuditEntry>();

    private SessionMemoryContext _activeSession;

    private float _accumulatedTime = 0f;

    public event Action<CognitiveState>? OnStateUpdated;

    public string ActiveProductId => _activeSession.ProductId;

    public IReadOnlyList<TickLog> Logs => _activeSession.ReadOnlyLogs;

    public IReadOnlyList<ContextSwitchRecord> SwitchLog => _switchLog;

    public IReadOnlyList<BoundaryViolationRecord> BoundaryViolationLog => _boundaryViolations;

    public IReadOnlyList<DeterminismViolationRecord> DeterminismViolationLog => _determinismViolations;

    public IReadOnlyList<AuditEntry> AuditLog => _auditLog;

    public event Action<BoundaryViolationRecord>? OnBoundaryViolation;

    public event Action<DeterminismViolationRecord>? OnDeterminismViolation;

    public event Action<AuditEntry>? OnAuditEntry;

    public CognitiveEngine()
        : this(null)
    {
    }

    public CognitiveEngine(EngineOptions? options)
    {
        var o = options ?? new EngineOptions();

        _decayFactor     = o.DecayFactor;
        _windowDuration  = o.WindowDuration;
        _fixedStep       = o.FixedStep;
        _maxLogs         = o.MaxLogs;
        _maxSignalWindow = o.MaxSignalWindow;
        _maxSwitchLog    = o.MaxSwitchLog;
        _maxBoundaryViolationLog = o.MaxBoundaryViolationLog;
        _maxDeterminismViolationLog = o.MaxDeterminismViolationLog;
        _maxAuditLog = o.MaxAuditLog;

        _activeSession = CreateSession("default");
    }

    private void AppendAudit(AuditEntry entry)
    {
        _auditLog.Add(entry);
        if (_auditLog.Count > _maxAuditLog)
            _auditLog.RemoveAt(0);
        OnAuditEntry?.Invoke(entry);
    }

    private void RecordAndThrowBoundaryViolation(BoundaryViolationKind kind, string actualProductId)
    {
        string expected = _activeSession.ProductId;
        var record = new BoundaryViolationRecord(kind, expected, actualProductId);
        _boundaryViolations.Add(record);
        if (_boundaryViolations.Count > _maxBoundaryViolationLog)
            _boundaryViolations.RemoveAt(0);
        OnBoundaryViolation?.Invoke(record);
        throw new InvalidOperationException(
            $"Boundary violation: {kind} called with productId '{actualProductId}' but active product is '{expected}'.");
    }

    private void GuardProductId(string productId, BoundaryViolationKind kind)
    {
        if (productId != _activeSession.ProductId)
            RecordAndThrowBoundaryViolation(kind, productId);
    }

    public void HandleProductContextSwitch(string newProductId)
    {
        if (string.IsNullOrWhiteSpace(newProductId))
            throw new ArgumentException("ProductId cannot be null or empty.", nameof(newProductId));

        if (newProductId == _activeSession.ProductId)
            return;

        string fromId = _activeSession.ProductId;

        if (!_sessions.TryGetValue(newProductId, out var incoming))
            incoming = CreateSession(newProductId);

        incoming.Reset();
        _accumulatedTime = 0f;
        _activeSession = incoming;

        _switchLog.Add(new ContextSwitchRecord(fromId, newProductId));
        if (_switchLog.Count > _maxSwitchLog)
            _switchLog.RemoveAt(0);
        AppendAudit(AuditEntry.ContextSwitch(fromId, newProductId));
    }

    public void InjectSignal(InputSignal signal)
    {
        if (_activeSession.SignalWindow.Count >= _maxSignalWindow)
            return;

        _activeSession.SignalWindow.Add(signal);
    }

    public void InjectSignal(InputSignal signal, string productId)
    {
        GuardProductId(productId, BoundaryViolationKind.InjectSignal);
        if (_activeSession.SignalWindow.Count >= _maxSignalWindow)
            return;

        _activeSession.SignalWindow.Add(signal);
    }

    public void Update(float deltaTime, float currentTime)
    {
        _accumulatedTime += deltaTime;

        while (_accumulatedTime >= _fixedStep)
        {
            float tickTime = currentTime - (_accumulatedTime - _fixedStep);
            Tick(tickTime);
            _accumulatedTime -= _fixedStep;
        }
    }

    public void Update(float deltaTime, float currentTime, string productId)
    {
        GuardProductId(productId, BoundaryViolationKind.Update);
        _accumulatedTime += deltaTime;

        while (_accumulatedTime >= _fixedStep)
        {
            float tickTime = currentTime - (_accumulatedTime - _fixedStep);
            Tick(tickTime);
            _accumulatedTime -= _fixedStep;
        }
    }

    public void ResetSession()
    {
        string productId = _activeSession.ProductId;
        _activeSession.Reset();
        _accumulatedTime = 0f;
        AppendAudit(AuditEntry.SessionReset(productId));
    }

    private SessionMemoryContext CreateSession(string productId)
    {
        var session = new SessionMemoryContext(productId);
        _sessions[productId] = session;
        return session;
    }

    private void Tick(float timestamp)
    {
        _activeSession.TickIndex++;

        CleanWindow(timestamp);
        AggregateSignals();
        ApplyDecay();

        var context  = new RuntimeMemoryContext(_activeSession.TickIndex, timestamp, _activeSession.Signals);
        var snapshot = new EvaluationSnapshot(context, _activeSession.CurrentState, _windowDuration);

        var (resolvedState, ruleName, confidence) = snapshot.Evaluate();

        var violations = DeterministicIntegrityValidator.Validate(
            _activeSession.TickIndex, _activeSession.CurrentState, resolvedState, ruleName, _activeSession.RecentStates);
        foreach (var v in violations)
        {
            _determinismViolations.Add(v);
            if (_determinismViolations.Count > _maxDeterminismViolationLog)
                _determinismViolations.RemoveAt(0);
            OnDeterminismViolation?.Invoke(v);
        }

        LogTick(context, ruleName, resolvedState, confidence);

        if (resolvedState != _activeSession.CurrentState)
        {
            _activeSession.CurrentState = resolvedState;
            OnStateUpdated?.Invoke(new CognitiveState(_activeSession.CurrentState, confidence, ruleName));
        }

        _activeSession.RecentStates.Add(resolvedState);
        if (_activeSession.RecentStates.Count > RecentStateHistorySize)
            _activeSession.RecentStates.RemoveAt(0);
    }

    private void CleanWindow(float currentTime)
    {
        _activeSession.SignalWindow.RemoveAll(
            s => currentTime - s.Timestamp > _windowDuration);
    }

    private void AggregateSignals()
    {
        _activeSession.Signals.Clear();

        foreach (var signal in _activeSession.SignalWindow)
        {
            if (!_activeSession.Signals.ContainsKey(signal.Type))
                _activeSession.Signals[signal.Type] = 0f;

            _activeSession.Signals[signal.Type] += signal.Value;
        }
    }

    private void ApplyDecay()
    {
        var orderedKeys = _activeSession.Signals.Keys.OrderBy(k => k).ToList();

        foreach (var key in orderedKeys)
        {
            _activeSession.Signals[key] *= _decayFactor;
        }
    }

    private void LogTick(RuntimeMemoryContext context, string rule, StateType state, float confidence)
    {
        var log = new TickLog(context.TickIndex, context.Timestamp, context.Signals, rule, state, confidence);
        _activeSession.Logs.Add(log);

        if (_activeSession.Logs.Count > _maxLogs)
            _activeSession.Logs.RemoveAt(0);

        AppendAudit(AuditEntry.Tick(context.Timestamp, _activeSession.ProductId, context.TickIndex, rule, state));
    }
}
