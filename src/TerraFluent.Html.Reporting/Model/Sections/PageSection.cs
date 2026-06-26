using TerraFluent.Html.Reporting.Layout;

namespace TerraFluent.Html.Reporting.Model.Sections;

/// <summary>Default <see cref="IPageSection"/> implementation backing the header and footer builders.</summary>
public sealed class PageSection : IPageSection
{
    /// <inheritdoc />
    public PageSectionKind Kind { get; }

    /// <inheritdoc />
    public IReadOnlyList<IReportElement> Elements { get; }

    /// <summary>Creates a page section with a fixed element list.</summary>
    public PageSection(PageSectionKind kind, IReadOnlyList<IReportElement> elements)
    {
        Kind = kind;
        Elements = elements ?? throw new ArgumentNullException(nameof(elements));
    }

    /// <inheritdoc />
    public double MeasureHeight(LayoutContext context)
    {
        var total = 0.0;
        foreach (var element in Elements)
        {
            total += element.Measure(context).HeightPx;
        }

        return total;
    }
}
