using System.Text;
using FluentHtmlReport.Layout;
using FluentHtmlReport.Measurement;
using FluentHtmlReport.Model.Styling;
using FluentHtmlReport.Rendering;

namespace FluentHtmlReport.Model.Elements;

/// <summary>
/// A table with a repeated header row. Row-break behavior is governed by
/// <see cref="Styling.TableStyle.RowSplitBehavior"/> on <see cref="Style"/>:
/// rows either move to the next page intact, or - for
/// <see cref="RowSplitBehavior.AllowSplitWithContinuedHeader"/> - a row that
/// only partly fits has its cells truncated at a shared line budget, with the
/// remainder continuing as the first row on the next page under a repeated,
/// "(continued)"-suffixed header.
/// </summary>
public sealed class Table : IReportElement
{
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

    // Row heights computed for a given content width, in Rows order. Populated
    // lazily by GetRowHeights and propagated forward (sliced, not recomputed)
    // when Split produces head/tail fragments - see GetRowHeights for why this
    // is what keeps pagination of a large table from being O(rows^2).
    private double[]? _cachedRowHeights;
    private double _cachedForContentWidthPx = double.NaN;

    /// <summary>Creates a table.</summary>
    /// <exception cref="ArgumentException">A row's cell count does not match <paramref name="columns"/>.Count.</exception>
    public Table(IReadOnlyList<TableColumn> columns, IReadOnlyList<TableRow> rows, TableStyle? style = null, bool isContinuation = false)
        : this(columns, rows, style, isContinuation, precomputedRowHeights: null, precomputedForContentWidthPx: double.NaN)
    {
    }

    private Table(
        IReadOnlyList<TableColumn> columns,
        IReadOnlyList<TableRow> rows,
        TableStyle? style,
        bool isContinuation,
        double[]? precomputedRowHeights,
        double precomputedForContentWidthPx)
    {
        Columns = columns ?? throw new ArgumentNullException(nameof(columns));
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
        Style = style ?? TableStyle.Default;
        IsContinuation = isContinuation;

        for (var i = 0; i < Rows.Count; i++)
        {
            if (Rows[i].Cells.Count != Columns.Count)
            {
                throw new ArgumentException(
                    $"Row {i} has {Rows[i].Cells.Count} cell(s) but the table has {Columns.Count} column(s). " +
                    "Every row must supply exactly one cell per column.",
                    nameof(rows));
            }
        }

        _cachedRowHeights = precomputedRowHeights;
        _cachedForContentWidthPx = precomputedForContentWidthPx;
    }

    private double[] ResolveColumnWidths(double contentWidthPx)
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

        var widths = new double[Columns.Count];
        for (var i = 0; i < Columns.Count; i++)
        {
            widths[i] = Columns[i].WidthPx ?? autoWidth;
        }

