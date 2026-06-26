using FluentHtmlReport.Layout;
using FluentHtmlReport.Rendering;

namespace FluentHtmlReport.Model;

/// <summary>
/// The contract every content, header, and footer element implements. The
/// layout engine drives an element purely through <see cref="Measure"/> and
/// <see cref="Split"/> - it never inspects an element's concrete type - so new
/// element kinds can be added without touching the engine.
/// </summary>
/// <remarks>
/// Typical engine usage for one element while filling a page:
/// <list type="number">
/// <item>Call <see cref="Measure"/>. If the result fits in the remaining content height, place it whole and move on.</item>
/// <item>Otherwise call <see cref="Split"/> with the remaining height. If it returns a non-null <c>Head</c>, place that on the current page.</item>
/// <item>If <c>Tail</c> is non-null, start a new page and repeat measurement against it (a tail may itself need further splitting, e.g. a table spanning three or more pages).</item>
/// </list>
/// </remarks>
public interface IReportElement
{
    /// <summary>
    /// Computes the height this element would occupy if placed in full, at the
    /// width given by <paramref name="context"/>. Must be a pure function of the
    /// element's own content/style and the context - the layout engine may call
    /// this more than once for the same element.
    /// </summary>
    ElementMeasurement Measure(LayoutContext context);

    /// <summary>
    /// Called only when <see cref="Measure"/> reported more height than is left
    /// on the current page. Attempts to break the element so the first part fits
    /// within <paramref name="availableHeightPx"/>; see <see cref="SplitResult"/>
    /// for what the head/tail mean. Elements that cannot be partially placed
    /// (images, rules, spacers) should always return <see cref="SplitResult.Unsplittable"/>.
    /// </summary>
    SplitResult Split(double availableHeightPx, LayoutContext context);

    /// <summary>
    /// Renders this element (or the head/tail fragment produced by <see cref="Split"/>)
    /// as an HTML string, positioned per <paramref name="placement"/>.
    /// </summary>
    string RenderHtml(ElementPlacement placement, RenderContext context);
}
