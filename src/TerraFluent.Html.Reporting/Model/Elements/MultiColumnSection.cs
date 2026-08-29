using System.Text;
using TerraFluent.Html.Reporting.Compatibility;
using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Rendering;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>
/// An opt-in, conservative multi-column content block: a flat list of
/// elements packed left-to-right, top-to-bottom-then-wrap into a fixed
/// number of equal-width columns - see <c>ContentBuilder.AddColumns</c>.
/// Coexists with, and never changes, the engine's default single-column page
/// flow: this is just another <see cref="IReportElement"/>, measured/split
/// like anything else.
/// </summary>
/// <remarks>
/// <para>
/// <b>v1 scope, deliberately narrow:</b> columns are always equal width (the
/// same "share what's left after gaps equally" convention <see cref="Table"/>/
/// <see cref="Row"/> already use for auto columns); columns fill strictly
/// top-to-bottom then wrap to the next column (no balanced/equal-height
/// rebalancing pass); no element can span more than one column; a
/// <see cref="PageBreak"/> is not supported inside a column's content (its
/// <see cref="Measure"/> reports zero height and is otherwise inert here -
/// the engine only gives it real page-break behavior at the top level); and
/// a multi-column section cannot contain another one (nesting throws at
/// construction).
/// </para>
/// <para>
/// <b>How packing/pagination works:</b> a child that itself supports
/// splitting (e.g. <see cref="Paragraph"/>, <see cref="Table"/>) may split
/// across a column boundary exactly as it already splits across a page
/// boundary - from the child's point of view, "the space I was offered" is
/// the same concept either way. A child that cannot split and does not fit
/// even at the top of an empty column is force-placed there and recorded as
/// a <see cref="LayoutWarning"/>, mirroring <see cref="LayoutEngine"/>'s own
/// empty-page guarantee at column granularity instead of page granularity.
/// If content still remains after all columns are filled, it becomes this
/// section's own <see cref="Split"/> tail, re-wrapped starting at column 0
/// of a fresh section instance on the next page.
/// </para>
/// </remarks>
public sealed class MultiColumnSection : IReportElement
{
    private readonly record struct PendingDiagnostic(LayoutWarningReason Reason, string Message);

    private readonly record struct PackResult(
        IReadOnlyList<IReportElement>[] ColumnAssignment,
        double[][] ChildHeights,
        double[] ColumnHeights,
        IReadOnlyList<IReportElement> Leftover,
        IReadOnlyList<PendingDiagnostic> PendingDiagnostics);

    private const double Epsilon = 0.01;

    /// <summary>The section's elements, in the order they're packed (flat - not pre-bucketed into columns).</summary>
    public IReadOnlyList<IReportElement> Elements { get; }

    /// <summary>The number of equal-width columns content is packed into.</summary>
    public int ColumnCount { get; }

    /// <summary>Horizontal gap between adjacent columns, in pixels.</summary>
    public double ColumnGapPx { get; }

    // Populated by Measure (a fresh, not-yet-split instance packs everything
    // into column 0 at an effectively unbounded height budget) or supplied
    // directly by Split when building a head fragment (which already ran a
    // real, height-bounded packing pass) - see BuildPrecomputed. RenderHtml
    // requires one of these to already be populated, the same precondition
    // Table/Row's own render-time caches rely on.
    private IReadOnlyList<IReportElement>[]? _cachedColumnAssignment;
    private double[][]? _cachedChildHeights;
    private double[]? _cachedColumnHeights;
    private double _cachedTotalHeight = double.NaN;
    private double _cachedForContentWidthPx = double.NaN;
    private readonly IReadOnlyList<PendingDiagnostic> _pendingDiagnostics;
    private readonly bool _isPrecomputed;

    /// <summary>Creates a multi-column section.</summary>
    /// <exception cref="ArgumentException"><paramref name="columnCount"/> is less than 2, or <paramref name="elements"/> contains another <see cref="MultiColumnSection"/> (nesting is not supported).</exception>
    public MultiColumnSection(IReadOnlyList<IReportElement> elements, int columnCount, double columnGapPx = 12)
        : this(elements, columnCount, columnGapPx, precomputed: null, pendingDiagnostics: Array.Empty<PendingDiagnostic>())
    {
    }

