using TerraFluent.Html.Reporting.Compatibility;

namespace TerraFluent.Html.Reporting.Layout;

/// <summary>
/// The result of measuring an <c>IReportElement</c>: the height it would
/// occupy if placed in full, at the width given by the <see cref="LayoutContext"/>
/// it was measured with.
/// </summary>
public readonly struct ElementMeasurement
{
    /// <summary>The element's natural height in pixels, assuming it is not split.</summary>
    public double HeightPx { get; }

    /// <summary>Creates an element measurement.</summary>
    public ElementMeasurement(double heightPx)
    {
        HeightPx = Guard.NonNegative(heightPx, nameof(heightPx));
    }
}
