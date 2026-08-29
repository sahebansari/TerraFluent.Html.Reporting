using System.Text;
using TerraFluent.Html.Reporting.Compatibility;
using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Measurement;
using TerraFluent.Html.Reporting.Model.Styling;
using TerraFluent.Html.Reporting.Rendering;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>
/// A table with a repeated header row. Row-break behavior is governed by
/// <see cref="Styling.TableStyle.RowSplitBehavior"/> on <see cref="Style"/>:
/// rows either move to the next page intact, or - for
/// <see cref="RowSplitBehavior.AllowSplitWithContinuedHeader"/> - a row that
/// only partly fits has its cells truncated at a shared line budget, with the
/// remainder continuing as the first row on the next page under a repeated,
/// "(continued)"-suffixed header.
/// </summary>
/// <remarks>
/// A cell's <see cref="TableCell.ColSpan"/> and <see cref="TableCell.RowSpan"/>
/// let it cover more than one column/row - see <see cref="BuildRowLayout"/> for
/// the exact-occupancy validation this requires. Rows linked by an active
/// <see cref="TableCell.RowSpan"/> are treated as one atomic group for
/// pagination: the group either fits together on a page or moves to the next
/// page as a whole. <see cref="RowSplitBehavior.AllowSplitWithContinuedHeader"/>'s
/// mid-row line truncation only ever applies to a lone row with no active
/// rowspan.
/// </remarks>
public sealed class Table : IReportElement
{
    /// <summary>One cell placed at its starting column, as resolved by <see cref="BuildRowLayout"/>.</summary>
    private readonly record struct CellSlot(TableCell Cell, int StartColumn);

    /// <summary>The column definitions.</summary>
    public IReadOnlyList<TableColumn> Columns { get; }

    /// <summary>The body rows.</summary>
    public IReadOnlyList<TableRow> Rows { get; }

    /// <summary>The table's visual style.</summary>
    public TableStyle Style { get; }

    /// <summary>
    /// True for the tail fragment produced by <see cref="Split"/>: its header
    /// is rendered with <see cref="Styling.TableStyle.ContinuedHeaderSuffix"/> appended.
    /// </summary>
    public bool IsContinuation { get; }

    // Per-row list of cells that start in that row (i.e. excluding columns
    // covered by an earlier row's RowSpan), each tagged with its starting
    // column index. Computed once - unlike row heights, this doesn't depend
    // on content width - and reused by Measure/Split/RenderHtml.
    private readonly IReadOnlyList<CellSlot>[] _rowLayout;

    // For row i, the exclusive end index of the maximal group of rows linked
    // by an active RowSpan starting at or before i (a lone row is its own
    // group: _groupEndForRow[i] == i + 1). See ComputeGroupBoundaries.
    private readonly int[] _groupEndForRow;

    // Row heights computed for a given content width, in Rows order. Populated
    // lazily by GetRowHeights and propagated forward (sliced, not recomputed)
    // when Split produces head/tail fragments - see GetRowHeights for why this
    // is what keeps pagination of a large table from being O(rows^2).
    private double[]? _cachedRowHeights;
    private double _cachedForContentWidthPx = double.NaN;

    /// <summary>Creates a table.</summary>
    /// <exception cref="ArgumentException">
    /// A row's cells don't exactly account for every column, once
    /// <see cref="TableCell.ColSpan"/> and any <see cref="TableCell.RowSpan"/>
    /// carried over from an earlier row are taken into account.
    /// </exception>
    public Table(IReadOnlyList<TableColumn> columns, IReadOnlyList<TableRow> rows, TableStyle? style = null, bool isContinuation = false)
        : this(columns, rows, style, isContinuation, precomputedRowHeights: null, precomputedForContentWidthPx: double.NaN, precomputedRowLayout: null)
    {
    }

    private Table(
        IReadOnlyList<TableColumn> columns,
        IReadOnlyList<TableRow> rows,
        TableStyle? style,
        bool isContinuation,
        double[]? precomputedRowHeights,
        double precomputedForContentWidthPx,
        IReadOnlyList<CellSlot>[]? precomputedRowLayout)
    {
        Columns = Guard.Snapshot(columns, nameof(columns));
        Rows = Guard.Snapshot(rows, nameof(rows));
        Style = style ?? TableStyle.Default;
        IsContinuation = isContinuation;

        _rowLayout = precomputedRowLayout ?? BuildRowLayout(Columns, Rows);
        _groupEndForRow = ComputeGroupBoundaries(_rowLayout, Columns.Count);

        _cachedRowHeights = precomputedRowHeights;
        _cachedForContentWidthPx = precomputedForContentWidthPx;
    }

