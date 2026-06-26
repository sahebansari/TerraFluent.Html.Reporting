using TerraFluent.Html.Reporting.Layout;

namespace TerraFluent.Html.Reporting.Model.Sections;

/// <summary>
/// A header or footer: a fixed-height block of elements repeated on every page.
/// The height is not configured directly - it is the sum of its elements'
/// measured heights, computed once (with the document's text measurer) before
/// pagination begins, then held constant across all pages.
/// </summary>
public interface IPageSection
{
    /// <summary>Whether this is the header or the footer.</summary>
    PageSectionKind Kind { get; }

    /// <summary>The elements stacked top-to-bottom within the section, in order.</summary>
    IReadOnlyList<IReportElement> Elements { get; }

    /// <summary>
    /// Sums the measured height of every element in <see cref="Elements"/> at the
    /// width given by <paramref name="context"/>. Header/footer elements are
    /// never split across pages, so this is a plain sum rather than a pagination pass.
    /// </summary>
    double MeasureHeight(LayoutContext context);
}
