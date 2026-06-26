using System.Globalization;
using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Styling;
using TerraFluent.Html.Reporting.Rendering;
using TerraFluent.Html.Reporting.Tests.TestHelpers;
using Xunit;

namespace TerraFluent.Html.Reporting.Tests.Rendering;

public class HtmlReportRendererTests
{
    private static LayoutResult Paginate(ReportDocument document) => LayoutEngine.Paginate(document);

    [Fact]
    public void RenderDocument_SinglePage_EmitsOnePageDivAndResolvesPageNumberTokens()
    {
        var document = ReportDocument.Create(PageSize.FromPixels(400, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Footer(f => f.AddPageNumber())
            .Content(c => c.AddParagraph("Hello"))
            .Build();

        var html = HtmlReportRenderer.Default.RenderDocument(Paginate(document));

        Assert.Equal(1, CountOccurrences(html, "class=\"fhr-page\""));
        Assert.Contains("Page 1 of 1", html);
        Assert.Contains("<html", html);
    }

    [Fact]
    public void RenderFragment_OmitsHtmlWrapper_ButKeepsStylesAndPages()
    {
        var document = ReportDocument.Create(PageSize.FromPixels(400, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c => c.AddParagraph("Hello"))
            .Build();

        var fragment = HtmlReportRenderer.Default.RenderFragment(Paginate(document));

        Assert.DoesNotContain("<html", fragment);
        Assert.Contains("<style>", fragment);
        Assert.Contains("class=\"fhr-page\"", fragment);
    }

    [Fact]
    public void RenderDocument_UserTextWithMarkup_IsHtmlEncodedNotInjected()
    {
        var document = ReportDocument.Create(PageSize.FromPixels(400, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c => c.AddParagraph("<script>alert(1)</script>"))
            .Build();

        var html = HtmlReportRenderer.Default.RenderDocument(Paginate(document));

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void RenderDocument_LongTable_SplitsIntoTwoTableFragmentsWithContinuedBanner()
    {
        var document = ReportDocument.Create(PageSize.FromPixels(300, 100))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c => c.AddTable(table =>
            {
                table.AddColumns("Col");
                for (var i = 0; i < 6; i++) table.AddRow($"Row {i}");
            }))
            .Build();

        var layout = Paginate(document);
        Assert.True(layout.Pages.Count > 1);

        var html = HtmlReportRenderer.Default.RenderDocument(layout);

        Assert.True(CountOccurrences(html, "<table") > 1);
        Assert.Contains("(continued)", html);
    }

    [Fact]
    public void RenderDocument_UnderCommaDecimalCulture_StillEmitsPeriodSeparatedCssLengths()
    {
        var document = ReportDocument.Create(PageSize.FromPixels(401.5, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c => c.AddParagraph("Hello"))
            .Build();

        // Built rather than looked up by name (e.g. "de-DE") so the test does
        // not depend on ICU/globalization data being available in the test
        // environment - only on a comma decimal separator being active.
        var commaDecimalCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        commaDecimalCulture.NumberFormat.NumberDecimalSeparator = ",";

        var layout = Paginate(document);
        var original = CultureInfo.CurrentCulture;
        string html;
        try
        {
            CultureInfo.CurrentCulture = commaDecimalCulture;
            html = HtmlReportRenderer.Default.RenderDocument(layout);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        Assert.Contains("401.5px", html);
        Assert.DoesNotContain("401,5", html);
    }

    [Fact]
    public void RenderDocument_ImageNarrowerThanContentArea_RendersAtItsOwnWidthNotStretched()
    {
        var pngBytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 100, 0, 0, 0, 50,
        };

        var document = ReportDocument.Create(PageSize.FromPixels(400, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c => c.AddImage(pngBytes, "image/png", widthPx: 100, heightPx: 50))
            .Build();

        var html = HtmlReportRenderer.Default.RenderDocument(Paginate(document));

        Assert.Contains("width:100px;height:50px;", html);
        Assert.DoesNotContain("width:400px", html);
    }

    [Fact]
    public void RenderDocument_HeaderRow_PlacesLogoAndTextSideBySideAboveTheContentArea()
    {
        var pngBytes = new byte[] { 1 }; // unrecognized format; fine since widthPx/heightPx are given explicitly.

        var document = ReportDocument.Create(PageSize.FromPixels(400, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Header(h => h.AddRow(row =>
            {
                row.AddColumn(50, col => col.AddImage(pngBytes, "image/png", widthPx: 50, heightPx: 50));
                row.AddColumn(col => col.AddText("Acme"));
            }, columnGapPx: 10))
            .Content(c => c.AddParagraph("Hello"))
            .Build();

        var html = HtmlReportRenderer.Default.RenderDocument(Paginate(document));

        // Logo column keeps its own 50x50 size at the row's top-left.
        Assert.Contains("left:0px;top:0px;width:50px;height:50px;", html);
        // Text column starts after the logo + gap, vertically centered against the taller logo column.
        Assert.Contains("left:60px;top:19px;width:340px;height:20px;", html);
        // Content starts below the full 58px (logo + its margin) + 8px row margin = 66px header.
        Assert.Contains("top:66px;width:400px;", html);
    }

    [Fact]
    public void RenderDocument_Table_ContainerHeightAccountsForBorderWidthSoLastRowBorderIsNotClipped()
    {
        var document = ReportDocument.Create(PageSize.FromPixels(200, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c => c.AddTable(table =>
            {
                table.AddColumns("Col");
                table.AddRow("Row 1");
            }, TableStyle.Default.With(cellPaddingPx: 0)))
            .Build();

        var html = HtmlReportRenderer.Default.RenderDocument(Paginate(document));

        // header(20) + row(20) + 3 default 1px border lines (header's own,
        // the row's own, and the table's outer top edge) = 43px. Before the
        // fix this div was only 42px tall with overflow:hidden, clipping the
        // last row's bottom border.
        Assert.Contains("height:43px;overflow:hidden;", html);
    }

    [Fact]
    public void RenderDocument_Table_DoesNotPinWidthTo100Percent()
    {
        // A border-collapse table-layout:fixed table whose <colgroup> widths
        // already sum to the full content width was previously pinned to
        // width:100% as well; per the CSS2.1 fixed-table-layout algorithm
        // that forces the browser to treat "100%" as authoritative and
        // distribute any (here, zero) leftover space across the columns,
        // leaving literally no room for the table's own outer border -
        // verified in a real browser to silently drop the rightmost column's
        // right border (and, less visibly, the leftmost column's left
        // border). Letting the table size itself from the column widths
        // (which already sum to the intended content width) leaves room for
        // its own border without changing the rendered column positions.
        var document = ReportDocument.Create(PageSize.FromPixels(200, 100))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Content(c => c.AddTable(table =>
            {
                table.AddColumns("Col");
                table.AddRow("Row 1");
            }))
            .Build();

        var html = HtmlReportRenderer.Default.RenderDocument(Paginate(document));

        Assert.Contains("<table style=\"border-collapse:collapse;table-layout:fixed;\">", html);
        Assert.DoesNotContain("width:100%", html);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
