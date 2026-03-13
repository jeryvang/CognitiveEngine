using System.Linq;
using CognitiveEngine.Core;
using Newtonsoft.Json.Linq;
using Xunit;

namespace CognitiveEngine.Tests
{
    public class SessionExportTests
    {
        [Fact]
        public void ExportSession_EmptySession_HasProductIdAndEmptyTicks()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            var dto = engine.ExportSession();
            Assert.Equal("default", dto.ProductId);
            Assert.NotNull(dto.Ticks);
            Assert.Empty(dto.Ticks);
        }

        [Fact]
        public void ExportSession_AfterTicks_ContainsTickEntries()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions { FixedStep = 0.1f };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.5f, 0f));
            engine.Update(0.3f, 0.3f);
            var dto = engine.ExportSession();
            Assert.Equal("default", dto.ProductId);
            Assert.NotNull(dto.Ticks);
            Assert.True(dto.Ticks.Count >= 1);
            var first = dto.Ticks[0];
            Assert.True(first.TickIndex >= 0);
            Assert.True(first.Timestamp >= 0f);
            Assert.NotNull(first.Signals);
            Assert.NotNull(first.Rule);
            Assert.NotNull(first.State);
            Assert.InRange(first.Confidence, 0f, 1f);
        }

        [Fact]
        public void ExportSessionToJson_ProducesValidJsonWithRootAndTicks()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions { FixedStep = 0.1f };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            engine.InjectSignal(new InputSignal(SignalType.ConfirmIntent, 0.9f, 0.1f));
            engine.Update(0.1f, 0.1f);
            string json = engine.ExportSessionToJson();
            Assert.NotNull(json);
            var root = JObject.Parse(json);
            Assert.True(root.ContainsKey("ProductId"));
            Assert.True(root.ContainsKey("Ticks"));
            var ticks = root["Ticks"] as JArray;
            Assert.NotNull(ticks);
            Assert.True(ticks.Count >= 1);
            var firstTick = ticks[0] as JObject;
            Assert.NotNull(firstTick);
            Assert.True(firstTick.ContainsKey("Timestamp"));
            Assert.True(firstTick.ContainsKey("State"));
            Assert.True(firstTick.ContainsKey("Rule"));
            Assert.True(firstTick.ContainsKey("Confidence"));
            Assert.True(firstTick.ContainsKey("Signals"));
        }

        [Fact]
        public void ExportSession_TickEntries_MatchEngineLogs()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions { FixedStep = 0.1f };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.5f, 0f));
            engine.Update(0.2f, 0.2f);
            var dto = engine.ExportSession();
            var logs = engine.Logs.ToList();
            Assert.Equal(logs.Count, dto.Ticks.Count);
            for (int i = 0; i < logs.Count; i++)
            {
                Assert.Equal(logs[i].TickIndex, dto.Ticks[i].TickIndex);
                Assert.Equal(logs[i].Timestamp, dto.Ticks[i].Timestamp);
                Assert.Equal(logs[i].TriggeredRule, dto.Ticks[i].Rule);
                Assert.Equal(logs[i].FinalState.ToString(), dto.Ticks[i].State);
                Assert.Equal(logs[i].Confidence, dto.Ticks[i].Confidence);
            }
        }

        [Fact]
        public void ExportSession_AfterContextSwitch_ExportsActiveSessionOnly()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions { FixedStep = 0.1f };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.3f, 0f));
            engine.Update(0.1f, 0.1f);
            engine.HandleProductContextSwitch("product-B");
            engine.InjectSignal(new InputSignal(SignalType.ConfirmIntent, 0.8f, 0.2f));
            engine.Update(0.1f, 0.2f);
            var dto = engine.ExportSession();
            Assert.Equal("product-B", dto.ProductId);
            Assert.True(dto.Ticks.Count >= 1);
        }
    }
}
