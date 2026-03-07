using CognitiveEngine.Core;
using Xunit;

namespace CognitiveEngine.Tests
{
    public class SignalAccumulatorTests
    {
        [Fact]
        public void Accumulator_BuildsWithPositiveInput()
        {
            var acc = new SignalAccumulator(decayRate: 0f, gainRate: 1.0f, ceiling: 1.0f);
            acc.Update(1.0f, 1.0f);
            Assert.Equal(1.0f, acc.Value);
        }

        [Fact]
        public void Accumulator_DecaysWhenNoInput()
        {
            var acc = new SignalAccumulator(decayRate: 2.0f, gainRate: 100.0f, ceiling: 1.0f);
            acc.Update(1.0f, 1.0f);
            acc.Update(0.5f, 0.0f);
            Assert.InRange(acc.Value, 0.34f, 0.40f);
        }

        [Fact]
        public void Accumulator_ClampedAtCeiling()
        {
            var acc = new SignalAccumulator(decayRate: 0f, gainRate: 100.0f, ceiling: 1.0f);
            acc.Update(1.0f, 1.0f);
            Assert.Equal(1.0f, acc.Value);
        }

        [Fact]
        public void Accumulator_ZeroDeltaTimeNoChange()
        {
            var acc = new SignalAccumulator(decayRate: 1.0f, gainRate: 1.0f, ceiling: 1.0f);
            acc.Update(1.0f, 1.0f);
            float before = acc.Value;
            acc.Update(0f, 1.0f);
            Assert.Equal(before, acc.Value);
        }

        [Fact]
        public void Accumulator_NormalizedStaysBounded()
        {
            var acc = new SignalAccumulator(decayRate: 1.5f, gainRate: 2.0f, ceiling: 1.0f);
            for (int i = 0; i < 50; i++)
                acc.Update(0.1f, 1.0f);
            Assert.InRange(acc.Normalized, 0f, 1f);
        }

        [Fact]
        public void Accumulator_ResetClearsValue()
        {
            var acc = new SignalAccumulator(decayRate: 0f, gainRate: 1.0f, ceiling: 1.0f);
            acc.Update(1.0f, 1.0f);
            acc.Reset();
            Assert.Equal(0f, acc.Value);
        }
    }

    public class StreamingEngineTests
    {
        private static StreamingCognitiveEngine.Options DwellHeavyOptions() => new()
        {
            DwellGainRate    = 2.0f,
            DwellDecayRate   = 1.5f,
            ConfirmGainRate  = 4.0f,
            ConfirmDecayRate = 3.0f
        };

        [Fact]
        public void Streaming_DwellBuildsToHesitation()
        {
            var engine = new StreamingCognitiveEngine(DwellHeavyOptions());

            for (int i = 0; i < 10; i++)
                engine.Update(0.1f, 1.0f, 0.0f);

            Assert.Equal(StateType.Hesitation, engine.Current.State);
        }

        [Fact]
        public void Streaming_ConfirmOverridesDwell()
        {
            var engine = new StreamingCognitiveEngine(DwellHeavyOptions());

            for (int i = 0; i < 10; i++)
                engine.Update(0.1f, 1.0f, 0.0f);

            Assert.Equal(StateType.Hesitation, engine.Current.State);

            for (int i = 0; i < 5; i++)
                engine.Update(0.1f, 0.0f, 1.0f);

            Assert.Equal(StateType.ReadyToConfirm, engine.Current.State);
        }

        [Fact]
        public void Streaming_DecaysToNeutralWhenSignalStops()
        {
            var engine = new StreamingCognitiveEngine(DwellHeavyOptions());

            for (int i = 0; i < 10; i++)
                engine.Update(0.1f, 1.0f, 0.0f);

            for (int i = 0; i < 50; i++)
                engine.Update(0.1f, 0.0f, 0.0f);

            Assert.Equal(StateType.Neutral, engine.Current.State);
        }

        [Fact]
        public void Streaming_OnStateUpdated_Fires()
        {
            var engine = new StreamingCognitiveEngine(DwellHeavyOptions());

            int calls = 0;
            engine.OnStateUpdated += _ => calls++;

            engine.Update(1.0f, 1.0f, 0.0f);

            Assert.Equal(1, calls);
        }

        [Fact]
        public void Streaming_ConfidenceIsNormalized()
        {
            var engine = new StreamingCognitiveEngine(DwellHeavyOptions());

            for (int i = 0; i < 20; i++)
                engine.Update(0.1f, 1.0f, 0.0f);

            Assert.InRange(engine.Current.Confidence, 0f, 1f);
        }

        [Fact]
        public void Streaming_ResetReturnsToNeutral()
        {
            var engine = new StreamingCognitiveEngine(DwellHeavyOptions());

            for (int i = 0; i < 10; i++)
                engine.Update(0.1f, 1.0f, 0.0f);

            engine.Reset();

            Assert.Equal(StateType.Neutral, engine.Current.State);
            Assert.Equal(0f, engine.Current.Confidence);
        }
    }
}
