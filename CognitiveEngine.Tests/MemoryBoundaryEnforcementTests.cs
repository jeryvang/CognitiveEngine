using System;
using CognitiveEngine.Core;
using Xunit;

namespace CognitiveEngine.Tests
{
    public class MemoryBoundaryEnforcementTests
    {
        [Fact]
        public void InjectSignal_WithMatchingProductId_Accepts()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            engine.HandleProductContextSwitch("product-A");
            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0.1f), "product-A");
            engine.Update(0.1f, 0.1f);
            Assert.Single(engine.Logs);
            Assert.Empty(engine.BoundaryViolationLog);
        }

        [Fact]
        public void InjectSignal_WithWrongProductId_ThrowsAndLogs()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            engine.HandleProductContextSwitch("product-A");
            var ex = Assert.Throws<InvalidOperationException>(
                () => engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0.1f), "product-B"));
            Assert.Contains("product-B", ex.Message);
            Assert.Contains("product-A", ex.Message);
            Assert.Contains("InjectSignal", ex.Message);
            Assert.Single(engine.BoundaryViolationLog);
            Assert.Equal(BoundaryViolationKind.InjectSignal, engine.BoundaryViolationLog[0].Kind);
            Assert.Equal("product-A", engine.BoundaryViolationLog[0].ExpectedProductId);
            Assert.Equal("product-B", engine.BoundaryViolationLog[0].ActualProductId);
        }

        [Fact]
        public void InjectSignal_WithWrongProductId_DoesNotInject()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            engine.HandleProductContextSwitch("product-A");
            try
            {
                engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0.1f), "product-B");
            }
            catch (InvalidOperationException) { }
            engine.Update(0.1f, 0.1f);
            Assert.Single(engine.Logs);
            Assert.False(engine.Logs[0].Signals.ContainsKey(SignalType.DwellTime));
        }

        [Fact]
        public void Update_WithMatchingProductId_Accepts()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            engine.HandleProductContextSwitch("product-A");
            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0.1f));
            engine.Update(0.1f, 0.1f, "product-A");
            Assert.Single(engine.Logs);
            Assert.Empty(engine.BoundaryViolationLog);
        }

        [Fact]
        public void Update_WithWrongProductId_ThrowsAndLogs()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            engine.HandleProductContextSwitch("product-A");
            var ex = Assert.Throws<InvalidOperationException>(
                () => engine.Update(0.1f, 0.1f, "product-B"));
            Assert.Contains("product-B", ex.Message);
            Assert.Contains("product-A", ex.Message);
            Assert.Contains("Update", ex.Message);
            Assert.Single(engine.BoundaryViolationLog);
            Assert.Equal(BoundaryViolationKind.Update, engine.BoundaryViolationLog[0].Kind);
            Assert.Equal("product-A", engine.BoundaryViolationLog[0].ExpectedProductId);
            Assert.Equal("product-B", engine.BoundaryViolationLog[0].ActualProductId);
        }

        [Fact]
        public void Update_WithWrongProductId_DoesNotTick()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            engine.HandleProductContextSwitch("product-A");
            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0.1f));
            try
            {
                engine.Update(0.1f, 0.1f, "product-B");
            }
            catch (InvalidOperationException) { }
            Assert.Empty(engine.Logs);
        }

        [Fact]
        public void OnBoundaryViolation_Raised_WhenInjectSignalWrongProductId()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            engine.HandleProductContextSwitch("product-A");
            BoundaryViolationRecord? received = null;
            engine.OnBoundaryViolation += r => received = r;
            try
            {
                engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0.1f), "product-B");
            }
            catch (InvalidOperationException) { }
            Assert.NotNull(received);
            Assert.Equal(BoundaryViolationKind.InjectSignal, received.Value.Kind);
            Assert.Equal("product-A", received.Value.ExpectedProductId);
            Assert.Equal("product-B", received.Value.ActualProductId);
        }

        [Fact]
        public void OnBoundaryViolation_Raised_WhenUpdateWrongProductId()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            engine.HandleProductContextSwitch("product-A");
            BoundaryViolationRecord? received = null;
            engine.OnBoundaryViolation += r => received = r;
            try
            {
                engine.Update(0.1f, 0.1f, "product-B");
            }
            catch (InvalidOperationException) { }
            Assert.NotNull(received);
            Assert.Equal(BoundaryViolationKind.Update, received.Value.Kind);
        }

        [Fact]
        public void Parameterless_InjectSignal_And_Update_Unchanged_NoProductIdGuard()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0.1f));
            engine.Update(0.1f, 0.1f);
            Assert.Single(engine.Logs);
            Assert.Empty(engine.BoundaryViolationLog);
        }

        [Fact]
        public void BoundaryViolationLog_CappedAtMax()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions { MaxBoundaryViolationLog = 2 };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            engine.HandleProductContextSwitch("A");
            void ThrowInject(string productId)
            {
                try { engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1f, 0f), productId); } catch (InvalidOperationException) { }
            }
            ThrowInject("B");
            ThrowInject("C");
            Assert.Equal(2, engine.BoundaryViolationLog.Count);
            ThrowInject("D");
            Assert.Equal(2, engine.BoundaryViolationLog.Count);
            Assert.Equal("C", engine.BoundaryViolationLog[0].ActualProductId);
            Assert.Equal("D", engine.BoundaryViolationLog[1].ActualProductId);
        }
    }
}