    private MultiColumnSection(
        IReadOnlyList<IReportElement> elements,
        int columnCount,
        double columnGapPx,
        (IReadOnlyList<IReportElement>[] ColumnAssignment, double[][] ChildHeights, double[] ColumnHeights, double ContentWidthPx)? precomputed,
        IReadOnlyList<PendingDiagnostic> pendingDiagnostics)
    {
        Elements = Guard.Snapshot(elements, nameof(elements));
        foreach (var element in Elements)
        {
            if (element is MultiColumnSection)
            {
                throw new ArgumentException("A multi-column section cannot contain another multi-column section.", nameof(elements));
            }
        }

        if (columnCount < 2)
        {
            throw new ArgumentException("A multi-column section must have at least 2 columns.", nameof(columnCount));
        }

        ColumnCount = columnCount;
        ColumnGapPx = Guard.NonNegative(columnGapPx, nameof(columnGapPx));
        _pendingDiagnostics = pendingDiagnostics;

        if (precomputed is { } p)
        {
            _cachedColumnAssignment = p.ColumnAssignment;
            _cachedChildHeights = p.ChildHeights;
            _cachedColumnHeights = p.ColumnHeights;
            _cachedTotalHeight = p.ColumnHeights.Length == 0 ? 0 : p.ColumnHeights.Max();
            _cachedForContentWidthPx = p.ContentWidthPx;
            _isPrecomputed = true;
        }
    }

    private double ColumnWidthPx(double contentWidthPx) =>
        Math.Max(1, (contentWidthPx - ColumnGapPx * (ColumnCount - 1)) / ColumnCount);

    /// <inheritdoc />
    public ElementMeasurement Measure(LayoutContext context)
    {
        if (_isPrecomputed)
        {
            ReportPendingDiagnostics(context);
            return new ElementMeasurement(_cachedTotalHeight);
        }

        if (_cachedColumnHeights is not null && _cachedForContentWidthPx.Equals(context.ContentWidthPx))
        {
            return new ElementMeasurement(_cachedTotalHeight);
        }

        var childContext = context.WithContentWidth(ColumnWidthPx(context.ContentWidthPx));
        var result = Pack(Elements, FindNaturalColumnHeight(Elements, childContext), childContext);

        _cachedColumnAssignment = result.ColumnAssignment;
        _cachedChildHeights = result.ChildHeights;
        _cachedColumnHeights = result.ColumnHeights;
        _cachedTotalHeight = result.ColumnHeights.Length == 0 ? 0 : result.ColumnHeights.Max();
        _cachedForContentWidthPx = context.ContentWidthPx;

        return new ElementMeasurement(_cachedTotalHeight);
    }

    /// <inheritdoc />
    public SplitResult Split(double availableHeightPx, LayoutContext context)
    {
        if (Elements.Count == 0)
        {
            return SplitResult.Unsplittable(this);
        }

        var childContext = context.WithContentWidth(ColumnWidthPx(context.ContentWidthPx));
        var result = Pack(Elements, availableHeightPx, childContext);

        var headElements = result.ColumnAssignment.SelectMany(c => c).ToList();
        if (headElements.Count == 0)
        {
            return SplitResult.Unsplittable(this);
        }

        var head = new MultiColumnSection(
            headElements,
            ColumnCount,
            ColumnGapPx,
            (result.ColumnAssignment, result.ChildHeights, result.ColumnHeights, context.ContentWidthPx),
            result.PendingDiagnostics);

        if (result.Leftover.Count == 0)
        {
            return SplitResult.Partial(head, null);
        }

        var tail = new MultiColumnSection(result.Leftover, ColumnCount, ColumnGapPx);
        return SplitResult.Partial(head, tail);
    }

    /// <summary>
    /// Finds the minimum column-height budget at which <see cref="Pack"/>
    /// places every one of <paramref name="children"/> across
    /// <see cref="ColumnCount"/> columns with nothing left over and no
    /// element forced into an empty column that's still too small for it.
    /// This is what makes a section that easily fits on the current page
    /// still visually spread across all its columns (side-by-side, the
    /// entire point of a multi-column layout) instead of collapsing
    /// everything into column 0 the moment there's "enough" vertical room -
    /// there's no way to know the right per-column height up front, so this
    /// binary-searches for it: packing at height H can only ever place
    /// equal-or-more content as H grows, so "does everything fit cleanly at
    /// height H" is a monotonic threshold, and packing at the sum of every
    /// child's own height is always a safe upper bound (trivially fits in a
    /// single column, even though that wastes every other column).
    /// </summary>
    private double FindNaturalColumnHeight(IReadOnlyList<IReportElement> children, LayoutContext childContext)
    {
        if (children.Count == 0) return 0;

        var low = 0.0;
        var high = 0.0;
        foreach (var child in children)
        {
            high += child.Measure(childContext).HeightPx;
        }

        if (high <= Epsilon) return 0;

        for (var i = 0; i < 50 && high - low >= 0.5; i++)
        {
            var mid = (low + high) / 2;
            var result = Pack(children, mid, childContext);

            if (result.Leftover.Count == 0 && result.PendingDiagnostics.Count == 0)
            {
                high = mid;
            }
            else
            {
                low = mid;
            }
        }

        return high;
    }

