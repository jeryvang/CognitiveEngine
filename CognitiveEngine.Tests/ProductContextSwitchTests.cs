using System;
using CognitiveEngine.Core;
using Xunit;

namespace CognitiveEngine.Tests
{
    public class ProductContextSwitchTests
    {
        [Fact]
        public void HandleProductContextSwitch_ChangesActiveProductId()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            Assert.Equal("default", engine.ActiveProductId);

            engine.HandleProductContextSwitch("product-B");
            Assert.Equal("product-B", engine.ActiveProductId);

            engine.HandleProductContextSwitch("product-C");
            Assert.Equal("product-C", engine.ActiveProductId);
        }

        [Fact]
        public void HandleProductContextSwitch_CreatesSession_WhenNewProductId()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            engine.HandleProductContextSwitch("new-product");
            Assert.Equal("new-product", engine.ActiveProductId);
            Assert.Empty(engine.Logs);
        }

        [Fact]
        public void HandleProductContextSwitch_ReusesSession_WhenExistingProductId()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            engine.HandleProductContextSwitch("product-B");
            engine.HandleProductContextSwitch("default");
            engine.HandleProductContextSwitch("product-B");
            Assert.Equal("product-B", engine.ActiveProductId);
        }

        [Fact]
        public void HandleProductContextSwitch_ResetsIncomingSession_NoCognitiveDrift()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0.1f));
            engine.Update(0.1f, 0.1f);
            Assert.NotEmpty(engine.Logs);

            engine.HandleProductContextSwitch("product-B");
            Assert.Empty(engine.Logs);
            Assert.Equal("product-B", engine.ActiveProductId);

            engine.HandleProductContextSwitch("default");
            Assert.Empty(engine.Logs);
        }

        [Fact]
        public void HandleProductContextSwitch_LogsStructuredSwitchEvent()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            Assert.Empty(engine.SwitchLog);

            engine.HandleProductContextSwitch("product-B");
            Assert.Single(engine.SwitchLog);
            Assert.Equal("default", engine.SwitchLog[0].FromProductId);
            Assert.Equal("product-B", engine.SwitchLog[0].ToProductId);

            engine.HandleProductContextSwitch("product-C");
            Assert.Equal(2, engine.SwitchLog.Count);
            Assert.Equal("product-B", engine.SwitchLog[1].FromProductId);
            Assert.Equal("product-C", engine.SwitchLog[1].ToProductId);
        }

        [Fact]
        public void HandleProductContextSwitch_NoOp_WhenSameProductId()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            engine.HandleProductContextSwitch("product-B");
            int countBefore = engine.SwitchLog.Count;

            engine.HandleProductContextSwitch("product-B");
            Assert.Equal("product-B", engine.ActiveProductId);
            Assert.Equal(countBefore, engine.SwitchLog.Count);
        }

        [Fact]
        public void HandleProductContextSwitch_Throws_OnNullProductId()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            Assert.Throws<ArgumentException>(() => engine.HandleProductContextSwitch(null!));
        }

        [Fact]
        public void HandleProductContextSwitch_Throws_OnEmptyProductId()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            Assert.Throws<ArgumentException>(() => engine.HandleProductContextSwitch(""));
        }

        [Fact]
        public void HandleProductContextSwitch_Throws_OnWhitespaceProductId()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            Assert.Throws<ArgumentException>(() => engine.HandleProductContextSwitch("   "));
        }

        [Fact]
        public void HandleProductContextSwitch_ResetsAccumulatedTime()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions { FixedStep = 0.1f };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            engine.Update(0.05f, 0.05f);
            engine.HandleProductContextSwitch("product-B");
            engine.Update(0.05f, 0.1f);
            Assert.Empty(engine.Logs);
            engine.Update(0.05f, 0.15f);
            Assert.Single(engine.Logs);
        }

        [Fact]
        public void SwitchLog_CappedAtMaxSwitchLog()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions { MaxSwitchLog = 3 };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);

            engine.HandleProductContextSwitch("A");
            engine.HandleProductContextSwitch("B");
            engine.HandleProductContextSwitch("C");
            Assert.Equal(3, engine.SwitchLog.Count);

            engine.HandleProductContextSwitch("D");
            Assert.Equal(3, engine.SwitchLog.Count);
            Assert.Equal("A", engine.SwitchLog[0].FromProductId);
            Assert.Equal("B", engine.SwitchLog[0].ToProductId);
            Assert.Equal("D", engine.SwitchLog[2].ToProductId);
        }
    }
}
