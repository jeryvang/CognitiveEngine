using System;
using System.IO;
using System.Linq;
using CognitiveEngine.Core;
using Xunit;

namespace CognitiveEngine.Tests
{
    public class ConfigDrivenParameterTests
    {
        [Fact]
        public void LoadFromJson_WithCustomDecayFactor_ProducesExpectedBehavior()
        {
            string json = @"{
                ""DecayFactor"": 0.5,
                ""WindowDuration"": 2.0,
                ""FixedStep"": 0.1
            }";
            var options = EngineConfigLoader.LoadFromJson(json);
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);

            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0f));
            engine.Update(0.1f, 0.1f);

            var val = engine.Logs.Last().Signals[SignalType.DwellTime];
            Assert.InRange(val, 0.49f, 0.51f);
        }

        [Fact]
        public void LoadFromJson_WithCustomWindowDuration_ProducesExpectedBehavior()
        {
            string json = @"{
                ""WindowDuration"": 0.2,
                ""FixedStep"": 0.1
            }";
            var options = EngineConfigLoader.LoadFromJson(json);
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);

            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0f));
            engine.Update(0.3f, 0.3f);

            var last = engine.Logs.Last();
            Assert.False(last.Signals.ContainsKey(SignalType.DwellTime));
        }

        [Fact]
        public void LoadFromJson_PartialConfig_UsesDefaultsForMissingValues()
        {
            string json = @"{ ""DecayFactor"": 0.3 }";
            var options = EngineConfigLoader.LoadFromJson(json);

            Assert.Equal(0.3f, options.DecayFactor);
            Assert.Equal(2.0f, options.WindowDuration);
            Assert.Equal(0.1f, options.FixedStep);
            Assert.Equal(1000, options.MaxLogs);
        }

        [Fact]
        public void LoadFromStream_LoadsSameAsLoadFromJson()
        {
            string json = @"{ ""WindowDuration"": 0.5, ""FixedStep"": 0.05 }";
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            var options = EngineConfigLoader.LoadFromStream(stream);

            Assert.Equal(0.5f, options.WindowDuration);
            Assert.Equal(0.05f, options.FixedStep);
        }

        [Fact]
        public void LoadFromFile_LoadsConfigAndEngineBehavesAccordingly()
        {
            string json = @"{
                ""WindowDuration"": 0.2,
                ""FixedStep"": 0.1
            }";
            string path = Path.Combine(Path.GetTempPath(), "cognitive-engine-config-test-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                File.WriteAllText(path, json);
                var options = EngineConfigLoader.LoadFromFile(path);
                var engine = new CognitiveEngine.Core.CognitiveEngine(options);

                engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0f));
                engine.Update(0.3f, 0.3f);

                Assert.False(engine.Logs.Last().Signals.ContainsKey(SignalType.DwellTime));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void LoadFromJson_EmptyObject_UsesAllDefaults()
        {
            var options = EngineConfigLoader.LoadFromJson("{}");
            var defaults = new CognitiveEngine.Core.CognitiveEngine.EngineOptions();

            Assert.Equal(defaults.DecayFactor, options.DecayFactor);
            Assert.Equal(defaults.WindowDuration, options.WindowDuration);
            Assert.Equal(defaults.FixedStep, options.FixedStep);
            Assert.Equal(defaults.MaxLogs, options.MaxLogs);
            Assert.Equal(defaults.MaxSignalWindow, options.MaxSignalWindow);
        }

        [Fact]
        public void ToEngineOptions_WithNullConfig_ReturnsDefaults()
        {
            var options = EngineConfigLoader.ToEngineOptions(null!);
            var defaults = new CognitiveEngine.Core.CognitiveEngine.EngineOptions();

            Assert.Equal(defaults.DecayFactor, options.DecayFactor);
            Assert.Equal(defaults.WindowDuration, options.WindowDuration);
        }
    }
}
