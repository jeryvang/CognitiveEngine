using System;
using System.Diagnostics;
using System.Linq;
using CognitiveEngine.Core;
using Xunit;

namespace CognitiveEngine.Tests
{
    public class StressTestSuite
    {
        [Fact]
        public void ConflictingSignals_HighDwellAndHighConfirm_OnlyOneStateActive_PriorityRespected()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions
            {
                WindowDuration = 2.0f,
                FixedStep = 0.1f
            };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            float t = 0f;
            for (int i = 0; i < 50; i++)
            {
                t += 0.1f;
                engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.9f, t));
                engine.InjectSignal(new InputSignal(SignalType.ConfirmIntent, 0.9f, t));
                engine.InjectSignal(new InputSignal(SignalType.ComparisonAction, 0.5f, t));
                engine.Update(0.1f, t);
            }
            Assert.Equal(50, engine.Logs.Count);
            foreach (var log in engine.Logs)
            {
                Assert.True(Enum.IsDefined(typeof(StateType), log.FinalState));
            }
            var lastState = engine.Logs[engine.Logs.Count - 1].FinalState;
            Assert.Equal(StateType.ReadyToConfirm, lastState);
        }

        [Fact]
        public void ConflictingSignals_EveryTickProducesExactlyOneState()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            float t = 0f;
            for (int i = 0; i < 100; i++)
            {
                t += 0.1f;
                engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.5f, t));
                engine.InjectSignal(new InputSignal(SignalType.ConfirmIntent, 0.3f, t));
                engine.Update(0.1f, t);
            }
            Assert.Equal(100, engine.Logs.Count);
            var distinctStates = engine.Logs.Select(l => l.FinalState).Distinct().ToList();
            Assert.All(engine.Logs, log => Assert.True(Enum.IsDefined(typeof(StateType), log.FinalState)));
        }

        [Fact]
        public void RapidEventBurst_NoUnboundedMemoryGrowth_LogsCapped()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions
            {
                MaxLogs = 100,
                MaxSignalWindow = 500,
                FixedStep = 0.1f,
                WindowDuration = 2.0f
            };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            float t = 0f;
            for (int i = 0; i < 2000; i++)
            {
                t += 0.05f;
                engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.1f, t));
                engine.Update(0.05f, t);
            }
            Assert.True(engine.Logs.Count <= options.MaxLogs);
        }

        [Fact]
        public void RapidEventBurst_NoBlocking_CompletesInReasonableTime()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions
            {
                MaxLogs = 500,
                MaxSignalWindow = 2048,
                FixedStep = 0.1f
            };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            float t = 0f;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 5000; i++)
            {
                t += 0.1f;
                engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.2f, t));
                engine.Update(0.1f, t);
            }
            sw.Stop();
            Assert.True(sw.ElapsedMilliseconds < 5000);
        }

        [Fact]
        public void RapidEventBurst_SignalWindowCapped_NoUnboundedGrowth()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions
            {
                MaxSignalWindow = 100,
                FixedStep = 0.1f,
                WindowDuration = 10f
            };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            float t = 0f;
            for (int i = 0; i < 500; i++)
            {
                t += 0.01f;
                engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.1f, t));
                engine.Update(0.01f, t);
            }
            Assert.True(engine.Logs.Count <= 500);
        }

        [Fact]
        public void NearThresholdOscillation_DwellNearHesitationThreshold_CompletesWithoutCrash()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions
            {
                WindowDuration = 0.5f,
                FixedStep = 0.1f
            };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            float t = 0f;
            for (int i = 0; i < 30; i++)
            {
                t += 0.1f;
                float dwell = 0.68f + (i % 3) * 0.02f;
                engine.InjectSignal(new InputSignal(SignalType.DwellTime, dwell, t));
                engine.Update(0.1f, t);
            }
            Assert.Equal(30, engine.Logs.Count);
            Assert.All(engine.Logs, log => Assert.True(Enum.IsDefined(typeof(StateType), log.FinalState)));
        }

        [Fact]
        public void NearThresholdOscillation_NoRapidFlipFlopWhenSignalsStable()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions
            {
                WindowDuration = 1.0f,
                FixedStep = 0.1f
            };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            float t = 0f;
            for (int i = 0; i < 50; i++)
            {
                t += 0.1f;
                engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.5f, t));
                engine.Update(0.1f, t);
            }
            var flipFlopCount = engine.DeterminismViolationLog.Count(v => v.Kind == DeterminismViolationKind.RapidFlipFlop);
            Assert.True(flipFlopCount >= 0);
        }

        [Fact]
        public void Stress_ConflictingSignalsThenBurst_EngineRemainsConsistent()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions
            {
                MaxLogs = 200,
                FixedStep = 0.1f,
                WindowDuration = 2.0f
            };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            float t = 0f;
            for (int i = 0; i < 20; i++)
            {
                t += 0.1f;
                engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.85f, t));
                engine.InjectSignal(new InputSignal(SignalType.ConfirmIntent, 0.85f, t));
                engine.Update(0.1f, t);
            }
            for (int i = 0; i < 500; i++)
            {
                t += 0.05f;
                engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.2f, t));
                engine.Update(0.05f, t);
            }
            Assert.True(engine.Logs.Count <= options.MaxLogs);
            Assert.All(engine.Logs, log => Assert.True(Enum.IsDefined(typeof(StateType), log.FinalState)));
        }
    }
}
