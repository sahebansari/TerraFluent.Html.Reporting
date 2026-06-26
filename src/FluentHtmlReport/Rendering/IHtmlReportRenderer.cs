using System.Threading;
using FluentHtmlReport.Layout;

namespace FluentHtmlReport.Rendering;

/// <summary>
/// Turns a paginated <see cref="LayoutResult"/> into HTML. This is the second
/// pass referred to throughout the layout engine's docs: by the time a
/// renderer runs, every page's geometry - and therefore the total page count -
/// is already known, which is what lets header/footer elements resolve
/// "Page {page} of {totalPages}" tokens correctly.
/// </summary>
public interface IHtmlReportRenderer
{
    /// <summary>
    /// Renders the full, self-contained HTML document: an <c>&lt;html&gt;</c>
    /// wrapper with an inline <c>&lt;style&gt;</c> block and one absolutely
    /// positioned page <c>&lt;div&gt;</c> per entry in <paramref name="layout"/>.
    /// </summary>
    string RenderDocument(LayoutResult layout);

    /// <summary>
    /// Renders just the page <c>&lt;div&gt;</c> elements (plus the styles they
    /// depend on, scoped so they do not leak), without the surrounding
    /// <c>&lt;html&gt;</c>/<c>&lt;head&gt;</c> wrapper, for embedding in an
    /// existing page.
    /// </summary>
    string RenderFragment(LayoutResult layout);

    /// <summary>
    /// Writes the same output as <see cref="RenderDocument"/> directly to
    /// <paramref name="writer"/>, one page at a time, instead of building the
    /// whole document as a single in-memory string first. For a very large
    /// report (many thousands of pages) this bounds peak memory to roughly one
    /// page's HTML rather than the entire document's.
    /// </summary>
    void RenderDocumentTo(TextWriter writer, LayoutResult layout, CancellationToken cancellationToken = default);

    /// <summary>Streaming equivalent of <see cref="RenderFragment"/>; see <see cref="RenderDocumentTo"/>.</summary>
    void RenderFragmentTo(TextWriter writer, LayoutResult layout, CancellationToken cancellationToken = default);
}
