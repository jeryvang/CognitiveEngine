using System;
using CognitiveEngine.Core;
using Xunit;

namespace CognitiveEngine.Tests
{
    public class SessionMemoryContextTests
    {
        [Fact]
        public void Constructor_StoresProductId()
        {
            var session = new SessionMemoryContext("product-A");
            Assert.Equal("product-A", session.ProductId);
        }

        [Fact]
        public void Constructor_Throws_OnNullProductId()
        {
            Assert.Throws<ArgumentException>(() => new SessionMemoryContext(null!));
        }

        [Fact]
        public void Constructor_Throws_OnEmptyProductId()
        {
            Assert.Throws<ArgumentException>(() => new SessionMemoryContext(""));
        }

        [Fact]
        public void Constructor_Throws_OnWhitespaceProductId()
        {
            Assert.Throws<ArgumentException>(() => new SessionMemoryContext("   "));
        }

        [Fact]
        public void ReadOnlyLogs_Empty_OnCreate()
        {
            var session = new SessionMemoryContext("product-A");
            Assert.Empty(session.ReadOnlyLogs);
        }

        [Fact]
        public void Reset_ClearsAllInternalState()
        {
            var session = new SessionMemoryContext("product-A");

            session.SignalWindow.Add(new InputSignal(SignalType.DwellTime, 1.0f, 0.1f));
            session.Signals[SignalType.DwellTime] = 0.9f;
            session.CurrentState = StateType.Hesitation;
            session.TickIndex    = 5;

            session.Reset();

            Assert.Empty(session.SignalWindow);
            Assert.Empty(session.Signals);
            Assert.Empty(session.Logs);
            Assert.Equal(StateType.Neutral, session.CurrentState);
            Assert.Equal(0, session.TickIndex);
        }

        [Fact]
        public void TwoSessions_DoNotShareSignalWindows()
        {
            var sessionA = new SessionMemoryContext("product-A");
            var sessionB = new SessionMemoryContext("product-B");

            sessionA.SignalWindow.Add(new InputSignal(SignalType.DwellTime, 1.0f, 0.1f));

            Assert.Single(sessionA.SignalWindow);
            Assert.Empty(sessionB.SignalWindow);
        }

        [Fact]
        public void TwoSessions_DoNotShareSignals()
        {
            var sessionA = new SessionMemoryContext("product-A");
            var sessionB = new SessionMemoryContext("product-B");

            sessionA.Signals[SignalType.ConfirmIntent] = 0.9f;

            Assert.True(sessionA.Signals.ContainsKey(SignalType.ConfirmIntent));
            Assert.False(sessionB.Signals.ContainsKey(SignalType.ConfirmIntent));
        }

        [Fact]
        public void TwoSessions_DoNotShareLogs()
        {
            var sessionA = new SessionMemoryContext("product-A");
            var sessionB = new SessionMemoryContext("product-B");

            var log = new TickLog(1, 0.1f, sessionA.Signals, "NoSignals", StateType.Neutral, 0f);
            sessionA.Logs.Add(log);

            Assert.Single(sessionA.ReadOnlyLogs);
            Assert.Empty(sessionB.ReadOnlyLogs);
        }

        [Fact]
        public void TwoSessions_DoNotShareCurrentState()
        {
            var sessionA = new SessionMemoryContext("product-A");
            var sessionB = new SessionMemoryContext("product-B");

            sessionA.CurrentState = StateType.ReadyToConfirm;

            Assert.Equal(StateType.ReadyToConfirm, sessionA.CurrentState);
            Assert.Equal(StateType.Neutral,        sessionB.CurrentState);
        }

        [Fact]
        public void Engine_ActiveProductId_IsDefault_OnCreate()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            Assert.Equal("default", engine.ActiveProductId);
        }

        [Fact]
        public void Engine_Logs_ReflectsActiveSession()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();

            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0.1f));
            engine.Update(0.1f, 0.1f);

            Assert.NotEmpty(engine.Logs);
            Assert.Equal("default", engine.ActiveProductId);
        }

        [Fact]
        public void Engine_ResetSession_ClearsActiveSessionLogs()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();

            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0.1f));
            engine.Update(0.1f, 0.1f);
            Assert.NotEmpty(engine.Logs);

            engine.ResetSession();
            Assert.Empty(engine.Logs);
        }
    }
}
