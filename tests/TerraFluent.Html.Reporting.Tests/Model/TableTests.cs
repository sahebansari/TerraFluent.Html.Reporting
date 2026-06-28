using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Measurement;
using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Model.Styling;
using TerraFluent.Html.Reporting.Tests.TestHelpers;
using Xunit;

namespace TerraFluent.Html.Reporting.Tests.Model;

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
    public void Split_AllowSplitWithContinuedHeader_PreservesCellLineBoundaries()
    {
        var table = new Table(
            new TableColumn[] { "Col" },
            new[] { new TableRow(new TableCell[] { "A\nB\nC\nD" }) },
            NoPaddingStyle(RowSplitBehavior.AllowSplitWithContinuedHeader));

        // Header (20) + three of the row's four lines (60) = 80.
        var split = table.Split(80, Context());

        var head = Assert.IsType<Table>(split.Head);
        var tail = Assert.IsType<Table>(split.Tail);
        Assert.Equal("A\nB\nC", head.Rows[0].Cells[0].Text);
        Assert.Equal("D", tail.Rows[0].Cells[0].Text);
        Assert.Equal(80, head.Measure(Context()).HeightPx);
    }

    [Fact]
    public void Split_MixedCellLineHeights_HeadNeverExceedsAvailableHeight()
    {
        var small = TextStyle.Default.With(fontSizePx: 10, lineHeightMultiplier: 1, marginBottomPx: 0);
        var large = TextStyle.Default.With(fontSizePx: 30, lineHeightMultiplier: 1, marginBottomPx: 0);
        var lines = string.Join("|", Enumerable.Range(1, 10));
        var style = TableStyle.Default.With(
            headerTextStyle: small,
            cellTextStyle: small,
            cellPaddingPx: 0,
            borderWidthPx: 2,
            rowSplitBehavior: RowSplitBehavior.AllowSplitWithContinuedHeader);
        var table = new Table(
            new TableColumn[] { "Small", "Large" },
            new[] { new TableRow(new[] { new TableCell(lines, small), new TableCell(lines, large) }) },
            style);
        var context = new LayoutContext(new FontSizedLineMeasurer(), 200);

        // 14px header + 62px remaining (60px text + 2px row border): two
        // 30px lines fit, never six 10px lines (which would make the
        // large-font cell 180px tall).
        var split = table.Split(76, context);

        var head = Assert.IsType<Table>(split.Head);
        Assert.True(head.Measure(context).HeightPx <= 76);
        Assert.Equal("1\n2", head.Rows[0].Cells[0].Text);
        Assert.Equal("1\n2", head.Rows[0].Cells[1].Text);
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

    private sealed class FontSizedLineMeasurer : ITextMeasurer
    {
        public TextMeasurement Measure(string text, FontSpecification font, double maxWidthPx)
        {
            var lines = text.Replace("\r\n", "\n").Split(new[] { '\n', '|' });
            return new TextMeasurement(lines, font.FontSizePx * font.LineHeightMultiplier, 0);
        }
    }
}
