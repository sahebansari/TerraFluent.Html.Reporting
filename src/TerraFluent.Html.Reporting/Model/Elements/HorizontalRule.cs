using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Rendering;
using TerraFluent.Html.Reporting.Compatibility;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>A horizontal divider line spanning the content width.</summary>
public sealed class HorizontalRule : IReportElement
{
    private double _thicknessPx = 1;
    private double _marginTopPx = 8;
    private double _marginBottomPx = 8;

    /// <summary>Line thickness in pixels.</summary>
    public double ThicknessPx
    {
        get => _thicknessPx;
        init => _thicknessPx = Guard.NonNegative(value, nameof(ThicknessPx));
    }

    /// <summary>Line color as a CSS color string.</summary>
    public string Color { get; init; } = "#d8dde0";

    /// <summary>Space above the line, in pixels.</summary>
    public double MarginTopPx
    {
        get => _marginTopPx;
        init => _marginTopPx = Guard.NonNegative(value, nameof(MarginTopPx));
    }

    /// <summary>Space below the line, in pixels.</summary>
    public double MarginBottomPx
    {
        get => _marginBottomPx;
        init => _marginBottomPx = Guard.NonNegative(value, nameof(MarginBottomPx));
    }

    /// <inheritdoc />
    public ElementMeasurement Measure(LayoutContext context) => new(MarginTopPx + ThicknessPx + MarginBottomPx);

    /// <inheritdoc />
    public SplitResult Split(double availableHeightPx, LayoutContext context) => SplitResult.Unsplittable(this);

    /// <inheritdoc />
    public string RenderHtml(ElementPlacement placement, RenderContext context) =>
        "<div style=\"position:absolute;" +
        "left:" + CssFormat.Px(placement.XPx) + ";top:" + CssFormat.Px(placement.YPx + MarginTopPx) +
        ";width:" + CssFormat.Px(placement.WidthPx) + ";height:" + CssFormat.Px(ThicknessPx) +
        ";background-color:" + CssFormat.Attribute(Color) + ";\"></div>";
}
