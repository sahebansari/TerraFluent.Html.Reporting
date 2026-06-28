using System.Text;
using TerraFluent.Html.Reporting.Compatibility;
using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Rendering;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>
/// A row of side-by-side <see cref="RowColumn"/>s - e.g. a logo next to a
/// company name - laid out horizontally within the content width. Unlike
/// <see cref="Table"/>, a row never splits across pages: if it does not fit
/// even on an empty page, it is force-placed and recorded as a
/// <see cref="LayoutWarning"/>, the same way an oversized <see cref="ReportImage"/> is.
/// </summary>
public sealed class Row : IReportElement
{
    private double _marginTopPx;
    private double _marginRightPx;
    private double _marginBottomPx = 8;
    private double _marginLeftPx;

    /// <summary>The row's columns, in left-to-right order.</summary>
    public IReadOnlyList<RowColumn> Columns { get; }

    /// <summary>Horizontal gap between adjacent columns, in pixels.</summary>
    public double ColumnGapPx { get; }

    /// <summary>How each column's content is positioned vertically when shorter than the row's tallest column.</summary>
    public RowVerticalAlignment VerticalAlignment { get; }

    /// <summary>Space above the row, in pixels.</summary>
    public double MarginTopPx
    {
        get => _marginTopPx;
        init => _marginTopPx = Guard.NonNegative(value, nameof(MarginTopPx));
    }

    /// <summary>Space to the right of the row, in pixels - shrinks the columns' available width from the right edge of the container.</summary>
    public double MarginRightPx
    {
        get => _marginRightPx;
        init => _marginRightPx = Guard.NonNegative(value, nameof(MarginRightPx));
    }

    /// <summary>Space below the row, in pixels.</summary>
    public double MarginBottomPx
    {
        get => _marginBottomPx;
        init => _marginBottomPx = Guard.NonNegative(value, nameof(MarginBottomPx));
    }

    /// <summary>Space to the left of the row, in pixels - shrinks the columns' available width from the left edge of the container and shifts them right.</summary>
    public double MarginLeftPx
    {
        get => _marginLeftPx;
        init => _marginLeftPx = Guard.NonNegative(value, nameof(MarginLeftPx));
    }

    // Populated by Measure and reused by RenderHtml, which has no LayoutContext
    // (and so cannot measure text itself) - safe because the layout engine
    // always measures an element before rendering it, the same precondition
    // Table's _cachedRowHeights relies on. Keyed by content width since a
    // header/footer row is re-measured once per page (see
    // LayoutEngine.BuildSectionPlacements) at what is always the same width.
    private double[]? _cachedColumnHeights;
    private double[][]? _cachedElementHeights;
    private double _cachedForContentWidthPx = double.NaN;

    /// <summary>Creates a row.</summary>
    public Row(IReadOnlyList<RowColumn> columns, double columnGapPx = 12, RowVerticalAlignment verticalAlignment = RowVerticalAlignment.Middle)
    {
        Columns = Guard.Snapshot(columns, nameof(columns));
        if (Columns.Count == 0) throw new ArgumentException("A row must have at least one column.", nameof(columns));
        ColumnGapPx = Guard.NonNegative(columnGapPx, nameof(columnGapPx));
        VerticalAlignment = verticalAlignment;
    }

    /// <summary>
    /// Returns a copy of this row with the given margin overridden, leaving its
    /// columns, column gap, and vertical alignment unchanged. The copy starts
    /// with no measurement cache of its own.
    /// </summary>
    public Row With(
        double? marginTopPx = null,
        double? marginRightPx = null,
        double? marginBottomPx = null,
        double? marginLeftPx = null) => new Row(Columns, ColumnGapPx, VerticalAlignment)
    {
        MarginTopPx = marginTopPx ?? MarginTopPx,
        MarginRightPx = marginRightPx ?? MarginRightPx,
        MarginBottomPx = marginBottomPx ?? MarginBottomPx,
        MarginLeftPx = marginLeftPx ?? MarginLeftPx,
    };

