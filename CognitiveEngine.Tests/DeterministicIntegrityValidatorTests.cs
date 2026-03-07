using System.Collections.Generic;
using CognitiveEngine.Core;
using Xunit;

namespace CognitiveEngine.Tests
{
    public class DeterministicIntegrityValidatorTests
    {
        [Fact]
        public void Validate_NoViolations_WhenRuleMatchesState()
        {
            var violations = DeterministicIntegrityValidator.Validate(
                1, StateType.Neutral, StateType.Exploration, "DefaultExploration", null);
            Assert.Empty(violations);
        }

        [Fact]
        public void Validate_RulePriorityViolation_WhenRuleDoesNotMatchState()
        {
            var violations = DeterministicIntegrityValidator.Validate(
                1, StateType.Neutral, StateType.Neutral, "ConfirmIntentHigh", null);
            Assert.Single(violations);
            Assert.Equal(DeterminismViolationKind.RulePriorityViolation, violations[0].Kind);
            Assert.Equal(1, violations[0].TickIndex);
            Assert.Equal("ConfirmIntentHigh", violations[0].RuleName);
        }

        [Fact]
        public void Validate_NoUndefinedTransition_WhenAllTransitionsAllowed()
        {
            foreach (StateType from in System.Enum.GetValues(typeof(StateType)))
            foreach (StateType to in System.Enum.GetValues(typeof(StateType)))
            {
                var violations = DeterministicIntegrityValidator.Validate(
                    1, from, to, "NoSignals", null);
                Assert.All(violations, v => Assert.NotEqual(DeterminismViolationKind.UndefinedTransition, v.Kind));
            }
        }

        [Fact]
        public void Validate_RapidFlipFlop_WhenResolvedStateEqualsStateTwoTicksAgo()
        {
            var recent = new List<StateType> { StateType.Neutral, StateType.Hesitation };
            var violations = DeterministicIntegrityValidator.Validate(
                2, StateType.Hesitation, StateType.Neutral, "NoSignals", recent);
            Assert.Single(violations);
            Assert.Equal(DeterminismViolationKind.RapidFlipFlop, violations[0].Kind);
            Assert.Equal(StateType.Neutral, violations[0].ToState);
        }

        [Fact]
        public void Validate_NoRapidFlipFlop_WhenRecentStatesTooShort()
        {
            var recent = new List<StateType> { StateType.Hesitation };
            var violations = DeterministicIntegrityValidator.Validate(
                1, StateType.Hesitation, StateType.Neutral, "NoSignals", recent);
            Assert.Empty(violations);
        }

        [Fact]
        public void Validate_NoRapidFlipFlop_WhenResolvedStateDifferentFromTwoTicksAgo()
        {
            var recent = new List<StateType> { StateType.Neutral, StateType.Exploration };
            var violations = DeterministicIntegrityValidator.Validate(
                2, StateType.Exploration, StateType.Comparison, "MediumDwell", recent);
            Assert.Empty(violations);
        }

        [Fact]
        public void Validate_NoRapidFlipFlop_WhenStateStayedSame_NeutralAllTicks()
        {
            var recent = new List<StateType> { StateType.Neutral, StateType.Neutral };
            var violations = DeterministicIntegrityValidator.Validate(
                2, StateType.Neutral, StateType.Neutral, "NoSignals", recent);
            Assert.Empty(violations);
        }

        [Fact]
        public void Engine_LogsRapidFlipFlop_WhenStateReturnsToSameAsTwoTicksAgo()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions { WindowDuration = 0.2f, FixedStep = 0.1f };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            engine.Update(0.1f, 0.1f);
            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.8f, 0.15f));
            engine.Update(0.1f, 0.2f);
            engine.Update(0.1f, 0.36f);
            var flipFlops = new List<DeterminismViolationRecord>();
            foreach (var v in engine.DeterminismViolationLog)
                if (v.Kind == DeterminismViolationKind.RapidFlipFlop) flipFlops.Add(v);
            Assert.NotEmpty(flipFlops);
        }

        [Fact]
        public void Engine_OnDeterminismViolation_Raised_WhenFlipFlopDetected()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions { WindowDuration = 0.2f, FixedStep = 0.1f };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            DeterminismViolationRecord? received = null;
            engine.OnDeterminismViolation += r => received = r;
            engine.Update(0.1f, 0.1f);
            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.8f, 0.15f));
            engine.Update(0.1f, 0.2f);
            engine.Update(0.1f, 0.36f);
            Assert.NotNull(received);
            Assert.Equal(DeterminismViolationKind.RapidFlipFlop, received.Value.Kind);
        }

        [Fact]
        public void Engine_DeterminismViolationLog_Empty_WhenNoViolations()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.5f, 0.1f));
            engine.Update(0.1f, 0.1f);
            Assert.Empty(engine.DeterminismViolationLog);
        }

        [Fact]
        public void Engine_DeterminismViolationLog_CappedAtMax()
        {
            var options = new CognitiveEngine.Core.CognitiveEngine.EngineOptions
            {
                WindowDuration = 0.2f,
                FixedStep = 0.1f,
                MaxDeterminismViolationLog = 2
            };
            var engine = new CognitiveEngine.Core.CognitiveEngine(options);
            for (int i = 0; i < 4; i++)
            {
                float tBase = 0.1f + i * 0.5f;
                engine.Update(0.1f, tBase);
                engine.InjectSignal(new InputSignal(SignalType.DwellTime, 0.8f, tBase + 0.05f));
                engine.Update(0.1f, tBase + 0.1f);
                engine.Update(0.1f, tBase + 0.26f);
            }
            Assert.Equal(2, engine.DeterminismViolationLog.Count);
        }
    }
}
