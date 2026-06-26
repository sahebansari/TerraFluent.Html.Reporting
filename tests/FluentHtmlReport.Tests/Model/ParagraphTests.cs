using FluentHtmlReport.Layout;
using FluentHtmlReport.Model;
using FluentHtmlReport.Model.Elements;
using FluentHtmlReport.Model.Styling;
using FluentHtmlReport.Rendering;
using FluentHtmlReport.Tests.TestHelpers;
using Xunit;

namespace FluentHtmlReport.Tests.Model;

public class ParagraphTests
{
    private static LayoutContext Context(double contentWidthPx = 100) => new(new FakeTextMeasurer(), contentWidthPx);

    private static string FiveWords() => string.Join(" ", Enumerable.Range(0, 5).Select(i => new string((char)('A' + i), 10)));

    [Fact]
    public void Split_WouldLeaveSingleOrphanLine_DefersWholeParagraphInstead()
    {
        // 5 ten-char words at 10px/char -> one word per 100px-wide line.
        var paragraph = new Paragraph(FiveWords(), TextStyle.Default.With(marginBottomPx: 0));

        // Room for exactly 1 of 5 lines would normally split 1/4, stranding an orphan.
        var split = paragraph.Split(20, Context());

        Assert.Null(split.Head);
        Assert.Same(paragraph, split.Tail);
    }

    [Fact]
    public void Split_WouldLeaveSingleWidowLine_CarriesTwoLinesOverInstead()
    {
        var paragraph = new Paragraph(FiveWords(), TextStyle.Default.With(marginBottomPx: 0));

        // Room for 4 of 5 lines would normally split 4/1, stranding a widow.
        var split = paragraph.Split(80, Context());

        var head = Assert.IsType<Paragraph>(split.Head);
        var tail = Assert.IsType<Paragraph>(split.Tail);
        Assert.Equal(3, head.Text.Split(' ').Length);
        Assert.Equal(2, tail.Text.Split(' ').Length);
    }

    [Fact]
    public void Split_ShortParagraph_AcceptsWidowWhenAvoidingItWouldCreateAnOrphanInstead()
    {
        // Only 3 lines total: splitting 2/1 leaves a widow, but the only
        // alternative (1/2) trades it for an orphan - so the widow is kept.
        var words = Enumerable.Range(0, 3).Select(i => new string((char)('A' + i), 10));
        var paragraph = new Paragraph(string.Join(" ", words), TextStyle.Default.With(marginBottomPx: 0));

        var split = paragraph.Split(40, Context());

        var head = Assert.IsType<Paragraph>(split.Head);
        var tail = Assert.IsType<Paragraph>(split.Tail);
        Assert.Equal(2, head.Text.Split(' ').Length);
        Assert.Single(tail.Text.Split(' '));
    }

    [Fact]
    public void Measure_HorizontalMarginAndPadding_NarrowTheWrapWidth()
    {
        // "AAAA BBBB" (9 chars) fits one 10-char-capacity line at the full
        // 100px width, but 100px - 20px left margin - 20px right padding =
        // 60px -> 6 chars/line, which is too narrow for both words together.
        var style = TextStyle.Default.With(marginBottomPx: 0, marginLeftPx: 20, paddingRightPx: 20);
        var paragraph = new Paragraph("AAAA BBBB", style);

        var measurement = paragraph.Measure(Context());

        Assert.Equal(40, measurement.HeightPx); // 2 lines * 20px
    }

    [Fact]
    public void Measure_VerticalMarginAndPadding_AddToHeight()
    {
        var style = TextStyle.Default.With(marginTopPx: 5, marginBottomPx: 6, paddingTopPx: 7, paddingBottomPx: 8);
        var paragraph = new Paragraph("Hi", style);

        var measurement = paragraph.Measure(Context());

        Assert.Equal(20 + 5 + 6 + 7 + 8, measurement.HeightPx);
    }

    [Fact]
    public void RenderHtml_MarginAndPadding_ShiftsBoxAndEmitsCssPadding()
    {
        var style = TextStyle.Default.With(marginTopPx: 4, marginRightPx: 6, marginBottomPx: 0, marginLeftPx: 5, paddingTopPx: 1, paddingRightPx: 2, paddingBottomPx: 3, paddingLeftPx: 4);
        var paragraph = new Paragraph("Hi", style);

        var placement = new ElementPlacement(10, 10, 100, 24, 0, PageSectionKind.Content);
        var html = paragraph.RenderHtml(placement, new RenderContext(1, 1));

        Assert.Contains("left:15px;top:14px;width:89px;height:20px;", html);
        Assert.Contains("padding:1px 2px 3px 4px;", html);
    }

    [Fact]
    public void Split_VerticalMarginAndPadding_AreSubtractedFromAvailableHeightAndNotDoubledAcrossFragments()
    {
        var style = TextStyle.Default.With(marginTopPx: 10, marginBottomPx: 10);
        var paragraph = new Paragraph(FiveWords(), style);

        // 100px available - 10px top margin - 10px bottom margin = 80px -> 4 lines fit.
        var split = paragraph.Split(100, Context());

        var head = Assert.IsType<Paragraph>(split.Head);
        var tail = Assert.IsType<Paragraph>(split.Tail);
        Assert.Equal(0, head.Style.MarginBottomPx);
        Assert.Equal(10, head.Style.MarginTopPx);
        Assert.Equal(0, tail.Style.MarginTopPx);
        Assert.Equal(10, tail.Style.MarginBottomPx);
    }
}
