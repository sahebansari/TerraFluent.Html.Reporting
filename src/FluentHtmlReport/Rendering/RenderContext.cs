namespace FluentHtmlReport.Rendering;

/// <summary>
/// Ambient information available while rendering a single placed element.
/// <see cref="TotalPages"/> is only known after the full document has been
/// paginated, so the renderer always runs as a second pass, once per page,
/// after layout completes - this is what lets header/footer elements resolve
/// "Page {page} of {totalPages}" tokens correctly.
/// </summary>
public sealed class RenderContext
{
    /// <summary>The 1-based number of the page currently being rendered.</summary>
    public int PageNumber { get; }

    /// <summary>The total number of pages in the document.</summary>
    public int TotalPages { get; }

    /// <summary>Creates a render context for one page.</summary>
    public RenderContext(int pageNumber, int totalPages)
    {
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        if (totalPages < pageNumber) throw new ArgumentOutOfRangeException(nameof(totalPages));
        PageNumber = pageNumber;
        TotalPages = totalPages;
    }
}
