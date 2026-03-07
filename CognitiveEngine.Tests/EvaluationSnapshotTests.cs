using System.Collections.Generic;
using CognitiveEngine.Core;
using Xunit;

namespace CognitiveEngine.Tests
{
    public class EvaluationSnapshotTests
    {
        private static RuntimeMemoryContext MakeContext(
            int tickIndex, float timestamp, IDictionary<SignalType, float> signals) =>
            new RuntimeMemoryContext(tickIndex, timestamp, signals);

        [Fact]
        public void SameSnapshot_ProducesSameOutput()
        {
            var ctx      = MakeContext(1, 0.1f, new Dictionary<SignalType, float> { { SignalType.DwellTime, 0.5f } });
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            var result1 = snapshot.Evaluate();
            var result2 = snapshot.Evaluate();

            Assert.Equal(result1.state,      result2.state);
            Assert.Equal(result1.rule,       result2.rule);
            Assert.Equal(result1.confidence, result2.confidence);
        }

        [Fact]
        public void ConfirmIntent_OverridesDwell_HighestPriority()
        {
            var ctx = MakeContext(1, 0f, new Dictionary<SignalType, float>
            {
                { SignalType.DwellTime,     1.0f },
                { SignalType.ConfirmIntent, 0.9f },
            });
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            var (state, rule, _) = snapshot.Evaluate();

            Assert.Equal(StateType.ReadyToConfirm, state);
            Assert.Equal("ConfirmIntentHigh", rule);
        }

        [Fact]
        public void HighDwell_ProducesHesitation()
        {
            var ctx      = MakeContext(1, 0f, new Dictionary<SignalType, float> { { SignalType.DwellTime, 0.8f } });
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            var (state, rule, _) = snapshot.Evaluate();

            Assert.Equal(StateType.Hesitation, state);
            Assert.Equal("HighDwell", rule);
        }

        [Fact]
        public void MediumDwell_ProducesComparison()
        {
            var ctx      = MakeContext(1, 0f, new Dictionary<SignalType, float> { { SignalType.DwellTime, 0.5f } });
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            var (state, rule, _) = snapshot.Evaluate();

            Assert.Equal(StateType.Comparison, state);
            Assert.Equal("MediumDwell", rule);
        }

        [Fact]
        public void NoSignals_ProducesNeutral()
        {
            var ctx      = MakeContext(1, 0f, new Dictionary<SignalType, float>());
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            var (state, rule, confidence) = snapshot.Evaluate();

            Assert.Equal(StateType.Neutral, state);
            Assert.Equal("NoSignals", rule);
            Assert.Equal(0f, confidence);
        }

        [Fact]
        public void Confidence_ScalesWithWindowDuration()
        {
            var ctx       = MakeContext(1, 0f, new Dictionary<SignalType, float> { { SignalType.DwellTime, 0.5f } });
            var snapshot1 = new EvaluationSnapshot(ctx, StateType.Neutral, 1.0f);
            var snapshot2 = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            var (_, _, conf1) = snapshot1.Evaluate();
            var (_, _, conf2) = snapshot2.Evaluate();

            Assert.True(conf1 > conf2, "Shorter window should produce higher confidence for same signal value");
        }

        [Fact]
        public void ZeroWindowDuration_ProducesZeroConfidence()
        {
            var ctx      = MakeContext(1, 0f, new Dictionary<SignalType, float> { { SignalType.DwellTime, 0.9f } });
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 0f);

            var (_, _, confidence) = snapshot.Evaluate();

            Assert.Equal(0f, confidence);
        }

        [Fact]
        public void PreviousState_IsPreserved()
        {
            var ctx      = MakeContext(1, 0f, new Dictionary<SignalType, float>());
            var snapshot = new EvaluationSnapshot(ctx, StateType.Hesitation, 2.0f);

            Assert.Equal(StateType.Hesitation, snapshot.PreviousState);
        }

        [Fact]
        public void WindowDuration_IsPreserved()
        {
            var ctx      = MakeContext(1, 0f, new Dictionary<SignalType, float>());
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 3.5f);