    private double[] ResolveColumnWidths(double availableWidthPx)
    {
        var totalGapPx = ColumnGapPx * (Columns.Count - 1);
        var available = Math.Max(0, availableWidthPx - totalGapPx);

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
        var autoWidth = autoCount > 0 ? Math.Max(0, available - explicitTotal) / autoCount : 0;

        var widths = new double[Columns.Count];
        for (var i = 0; i < Columns.Count; i++)
        {
            widths[i] = Columns[i].WidthPx ?? autoWidth;
        }

        return widths;
    }

    private void EnsureMeasured(LayoutContext context)
    {
        if (_cachedColumnHeights is not null && _cachedForContentWidthPx.Equals(context.ContentWidthPx)) return;

        var availableWidthPx = Math.Max(0, context.ContentWidthPx - MarginLeftPx - MarginRightPx);
        var widths = ResolveColumnWidths(availableWidthPx);
        var columnHeights = new double[Columns.Count];
        var elementHeights = new double[Columns.Count][];

        for (var i = 0; i < Columns.Count; i++)
        {
            var column = Columns[i];
            var innerWidthPx = Math.Max(1, widths[i] - column.PaddingLeftPx - column.PaddingRightPx);
            var childContext = context.WithContentWidth(innerWidthPx);
            var heights = new double[column.Elements.Count];
            var total = 0.0;
            for (var j = 0; j < column.Elements.Count; j++)
            {
                var h = column.Elements[j].Measure(childContext).HeightPx;
                heights[j] = h;
                total += h;
            }

            elementHeights[i] = heights;
            columnHeights[i] = total + column.PaddingTopPx + column.PaddingBottomPx;
        }

        _cachedColumnHeights = columnHeights;
        _cachedElementHeights = elementHeights;
        _cachedForContentWidthPx = context.ContentWidthPx;
    }

    /// <inheritdoc />
    public ElementMeasurement Measure(LayoutContext context)
    {
        EnsureMeasured(context);

        var maxHeight = 0.0;
        foreach (var h in _cachedColumnHeights!)
        {
            if (h > maxHeight) maxHeight = h;
        }

        return new ElementMeasurement(maxHeight + MarginTopPx + MarginBottomPx);
    }

    /// <inheritdoc />
    public SplitResult Split(double availableHeightPx, LayoutContext context) => SplitResult.Unsplittable(this);

    /// <inheritdoc />
    public string RenderHtml(ElementPlacement placement, RenderContext context)
    {
        var columnHeights = _cachedColumnHeights ?? throw new InvalidOperationException("Row.Measure must be called before RenderHtml.");
        var elementHeights = _cachedElementHeights!;
        var availableWidthPx = Math.Max(0, placement.WidthPx - MarginLeftPx - MarginRightPx);
        var widths = ResolveColumnWidths(availableWidthPx);

        var rowContentHeightPx = 0.0;
        foreach (var h in columnHeights)
        {
            if (h > rowContentHeightPx) rowContentHeightPx = h;
        }

        var originX = placement.XPx + MarginLeftPx;
        var originY = placement.YPx + MarginTopPx;

        var sb = new StringBuilder();
        var x = 0.0;
        for (var i = 0; i < Columns.Count; i++)
        {
            var column = Columns[i];
            var topOffset = VerticalAlignment switch
            {
                RowVerticalAlignment.Middle => (rowContentHeightPx - columnHeights[i]) / 2,
                RowVerticalAlignment.Bottom => rowContentHeightPx - columnHeights[i],
                _ => 0,
            };

            var innerWidthPx = Math.Max(0, widths[i] - column.PaddingLeftPx - column.PaddingRightPx);
            var y = topOffset + column.PaddingTopPx;
            for (var j = 0; j < column.Elements.Count; j++)
            {
                var elementHeightPx = elementHeights[i][j];
                var childPlacement = new ElementPlacement(originX + x + column.PaddingLeftPx, originY + y, innerWidthPx, elementHeightPx, placement.PageIndex, placement.Section);
                sb.Append(column.Elements[j].RenderHtml(childPlacement, context));
                y += elementHeightPx;
            }

            x += widths[i] + ColumnGapPx;
        }

        return sb.ToString();
    }
}
