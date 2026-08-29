using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Model.Styling;
using TerraFluent.Html.Reporting.Rendering;
using TerraFluent.Html.Reporting.Tests.TestHelpers;
using Xunit;

namespace TerraFluent.Html.Reporting.Tests.Model;

public class MultiColumnSectionTests
{
    private static readonly TextStyle NoMargin = TextStyle.Default.With(marginBottomPx: 0);

    private static LayoutContext Context(double contentWidthPx = 100) => new(new FakeTextMeasurer(), contentWidthPx);

    private static string Render(MultiColumnSection section, double widthPx, double heightPx)
    {
        var placement = new ElementPlacement(0, 0, widthPx, heightPx, 0, PageSectionKind.Content);
        return section.RenderHtml(placement, new RenderContext(1, 1));
    }

    [Fact]
    public void Constructor_ColumnCountLessThanTwo_Throws()
    {
        Assert.Throws<ArgumentException>(() => new MultiColumnSection(new IReportElement[] { new Paragraph("A", NoMargin) }, columnCount: 1));
    }

    [Fact]
    public void Constructor_NestedMultiColumnSection_Throws()
    {
        var inner = new MultiColumnSection(new IReportElement[] { new Paragraph("A", NoMargin) }, columnCount: 2);

        Assert.Throws<ArgumentException>(() => new MultiColumnSection(new IReportElement[] { inner }, columnCount: 2));
    }

    [Fact]
    public void Measure_FreshSection_DistributesContentAcrossAllColumnsInsteadOfJustColumnZero()
    {
        // 6 single-line (20px) paragraphs, 3 columns: content that easily fits
        // as a single 120px-tall column must still spread 2-per-column (40px
        // natural height) - the whole point of a multi-column layout is the
        // side-by-side distribution, not just an overflow mechanism.
        var section = new MultiColumnSection(new IReportElement[]
        {
            new Paragraph("Item1", NoMargin),
            new Paragraph("Item2", NoMargin),
            new Paragraph("Item3", NoMargin),
            new Paragraph("Item4", NoMargin),
            new Paragraph("Item5", NoMargin),
            new Paragraph("Item6", NoMargin),
        }, columnCount: 3, columnGapPx: 0);

        var measurement = section.Measure(Context(90));

        Assert.InRange(measurement.HeightPx, 39.5, 40.0);

        var html = Render(section, 90, measurement.HeightPx);
        Assert.Contains("left:0px;top:0px;", html);
        // contentWidth 90 / 3 columns, no gap -> 30px per column.
        Assert.Contains("left:30px;top:0px;", html);
        Assert.Contains("left:60px;top:0px;", html);
        Assert.Contains("Item5", html);
    }

    [Fact]
    public void Split_PacksChildrenLeftToRightTopToBottomAcrossColumns()
    {
        // 4 single-line (20px) paragraphs, 2 columns, a 40px column budget -
        // exactly 2 paragraphs fit per column, none left over.
        var section = new MultiColumnSection(new IReportElement[]
        {
            new Paragraph("Item1", NoMargin),
            new Paragraph("Item2", NoMargin),
            new Paragraph("Item3", NoMargin),
            new Paragraph("Item4", NoMargin),
        }, columnCount: 2, columnGapPx: 10);

        var split = section.Split(40, Context(100));

        Assert.NotNull(split.Head);
        Assert.Null(split.Tail);

        // contentWidth 100, gap 10 -> (100-10)/2 = 45px per column.
        var html = Render((MultiColumnSection)split.Head!, 100, 40);
        Assert.Contains("left:0px;top:0px;width:45px;height:20px;", html);
        Assert.Contains("left:0px;top:20px;width:45px;height:20px;", html);
        Assert.Contains("left:55px;top:0px;width:45px;height:20px;", html);
        Assert.Contains("left:55px;top:20px;width:45px;height:20px;", html);
        Assert.Contains("Item1", html);
        Assert.Contains("Item3", html);
    }

    [Fact]
    public void Split_LeftoverChildrenBecomeTail_ResumingAtColumnZeroOfAFreshInstance()
    {
        // 5 single-line paragraphs, 2 columns, budget 20px (one line per column)
        // -> 2 fit (one per column), 3 left over as the tail.
        var section = new MultiColumnSection(new IReportElement[]
        {
            new Paragraph("Item1", NoMargin),
            new Paragraph("Item2", NoMargin),
            new Paragraph("Item3", NoMargin),
            new Paragraph("Item4", NoMargin),
            new Paragraph("Item5", NoMargin),
        }, columnCount: 2);

        var split = section.Split(20, Context(100));

        Assert.NotNull(split.Head);
        Assert.NotNull(split.Tail);

        var tail = (MultiColumnSection)split.Tail!;
        Assert.Equal(3, tail.Elements.Count);
        Assert.Equal("Item3", ((Paragraph)tail.Elements[0]).Text);

        // The tail is a fresh instance - measuring it distributes its 3
        // remaining items across the 2 columns again (2 in column 0, 1 in
        // column 1), rather than resuming mid-column from where the head left off.
        var tailMeasurement = tail.Measure(Context(100));
        Assert.InRange(tailMeasurement.HeightPx, 39.5, 40.0);
    }

    [Fact]
    public void Split_ParagraphTallerThanOneColumn_SplitsAcrossTheColumnBoundary()
    {
        // A 4-line paragraph (80px) with a 50px column budget: each 10-char
        // word is forced onto its own line regardless of width (FakeTextMeasurer
        // never hyphenates), so 2 lines (40px) fit in column 0 and the
        // remaining 2 lines continue in column 1.
        var longParagraph = new Paragraph("AAAAAAAAAA BBBBBBBBBB CCCCCCCCCC DDDDDDDDDD", NoMargin);
        var section = new MultiColumnSection(new IReportElement[] { longParagraph }, columnCount: 2, columnGapPx: 0);

        var split = section.Split(50, Context(100));

        Assert.NotNull(split.Head);
        Assert.Null(split.Tail);

        var head = (MultiColumnSection)split.Head!;
        var html = Render(head, 100, 50);

        Assert.Contains("left:0px;top:0px;width:50px;height:40px;", html);
        Assert.Contains("left:50px;top:0px;width:50px;height:40px;", html);
    }

    [Fact]
    public void RenderHtml_WithoutPriorMeasure_Throws()
    {
        var section = new MultiColumnSection(new IReportElement[] { new Paragraph("A", NoMargin) }, columnCount: 2);

        Assert.Throws<InvalidOperationException>(() => Render(section, 100, 20));
    }
}
