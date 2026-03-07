using System.Collections.Generic;
using System.Linq;
using CognitiveEngine.Core;
using Xunit;

namespace CognitiveEngine.Tests
{
    public class RuntimeMemoryContextTests
    {
        [Fact]
        public void Constructor_StoresTickIndex()
        {
            var ctx = new RuntimeMemoryContext(7, 0.5f, new Dictionary<SignalType, float>());
            Assert.Equal(7, ctx.TickIndex);
        }

        [Fact]
        public void Constructor_StoresTimestamp()
        {
            var ctx = new RuntimeMemoryContext(1, 1.23f, new Dictionary<SignalType, float>());
            Assert.Equal(1.23f, ctx.Timestamp);
        }

        [Fact]
        public void Signals_AreSortedDeterministically()
        {
            var input = new Dictionary<SignalType, float>
            {
                { SignalType.SwipeVelocity,    0.5f },
                { SignalType.DwellTime,        0.9f },
                { SignalType.ConfirmIntent,    0.3f },
            };

            var ctx  = new RuntimeMemoryContext(1, 0f, input);
            var keys = ctx.Signals.Keys.ToList();

            var expected = input.Keys.OrderBy(k => k).ToList();
            Assert.Equal(expected, keys);
        }

        [Fact]
        public void GetSignal_ReturnsZero_ForMissingType()
        {
            var ctx = new RuntimeMemoryContext(1, 0f, new Dictionary<SignalType, float>());
            Assert.Equal(0f, ctx.GetSignal(SignalType.DwellTime));
        }

        [Fact]
        public void GetSignal_ReturnsValue_ForPresentType()
        {
            var input = new Dictionary<SignalType, float> { { SignalType.DwellTime, 0.75f } };
            var ctx   = new RuntimeMemoryContext(1, 0f, input);
            Assert.Equal(0.75f, ctx.GetSignal(SignalType.DwellTime));
        }

        [Fact]
        public void HasSignals_True_WhenSignalsPresent()
        {
            var input = new Dictionary<SignalType, float> { { SignalType.ConfirmIntent, 1.0f } };
            var ctx   = new RuntimeMemoryContext(1, 0f, input);
            Assert.True(ctx.HasSignals);
        }

        [Fact]
        public void HasSignals_False_WhenSignalsEmpty()
        {
            var ctx = new RuntimeMemoryContext(1, 0f, new Dictionary<SignalType, float>());
            Assert.False(ctx.HasSignals);
        }

        [Fact]
        public void Signals_AreIsolatedFromSourceDictionary()
        {
            var source = new Dictionary<SignalType, float> { { SignalType.DwellTime, 0.5f } };
            var ctx    = new RuntimeMemoryContext(1, 0f, source);

            source[SignalType.DwellTime] = 9999f;
            source[SignalType.ConfirmIntent] = 1.0f;

            Assert.Equal(0.5f, ctx.GetSignal(SignalType.DwellTime));
            Assert.Equal(0f,   ctx.GetSignal(SignalType.ConfirmIntent));
        }

        [Fact]
        public void Engine_TickContextDoesNotLeakBetweenTicks()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions
            {
                WindowDuration = 2.0f,
                FixedStep      = 0.1f
            };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);

            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 1.0f, 0.1f));
            engine.Update(0.1f, 0.1f);

            var firstLog = engine.Logs.First();

            engine.InjectSignal(new InputSignal(SignalType.ConfirmIntent, 1.0f, 0.2f));
            engine.Update(0.1f, 0.2f);

            var secondLog = engine.Logs.Last();

            Assert.False(firstLog.Signals.ContainsKey(SignalType.ConfirmIntent));
            Assert.True(secondLog.Signals.ContainsKey(SignalType.ConfirmIntent));
        }

        [Fact]
        public void Engine_ResetSession_ClearsTicks()
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