            Assert.Equal(3.5f, snapshot.WindowDuration);
        }

        [Fact]
        public void Context_ReferenceIsPreserved()
        {
            var ctx      = MakeContext(1, 0f, new Dictionary<SignalType, float>());
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            Assert.Same(ctx, snapshot.Context);
        }

        [Fact]
        public void Engine_UsesSnapshot_ForEvaluation()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();

            float time = 0f;
            for (int i = 0; i < 10; i++)
            {
                time += 0.1f;
                engine.InjectSignal(new InputSignal(SignalType.ConfirmIntent, 1.0f, time));
                engine.Update(0.1f, time);
            }

            Assert.Equal(StateType.ReadyToConfirm, engine.Logs[^1].FinalState);
            Assert.Equal("ConfirmIntentHigh",       engine.Logs[^1].TriggeredRule);
        }

        [Fact]
        public void ProductFocus_AboveThreshold_ProducesExploration_WithProductFocusRule()
        {
            var ctx = MakeContext(1, 0f, new Dictionary<SignalType, float> { { SignalType.ProductFocus, 0.6f } });
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            var (state, rule, _) = snapshot.Evaluate();

            Assert.Equal(StateType.Exploration, state);
            Assert.Equal("ProductFocus", rule);
        }

        [Fact]
        public void ComparisonAction_AboveThreshold_ProducesComparison_WithComparisonActionRule()
        {
            var ctx = MakeContext(1, 0f, new Dictionary<SignalType, float> { { SignalType.ComparisonAction, 0.6f } });
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            var (state, rule, _) = snapshot.Evaluate();

            Assert.Equal(StateType.Comparison, state);
            Assert.Equal("ComparisonAction", rule);
        }

        [Fact]
        public void ProductFocus_OverridesDwell_DiscreteEventPriority()
        {
            var ctx = MakeContext(1, 0f, new Dictionary<SignalType, float>
            {
                { SignalType.DwellTime, 0.9f },
                { SignalType.ProductFocus, 0.6f }
            });
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            var (state, rule, _) = snapshot.Evaluate();

            Assert.Equal(StateType.Exploration, state);
            Assert.Equal("ProductFocus", rule);
        }

        [Fact]
        public void ComparisonAction_OverridesDwell()
        {
            var ctx = MakeContext(1, 0f, new Dictionary<SignalType, float>
            {
                { SignalType.DwellTime, 0.9f },
                { SignalType.ComparisonAction, 0.6f }
            });
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            var (state, rule, _) = snapshot.Evaluate();

            Assert.Equal(StateType.Comparison, state);
            Assert.Equal("ComparisonAction", rule);
        }

        [Fact]
        public void ConfirmIntent_StillOverrides_ProductFocus()
        {
            var ctx = MakeContext(1, 0f, new Dictionary<SignalType, float>
            {
                { SignalType.ProductFocus, 1.0f },
                { SignalType.ConfirmIntent, 0.9f }
            });
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            var (state, rule, _) = snapshot.Evaluate();

            Assert.Equal(StateType.ReadyToConfirm, state);
            Assert.Equal("ConfirmIntentHigh", rule);
        }

        [Fact]
        public void SwipeVelocity_AboveThreshold_ProducesExploration_WithSwipeVelocityRule()
        {
            var ctx = MakeContext(1, 0f, new Dictionary<SignalType, float> { { SignalType.SwipeVelocity, 0.6f } });
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            var (state, rule, _) = snapshot.Evaluate();

            Assert.Equal(StateType.Exploration, state);
            Assert.Equal("SwipeVelocity", rule);
        }

        [Fact]
        public void SwipeVelocity_NegativeMagnitude_TriggersRule()
        {
            var ctx = MakeContext(1, 0f, new Dictionary<SignalType, float> { { SignalType.SwipeVelocity, -0.6f } });
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            var (state, rule, _) = snapshot.Evaluate();

            Assert.Equal(StateType.Exploration, state);
            Assert.Equal("SwipeVelocity", rule);
        }

        [Fact]
        public void SwipeVelocity_OverridesDwell()
        {
            var ctx = MakeContext(1, 0f, new Dictionary<SignalType, float>
            {
                { SignalType.DwellTime, 0.9f },
                { SignalType.SwipeVelocity, 0.6f }
            });
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            var (state, rule, _) = snapshot.Evaluate();

            Assert.Equal(StateType.Exploration, state);
            Assert.Equal("SwipeVelocity", rule);
        }

        [Fact]
        public void ContextChange_AboveThreshold_ProducesNeutral_WithContextChangeRule()
        {
            var ctx = MakeContext(1, 0f, new Dictionary<SignalType, float> { { SignalType.ContextChange, 0.6f } });
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            var (state, rule, _) = snapshot.Evaluate();

            Assert.Equal(StateType.Neutral, state);
            Assert.Equal("ContextChange", rule);
        }

        [Fact]
        public void ContextChange_OverridesProductFocus_ResetsIntent()
        {
            var ctx = MakeContext(1, 0f, new Dictionary<SignalType, float>
            {
                { SignalType.ProductFocus, 0.8f },
                { SignalType.ContextChange, 0.6f }
            });
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            var (state, rule, _) = snapshot.Evaluate();

            Assert.Equal(StateType.Neutral, state);
            Assert.Equal("ContextChange", rule);
        }

        [Fact]
        public void ConfirmIntent_StillOverrides_ContextChange()
        {
            var ctx = MakeContext(1, 0f, new Dictionary<SignalType, float>
            {
                { SignalType.ContextChange, 1.0f },
                { SignalType.ConfirmIntent, 0.9f }
            });
            var snapshot = new EvaluationSnapshot(ctx, StateType.Neutral, 2.0f);

            var (state, rule, _) = snapshot.Evaluate();

            Assert.Equal(StateType.ReadyToConfirm, state);
            Assert.Equal("ConfirmIntentHigh", rule);
        }
    }
}