    /// <summary>
    /// Walks each row left to right, resolving which column each cell starts
    /// at. A column already covered by an earlier row's <see cref="TableCell.RowSpan"/>
    /// is skipped (no cell consumed for it); otherwise the row's next cell is
    /// placed there and, if its <see cref="TableCell.ColSpan"/> is greater
    /// than 1, occupies the following columns too. A row must consume exactly
    /// enough cells to account for every column - too few or too many throws.
    /// </summary>
    private static IReadOnlyList<CellSlot>[] BuildRowLayout(IReadOnlyList<TableColumn> columns, IReadOnlyList<TableRow> rows)
    {
        var columnCount = columns.Count;
        var rowLayout = new IReadOnlyList<CellSlot>[rows.Count];
        var pending = new int[columnCount];

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var slots = new List<CellSlot>();
            var cellIndex = 0;
            var col = 0;

            while (col < columnCount)
            {
                if (pending[col] > 0)
                {
                    pending[col]--;
                    col++;
                    continue;
                }

                if (cellIndex >= row.Cells.Count)
                {
                    throw new ArgumentException(
                        $"Row {r} supplies {row.Cells.Count} cell(s) but needs at least one more to fill column {col} " +
                        $"(not covered by a RowSpan from an earlier row) of the table's {columnCount} column(s).",
                        nameof(rows));
                }

                var cell = row.Cells[cellIndex];
                var span = cell.ColSpan;
                if (col + span > columnCount)
                {
                    throw new ArgumentException(
                        $"Row {r}'s cell {cellIndex} has ColSpan={span} starting at column {col}, which extends past the table's {columnCount} column(s).",
                        nameof(rows));
                }

                slots.Add(new CellSlot(cell, col));

                if (cell.RowSpan > 1)
                {
                    if (r + cell.RowSpan > rows.Count)
                    {
                        throw new ArgumentException(
                            $"Row {r}'s cell {cellIndex} has RowSpan={cell.RowSpan} starting at row {r}, which extends past the table's {rows.Count} row(s).",
                            nameof(rows));
                    }

                    for (var k = 0; k < span; k++)
                    {
                        pending[col + k] = cell.RowSpan - 1;
                    }
                }

                cellIndex++;
                col += span;
            }

            if (cellIndex != row.Cells.Count)
            {
                throw new ArgumentException(
                    $"Row {r} supplies {row.Cells.Count} cell(s) but only {cellIndex} fit within the table's {columnCount} column(s) " +
                    "after accounting for column spans and columns carried over by a RowSpan from an earlier row.",
                    nameof(rows));
            }

            rowLayout[r] = slots;
        }

