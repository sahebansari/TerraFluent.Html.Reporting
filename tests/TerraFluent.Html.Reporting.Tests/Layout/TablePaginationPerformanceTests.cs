using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Styling;
using TerraFluent.Html.Reporting.Tests.TestHelpers;
using Xunit;

namespace TerraFluent.Html.Reporting.Tests.Layout;

public class TablePaginationPerformanceTests
{
    [Fact]
    public void Paginate_LargeTableSpanningManyPages_MeasuresEachRowAtMostOnce()
    {
        // Single-char cells, padding 0, and no "(continued)" banner, so each
        // row/header is exactly one 20px line (FakeTextMeasurer). Page content
        // height fits exactly 5 body rows per page (header 20px + 5 rows *
        // 20px = 120px), so 200 rows span 40 pages.
        const int rowCount = 200;
        const int rowsPerPage = 5;

        var measurer = new CountingTextMeasurer(new FakeTextMeasurer());
        var document = ReportDocument.Create(PageSize.FromPixels(200, 20 + rowsPerPage * 20))
            .SetMargins(0)
            .UseTextMeasurer(measurer)
            .Content(c => c.AddTable(
                table =>
                {
                    table.AddColumns("C");
                    for (var i = 0; i < rowCount; i++) table.AddRow("x");
                },
                TableStyle.Default.With(cellPaddingPx: 0, borderWidthPx: 0, continuedHeaderSuffix: string.Empty)))
            .Build();

        var result = LayoutEngine.Paginate(document);

        Assert.Equal(rowCount / rowsPerPage, result.Pages.Count);

        // Each row's height should be computed once, ever, regardless of how
        // many pages the table spans (the header is cheap and re-measured
        // once or twice per page transition, which is fine - that part is
        // O(pages), not O(rows)). Without the row-height cache that propagates
        // across Split() calls, this count would scale roughly with rowCount^2
        // (tens of thousands of calls for 200 rows); with it, it stays close
        // to rowCount plus a small per-page constant.
        var pageCount = result.Pages.Count;
        Assert.True(
            measurer.CallCount <= rowCount + pageCount * 4,
            $"Expected close to {rowCount} measurer calls (plus a small per-page constant), but got {measurer.CallCount} - row heights are likely being re-measured on every page transition again.");
    }
}
