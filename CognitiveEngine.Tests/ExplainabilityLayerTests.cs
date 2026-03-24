using CognitiveEngine.Core;
using Xunit;

namespace CognitiveEngine.Tests
{
    public class ExplainabilityLayerTests
    {
        [Fact]
        public void GetExplanation_ConfirmIntentHigh_ReturnsExpected()
        {
            Assert.Equal("User confirmed intent with high confidence",
                ExplainabilityLayer.GetExplanation("ConfirmIntentHigh"));
        }

        [Fact]
        public void GetExplanation_HighDwell_ReturnsExpected()
        {
            Assert.Equal("Prolonged dwell suggests hesitation",
                ExplainabilityLayer.GetExplanation("HighDwell"));
        }

        [Fact]
        public void GetExplanation_ComparisonAction_ReturnsExpected()
        {
            Assert.Equal("User is comparing options before deciding",
                ExplainabilityLayer.GetExplanation("ComparisonAction"));
        }

        [Fact]
        public void GetExplanation_NoSignals_ReturnsExpected()
        {
            Assert.Equal("No significant signals in the window",
                ExplainabilityLayer.GetExplanation("NoSignals"));
        }

        [Fact]
        public void GetExplanation_ContextChange_ReturnsExpected()
        {
            Assert.Equal("User changed context or navigated away",
                ExplainabilityLayer.GetExplanation("ContextChange"));
        }

        [Fact]
        public void GetExplanation_ProductFocus_ReturnsExpected()
        {
            Assert.Equal("User is focusing on a product",
                ExplainabilityLayer.GetExplanation("ProductFocus"));
        }

        [Fact]
        public void GetExplanation_SwipeVelocity_ReturnsExpected()
        {
            Assert.Equal("Swipe or scroll indicates exploration",
                ExplainabilityLayer.GetExplanation("SwipeVelocity"));
        }

        [Fact]
        public void GetExplanation_MediumDwell_ReturnsExpected()
        {
            Assert.Equal("Moderate dwell suggests comparison",
                ExplainabilityLayer.GetExplanation("MediumDwell"));
        }

        [Fact]
        public void GetExplanation_DefaultExploration_ReturnsExpected()
        {
            Assert.Equal("Default exploration state from dominant signal",
                ExplainabilityLayer.GetExplanation("DefaultExploration"));
        }

        [Fact]
        public void GetExplanation_UnknownRule_ReturnsRuleName()
        {
            Assert.Equal("UnknownRule", ExplainabilityLayer.GetExplanation("UnknownRule"));
        }

        [Fact]
        public void GetExplanation_NullOrEmpty_ReturnsEmpty()
        {
            Assert.Equal("", ExplainabilityLayer.GetExplanation(null));
            Assert.Equal("", ExplainabilityLayer.GetExplanation(""));
        }

        [Fact]
        public void CognitiveState_Explanation_IsSetFromReasoningTag()
        {
            var state = new CognitiveState(StateType.Hesitation, 0.8f, "HighDwell");
            Assert.Equal("Prolonged dwell suggests hesitation", state.Explanation);
        }

        [Fact]
        public void CognitiveState_Explanation_MatchesEngineStateUpdate()
        {
            var engine = new CognitiveEngine.Core.CognitiveEngine();
            string explanation = null;
            engine.OnStateUpdated += s => explanation = s.Explanation;
            engine.InjectSignal(new InputSignal(SignalType.ConfirmIntent, 0.9f, 0.1f));
            engine.Update(0.1f, 0.1f);
            Assert.NotNull(explanation);
            Assert.Equal("User confirmed intent with high confidence", explanation);
        }

        [Fact]
        public void StreamingEngine_Current_Explanation_IsHumanReadable()
        {
            var engine = new StreamingCognitiveEngine();
            engine.Update(0.5f, 0.9f, 0f);
            Assert.Equal(StateType.Hesitation, engine.Current.State);
            Assert.Equal("Prolonged dwell suggests hesitation", engine.Current.Explanation);
        }
    }
}
