using TerraFluent.Html.Reporting.Compatibility;

namespace TerraFluent.Html.Reporting.Layout;

/// <summary>One fully laid-out page: the placed header, content, and footer fragments it contains.</summary>
public sealed class PageLayout
{
    /// <summary>Zero-based page index.</summary>
    public int PageIndex { get; }

    /// <summary>Header elements, identical in geometry on every page (content may still vary, e.g. page-number tokens resolved at render time).</summary>
    public IReadOnlyList<PlacedElement> HeaderElements { get; }

    /// <summary>The content fragments placed on this page.</summary>
    public IReadOnlyList<PlacedElement> ContentElements { get; }

    /// <summary>Footer elements, identical in geometry on every page.</summary>
    public IReadOnlyList<PlacedElement> FooterElements { get; }

    /// <summary>Creates a page layout.</summary>
    public PageLayout(
        int pageIndex,
        IReadOnlyList<PlacedElement> headerElements,
        IReadOnlyList<PlacedElement> contentElements,
        IReadOnlyList<PlacedElement> footerElements)
    {
        if (pageIndex < 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        PageIndex = pageIndex;
        HeaderElements = Guard.Snapshot(headerElements, nameof(headerElements));
        ContentElements = Guard.Snapshot(contentElements, nameof(contentElements));
        FooterElements = Guard.Snapshot(footerElements, nameof(footerElements));
    }
}
