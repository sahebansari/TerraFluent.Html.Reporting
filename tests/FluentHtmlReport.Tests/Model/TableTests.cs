using FluentHtmlReport.Layout;
using FluentHtmlReport.Model;
using FluentHtmlReport.Model.Elements;
using FluentHtmlReport.Model.Styling;
using FluentHtmlReport.Tests.TestHelpers;
using Xunit;

namespace FluentHtmlReport.Tests.Model;

public class TableTests
{
    private static LayoutContext Context(double contentWidthPx = 100) => new(new FakeTextMeasurer(), contentWidthPx);

    private static TableStyle NoPaddingStyle(RowSplitBehavior behavior) => TableStyle.Default.With(
        cellPaddingPx: 0,
        borderWidthPx: 0,
        rowSplitBehavior: behavior);

    [Fact]
    public void Measure_SumsHeaderAndRowHeights()
    {
        var table = new Table(
            new TableColumn[] { "Col" },
            new[] { new TableRow(new TableCell[] { "Row 1" }), new TableRow(new TableCell[] { "Row 2" }) },
            NoPaddingStyle(RowSplitBehavior.KeepRowIntact));

        var measurement = table.Measure(Context());

        // header (1 line) + 2 single-line rows, 20px per line, no padding.
        Assert.Equal(60, measurement.HeightPx);
    }

    [Fact]
    public void Measure_ContinuationFragment_IncludesContinuedHeaderBannerHeight()
    {
        // RenderHtml adds an extra "(continued)" banner row above the header
        // for continuation fragments; Measure/Split must account for it too,
        // or a continuation table renders taller than the engine planned for.
        var rows = new[] { new TableRow(new TableCell[] { "Row 1" }) };
        var style = NoPaddingStyle(RowSplitBehavior.KeepRowIntact);

        var normal = new Table(new TableColumn[] { "Col" }, rows, style, isContinuation: false);
        var continuation = new Table(new TableColumn[] { "Col" }, rows, style, isContinuation: true);

        var normalHeight = normal.Measure(Context()).HeightPx;
        var continuationHeight = continuation.Measure(Context()).HeightPx;

        Assert.Equal(normalHeight + 20, continuationHeight);
    }

    [Fact]
    public void Split_KeepRowIntact_DefersWholeOverflowingRowToTail()
    {
        var rows = new[]
        {
            new TableRow(new TableCell[] { "Row 1" }),
            new TableRow(new TableCell[] { "Row 2" }),
            new TableRow(new TableCell[] { "Row 3" }),
        };
        var table = new Table(new TableColumn[] { "Col" }, rows, NoPaddingStyle(RowSplitBehavior.KeepRowIntact));

        // header (20) + exactly 1 row (20) = 40; the next row does not fit and is not split.
        var split = table.Split(40, Context());

        var head = Assert.IsType<Table>(split.Head);
        Assert.Single(head.Rows);
        Assert.Equal("Row 1", head.Rows[0].Cells[0].Text);

        var tail = Assert.IsType<Table>(split.Tail);
        Assert.True(tail.IsContinuation);
        Assert.Equal(2, tail.Rows.Count);
        Assert.Equal("Row 2", tail.Rows[0].Cells[0].Text);
        Assert.Equal("Row 3", tail.Rows[1].Cells[0].Text);
    }

    [Fact]
    public void Split_AllowSplitWithContinuedHeader_TruncatesRowAtLineBoundary()
    {
        // Column width 100px / 10px-per-char = 10 chars/line, so each 10-char
        // word is exactly one line; this cell wraps to exactly 2 lines.
        var cellText = $"{new string('A', 10)} {new string('B', 10)}";
        var table = new Table(
            new TableColumn[] { "Col" },
            new[] { new TableRow(new TableCell[] { cellText }) },
            NoPaddingStyle(RowSplitBehavior.AllowSplitWithContinuedHeader));

        // header (20) + 1 of the row's 2 lines (20) = 40.
        var split = table.Split(40, Context());

        var head = Assert.IsType<Table>(split.Head);
        Assert.Single(head.Rows);
        Assert.Equal(new string('A', 10), head.Rows[0].Cells[0].Text);

        var tail = Assert.IsType<Table>(split.Tail);
        Assert.True(tail.IsContinuation);
        Assert.Single(tail.Rows);
        Assert.Equal(new string('B', 10), tail.Rows[0].Cells[0].Text);
    }

    [Fact]
    public void Split_NoRoomForEvenTheHeader_IsUnsplittable()
    {
        var table = new Table(
            new TableColumn[] { "Col" },
            new[] { new TableRow(new TableCell[] { "Row 1" }) },
            NoPaddingStyle(RowSplitBehavior.AllowSplitWithContinuedHeader));

        var split = table.Split(10, Context());

        Assert.Null(split.Head);
        Assert.Same(table, split.Tail);
    }

    [Fact]
    public void Measure_IncludesOneBorderWidthPerRowPlusOneOuterTopEdge()
    {
        var table = new Table(
            new TableColumn[] { "Col" },
            new[] { new TableRow(new TableCell[] { "Row 1" }), new TableRow(new TableCell[] { "Row 2" }) },
            TableStyle.Default.With(cellPaddingPx: 0, borderWidthPx: 2, rowSplitBehavior: RowSplitBehavior.KeepRowIntact));

        var measurement = table.Measure(Context());

        // header(20) + row1(20) + row2(20) = 60px text height, plus one 2px
        // border per row (header + 2 body rows = 3) plus one extra 2px for
        // the table's own top edge (not shared with anything above it) = 8px.
        Assert.Equal(60 + 4 * 2, measurement.HeightPx);
    }

    [Fact]
    public void Constructor_RowCellCountDoesNotMatchColumnCount_ThrowsWithActionableMessage()
    {
        var columns = new TableColumn[] { "A", "B", "C" };
        var rows = new[] { new TableRow(new TableCell[] { "Only", "Two" }) };

        var ex = Assert.Throws<ArgumentException>(() => new Table(columns, rows));

        Assert.Contains("Row 0", ex.Message);
        Assert.Contains("2 cell", ex.Message);
        Assert.Contains("3 column", ex.Message);
    }
}
