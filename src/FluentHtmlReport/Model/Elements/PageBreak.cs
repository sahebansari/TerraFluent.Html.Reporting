using FluentHtmlReport.Layout;
using FluentHtmlReport.Rendering;

namespace FluentHtmlReport.Model.Elements;

/// <summary>
/// A structural marker that forces the next content element to start on a
/// fresh page. Unlike every other element, this one is not measured/placed by
/// the general fit-or-split algorithm - <c>LayoutEngine</c> special-cases it
/// directly: a page break is a no-op if the current page is already empty
/// (so a leading page break does not produce a blank first page), otherwise
/// it closes the current page immediately. It has no visual output.
/// </summary>
public sealed class PageBreak : IReportElement
{
    /// <inheritdoc />
    public ElementMeasurement Measure(LayoutContext context) => new(0);

    /// <inheritdoc />
    public SplitResult Split(double availableHeightPx, LayoutContext context) => SplitResult.Unsplittable(this);

    /// <inheritdoc />
    public string RenderHtml(ElementPlacement placement, RenderContext context) => string.Empty;
}
