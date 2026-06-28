using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Compatibility;

namespace TerraFluent.Html.Reporting.Layout;

/// <summary>
/// The resolved position and size of an element (or a head/tail fragment of one,
/// see <see cref="SplitResult"/>) on a specific page, as computed by the
/// <c>LayoutEngine</c>. Renderers use this to emit absolutely-positioned CSS.
/// </summary>
public sealed class ElementPlacement
{
    /// <summary>Distance from the left edge of the page's content area, in pixels.</summary>
    public double XPx { get; }

    /// <summary>Distance from the top edge of the section (header/content/footer) it belongs to, in pixels.</summary>
    public double YPx { get; }

    /// <summary>The width allotted to the element, in pixels.</summary>
    public double WidthPx { get; }

    /// <summary>The height occupied by this placed fragment, in pixels.</summary>
    public double HeightPx { get; }

    /// <summary>The zero-based index of the page this fragment is placed on.</summary>
    public int PageIndex { get; }

    /// <summary>Which page section this fragment belongs to.</summary>
    public PageSectionKind Section { get; }

    /// <summary>Creates an element placement.</summary>
    public ElementPlacement(double xPx, double yPx, double widthPx, double heightPx, int pageIndex, PageSectionKind section)
    {
        XPx = Guard.Finite(xPx, nameof(xPx));
        YPx = Guard.Finite(yPx, nameof(yPx));
        WidthPx = Guard.NonNegative(widthPx, nameof(widthPx));
        HeightPx = Guard.NonNegative(heightPx, nameof(heightPx));
        if (pageIndex < 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        PageIndex = pageIndex;
        Section = section;
    }

    /// <summary>
    /// Returns a copy shifted by <paramref name="dx"/>/<paramref name="dy"/>. Used
    /// by the renderer to turn a section-relative placement (X relative to the
    /// content area, Y relative to the header/content/footer box it belongs to)
    /// into page-absolute coordinates before emitting CSS.
    /// </summary>
    public ElementPlacement Translate(double dx, double dy) => new(XPx + dx, YPx + dy, WidthPx, HeightPx, PageIndex, Section);
}
