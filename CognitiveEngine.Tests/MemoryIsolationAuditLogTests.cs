using System.Collections.Generic;
using System.Linq;
using CognitiveEngine.Core;
using Xunit;

namespace CognitiveEngine.Tests
{
    public class MemoryIsolationAuditLogTests
    {
        [Fact]
        public void AuditLog_ContextSwitch_AppendsEntry()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            Assert.Empty(engine.AuditLog);
            engine.HandleProductContextSwitch("product-B");
            Assert.Single(engine.AuditLog);
            var e = engine.AuditLog[0];
            Assert.Equal(AuditEntryKind.ContextSwitch, e.Kind);
            Assert.Equal("default", e.FromProductId);
            Assert.Equal("product-B", e.ToProductId);
            Assert.Equal("product-B", e.ProductId);
        }

        [Fact]
        public void AuditLog_SessionReset_AppendsEntry()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.5f, 0.1f));
            engine.ResetSession();
            var resets = engine.AuditLog.Where(x => x.Kind == AuditEntryKind.SessionReset).ToList();
            Assert.Single(resets);
            Assert.Equal("default", resets[0].ProductId);
        }

        [Fact]
        public void AuditLog_Tick_AppendsEntryWithProductIdRuleAndState()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.5f, 0.1f));
            engine.Update(0.1f, 0.1f);
            var ticks = engine.AuditLog.Where(x => x.Kind == AuditEntryKind.Tick).ToList();
            Assert.Single(ticks);
            Assert.Equal("default", ticks[0].ProductId);
            Assert.True(ticks[0].TickIndex > 0);
            Assert.False(string.IsNullOrEmpty(ticks[0].RuleName));
            Assert.True(System.Enum.IsDefined(typeof(StateType), ticks[0].State));
        }

        [Fact]
        public void AuditLog_OrderedTrace_ContextSwitchThenTick()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            engine.HandleProductContextSwitch("product-A");
            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.5f, 0.1f));
            engine.Update(0.1f, 0.1f);
            Assert.True(engine.AuditLog.Count >= 2);
            Assert.Equal(AuditEntryKind.ContextSwitch, engine.AuditLog[0].Kind);
            Assert.Equal(AuditEntryKind.Tick, engine.AuditLog[1].Kind);
            Assert.Equal("product-A", engine.AuditLog[1].ProductId);
        }

        [Fact]
        public void AuditLog_OnAuditEntry_RaisedForEachEntry()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            var received = new List<AuditEntry>();
            engine.OnAuditEntry += received.Add;
            engine.HandleProductContextSwitch("product-B");
            engine.Update(0.1f, 0.1f);
            Assert.True(received.Count >= 2);
            Assert.Equal(AuditEntryKind.ContextSwitch, received[0].Kind);
            Assert.Contains(received, e => e.Kind == AuditEntryKind.Tick);
        }

        [Fact]
        public void AuditLog_CappedAtMaxAuditLog()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions
            {
                MaxAuditLog = 10,
                FixedStep = 0.1f
            };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            for (int i = 0; i < 50; i++)
            {
                engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.2f, i * 0.1f));
                engine.Update(0.1f, (i + 1) * 0.1f);
            }
            Assert.Equal(10, engine.AuditLog.Count);
        }

        [Fact]
        public void AuditLog_TickEntry_HasTimestamp()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions { FixedStep = 0.1f };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            engine.Update(0.1f, 1.0f);
            var tickEntry = engine.AuditLog.First(e => e.Kind == AuditEntryKind.Tick);
            Assert.True(tickEntry.Timestamp > 0f);
        }
    }
}
