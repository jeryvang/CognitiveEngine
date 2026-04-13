using CognitiveEngine.Core.DecisionGuidance;
using CognitiveEngine.Core.TrialIntelligence;
using Xunit;

namespace CognitiveEngine.Tests;

public class DecisionOutputBuilderTests
{
    [Fact]
    public void Compare_Routes_To_ComparisonShape()
    {
        var trigger = ResolvedDecisionTrigger.ForCompare("b", "a");
        var result = DecisionOutputBuilder.Build(trigger, new DecisionOutputContent
        {
            KeyDifference = "A is faster.",
            WhichToChooseIf = "If speed matters, lean A."
        });

        Assert.Equal(DecisionOutputKind.Comparison, result.Shape);
        Assert.Null(result.Single);
        Assert.NotNull(result.Comparison);
        Assert.Equal(DecisionOutputKind.Comparison, result.Comparison!.OutputKind);
        Assert.Equal("a", result.Comparison.ProductIdA);
        Assert.Equal("b", result.Comparison.ProductIdB);
        Assert.Equal("A is faster.", result.Comparison.KeyDifference);
        Assert.Equal("If speed matters, lean A.", result.Comparison.WhichToChooseIf);
    }

    [Fact]
    public void Dwell_And_Revisit_Route_To_SingleShape()
    {
        var dwell = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "p1");
        var rd = DecisionOutputBuilder.Build(dwell, new DecisionOutputContent
        {
            WhatThisGivesYou = "Clear pricing",
            WhatYouTradeOff = "Fewer add-ons"
        });
        Assert.NotNull(rd.Single);
        Assert.Null(rd.Comparison);
        Assert.Equal("p1", rd.Single!.ProductId);
        Assert.Equal("Clear pricing", rd.Single.WhatThisGivesYou);

        var revisit = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Revisit, "p1");
        var rr = DecisionOutputBuilder.Build(revisit, new DecisionOutputContent
        {
            WhatThisGivesYou = "X",
            WhatYouTradeOff = "Y"
        });
        Assert.NotNull(rr.Single);
        Assert.Null(rr.Comparison);
    }

    [Fact]
    public void Null_Content_Produces_Empty_String_Fields()
    {
        var t = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "x");
        var r = DecisionOutputBuilder.Build(t, null);
        Assert.Equal("", r.Single!.WhatThisGivesYou);
        Assert.Equal("", r.Single.WhatYouTradeOff);
    }

    [Fact]
    public void Partial_Content_Trims_And_Allows_One_Side_Empty()
    {
        var t = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "x");
        var r = DecisionOutputBuilder.Build(t, new DecisionOutputContent
        {
            WhatThisGivesYou = "  only  ",
            WhatYouTradeOff = null
        });
        Assert.Equal("only", r.Single!.WhatThisGivesYou);
        Assert.Equal("", r.Single.WhatYouTradeOff);
    }

    [Fact]
    public void Compare_Ignores_Single_Product_Fields()
    {
        var trigger = ResolvedDecisionTrigger.ForCompare("a", "z");
        var r = DecisionOutputBuilder.Build(trigger, new DecisionOutputContent
        {
            WhatThisGivesYou = "should not appear",
            WhatYouTradeOff = "nor this",
            KeyDifference = "real",
            WhichToChooseIf = "steer"
        });
        Assert.Equal("real", r.Comparison!.KeyDifference);
        Assert.Equal("steer", r.Comparison.WhichToChooseIf);
        Assert.DoesNotContain("should not", r.Comparison.KeyDifference);
    }

    [Fact]
    public void Single_Ignores_Comparison_Fields()
    {
        var t = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Dwell, "p");
        var r = DecisionOutputBuilder.Build(t, new DecisionOutputContent
        {
            WhatThisGivesYou = "g",
            KeyDifference = "ignored",
            WhichToChooseIf = "ignored"
        });
        Assert.Equal("g", r.Single!.WhatThisGivesYou);
        Assert.Equal("", r.Single.WhatYouTradeOff);
    }

    [Fact]
    public void Built_Single_RoundTrips_ExportJson()
    {
        var t = ResolvedDecisionTrigger.ForSingle(DecisionTriggerKind.Revisit, "prod");
        var built = DecisionOutputBuilder.Build(t, new DecisionOutputContent { WhatThisGivesYou = "a", WhatYouTradeOff = "b" }).Single!;
        var json = ExportJson.Serialize(built);
        var back = ExportJson.Deserialize<SingleProductDecisionOutput>(json);
        Assert.Equal(built.WhatThisGivesYou, back.WhatThisGivesYou);
        Assert.Equal(DecisionOutputKind.SingleProduct, back.OutputKind);
    }

    [Fact]
    public void Built_Comparison_RoundTrips_ExportJson()
    {
        var t = ResolvedDecisionTrigger.ForCompare("m", "n");
        var built = DecisionOutputBuilder.Build(t, new DecisionOutputContent { KeyDifference = "k", WhichToChooseIf = "w" }).Comparison!;
        var json = ExportJson.Serialize(built);
        var back = ExportJson.Deserialize<ComparisonDecisionOutput>(json);
        Assert.Equal("m", back.ProductIdA);
        Assert.Equal("n", back.ProductIdB);
        Assert.Equal("k", back.KeyDifference);
    }

    [Fact]
    public void Same_Trigger_And_Content_Yields_Equivalent_Output()
    {
        var trigger = ResolvedDecisionTrigger.ForCompare("x", "y");
        var content = new DecisionOutputContent { KeyDifference = "d", WhichToChooseIf = "c" };
        var a = DecisionOutputBuilder.Build(trigger, content).Comparison!;
        var b = DecisionOutputBuilder.Build(trigger, content).Comparison!;
        Assert.Equal(ExportJson.Serialize(a), ExportJson.Serialize(b));
    }
}
