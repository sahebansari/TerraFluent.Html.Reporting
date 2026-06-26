using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Rendering;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>A horizontal divider line spanning the content width.</summary>
public sealed class HorizontalRule : IReportElement
{
    /// <summary>Line thickness in pixels.</summary>
    public double ThicknessPx { get; init; } = 1;

    /// <summary>Line color as a CSS color string.</summary>
    public string Color { get; init; } = "#d8dde0";

    /// <summary>Space above the line, in pixels.</summary>
    public double MarginTopPx { get; init; } = 8;

    /// <summary>Space below the line, in pixels.</summary>
    public double MarginBottomPx { get; init; } = 8;

    /// <inheritdoc />
    public ElementMeasurement Measure(LayoutContext context) => new(MarginTopPx + ThicknessPx + MarginBottomPx);

    /// <inheritdoc />
    public SplitResult Split(double availableHeightPx, LayoutContext context) => SplitResult.Unsplittable(this);

    /// <inheritdoc />
    public string RenderHtml(ElementPlacement placement, RenderContext context) =>
        "<div style=\"position:absolute;" +
        "left:" + CssFormat.Px(placement.XPx) + ";top:" + CssFormat.Px(placement.YPx + MarginTopPx) +
        ";width:" + CssFormat.Px(placement.WidthPx) + ";height:" + CssFormat.Px(ThicknessPx) +
        ";background-color:" + Color + ";\"></div>";
}
