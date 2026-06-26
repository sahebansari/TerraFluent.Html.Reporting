using FluentHtmlReport.Model;

namespace FluentHtmlReport.Layout;

/// <summary>
/// The output of <see cref="LayoutEngine.Paginate"/>: every page the document
/// was broken into. <see cref="Pages"/>.Count is the "totalPages" value the
/// renderer's second pass uses to resolve page-number tokens.
/// </summary>
public sealed class LayoutResult
{
    /// <summary>The page geometry for the document, in order.</summary>
    public IReadOnlyList<PageLayout> Pages { get; }

    /// <summary>The page size pagination was performed against (already orientation-adjusted).</summary>
    public PageSize PageSize { get; }

    /// <summary>The margins pagination was performed against.</summary>
    public Margins Margins { get; }

    /// <summary>
    /// Non-fatal problems noticed during pagination - see <see cref="LayoutWarning"/>.
    /// Empty for the common case where everything fit.
    /// </summary>
    public IReadOnlyList<LayoutWarning> Warnings { get; }

    /// <summary>Creates a layout result.</summary>
    public LayoutResult(IReadOnlyList<PageLayout> pages, PageSize pageSize, Margins margins, IReadOnlyList<LayoutWarning>? warnings = null)
    {
        Pages = pages ?? throw new ArgumentNullException(nameof(pages));
        PageSize = pageSize;
        Margins = margins;
        Warnings = warnings ?? Array.Empty<LayoutWarning>();
    }
}