        return widths;
    }

    private TableRow HeaderRow() => new(Columns.Select(c => (TableCell)c.Header).ToList());

    /// <summary>
    /// A row's measured height includes one <see cref="Styling.TableStyle.BorderWidthPx"/>
    /// for its own border line - with <c>border-collapse: collapse</c>, that
    /// line is shared with the row below, so summing one per row (plus one
    /// extra for the table's outermost top edge, added once in <see cref="Measure"/>/
    /// <see cref="Split"/>) matches the real rendered height. Omitting this
    /// entirely previously under-measured the table, and since the container
    /// <c>RenderHtml</c> wraps it in is <c>overflow:hidden</c> at that
    /// (too-short) height, the last row's bottom border was silently clipped.
    /// </summary>
    private double MeasureRowHeight(TableRow row, double[] columnWidths, LayoutContext context, TextStyle defaultStyle)
    {
        var maxHeight = 0.0;
        for (var c = 0; c < row.Cells.Count; c++)
        {
            var cell = row.Cells[c];
            var style = cell.Style ?? defaultStyle;
            var width = Math.Max(1, columnWidths[c] - 2 * Style.CellPaddingPx);
            var measured = context.TextMeasurer.Measure(cell.Text, style.ToFontSpecification(), width);
            var height = measured.TotalHeightPx + 2 * Style.CellPaddingPx;
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
            heights[i] = MeasureRowHeight(Rows[i], columnWidths, context, Style.CellTextStyle);
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
        var widths = ResolveColumnWidths(context.ContentWidthPx);
        var rowHeights = GetRowHeights(widths, context);
        // +1 border width for the table's outermost top edge - every row
        // already counts one border line for its own bottom edge (see
        // MeasureRowHeight), so this is the one edge nothing else accounts for.
        var total = Style.BorderWidthPx + MeasureRowHeight(HeaderRow(), widths, context, Style.HeaderTextStyle) + ContinuationBannerHeight(widths, context);
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
        var headerHeight = Style.BorderWidthPx + MeasureRowHeight(HeaderRow(), widths, context, Style.HeaderTextStyle) + ContinuationBannerHeight(widths, context);
        if (headerHeight >= availableHeightPx)
        {
            return SplitResult.Unsplittable(this);
        }

        var used = headerHeight;
        var headRows = new List<TableRow>();
        var headHeights = new List<double>();
        var index = 0;

        for (; index < Rows.Count; index++)
        {
            var rowHeight = rowHeights[index];
            if (used + rowHeight <= availableHeightPx)
            {
                headRows.Add(Rows[index]);
                headHeights.Add(rowHeight);
                used += rowHeight;
                continue;
            }

            if (Style.RowSplitBehavior == RowSplitBehavior.AllowSplitWithContinuedHeader &&
                TrySplitRow(Rows[index], widths, context, availableHeightPx - used, out var rowHead, out var rowTail))
            {
                headRows.Add(rowHead);
                headHeights.Add(MeasureRowHeight(rowHead, widths, context, Style.CellTextStyle));

                var tailRows = new List<TableRow> { rowTail };
                var tailHeights = new List<double> { MeasureRowHeight(rowTail, widths, context, Style.CellTextStyle) };
                tailRows.AddRange(Rows.Skip(index + 1));
                for (var t = index + 1; t < rowHeights.Length; t++) tailHeights.Add(rowHeights[t]);

                return BuildSplitResult(headRows, headHeights, tailRows, tailHeights, context.ContentWidthPx);
            }

            break;
        }

        if (headRows.Count == 0)
        {
            return SplitResult.Unsplittable(this);
        }

        var remainingRows = Rows.Skip(index).ToList();
        var remainingHeights = new List<double>();
        for (var t = index; t < rowHeights.Length; t++) remainingHeights.Add(rowHeights[t]);

        return BuildSplitResult(headRows, headHeights, remainingRows, remainingHeights, context.ContentWidthPx);
    }

    private SplitResult BuildSplitResult(
        List<TableRow> headRows,
        List<double> headRowHeights,
        List<TableRow> tailRows,
        List<double> tailRowHeights,
        double contentWidthPx)
    {
        var head = new Table(Columns, headRows, Style, IsContinuation, headRowHeights.ToArray(), contentWidthPx);
        if (tailRows.Count == 0) return SplitResult.Partial(head, null);
        var tail = new Table(Columns, tailRows, Style, isContinuation: true, tailRowHeights.ToArray(), contentWidthPx);
        return SplitResult.Partial(head, tail);
    }

    /// <summary>
    /// Truncates every cell in <paramref name="row"/> to a shared line budget
    /// derived from <paramref name="remainingHeightPx"/>, so the row's visual
    /// split lines up across columns. Returns false if no line fits, or if no
    /// cell actually needed truncation (i.e. the row would not have overflowed).
    /// </summary>
    private bool TrySplitRow(TableRow row, double[] widths, LayoutContext context, double remainingHeightPx, out TableRow head, out TableRow tail)
    {
        head = null!;
        tail = null!;

        var usableForText = remainingHeightPx - 2 * Style.CellPaddingPx;
        if (usableForText <= 0) return false;

        var measurements = new TextMeasurement[row.Cells.Count];
        var minLineHeight = double.MaxValue;
        for (var c = 0; c < row.Cells.Count; c++)
        {
            var cell = row.Cells[c];
            var style = cell.Style ?? Style.CellTextStyle;
            var width = Math.Max(1, widths[c] - 2 * Style.CellPaddingPx);
            measurements[c] = context.TextMeasurer.Measure(cell.Text, style.ToFontSpecification(), width);
            if (measurements[c].LineHeightPx < minLineHeight) minLineHeight = measurements[c].LineHeightPx;
        }

        var lineBudget = (int)Math.Floor(usableForText / minLineHeight);
        if (lineBudget <= 0) return false;

        var headCells = new List<TableCell>();
        var tailCells = new List<TableCell>();
        var splitSomething = false;

        for (var c = 0; c < row.Cells.Count; c++)
        {
            var style = row.Cells[c].Style ?? Style.CellTextStyle;
            var lines = measurements[c].Lines;

            if (lineBudget >= lines.Count)
            {
                headCells.Add(row.Cells[c]);
                tailCells.Add(new TableCell(string.Empty, style));
                continue;
            }

            headCells.Add(new TableCell(string.Join(" ", lines.Take(lineBudget)), style));
            tailCells.Add(new TableCell(string.Join(" ", lines.Skip(lineBudget)), style));
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
              .Append(Style.HeaderBackgroundColor).Append(";color:").Append(Style.HeaderTextStyle.Color)
              .Append(";font-style:italic;padding:").Append(CssFormat.Px(Style.CellPaddingPx))
              .Append(";border:").Append(CssFormat.Px(Style.BorderWidthPx)).Append(" solid ").Append(Style.BorderColor)
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
                AppendCell(sb, "td", cell.Text, cell.Style, Style.CellTextStyle, rowBackgroundColor);
            }

            sb.Append("</tr>");
        }

        sb.Append("</tbody></table></div>");
        return sb.ToString();
    }

    private void AppendCell(StringBuilder sb, string tag, string text, TextStyle? cellStyleOverride, TextStyle defaultStyle, string backgroundColor)
    {
        var style = cellStyleOverride ?? defaultStyle;
        sb.Append('<').Append(tag).Append(" style=\"background-color:").Append(backgroundColor)
          .Append(";color:").Append(style.Color)
          .Append(";font-family:").Append(style.FontFamily)
          .Append(";font-size:").Append(CssFormat.Px(style.FontSizePx))
          .Append(";font-weight:").Append(CssFormat.FontWeightCss(style.FontWeight))
          .Append(";font-style:").Append(CssFormat.FontStyleCss(style.FontStyle))
          .Append(";line-height:").Append(CssFormat.Number(style.LineHeightMultiplier))
          .Append(";text-align:").Append(CssFormat.TextAlign(style.Alignment))
          .Append(";padding:").Append(CssFormat.Px(Style.CellPaddingPx))
          .Append(";border:").Append(CssFormat.Px(Style.BorderWidthPx)).Append(" solid ").Append(Style.BorderColor)
          .Append(";white-space:pre-wrap;\">").Append(CssFormat.Encode(text)).Append("</").Append(tag).Append('>');
    }
}
