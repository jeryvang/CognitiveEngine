using System.Linq;
using CognitiveEngine.Core;
using Xunit;

namespace CognitiveEngine.Tests
{
    public class ConfidenceModelRefinementTests
    {
        [Fact]
        public void Confidence_DecaysWhenSignalsDrop()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions
            {
                FixedStep = 0.1f,
                WindowDuration = 0.3f,
                ConfidenceSmoothingAlpha = 0.5f,
                ConfidenceDecayRate = 4f,
                ConfidenceMinChange = 0f
            };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);

            for (int i = 0; i < 5; i++)
            {
                float t = i * 0.1f;
                engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.5f, t));
                engine.Update(0.1f, t + 0.1f);
            }
            float withSignals = engine.Logs.Last().Confidence;
            Assert.True(withSignals > 0.1f);

            for (int i = 5; i < 25; i++)
            {
                float t = i * 0.1f;
                engine.Update(0.1f, t);
            }
            float afterNoSignals = engine.Logs.Last().Confidence;
            Assert.True(afterNoSignals < withSignals);
        }

        [Fact]
        public void Confidence_SmoothingReducesErraticFluctuation()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions
            {
                FixedStep = 0.1f,
                WindowDuration = 2f,
                ConfidenceSmoothingAlpha = 0.2f,
                ConfidenceDecayRate = 0f,
                ConfidenceMinChange = 0.02f
            };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);

            for (int i = 0; i < 20; i++)
            {
                float t = i * 0.1f;
                engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.4f + (i % 3) * 0.05f, t));
                engine.Update(0.1f, t + 0.1f);
            }
            var confidences = engine.Logs.Skip(5).Select(l => l.Confidence).ToList();
            for (int i = 1; i < confidences.Count; i++)
            {
                float delta = System.Math.Abs(confidences[i] - confidences[i - 1]);
                Assert.True(delta < 0.35f);
            }
        }

        [Fact]
        public void Confidence_RespondsToSignalStrength()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions
            {
                FixedStep = 0.1f,
                WindowDuration = 1f,
                ConfidenceSmoothingAlpha = 0.5f,
                ConfidenceDecayRate = 0f,
                ConfidenceMinChange = 0f
            };
            var engineLow = new CognitiveEngine.Core.CognitiveEngine(options);
            var optionsHigh = new CognitiveEngine.Core.CognitiveEngine.EngineOptions
            {
                FixedStep = 0.1f,
                WindowDuration = 1f,
                ConfidenceSmoothingAlpha = 0.5f,
                ConfidenceDecayRate = 0f,
                ConfidenceMinChange = 0f
            };
            var engineHigh = new CognitiveEngine.Core.CognitiveEngine(optionsHigh);

            for (int i = 0; i < 15; i++)
            {
                float t = i * 0.1f;
                engineLow.InjectSignal(new InputSignal(SignalType.DwellTime, 0.2f, t));
                engineLow.Update(0.1f, t + 0.1f);
            }
            for (int i = 0; i < 15; i++)
            {
                float t = i * 0.1f;
                engineHigh.InjectSignal(new InputSignal(SignalType.DwellTime, 0.9f, t));
                engineHigh.Update(0.1f, t + 0.1f);
            }
            float confLow = engineLow.Logs.Last().Confidence;
            float confHigh = engineHigh.Logs.Last().Confidence;
            Assert.True(confHigh > confLow);
        }

        [Fact]
        public void Confidence_DefaultOptions_PreservesRawBehavior()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions
            {
                WindowDuration = 0.2f,
                FixedStep = 0.1f
            };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0f));
            engine.Update(0.3f, 0.3f);
            Assert.False(engine.Logs.Last().Signals.ContainsKey(SignalType.DwellTime));
        }
    }
}
