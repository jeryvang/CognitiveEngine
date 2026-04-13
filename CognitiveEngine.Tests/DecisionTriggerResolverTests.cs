using CognitiveEngine.Core.DecisionGuidance;
using Xunit;

namespace CognitiveEngine.Tests;

public class DecisionTriggerResolverTests
{
    [Fact]
    public void Compare_Wins_Over_Revisit_InSameFrame()
    {
        var r = new DecisionTriggerResolver();
        Assert.Null(r.Advance(DecisionTriggerInput.FocusChanged("p1")));
        Assert.Null(r.Advance(DecisionTriggerInput.FocusChanged("p2")));

        var frame = new DecisionTriggerFrame("p1", compareInvoked: true, dwellProductIfThreshold: null);
        var t = r.AdvanceFrame(in frame);

        Assert.NotNull(t);
        Assert.Equal(DecisionTriggerKind.Compare, t.Value.Kind);
        Assert.Equal("p1", t.Value.ProductIdLow);
        Assert.Equal("p2", t.Value.ProductIdHigh);
    }

    [Fact]
    public void Compare_Wins_Over_Dwell_InSameFrame()
    {
        var r = new DecisionTriggerResolver();
        r.Advance(DecisionTriggerInput.FocusChanged("x"));
        r.Advance(DecisionTriggerInput.FocusChanged("y"));

        var t = r.AdvanceFrame(new DecisionTriggerFrame(null, compareInvoked: true, dwellProductIfThreshold: "x"));

        Assert.NotNull(t);
        Assert.Equal(DecisionTriggerKind.Compare, t.Value.Kind);
    }

    [Fact]
    public void Revisit_Wins_Over_Dwell_WhenBothInSameFrame()
    {
        var r = new DecisionTriggerResolver();
        r.Advance(DecisionTriggerInput.FocusChanged("a"));
        r.Advance(DecisionTriggerInput.FocusChanged("b"));

        var t = r.AdvanceFrame(new DecisionTriggerFrame("a", compareInvoked: false, dwellProductIfThreshold: "a"));

        Assert.NotNull(t);
        Assert.Equal(DecisionTriggerKind.Revisit, t.Value.Kind);
        Assert.Equal("a", t.Value.ProductIdLow);
    }

    [Fact]
    public void Compare_Scope_UsesLatestTwoDistinctProducts()
    {
        var r = new DecisionTriggerResolver();
        r.Advance(DecisionTriggerInput.FocusChanged("A"));
        r.Advance(DecisionTriggerInput.FocusChanged("B"));
        r.Advance(DecisionTriggerInput.FocusChanged("C"));

        var t = r.Advance(DecisionTriggerInput.CompareInvoked());
        Assert.NotNull(t);
        Assert.Equal(DecisionTriggerKind.Compare, t.Value.Kind);
        Assert.Equal("B", t.Value.ProductIdLow);
        Assert.Equal("C", t.Value.ProductIdHigh);
    }

    [Fact]
    public void Compare_ProductIds_AreLexicallyOrdered()
    {
        var r = new DecisionTriggerResolver();
        r.Advance(DecisionTriggerInput.FocusChanged("zebra"));
        r.Advance(DecisionTriggerInput.FocusChanged("apple"));

        var t = r.Advance(DecisionTriggerInput.CompareInvoked());
        Assert.NotNull(t);
        Assert.Equal("apple", t.Value.ProductIdLow);
        Assert.Equal("zebra", t.Value.ProductIdHigh);
    }

    [Fact]
    public void Repeated_Compare_SamePair_Suppressed_UntilMruPairChanges()
    {
        var r = new DecisionTriggerResolver();
        r.Advance(DecisionTriggerInput.FocusChanged("p1"));
        r.Advance(DecisionTriggerInput.FocusChanged("p2"));
        Assert.NotNull(r.Advance(DecisionTriggerInput.CompareInvoked()));
        Assert.Null(r.Advance(DecisionTriggerInput.CompareInvoked()));

        r.Advance(DecisionTriggerInput.FocusChanged("p3"));
        Assert.NotNull(r.Advance(DecisionTriggerInput.CompareInvoked()));
    }

    [Fact]
    public void Revisit_Fires_AfterLeavingAndReturning()
    {
        var r = new DecisionTriggerResolver();
        Assert.Null(r.Advance(DecisionTriggerInput.FocusChanged("A")));
        Assert.Null(r.Advance(DecisionTriggerInput.FocusChanged("B")));
        var t = r.Advance(DecisionTriggerInput.FocusChanged("A"));
        Assert.NotNull(t);
        Assert.Equal(DecisionTriggerKind.Revisit, t.Value.Kind);
        Assert.Equal("A", t.Value.ProductIdLow);
    }

    [Fact]
    public void Repeated_Dwell_Suppressed_UntilFocusChanges()
    {
        var r = new DecisionTriggerResolver();
        r.Advance(DecisionTriggerInput.FocusChanged("only"));
        Assert.NotNull(r.Advance(DecisionTriggerInput.DwellThresholdMet("only")));
        Assert.Null(r.Advance(DecisionTriggerInput.DwellThresholdMet("only")));

        r.Advance(DecisionTriggerInput.FocusChanged("other"));
        r.Advance(DecisionTriggerInput.FocusChanged("only"));
        Assert.NotNull(r.Advance(DecisionTriggerInput.DwellThresholdMet("only")));
    }

    [Fact]
    public void Dwell_NotEmitted_ForNonFocusedProduct()
    {
        var r = new DecisionTriggerResolver();
        r.Advance(DecisionTriggerInput.FocusChanged("x"));
        Assert.Null(r.Advance(DecisionTriggerInput.DwellThresholdMet("y")));
    }

    [Fact]
    public void SameInputSequence_IsRepeatable()
    {
        static System.Collections.Generic.List<string> Run()
        {
            var r = new DecisionTriggerResolver();
            var sigs = new System.Collections.Generic.List<string>();
            r.Advance(DecisionTriggerInput.FocusChanged("a"));
            r.Advance(DecisionTriggerInput.FocusChanged("b"));
            Add(r.Advance(DecisionTriggerInput.CompareInvoked()), sigs);
            Add(r.Advance(DecisionTriggerInput.FocusChanged("a")), sigs);
            Add(r.Advance(DecisionTriggerInput.DwellThresholdMet("a")), sigs);
            return sigs;
        }

        static void Add(ResolvedDecisionTrigger? t, System.Collections.Generic.List<string> sigs)
        {
            if (t == null)
                sigs.Add("null");
            else
                sigs.Add(ResolvedDecisionTrigger.Signature(t.Value));
        }

        var a = Run();
        var b = Run();
        Assert.Equal(a, b);
    }

    [Fact]
    public void Compare_NotEmitted_UntilTwoDistinctProducts()
    {
        var r = new DecisionTriggerResolver();
        r.Advance(DecisionTriggerInput.FocusChanged("solo"));
        Assert.Null(r.Advance(DecisionTriggerInput.CompareInvoked()));
    }

    [Fact]
    public void Duplicate_Focus_DoesNotEmit()
    {
        var r = new DecisionTriggerResolver();
        r.Advance(DecisionTriggerInput.FocusChanged("p"));
        Assert.Null(r.Advance(DecisionTriggerInput.FocusChanged("p")));
    }
}
