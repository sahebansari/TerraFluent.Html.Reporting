using FluentHtmlReport.Layout;
using FluentHtmlReport.Model;
using FluentHtmlReport.Model.Elements;
using FluentHtmlReport.Model.Styling;
using FluentHtmlReport.Rendering;
using FluentHtmlReport.Tests.TestHelpers;
using Xunit;

namespace FluentHtmlReport.Tests.Model;

public class RowTests
{
    private static readonly TextStyle NoMargin = TextStyle.Default.With(marginBottomPx: 0);

    private static LayoutContext Context(double contentWidthPx = 100) => new(new FakeTextMeasurer(), contentWidthPx);

    private static string Render(Row row, double widthPx, double heightPx)
    {
        var placement = new ElementPlacement(0, 0, widthPx, heightPx, 0, PageSectionKind.Content);
        return row.RenderHtml(placement, new RenderContext(1, 1));
    }

    [Fact]
    public void Measure_RowHeight_IsTallestColumnHeightPlusMargin()
    {
        // 20px-tall single-line "Hi" vs a 40px-tall two-line wrap (5 chars/line at width 50).
        var row = new Row(new[]
        {
            new RowColumn(new IReportElement[] { new Paragraph("Hi", NoMargin) }, widthPx: 40),
            new RowColumn(new IReportElement[] { new Paragraph("AAAAA BBBBB", NoMargin) }),
        }, columnGapPx: 10) { MarginBottomPx = 5 };

        var measurement = row.Measure(Context(100));

        Assert.Equal(45, measurement.HeightPx);
    }

    [Fact]
    public void ResolveColumnWidths_MixesFixedAndAutoColumns_AutoColumnsShareLeftoverAfterGaps()
    {
        // contentWidth 100 - gap 10 = 90 available; col0 fixed at 40 leaves 50 for the one auto column.
        var row = new Row(new[]
        {
            new RowColumn(new IReportElement[] { new Paragraph("Hi", NoMargin) }, widthPx: 40),
            new RowColumn(new IReportElement[] { new Paragraph("Hi", NoMargin) }),
        }, columnGapPx: 10) { MarginBottomPx = 0 };

        row.Measure(Context(100));
        var html = Render(row, 100, 20);

        Assert.Contains("left:0px;top:0px;width:40px;", html);
        Assert.Contains("left:50px;top:0px;width:50px;", html);
    }

    [Theory]
    [InlineData(RowVerticalAlignment.Top, 0)]
    [InlineData(RowVerticalAlignment.Middle, 10)]
    [InlineData(RowVerticalAlignment.Bottom, 20)]
    public void RenderHtml_VerticalAlignment_OffsetsShorterColumnWithinTallestColumnHeight(RowVerticalAlignment alignment, double expectedTopOffsetPx)
    {
        var row = new Row(new[]
        {
            new RowColumn(new IReportElement[] { new Paragraph("Hi", NoMargin) }, widthPx: 40), // 20px tall
            new RowColumn(new IReportElement[] { new Paragraph("AAAAA BBBBB", NoMargin) }), // 40px tall
        }, columnGapPx: 10, verticalAlignment: alignment) { MarginBottomPx = 0 };

        var measurement = row.Measure(Context(100));
        var html = Render(row, 100, measurement.HeightPx);

        Assert.Contains($"left:0px;top:{expectedTopOffsetPx:0.##}px;width:40px;height:20px;", html);
        Assert.Contains("left:50px;top:0px;width:50px;height:40px;", html);
    }

    [Fact]
    public void RenderHtml_MultipleElementsInOneColumn_StacksThemTopToBottom()
    {
        var row = new Row(new[]
        {
            new RowColumn(new IReportElement[] { new Paragraph("Line 1", NoMargin), new Paragraph("Line 2", NoMargin) }),
        }) { MarginBottomPx = 0 };

        var measurement = row.Measure(Context(100));
        var html = Render(row, 100, measurement.HeightPx);

        Assert.Equal(40, measurement.HeightPx);
        Assert.Contains("top:0px;width:100px;height:20px;", html);
        Assert.Contains("top:20px;width:100px;height:20px;", html);
    }

    [Fact]
    public void Measure_RowMargin_AddsTopAndBottomToHeight()
    {
        var row = new Row(new[] { new RowColumn(new IReportElement[] { new Paragraph("Hi", NoMargin) }) })
        {
            MarginTopPx = 5,
            MarginBottomPx = 7,
        };

        var measurement = row.Measure(Context(100));

        Assert.Equal(20 + 5 + 7, measurement.HeightPx);
    }

    [Fact]
    public void RenderHtml_RowMargin_ShiftsColumnsAndShrinksAvailableWidth()
    {
        // contentWidth 100 - marginLeft 10 - marginRight 10 = 80 available for the single auto column.
        var row = new Row(new[] { new RowColumn(new IReportElement[] { new Paragraph("Hi", NoMargin) }) })
        {
            MarginTopPx = 3,
            MarginBottomPx = 0,
            MarginLeftPx = 10,
            MarginRightPx = 10,
        };

        row.Measure(Context(100));
        var html = Render(row, 100, 20);

        Assert.Contains("left:10px;top:3px;width:80px;height:20px;", html);
    }

    [Fact]
    public void Measure_ColumnPadding_AddsToColumnHeightAndNarrowsChildWrapWidth()
    {
        // 100px column - 20px left padding - 20px right padding = 60px wrap
        // width -> 6 chars/line, too narrow for "AAAA BBBB" (9 chars) together.
        var column = new RowColumn(new IReportElement[] { new Paragraph("AAAA BBBB", NoMargin) })
            .With(paddingTopPx: 5, paddingRightPx: 20, paddingBottomPx: 6, paddingLeftPx: 20);
        var row = new Row(new[] { column }) { MarginBottomPx = 0 };

        var measurement = row.Measure(Context(100));

        Assert.Equal(40 + 5 + 6, measurement.HeightPx); // 2 wrapped lines (20px each) + top/bottom padding
    }

    [Fact]
    public void RenderHtml_ColumnPadding_InsetsChildFromColumnEdges()
    {
        var column = new RowColumn(new IReportElement[] { new Paragraph("Hi", NoMargin) })
            .With(paddingTopPx: 4, paddingRightPx: 6, paddingBottomPx: 0, paddingLeftPx: 5);
        var row = new Row(new[] { column }) { MarginBottomPx = 0 };

        var measurement = row.Measure(Context(100));
        var html = Render(row, 100, measurement.HeightPx);

        // width = column width(100) - paddingLeft(5) - paddingRight(6) = 89; left/top offset by padding.
        Assert.Contains("left:5px;top:4px;width:89px;height:20px;", html);
    }

    [Fact]
    public void Constructor_NoColumns_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Row(Array.Empty<RowColumn>()));
    }

    [Fact]
    public void RenderHtml_WithoutMeasureFirst_ThrowsActionableException()
    {
        var row = new Row(new[] { new RowColumn(new IReportElement[] { new Paragraph("Hi") }) });

        var ex = Assert.Throws<InvalidOperationException>(() => Render(row, 100, 20));

        Assert.Contains("Measure", ex.Message);
    }
}
