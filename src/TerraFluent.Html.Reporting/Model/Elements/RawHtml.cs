using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Rendering;
using TerraFluent.Html.Reporting.Compatibility;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>
/// An escape hatch for power users to inject arbitrary HTML. Since the engine
/// cannot measure the height of opaque markup, the caller must state the
/// height it will occupy; pagination treats it like an opaque image.
/// </summary>
public sealed class RawHtml : IReportElement
{
    /// <summary>The raw HTML markup to emit verbatim.</summary>
    public string Html { get; }

    /// <summary>The caller-supplied height this markup is expected to occupy, in pixels.</summary>
    public double HeightPx { get; }

    /// <summary>Creates a raw HTML element with a caller-supplied height.</summary>
    public RawHtml(string html, double heightPx)
    {
        Html = html ?? throw new ArgumentNullException(nameof(html));
        HeightPx = Guard.NonNegative(heightPx, nameof(heightPx));
    }

    /// <inheritdoc />
    public ElementMeasurement Measure(LayoutContext context) => new(HeightPx);

    /// <inheritdoc />
    public SplitResult Split(double availableHeightPx, LayoutContext context) => SplitResult.Unsplittable(this);

    /// <inheritdoc />
    public string RenderHtml(ElementPlacement placement, RenderContext context) =>
        "<div style=\"position:absolute;overflow:hidden;" +
        "left:" + CssFormat.Px(placement.XPx) + ";top:" + CssFormat.Px(placement.YPx) +
        ";width:" + CssFormat.Px(placement.WidthPx) + ";height:" + CssFormat.Px(placement.HeightPx) + ";\">" +
        Html + "</div>";
}
