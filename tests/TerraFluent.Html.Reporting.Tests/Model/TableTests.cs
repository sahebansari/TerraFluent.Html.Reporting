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

    [Fact]
    public void Constructor_RowSpanCell_LetsFollowingRowOmitThatColumn()
    {
        var table = new Table(
            new TableColumn[] { "Category", "Item" },
            new[]
            {
                new TableRow(new TableCell[] { new TableCell("Fruit") { RowSpan = 2 }, "Apple" }),
                new TableRow(new TableCell[] { "Banana" }), // omits "Category" - covered by the RowSpan above.
            },
            NoPaddingStyle(RowSplitBehavior.KeepRowIntact));

        Assert.Equal(2, table.Rows.Count);
        Assert.Single(table.Rows[1].Cells);
    }

    [Fact]
    public void Constructor_ColSpanExtendsPastColumnCount_Throws()
    {
        var columns = new TableColumn[] { "A", "B" };
        var rows = new[] { new TableRow(new TableCell[] { new TableCell("X") { ColSpan = 3 } }) };

        var ex = Assert.Throws<ArgumentException>(() => new Table(columns, rows));
        Assert.Contains("ColSpan=3", ex.Message);
    }

    [Fact]
    public void Constructor_RowSpanExtendsPastRowCount_Throws()
    {
        var columns = new TableColumn[] { "A" };
        var rows = new[] { new TableRow(new TableCell[] { new TableCell("X") { RowSpan = 2 } }) };

        var ex = Assert.Throws<ArgumentException>(() => new Table(columns, rows));
        Assert.Contains("RowSpan=2", ex.Message);
    }

    [Fact]
    public void Measure_ColSpanCell_MeasuresAgainstSummedColumnWidth()
    {
        // Two auto-width columns share the 100px content width (50px each,
        // 5 chars/line). A ColSpan=2 cell instead gets the full 100px
        // (10 chars/line), so a 10-char word fits on one line rather than
        // wrapping across two 5-char lines.
        var table = new Table(
            new TableColumn[] { "A", "B" },
            new[] { new TableRow(new TableCell[] { new TableCell(new string('X', 10)) { ColSpan = 2 } }) },
            NoPaddingStyle(RowSplitBehavior.KeepRowIntact));

        var measurement = table.Measure(Context(100));

        // header (20, single-char cells) + 1 row (20, one line thanks to the
        // summed colspan width) = 40.
        Assert.Equal(40, measurement.HeightPx);
    }

    [Fact]
    public void Measure_RowSpanCell_InflatesLastRowOfSpanWhenTallerThanNaturalSum()
    {
        // The RowSpan=2 cell needs 3 lines (60px); the two rows it spans
        // would naturally only be 20px each (40px total) based on their
        // other cells, so the 20px deficit is added to the last row of the span.
        var tall = new TableCell("A\nB\nC") { RowSpan = 2 };
        var table = new Table(
            new TableColumn[] { "Spans", "Other" },
            new[]
            {
                new TableRow(new TableCell[] { tall, "x" }),
                new TableRow(new TableCell[] { "y" }),
            },
            NoPaddingStyle(RowSplitBehavior.KeepRowIntact));

        var measurement = table.Measure(Context());

        // header(20) + row0(20, "x" only - the RowSpan cell is excluded from
        // the per-row natural max) + row1(20 natural + 20 deficit) = 80.
        Assert.Equal(80, measurement.HeightPx);
    }

    [Fact]
    public void Split_AllowSplitWithContinuedHeader_RowSpanGroupNeverSplitsMidGroup()
    {
        var rows = new[]
        {
            new TableRow(new TableCell[] { new TableCell("Group") { RowSpan = 2 }, "Row A" }),
            new TableRow(new TableCell[] { "Row B" }),
            new TableRow(new TableCell[] { "Solo", "Row C" }),
        };
        var table = new Table(new TableColumn[] { "G", "Item" }, rows, NoPaddingStyle(RowSplitBehavior.AllowSplitWithContinuedHeader));

        // header(20) + the 2-row group (20+20=40) = 60 exactly: the group
        // must be added as a whole, and the unrelated row after it can't
        // also fit, so it defers to the tail rather than the engine trying
        // a mid-row split anywhere inside the group.
        var split = table.Split(60, Context());

        var head = Assert.IsType<Table>(split.Head);
        Assert.Equal(2, head.Rows.Count);
        Assert.Equal("Row A", head.Rows[0].Cells[1].Text);
        Assert.Equal("Row B", head.Rows[1].Cells[0].Text);

        var tail = Assert.IsType<Table>(split.Tail);
        Assert.Single(tail.Rows);
        Assert.Equal("Solo", tail.Rows[0].Cells[0].Text);
    }

    [Fact]
    public void Split_RowSpanGroupTallerThanEmptyPage_IsUnsplittable()
    {
        var rows = new[]
        {
            new TableRow(new TableCell[] { new TableCell("Group") { RowSpan = 2 }, "Row A" }),
            new TableRow(new TableCell[] { "Row B" }),
        };
        var table = new Table(new TableColumn[] { "G", "Item" }, rows, NoPaddingStyle(RowSplitBehavior.AllowSplitWithContinuedHeader));

        // header(20) + the group (40) = 60 total; offering only 50 isn't
        // enough even on an otherwise-empty page, and the group can't
        // partially split to make up the difference.
        var split = table.Split(50, Context());

        Assert.Null(split.Head);
        Assert.Same(table, split.Tail);
    }

    [Fact]
    public void Split_AllowSplitWithContinuedHeader_ColSpanCellStillSplitsAtLineBoundaryAndPreservesSpan()
    {
        // Two columns share the 100px content width (50px each); a ColSpan=2
        // cell gets the full 100px (10 chars/line) instead of one column's
        // 50px, so this text still wraps to exactly 2 lines as in the
        // single-column case.
        var cellText = $"{new string('A', 10)} {new string('B', 10)}";
        var table = new Table(
            new TableColumn[] { "Col1", "Col2" },
            new[] { new TableRow(new TableCell[] { new TableCell(cellText) { ColSpan = 2 } }) },
            NoPaddingStyle(RowSplitBehavior.AllowSplitWithContinuedHeader));

        // header (20) + 1 of the row's 2 lines (20) = 40.
        var split = table.Split(40, Context());

        var head = Assert.IsType<Table>(split.Head);
        Assert.Single(head.Rows);
        Assert.Equal(new string('A', 10), head.Rows[0].Cells[0].Text);
        Assert.Equal(2, head.Rows[0].Cells[0].ColSpan);

        var tail = Assert.IsType<Table>(split.Tail);
        Assert.True(tail.IsContinuation);
        Assert.Single(tail.Rows);
        Assert.Equal(new string('B', 10), tail.Rows[0].Cells[0].Text);
        Assert.Equal(2, tail.Rows[0].Cells[0].ColSpan);
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
