using System;
using System.Linq;
using CognitiveEngine.Core;
using Xunit;

namespace CognitiveEngine.Tests
{
    public class EngineCoreTests
    {
        [Fact]
        public void WindowExpiry_RemovesOldSignals()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions { WindowDuration = 0.2f, FixedStep = 0.1f };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);

            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0f));

            engine.Update(0.3f, 0.3f);

            var last = engine.Logs.Last();

            Assert.False(last.Signals.ContainsKey(SignalType.DwellTime));
        }

        [Fact]
        public void Decay_IsAppliedToAggregatedSignals()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions { DecayFactor = 0.5f, WindowDuration = 2.0f, FixedStep = 0.1f };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);

            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0f));

            engine.Update(0.1f, 0.1f);

            var val = engine.Logs.Last().Signals[SignalType.DwellTime];

            Assert.InRange(val, 0.49f, 0.51f);
        }

        [Fact]
        public void ConfirmIntent_OverridesDwell_HighPriorityRule()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();

            float time = 0f;

            for (int i = 0; i < 30; i++)
            {
                time += 0.1f;
                engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.1f, time));
                engine.Update(0.1f, time);
            }

            var lastBefore = engine.Logs.Last();
            Assert.Equal(StateType.Hesitation, lastBefore.FinalState);

            for (int i = 0; i < 5; i++)
            {
                time += 0.1f;
                engine.InjectSignal(new InputSignal(SignalType.ConfirmIntent, 1.0f, time));
                engine.Update(0.1f, time);
            }

            var last = engine.Logs.Last();
            Assert.Equal(StateType.ReadyToConfirm, last.FinalState);
        }

        [Fact]
        public void OnStateUpdated_IsRaised_OnChange()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();

            int calls = 0;
            engine.OnStateUpdated += s => calls++;

            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0.1f));
            engine.Update(0.1f, 0.1f);

            Assert.Equal(1, calls);
        }

        [Fact]
        public void Neutral_ReturnedWhenNoSignalsInWindow()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions
            {
                WindowDuration = 0.1f,
                FixedStep = 0.1f
            };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);

            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.5f, 0f));

            engine.Update(0.1f, 0.1f);
            engine.Update(0.1f, 0.2f);

            Assert.Equal(StateType.Neutral, engine.Logs.Last().FinalState);
        }

        [Fact]
        public void BurstTick_ProducesDistinctTimestamps()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions { FixedStep = 0.1f };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);

            engine.Update(0.5f, 0.5f);

            var timestamps = engine.Logs.Select(l => l.Timestamp).ToList();

            Assert.Equal(5, timestamps.Count);
            Assert.Equal(timestamps.Count, timestamps.Distinct().Count());

            for (int i = 1; i < timestamps.Count; i++)
                Assert.True(timestamps[i] > timestamps[i - 1]);
        }
    }
}