        return rowLayout;
    }

    /// <summary>
    /// Derives, for each row, the exclusive end of the maximal group of rows
    /// an active <see cref="TableCell.RowSpan"/> links it to - a plain
    /// re-derivation from already-known cell placements (no validation), so
    /// it's just as cheap to run again on a <see cref="Split"/> fragment's
    /// sliced <see cref="_rowLayout"/> as it would be to thread the original
    /// table's boundaries through with index translation.
    /// </summary>
    private static int[] ComputeGroupBoundaries(IReadOnlyList<CellSlot>[] rowLayout, int columnCount)
    {
        var groupEndForRow = new int[rowLayout.Length];
        var pending = new int[columnCount];
        var groupStart = 0;

        for (var r = 0; r < rowLayout.Length; r++)
        {
            for (var c = 0; c < columnCount; c++)
            {
                if (pending[c] > 0) pending[c]--;
            }

            foreach (var slot in rowLayout[r])
            {
                if (slot.Cell.RowSpan > 1)
                {
                    for (var k = 0; k < slot.Cell.ColSpan; k++)
                    {
                        pending[slot.StartColumn + k] = slot.Cell.RowSpan - 1;
                    }
                }
            }

            if (Array.TrueForAll(pending, p => p == 0))
            {
                for (var g = groupStart; g <= r; g++) groupEndForRow[g] = r + 1;
                groupStart = r + 1;
            }
        }

        return groupEndForRow;
    }

    private double[] ResolveColumnWidths(double contentWidthPx) => ResolveColumnWidths(contentWidthPx, diagnosticsContext: null);

    /// <summary>
    /// Resolves each column's width. When <paramref name="diagnosticsContext"/>
    /// is supplied (from <see cref="Measure"/> - <see cref="Split"/>/<see cref="RenderHtml"/>
    /// don't pass one, to avoid double-reporting the same condition once per
    /// page), reports <see cref="LayoutWarningReason.ColumnWidthCollapsed"/>
    /// when an auto-width column resolves to 0px because the table's
    /// fixed-width columns already consume the full available width.
    /// </summary>
    private double[] ResolveColumnWidths(double contentWidthPx, LayoutContext? diagnosticsContext)
    {
        var explicitTotal = 0.0;
        var explicitCount = 0;
        foreach (var column in Columns)
        {
            if (column.WidthPx is { } w)
            {
                explicitTotal += w;
                explicitCount++;
            }
        }

        var autoCount = Columns.Count - explicitCount;
        var autoWidth = autoCount > 0 ? Math.Max(0, contentWidthPx - explicitTotal) / autoCount : 0;

        if (autoCount > 0 && autoWidth <= 0 && diagnosticsContext?.Diagnostics is { } diagnostics)
        {
            var strict = Style.ColumnWidthOverflowMode == ColumnWidthOverflowMode.Throw || diagnosticsContext.StrictMode;
            diagnostics.Report(
                LayoutWarningReason.ColumnWidthCollapsed,
                $"{autoCount} auto-width column(s) in this table resolved to 0px because its fixed-width columns ({explicitTotal:0.#}px) already consume the full {contentWidthPx:0.#}px available - their content is not visible.",
                nameof(Table),
                strict);
        }

        var widths = new double[Columns.Count];
        for (var i = 0; i < Columns.Count; i++)
        {
            widths[i] = Columns[i].WidthPx ?? autoWidth;
        }

        return widths;
    }

    private IReadOnlyList<CellSlot> HeaderRowSlots() =>
        Columns.Select((c, i) => new CellSlot(c.Header, i)).ToList();

    private static double SpanWidth(double[] columnWidths, int startColumn, int span)
    {
        var total = 0.0;
        for (var i = 0; i < span; i++) total += columnWidths[startColumn + i];
        return total;
    }

    /// <summary>A cell's own box height: its wrapped text height plus <see cref="Styling.TableStyle.CellPaddingPx"/> on top and bottom.</summary>
    private double MeasureCellBoxHeight(TableCell cell, double widthPx, LayoutContext context, TextStyle defaultStyle)
    {
        var style = cell.Style ?? defaultStyle;
        var usableWidth = Math.Max(1, widthPx - 2 * Style.CellPaddingPx);
        var measured = context.TextMeasurer.Measure(cell.Text, style.ToFontSpecification(), usableWidth);
        return measured.TotalHeightPx + 2 * Style.CellPaddingPx;
    }

    /// <summary>
    /// A row's measured height includes one <see cref="Styling.TableStyle.BorderWidthPx"/>
    /// for its own border line - with <c>border-collapse: collapse</c>, that
    /// line is shared with the row below, so summing one per row (plus one
    /// extra for the table's outermost top edge, added once in <see cref="Measure"/>/
    /// <see cref="Split"/>) matches the real rendered height. Omitting this
    /// entirely previously under-measured the table, and since the container
    /// <c>RenderHtml</c> wraps it in is <c>overflow:hidden</c> at that
    /// (too-short) height, the last row's bottom border was silently clipped.
    /// Cells with an active <see cref="TableCell.RowSpan"/> are excluded from
    /// this per-row max: their height requirement is satisfied by the *sum*
    /// of the rows they span instead - see the inflation pass in <see cref="GetRowHeights"/>.
    /// </summary>
    private double MeasureRowHeight(IReadOnlyList<CellSlot> cellSlots, double[] columnWidths, LayoutContext context, TextStyle defaultStyle)
    {
        var maxHeight = 0.0;
        foreach (var slot in cellSlots)
        {
            if (slot.Cell.RowSpan > 1) continue;
            var height = MeasureCellBoxHeight(slot.Cell, SpanWidth(columnWidths, slot.StartColumn, slot.Cell.ColSpan), context, defaultStyle);
            if (height > maxHeight) maxHeight = height;
        }

        return maxHeight + Style.BorderWidthPx;
    }

    /// <summary>
    /// Returns each row's measured height, in <see cref="Rows"/> order, computing
    /// it only the first time it's needed for a given content width. Without
    /// this cache, a large table split across many pages would re-measure its
    /// entire remaining row list on every single page transition (both in
    /// <see cref="Measure"/> and at the start of <see cref="Split"/>), making
    /// pagination of an N-row table O(N^2). Instead, <see cref="Split"/>
    /// slices this array and threads the relevant slice into the head/tail
    /// fragments it creates, so each row's height is computed at most once
    /// across the table's entire pagination, however many pages it spans.
    /// </summary>
    private double[] GetRowHeights(double[] columnWidths, LayoutContext context)
    {
        if (_cachedRowHeights is not null && _cachedForContentWidthPx.Equals(context.ContentWidthPx))
        {
            return _cachedRowHeights;
        }

        var heights = new double[Rows.Count];
        for (var i = 0; i < Rows.Count; i++)
        {
            heights[i] = MeasureRowHeight(_rowLayout[i], columnWidths, context, Style.CellTextStyle);
        }

        // A RowSpan cell's own required height must fit within the sum of the
        // rows it spans; if the natural heights above don't add up to enough,
        // add the whole deficit to the last row of its span. This is an
        // approximation (a real browser may distribute the extra height
        // differently), consistent with the library's approximate text
        // measurement elsewhere, but it guarantees the combined box is tall
        // enough for the cell's content.
        for (var i = 0; i < Rows.Count; i++)
        {
            foreach (var slot in _rowLayout[i])
            {
                if (slot.Cell.RowSpan <= 1) continue;

                var spanWidth = SpanWidth(columnWidths, slot.StartColumn, slot.Cell.ColSpan);
                var required = MeasureCellBoxHeight(slot.Cell, spanWidth, context, Style.CellTextStyle);
                var lastRow = i + slot.Cell.RowSpan - 1;

                var sum = 0.0;
                for (var r = i; r <= lastRow; r++) sum += heights[r];

                if (required > sum)
                {
                    heights[lastRow] += required - sum;
                }
            }
        }

        _cachedRowHeights = heights;
        _cachedForContentWidthPx = context.ContentWidthPx;
        return heights;
    }

    /// <summary>
    /// The height of the "(continued)" banner row <see cref="RenderHtml"/> adds
    /// above the header when <see cref="IsContinuation"/> is true and the
    /// suffix isn't empty. Must be included in <see cref="Measure"/>/<see cref="Split"/>
    /// or a continuation fragment would render taller than the engine thinks
    /// it placed it as, silently overflowing its allotted space by about one line.
    /// </summary>
    private double ContinuationBannerHeight(double[] columnWidths, LayoutContext context)
    {
        if (!IsContinuation) return 0;
        var suffix = Style.ContinuedHeaderSuffix.Trim();
        if (suffix.Length == 0) return 0;

        var fullWidth = 0.0;
        foreach (var width in columnWidths) fullWidth += width;

        var usableWidth = Math.Max(1, fullWidth - 2 * Style.CellPaddingPx);
        var measured = context.TextMeasurer.Measure(suffix, Style.HeaderTextStyle.ToFontSpecification(), usableWidth);
        return measured.TotalHeightPx + 2 * Style.CellPaddingPx + Style.BorderWidthPx;
    }

    /// <inheritdoc />
    public ElementMeasurement Measure(LayoutContext context)
    {
        var widths = ResolveColumnWidths(context.ContentWidthPx, context);
        var rowHeights = GetRowHeights(widths, context);
        // +1 border width for the table's outermost top edge - every row
        // already counts one border line for its own bottom edge (see
        // MeasureRowHeight), so this is the one edge nothing else accounts for.
        var total = Style.BorderWidthPx + MeasureRowHeight(HeaderRowSlots(), widths, context, Style.HeaderTextStyle) + ContinuationBannerHeight(widths, context);
        for (var i = 0; i < rowHeights.Length; i++)
        {
            total += rowHeights[i];
        }

        return new ElementMeasurement(total);
    }

    /// <inheritdoc />
    public SplitResult Split(double availableHeightPx, LayoutContext context)
    {
        var widths = ResolveColumnWidths(context.ContentWidthPx);
        var rowHeights = GetRowHeights(widths, context);
        var headerHeight = Style.BorderWidthPx + MeasureRowHeight(HeaderRowSlots(), widths, context, Style.HeaderTextStyle) + ContinuationBannerHeight(widths, context);
        if (headerHeight >= availableHeightPx)
        {
            return SplitResult.Unsplittable(this);
        }

        var used = headerHeight;
        var headRows = new List<TableRow>();
        var headHeights = new List<double>();
        var headLayout = new List<IReadOnlyList<CellSlot>>();
        var index = 0;

        while (index < Rows.Count)
        {
            // A group of rows linked by an active RowSpan is atomic: it's
            // added whole or not at all, regardless of RowSplitBehavior.
            var groupEnd = _groupEndForRow[index];
            var groupHeight = 0.0;
            for (var r = index; r < groupEnd; r++) groupHeight += rowHeights[r];

            if (used + groupHeight <= availableHeightPx)
            {
                for (var r = index; r < groupEnd; r++)
                {
                    headRows.Add(Rows[r]);
                    headHeights.Add(rowHeights[r]);
                    headLayout.Add(_rowLayout[r]);
                }

                used += groupHeight;
                index = groupEnd;
                continue;
            }

            // Mid-row line truncation only ever applies to a lone row with no
            // active rowspan (groupEnd - index == 1) - never inside a
            // multi-row group, where splitting a spanned cell's content
            // mid-page isn't well-defined.
            if (groupEnd - index == 1 &&
                Style.RowSplitBehavior == RowSplitBehavior.AllowSplitWithContinuedHeader &&
                TrySplitRow(_rowLayout[index], widths, context, availableHeightPx - used, out var rowHead, out var rowTail))
            {
                var headSlots = RemapSlots(_rowLayout[index], rowHead);
                var tailSlots = RemapSlots(_rowLayout[index], rowTail);

                headRows.Add(rowHead);
                headHeights.Add(MeasureRowHeight(headSlots, widths, context, Style.CellTextStyle));
                headLayout.Add(headSlots);

                var tailRows = new List<TableRow> { rowTail };
                var tailHeights = new List<double> { MeasureRowHeight(tailSlots, widths, context, Style.CellTextStyle) };
                var tailLayout = new List<IReadOnlyList<CellSlot>> { tailSlots };

                tailRows.AddRange(Rows.Skip(index + 1));
                for (var t = index + 1; t < rowHeights.Length; t++)
                {
                    tailHeights.Add(rowHeights[t]);
                    tailLayout.Add(_rowLayout[t]);
                }

                return BuildSplitResult(headRows, headHeights, headLayout.ToArray(), tailRows, tailHeights, tailLayout.ToArray(), context.ContentWidthPx);
            }

            break;
        }

        if (headRows.Count == 0)
        {
            return SplitResult.Unsplittable(this);
        }

        var remainingRows = Rows.Skip(index).ToList();
        var remainingHeights = new List<double>();
        var remainingLayout = new List<IReadOnlyList<CellSlot>>();
        for (var t = index; t < rowHeights.Length; t++)
        {
            remainingHeights.Add(rowHeights[t]);
            remainingLayout.Add(_rowLayout[t]);
        }

        return BuildSplitResult(headRows, headHeights, headLayout.ToArray(), remainingRows, remainingHeights, remainingLayout.ToArray(), context.ContentWidthPx);
    }

    private SplitResult BuildSplitResult(
        List<TableRow> headRows,
        List<double> headRowHeights,
        IReadOnlyList<CellSlot>[] headLayout,
        List<TableRow> tailRows,
        List<double> tailRowHeights,
        IReadOnlyList<CellSlot>[] tailLayout,
        double contentWidthPx)
    {
        var head = new Table(Columns, headRows, Style, IsContinuation, headRowHeights.ToArray(), contentWidthPx, headLayout);
        if (tailRows.Count == 0) return SplitResult.Partial(head, null);
        var tail = new Table(Columns, tailRows, Style, isContinuation: true, tailRowHeights.ToArray(), contentWidthPx, tailLayout);
        return SplitResult.Partial(head, tail);
    }

    /// <summary>Rebuilds a row's cell-slot list against a freshly split <see cref="TableRow"/>, preserving each cell's original starting column.</summary>
    private static IReadOnlyList<CellSlot> RemapSlots(IReadOnlyList<CellSlot> original, TableRow newRow)
    {
        var slots = new List<CellSlot>(newRow.Cells.Count);
        for (var i = 0; i < newRow.Cells.Count; i++)
        {
            slots.Add(new CellSlot(newRow.Cells[i], original[i].StartColumn));
        }

        return slots;
    }

    /// <summary>
    /// Truncates every cell in a lone (non-grouped) row to a shared line budget
    /// derived from <paramref name="remainingHeightPx"/>, so the row's visual
    /// split lines up across columns. Returns false if no line fits, or if no
    /// cell actually needed truncation (i.e. the row would not have overflowed).
    /// </summary>
    private bool TrySplitRow(IReadOnlyList<CellSlot> cellSlots, double[] widths, LayoutContext context, double remainingHeightPx, out TableRow head, out TableRow tail)
    {
        head = null!;
        tail = null!;

        // MeasureRowHeight includes the row's border line as well as its
        // padding, so reserve all of them before budgeting text lines.
        var usableForText = remainingHeightPx - 2 * Style.CellPaddingPx - Style.BorderWidthPx;
        if (usableForText <= 0) return false;

        var measurements = new TextMeasurement[cellSlots.Count];
        var maxLineHeight = 0.0;
        for (var c = 0; c < cellSlots.Count; c++)
        {
            var slot = cellSlots[c];
            var style = slot.Cell.Style ?? Style.CellTextStyle;
            var width = Math.Max(1, SpanWidth(widths, slot.StartColumn, slot.Cell.ColSpan) - 2 * Style.CellPaddingPx);
            measurements[c] = context.TextMeasurer.Measure(slot.Cell.Text, style.ToFontSpecification(), width);
            if (measurements[c].LineHeightPx > maxLineHeight) maxLineHeight = measurements[c].LineHeightPx;
        }

        // A shared line budget keeps cells aligned, but it must be based on
        // the tallest line. Using the shortest line allowed a large-font cell
        // to render a head fragment taller than the space offered to Split.
        var lineBudget = (int)Math.Floor(usableForText / maxLineHeight);
        if (lineBudget <= 0) return false;

        var headCells = new List<TableCell>();
        var tailCells = new List<TableCell>();
        var splitSomething = false;

        for (var c = 0; c < cellSlots.Count; c++)
        {
            var cell = cellSlots[c].Cell;
            var style = cell.Style ?? Style.CellTextStyle;
            var lines = measurements[c].Lines;

            if (lineBudget >= lines.Count)
            {
                headCells.Add(cell);
                tailCells.Add(new TableCell(string.Empty, style) { ColSpan = cell.ColSpan });
                continue;
            }

            headCells.Add(new TableCell(string.Join("\n", lines.Take(lineBudget)), style) { ColSpan = cell.ColSpan });
            tailCells.Add(new TableCell(string.Join("\n", lines.Skip(lineBudget)), style) { ColSpan = cell.ColSpan });
            splitSomething = true;
        }

        if (!splitSomething) return false;

        head = new TableRow(headCells);
        tail = new TableRow(tailCells);
        return true;
    }

    /// <inheritdoc />
    public string RenderHtml(ElementPlacement placement, RenderContext context)
    {
        var widths = ResolveColumnWidths(placement.WidthPx);
        var sb = new StringBuilder();

        sb.Append("<div style=\"position:absolute;left:").Append(CssFormat.Px(placement.XPx))
          .Append(";top:").Append(CssFormat.Px(placement.YPx))
          .Append(";width:").Append(CssFormat.Px(placement.WidthPx))
          .Append(";height:").Append(CssFormat.Px(placement.HeightPx)).Append(";overflow:hidden;\">");

        // No explicit width here (e.g. "width:100%"): the <colgroup> widths
        // below already sum to placement.WidthPx, which is all table-layout:fixed
        // needs to size the columns. Pinning the table to a width as well makes
        // that width authoritative per the CSS2.1 fixed-layout algorithm,
        // which then has zero leftover space to give the table's own outer
        // border - silently dropping the rightmost (and, less visibly, the
        // leftmost) column's outer border in real browsers.
        sb.Append("<table style=\"border-collapse:collapse;table-layout:fixed;\"><colgroup>");
        foreach (var width in widths)
        {
            sb.Append("<col style=\"width:").Append(CssFormat.Px(width)).Append("\" />");
        }

        sb.Append("</colgroup><thead>");

        if (IsContinuation && Style.ContinuedHeaderSuffix.Trim().Length > 0)
        {
            sb.Append("<tr><td colspan=\"").Append(Columns.Count).Append("\" style=\"background-color:")
              .Append(CssFormat.Attribute(Style.HeaderBackgroundColor)).Append(";color:").Append(CssFormat.Attribute(Style.HeaderTextStyle.Color))
              .Append(";font-style:italic;padding:").Append(CssFormat.Px(Style.CellPaddingPx))
              .Append(";border:").Append(CssFormat.Px(Style.BorderWidthPx)).Append(" solid ").Append(CssFormat.Attribute(Style.BorderColor))
              .Append(";\">").Append(CssFormat.Encode(Style.ContinuedHeaderSuffix.Trim())).Append("</td></tr>");
        }

        sb.Append("<tr>");
        foreach (var column in Columns)
        {
            AppendCell(sb, "th", column.Header, null, Style.HeaderTextStyle, Style.HeaderBackgroundColor);
        }

        sb.Append("</tr></thead><tbody>");

        for (var r = 0; r < Rows.Count; r++)
        {
            var rowBackgroundColor = Style.StripedRows && r % 2 != 0 ? Style.OddRowBackgroundColor : Style.EvenRowBackgroundColor;
            sb.Append("<tr>");
            foreach (var cell in Rows[r].Cells)
            {
                AppendCell(sb, "td", cell.Text, cell.Style, Style.CellTextStyle, rowBackgroundColor, cell.ColSpan, cell.RowSpan);
            }

            sb.Append("</tr>");
        }

        sb.Append("</tbody></table></div>");
        return sb.ToString();
    }

    private void AppendCell(StringBuilder sb, string tag, string text, TextStyle? cellStyleOverride, TextStyle defaultStyle, string backgroundColor, int colSpan = 1, int rowSpan = 1)
    {
        var style = cellStyleOverride ?? defaultStyle;
        sb.Append('<').Append(tag);
        if (colSpan > 1) sb.Append(" colspan=\"").Append(colSpan).Append('"');
        if (rowSpan > 1) sb.Append(" rowspan=\"").Append(rowSpan).Append('"');
        sb.Append(" dir=\"").Append(CssFormat.Direction(style.Direction)).Append('"');
        sb.Append(" style=\"background-color:").Append(CssFormat.Attribute(backgroundColor))
          .Append(";color:").Append(CssFormat.Attribute(style.Color))
          .Append(";font-family:").Append(CssFormat.Attribute(style.FontFamily))
          .Append(";font-size:").Append(CssFormat.Px(style.FontSizePx))
          .Append(";font-weight:").Append(CssFormat.FontWeightCss(style.FontWeight))
          .Append(";font-style:").Append(CssFormat.FontStyleCss(style.FontStyle))
          .Append(";line-height:").Append(CssFormat.Number(style.LineHeightMultiplier))
          .Append(";text-align:").Append(CssFormat.TextAlign(style.Alignment))
          .Append(";direction:").Append(CssFormat.Direction(style.Direction))
          .Append(";padding:").Append(CssFormat.Px(Style.CellPaddingPx))
          .Append(";border:").Append(CssFormat.Px(Style.BorderWidthPx)).Append(" solid ").Append(CssFormat.Attribute(Style.BorderColor))
          .Append(";white-space:pre-wrap;\">").Append(CssFormat.Encode(text)).Append("</").Append(tag).Append('>');
    }
}
