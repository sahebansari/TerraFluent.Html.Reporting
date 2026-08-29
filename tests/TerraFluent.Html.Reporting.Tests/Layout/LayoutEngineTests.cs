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
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(0, warning.PageIndex);
        Assert.Equal(LayoutWarningReason.Overflow, warning.Reason);
        Assert.Equal(nameof(ReportImage), warning.ElementType);
        Assert.Equal(0, warning.ElementIndex);
    }

    [Fact]
    public void Paginate_TableAutoColumnWidthCollapsesToZero_RecordsColumnWidthCollapsedWarning()
    {
        // Content width is 100px; the 150px fixed column alone already exceeds
        // it, so the one auto-width column has nothing left and collapses to 0px.
        var document = ReportDocument.Create(PageSize.FromPixels(100, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c => c.AddTable(table =>
            {
                table.AddColumn("Fixed", widthPx: 150);
                table.AddColumn("Auto");
                table.AddRow("A", "B");
            }))
            .Build();

        var result = LayoutEngine.Paginate(document);

        var warning = Assert.Single(result.Warnings);
        Assert.Equal(LayoutWarningReason.ColumnWidthCollapsed, warning.Reason);
        Assert.Equal(nameof(Table), warning.ElementType);
        Assert.Equal(0, warning.PageIndex);
        Assert.Equal(0, warning.ElementIndex);
    }

    [Fact]
    public void Paginate_RowAutoColumnWidthCollapsesToZero_RecordsColumnWidthCollapsedWarning()
    {
        // Content width is 100px; a 150px fixed column alone already exceeds
        // it (even before the column gap), so the auto-width column collapses to 0px.
        var document = ReportDocument.Create(PageSize.FromPixels(100, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c => c.AddRow(row =>
            {
                row.AddColumn(150, col => col.AddText("Fixed"));
                row.AddColumn(col => col.AddText("Auto"));
            }))
            .Build();

        var result = LayoutEngine.Paginate(document);

        var warning = Assert.Single(result.Warnings);
        Assert.Equal(LayoutWarningReason.ColumnWidthCollapsed, warning.Reason);
        Assert.Equal(nameof(Row), warning.ElementType);
        Assert.Equal(0, warning.PageIndex);
        Assert.Equal(0, warning.ElementIndex);
    }

    [Fact]
    public void Paginate_TableColumnWidthCollapse_DocumentLevelStrictMode_ThrowsInsteadOfWarning()
    {
        var document = ReportDocument.Create(PageSize.FromPixels(100, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .UseStrictLayoutValidation()
            .Content(c => c.AddTable(table =>
            {
                table.AddColumn("Fixed", widthPx: 150);
                table.AddColumn("Auto");
                table.AddRow("A", "B");
            }))
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => LayoutEngine.Paginate(document));
        Assert.Contains("auto-width column", ex.Message);
    }

    [Fact]
    public void Paginate_TableColumnWidthCollapse_PerTableStrictMode_ThrowsEvenWithoutDocumentLevelStrictMode()
    {
        var document = ReportDocument.Create(PageSize.FromPixels(100, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c => c.AddTable(
                table =>
                {
                    table.AddColumn("Fixed", widthPx: 150);
                    table.AddColumn("Auto");
                    table.AddRow("A", "B");
                },
                TableStyle.Default.With(columnWidthOverflowMode: ColumnWidthOverflowMode.Throw)))
            .Build();

        Assert.Throws<InvalidOperationException>(() => LayoutEngine.Paginate(document));
    }

    [Fact]
    public void Paginate_RowColumnWidthCollapse_DocumentLevelStrictMode_ThrowsInsteadOfWarning()
    {
        var document = ReportDocument.Create(PageSize.FromPixels(100, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .UseStrictLayoutValidation()
            .Content(c => c.AddRow(row =>
            {
                row.AddColumn(150, col => col.AddText("Fixed"));
                row.AddColumn(col => col.AddText("Auto"));
            }))
            .Build();

        Assert.Throws<InvalidOperationException>(() => LayoutEngine.Paginate(document));
    }

    [Fact]
    public void Paginate_RowColumnWidthCollapse_PerRowStrictMode_ThrowsEvenWithoutDocumentLevelStrictMode()
    {
        var document = ReportDocument.Create(PageSize.FromPixels(100, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c => c.AddRow(
                row =>
                {
                    row.AddColumn(150, col => col.AddText("Fixed"));
                    row.AddColumn(col => col.AddText("Auto"));
                },
                style: RowStyle.Default.With(columnWidthOverflowMode: ColumnWidthOverflowMode.Throw)))
            .Build();

        Assert.Throws<InvalidOperationException>(() => LayoutEngine.Paginate(document));
    }

    [Fact]
    public void Paginate_OversizedImageInsideMultiColumnSection_ForcePlacesAndRecordsWarning()
    {
        // Content area is 40px tall; an 80px-tall image inside a 2-column
        // section can't fit even an empty column.
        var document = ReportDocument.Create(PageSize.FromPixels(400, 40))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c => c.AddColumns(2, columns => columns.AddImage(new byte[] { 1 }, "image/png", widthPx: 10, heightPx: 80)))
            .Build();

        var result = LayoutEngine.Paginate(document);

        Assert.Single(result.Pages);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(LayoutWarningReason.Overflow, warning.Reason);
        Assert.Equal(nameof(MultiColumnSection), warning.ElementType);
        Assert.Contains("empty column", warning.Message);
    }

    [Fact]
    public void Paginate_MultiColumnSectionTallerThanOnePage_SplitsAndTailResumesAtColumnZeroOnNextPage()
    {
        // Content area is 40px tall (2 lines); 6 single-word (never-wrapped)
        // paragraphs in 2 columns need 3 lines per column (60px) - too tall
        // for one page, so it must split, continuing on a second page.
        var document = ReportDocument.Create(PageSize.FromPixels(400, 40))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c => c.AddColumns(2, columns =>
            {
                for (var i = 1; i <= 6; i++)
                {
                    columns.AddParagraph($"Item{i}", TextStyle.Default.With(marginBottomPx: 0));
                }
            }))
            .Build();

        var result = LayoutEngine.Paginate(document);

        Assert.Equal(2, result.Pages.Count);
        Assert.Empty(result.Warnings);

        var page1Section = Assert.IsType<MultiColumnSection>(Assert.Single(result.Pages[0].ContentElements).Element);
        var page2Section = Assert.IsType<MultiColumnSection>(Assert.Single(result.Pages[1].ContentElements).Element);

        // Page 1: 2 columns * 2 lines (40px) each = 4 items placed.
        Assert.Equal(4, page1Section.Elements.Count);
        // Page 2's section is the tail, resuming fresh at column 0 with the 2 leftover items.
        Assert.Equal(2, page2Section.Elements.Count);
        Assert.Equal("Item5", ((Paragraph)page2Section.Elements[0]).Text);
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
