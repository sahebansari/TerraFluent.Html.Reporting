using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Rendering;
using TerraFluent.Html.Reporting.Compatibility;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>An invisible fixed-height gap, useful for manual spacing between elements.</summary>
public sealed class Spacer : IReportElement
{
    /// <summary>The gap's height in pixels.</summary>
    public double HeightPx { get; }

    /// <summary>Creates a spacer of the given height.</summary>
    public Spacer(double heightPx)
    {
        HeightPx = Guard.NonNegative(heightPx, nameof(heightPx));
    }

    /// <inheritdoc />
    public ElementMeasurement Measure(LayoutContext context) => new(HeightPx);

    /// <inheritdoc />
    public SplitResult Split(double availableHeightPx, LayoutContext context) => SplitResult.Unsplittable(this);

    /// <inheritdoc />
    public string RenderHtml(ElementPlacement placement, RenderContext context) => string.Empty;
}
