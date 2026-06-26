using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Rendering;
using TerraFluent.Html.Reporting.Tests.TestHelpers;
using Xunit;

namespace TerraFluent.Html.Reporting.Tests.Model;

public class ReportListTests
{
    private static LayoutContext Context(double contentWidthPx = 200) => new(new FakeTextMeasurer(), contentWidthPx);

    [Fact]
    public void Measure_SumsEachItemHeightPlusSpacing()
    {
        var list = new ReportList(ListStyle.Bulleted, new[] { "One", "Two", "Three" }) { ItemSpacingPx = 4 };

        var measurement = list.Measure(Context());

        // Each item is one 20px line (FakeTextMeasurer) plus 4px spacing.
        Assert.Equal(3 * (20 + 4), measurement.HeightPx);
    }

    [Fact]
    public void Split_BreaksAtItemBoundary_AndCarriesStartIndexForward()
    {
        var list = new ReportList(ListStyle.Numbered, new[] { "One", "Two", "Three", "Four" }) { ItemSpacingPx = 0 };

        // Each item is 20px; offer room for exactly 2.
        var split = list.Split(40, Context());

        var head = Assert.IsType<ReportList>(split.Head);
        Assert.Equal(new[] { "One", "Two" }, head.Items);
        Assert.Equal(0, head.StartIndex);

        var tail = Assert.IsType<ReportList>(split.Tail);
        Assert.Equal(new[] { "Three", "Four" }, tail.Items);
        Assert.Equal(2, tail.StartIndex);
    }

    [Fact]
    public void Split_NothingFits_IsUnsplittable()
    {
        var list = new ReportList(ListStyle.Bulleted, new[] { "One" });

        var split = list.Split(5, Context());

        Assert.Null(split.Head);
        Assert.Same(list, split.Tail);
    }

    [Fact]
    public void RenderHtml_NumberedListContinuation_ResumesNumberingViaStartAttribute()
    {
        var tail = new ReportList(ListStyle.Numbered, new[] { "Third item" }) { StartIndex = 2 };

        var placement = new ElementPlacement(0, 0, 200, 20, pageIndex: 1, PageSectionKind.Content);
        var html = tail.RenderHtml(placement, new RenderContext(2, 2));

        Assert.Contains("<ol start=\"3\"", html);
        Assert.Contains("Third item", html);
    }
}
