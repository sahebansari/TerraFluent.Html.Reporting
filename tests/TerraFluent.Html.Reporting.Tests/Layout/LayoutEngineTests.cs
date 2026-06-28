using System.Threading;
using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Model.Styling;
using TerraFluent.Html.Reporting.Tests.TestHelpers;
using Xunit;

namespace TerraFluent.Html.Reporting.Tests.Layout;

public class LayoutEngineTests
{
    [Fact]
    public void Paginate_ShortContent_FitsOnSinglePage()
    {
        var document = ReportDocument.Create(PageSize.FromPixels(400, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c => c.AddParagraph("Hello world"))
            .Build();

        var result = LayoutEngine.Paginate(document);

        Assert.Single(result.Pages);
        Assert.Single(result.Pages[0].ContentElements);
    }

    [Fact]
    public void Paginate_ManyOneLineParagraphs_SpansMultiplePagesWithoutLosingAny()
    {
        // Content area is 100px tall; each paragraph is exactly one 20px line
        // with no margin, so exactly 5 fit per page and none should split.
        var document = ReportDocument.Create(PageSize.FromPixels(400, 100))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c =>
            {
                for (var i = 0; i < 20; i++)
                {
                    c.AddParagraph($"Line {i}", TextStyle.Default.With(marginBottomPx: 0));
                }
            })
            .Build();

        var result = LayoutEngine.Paginate(document);

        Assert.Equal(4, result.Pages.Count);
        Assert.All(result.Pages, page => Assert.Equal(5, page.ContentElements.Count));

        var renderedOrder = result.Pages.SelectMany(p => p.ContentElements)
            .Select(e => ((Paragraph)e.Element).Text)
            .ToList();
        Assert.Equal(Enumerable.Range(0, 20).Select(i => $"Line {i}"), renderedOrder);
    }

    [Fact]
    public void Paginate_LongParagraph_SplitsAtLineBoundaryAcrossPages()
    {
        // Content width 100px / 10px-per-char = 10 chars/line, so each 10-char
        // word becomes exactly one line. Content height 40px / 20px-per-line = 2
        // lines/page, so a 5-line paragraph must split 2/2/1 across three pages.
        var measurer = new FakeTextMeasurer();
        var words = Enumerable.Range(0, 5).Select(i => new string((char)('A' + i), 10)).ToList();
        var text = string.Join(" ", words);

        var document = ReportDocument.Create(PageSize.FromPixels(100, 40))
            .SetMargins(0)
            .UseTextMeasurer(measurer)
            .Content(c => c.AddParagraph(text, TextStyle.Default.With(marginBottomPx: 0)))
            .Build();

        var result = LayoutEngine.Paginate(document);

        Assert.Equal(3, result.Pages.Count);

        var fragments = result.Pages
            .Select(p => ((Paragraph)p.ContentElements.Single().Element).Text.Split('\n'))
            .ToList();
        Assert.Equal(new[] { 2, 2, 1 }, fragments.Select(f => f.Length));
        Assert.Equal(words, fragments.SelectMany(f => f));
    }

    [Fact]
    public void Paginate_WithHeaderAndFooter_RepeatsOnEveryPageWithCorrectPageIndex()
    {
        // header (1 line=20px) + footer (1 line=20px) + content area (100px) = 140px page.
        var document = ReportDocument.Create(PageSize.FromPixels(400, 140))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Header(h => h.AddText("Header"))
            .Footer(f => f.AddPageNumber())
            .Content(c =>
            {
                for (var i = 0; i < 10; i++)
                {
                    c.AddParagraph($"Line {i}", TextStyle.Default.With(marginBottomPx: 0));
                }
            })
            .Build();

        var result = LayoutEngine.Paginate(document);

        Assert.Equal(2, result.Pages.Count);
        for (var i = 0; i < result.Pages.Count; i++)
        {
            var page = result.Pages[i];
            Assert.Single(page.HeaderElements);
            Assert.Equal(i, page.HeaderElements[0].Placement.PageIndex);
            Assert.Single(page.FooterElements);
            Assert.Equal(i, page.FooterElements[0].Placement.PageIndex);
        }
    }

    [Fact]
    public void Paginate_PageBreak_ForcesNextElementOntoAFreshPage()
    {
        var document = ReportDocument.Create(PageSize.FromPixels(400, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c =>
            {
                c.AddParagraph("First", TextStyle.Default.With(marginBottomPx: 0));
                c.AddPageBreak();
                c.AddParagraph("Second", TextStyle.Default.With(marginBottomPx: 0));
            })
            .Build();

        var result = LayoutEngine.Paginate(document);

        Assert.Equal(2, result.Pages.Count);
        Assert.Equal("First", ((Paragraph)result.Pages[0].ContentElements.Single().Element).Text);
        Assert.Equal("Second", ((Paragraph)result.Pages[1].ContentElements.Single().Element).Text);
    }

    [Fact]
    public void Paginate_LeadingPageBreak_DoesNotProduceABlankFirstPage()
    {
        var document = ReportDocument.Create(PageSize.FromPixels(400, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c =>
            {
                c.AddPageBreak();
                c.AddParagraph("Only content", TextStyle.Default.With(marginBottomPx: 0));
            })
            .Build();

        var result = LayoutEngine.Paginate(document);

        Assert.Single(result.Pages);
        Assert.Single(result.Pages[0].ContentElements);
    }

    [Fact]
    public void Paginate_ElementTallerThanWholePage_IsForcePlacedAndRecordsWarning()
    {
        // A single paragraph whose height (200px) exceeds the entire content
        // area (40px) and cannot be split because lineBudget never exceeds 0.
        var document = ReportDocument.Create(PageSize.FromPixels(400, 40))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c => c.AddImage(new byte[] { 1 }, "image/png", widthPx: 10, heightPx: 200))
            .Build();

        var result = LayoutEngine.Paginate(document);

        Assert.Single(result.Pages);
        Assert.Single(result.Warnings);
        Assert.Equal(0, result.Warnings[0].PageIndex);
    }

    [Fact]
    public void Paginate_AlreadyCanceledToken_ThrowsBeforeCompletingLayout()
    {
        var document = ReportDocument.Create(PageSize.FromPixels(400, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c => c.AddParagraph("Hello"))
            .Build();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => LayoutEngine.Paginate(document, cts.Token));
    }
}
