using TerraFluent.Html.Reporting.Model;

namespace TerraFluent.Html.Reporting.Layout;

/// <summary>
/// An element (or the head/tail fragment <see cref="SplitResult"/> produced for
/// it) paired with where the layout engine decided to put it.
/// </summary>
public sealed class PlacedElement
{
    /// <summary>The element instance to render - may be a fragment produced by <c>IReportElement.Split</c>, not the original.</summary>
    public IReportElement Element { get; }

    /// <summary>Where and how large to render <see cref="Element"/>.</summary>
    public ElementPlacement Placement { get; }

    /// <summary>Creates a placed element.</summary>
    public PlacedElement(IReportElement element, ElementPlacement placement)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
        Placement = placement ?? throw new ArgumentNullException(nameof(placement));
    }
}