    /// <summary>
    /// Packs <paramref name="children"/> into <see cref="ColumnCount"/> columns,
    /// each up to <paramref name="columnHeightBudget"/> tall, using the same
    /// fit/split/defer decision <see cref="LayoutEngine.Paginate"/> makes per
    /// page - just capped at <see cref="ColumnCount"/> columns instead of an
    /// unlimited number of pages, with any children left over past the last
    /// column returned as <see cref="PackResult.Leftover"/>.
    /// </summary>
    private PackResult Pack(IReadOnlyList<IReportElement> children, double columnHeightBudget, LayoutContext childContext)
    {
        var columnAssignment = new List<IReportElement>[ColumnCount];
        var childHeights = new List<double>[ColumnCount];
        var columnHeights = new double[ColumnCount];
        for (var i = 0; i < ColumnCount; i++)
        {
            columnAssignment[i] = new List<IReportElement>();
            childHeights[i] = new List<double>();
        }

        var pendingDiagnostics = new List<PendingDiagnostic>();
        var pending = new LinkedList<IReportElement>(children);
        var columnIndex = 0;
        var columnUsedHeight = 0.0;

        void FinishColumnAndAdvance()
        {
            columnHeights[columnIndex] = columnUsedHeight;
            columnIndex++;
            columnUsedHeight = 0;
        }

        while (pending.Count > 0 && columnIndex < ColumnCount)
        {
            var element = pending.First!.Value;
            pending.RemoveFirst();

            var measurement = element.Measure(childContext);
            var remaining = columnHeightBudget - columnUsedHeight;

            if (measurement.HeightPx <= remaining + Epsilon)
            {
                columnAssignment[columnIndex].Add(element);
                childHeights[columnIndex].Add(measurement.HeightPx);
                columnUsedHeight += measurement.HeightPx;
                continue;
            }

            if (remaining > Epsilon)
            {
                var split = element.Split(remaining, childContext);
                if (split.Head is not null)
                {
                    var headHeight = split.Head.Measure(childContext).HeightPx;
                    columnAssignment[columnIndex].Add(split.Head);
                    childHeights[columnIndex].Add(headHeight);
                    columnUsedHeight += headHeight;

                    if (split.Tail is not null)
                    {
                        pending.AddFirst(split.Tail);
                    }

                    FinishColumnAndAdvance();
                    continue;
                }
            }

            if (columnUsedHeight <= Epsilon)
            {
                // Even an empty column can't fit or split this element - force it through so we make progress (mirrors LayoutEngine's own empty-page guarantee).
                pendingDiagnostics.Add(new PendingDiagnostic(
                    LayoutWarningReason.Overflow,
                    $"A {element.GetType().Name} required {measurement.HeightPx:0.#}px but only {columnHeightBudget:0.#}px was available in an empty column of this multi-column section; it was placed anyway and will overflow visually."));
                columnAssignment[columnIndex].Add(element);
                childHeights[columnIndex].Add(measurement.HeightPx);
                columnUsedHeight += measurement.HeightPx;
                FinishColumnAndAdvance();
                continue;
            }

            FinishColumnAndAdvance();
            pending.AddFirst(element);
        }

        if (columnIndex < ColumnCount)
        {
            columnHeights[columnIndex] = columnUsedHeight;
        }

        return new PackResult(
            Array.ConvertAll(columnAssignment, c => (IReadOnlyList<IReportElement>)c),
            Array.ConvertAll(childHeights, c => c.ToArray()),
            columnHeights,
            pending.ToList(),
            pendingDiagnostics);
    }

    private void ReportPendingDiagnostics(LayoutContext context)
    {
        if (_pendingDiagnostics.Count == 0 || context.Diagnostics is not { } diagnostics) return;

        foreach (var diagnostic in _pendingDiagnostics)
        {
            diagnostics.Report(diagnostic.Reason, diagnostic.Message, nameof(MultiColumnSection), context.StrictMode);
        }
    }

    /// <inheritdoc />
    public string RenderHtml(ElementPlacement placement, RenderContext context)
    {
        if (_cachedColumnAssignment is null || _cachedChildHeights is null)
        {
            throw new InvalidOperationException("MultiColumnSection.Measure must be called before RenderHtml.");
        }

        var columnWidthPx = ColumnWidthPx(placement.WidthPx);
        var sb = new StringBuilder();
        var x = 0.0;

        for (var col = 0; col < ColumnCount; col++)
        {
            var columnChildren = _cachedColumnAssignment[col];
            var heights = _cachedChildHeights[col];
            var y = 0.0;

            for (var i = 0; i < columnChildren.Count; i++)
            {
                var childPlacement = new ElementPlacement(placement.XPx + x, placement.YPx + y, columnWidthPx, heights[i], placement.PageIndex, placement.Section);
                sb.Append(columnChildren[i].RenderHtml(childPlacement, context));
                y += heights[i];
            }

            x += columnWidthPx + ColumnGapPx;
        }

        return sb.ToString();
    }
}
