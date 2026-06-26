using System.Threading;
using System.Threading.Tasks;
using FluentHtmlReport.Fluent;
using FluentHtmlReport.Layout;
using FluentHtmlReport.Measurement;
using FluentHtmlReport.Model.Sections;
using FluentHtmlReport.Rendering;

namespace FluentHtmlReport.Model;

/// <summary>
/// The root of a built report: page geometry, an optional header/footer, and the
/// top-level content elements. Instances are immutable and are only constructed
/// via <see cref="Create"/> followed by <see cref="ReportDocumentBuilder.Build"/>.
/// </summary>
public sealed class ReportDocument
{
    /// <summary>The page size, already adjusted for <see cref="Orientation"/>.</summary>
    public PageSize PageSize { get; }

    /// <summary>The page orientation.</summary>
    public PageOrientation Orientation { get; }

    /// <summary>The page margins.</summary>
    public Margins Margins { get; }

    /// <summary>The repeating header section, or <see langword="null"/> if none was configured.</summary>
    public IPageSection? Header { get; }

    /// <summary>The repeating footer section, or <see langword="null"/> if none was configured.</summary>
    public IPageSection? Footer { get; }

    /// <summary>The top-level content elements, in document order, before pagination.</summary>
    public IReadOnlyList<IReportElement> ContentElements { get; }

    /// <summary>The text measurer used for layout; defaults to <c>ApproximateTextMeasurer</c> unless overridden on the builder.</summary>
    public ITextMeasurer TextMeasurer { get; }

    /// <summary>Constructs a document. Intended to be called only by <see cref="ReportDocumentBuilder.Build"/>.</summary>
    internal ReportDocument(
        PageSize pageSize,
        PageOrientation orientation,
        Margins margins,
        IPageSection? header,
        IPageSection? footer,
        IReadOnlyList<IReportElement> contentElements,
        ITextMeasurer textMeasurer)
    {
        PageSize = pageSize.WithOrientation(orientation);
        Orientation = orientation;
        Margins = margins;
        Header = header;
        Footer = footer;
        ContentElements = contentElements ?? throw new ArgumentNullException(nameof(contentElements));
        TextMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
    }

    /// <summary>
    /// Starts building a new report. This is the fluent entry point - despite
    /// living on <see cref="ReportDocument"/>, it returns a <see cref="ReportDocumentBuilder"/>;
    /// the immutable document itself is only produced by the builder's <c>Build()</c>.
    /// </summary>
    public static ReportDocumentBuilder Create(PageSize pageSize, PageOrientation orientation = PageOrientation.Portrait) =>
        new(pageSize, orientation);

    /// <summary>
    /// Lays out and renders the full document to a single, self-contained HTML
    /// string (inline <c>&lt;style&gt;</c>, no external CSS), ready to open in a
    /// browser or print to PDF.
    /// </summary>
    /// <param name="renderer">An alternate renderer, or <see langword="null"/> to use <see cref="HtmlReportRenderer.Default"/>.</param>
    /// <param name="cancellationToken">Checked periodically while paginating and rendering a large document.</param>
    public string RenderHtml(IHtmlReportRenderer? renderer = null, CancellationToken cancellationToken = default)
    {
        var layout = LayoutEngine.Paginate(this, cancellationToken);
        var writer = new System.IO.StringWriter();
        (renderer ?? HtmlReportRenderer.Default).RenderDocumentTo(writer, layout, cancellationToken);
        return writer.ToString();
    }

    /// <summary>
    /// Renders the document and writes it to <paramref name="filePath"/>,
    /// streaming page-by-page rather than building the entire HTML string in
    /// memory first - see <see cref="IHtmlReportRenderer.RenderDocumentTo"/>.
    /// </summary>
    public void RenderHtmlDocument(string filePath, IHtmlReportRenderer? renderer = null, CancellationToken cancellationToken = default)
    {
        var layout = LayoutEngine.Paginate(this, cancellationToken);
        using var writer = new System.IO.StreamWriter(filePath);
        (renderer ?? HtmlReportRenderer.Default).RenderDocumentTo(writer, layout, cancellationToken);
    }

    /// <summary>
    /// Asynchronous, streaming equivalent of <see cref="RenderHtmlDocument"/>.
    /// Pagination and HTML generation are CPU-bound and run synchronously, as
    /// there is no I/O to await until the final flush to disk - this overload
    /// exists so that flush, and the writer's disposal, do not block a thread
    /// pool thread in an async call chain (e.g. an ASP.NET request handler).
    /// </summary>
    public async Task RenderHtmlDocumentAsync(string filePath, IHtmlReportRenderer? renderer = null, CancellationToken cancellationToken = default)
    {
        var layout = LayoutEngine.Paginate(this, cancellationToken);

#if NETSTANDARD2_0
        using var writer = new System.IO.StreamWriter(filePath);
#else
        await using var writer = new System.IO.StreamWriter(filePath);
#endif
        (renderer ?? HtmlReportRenderer.Default).RenderDocumentTo(writer, layout, cancellationToken);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Renders the document's page <c>&lt;div&gt;</c> elements without the
    /// surrounding <c>&lt;html&gt;</c>/<c>&lt;head&gt;</c> wrapper, for embedding
    /// inside an existing page.
    /// </summary>
    public string RenderFragment(IHtmlReportRenderer? renderer = null, CancellationToken cancellationToken = default)
    {
        var layout = LayoutEngine.Paginate(this, cancellationToken);
        var writer = new System.IO.StringWriter();
        (renderer ?? HtmlReportRenderer.Default).RenderFragmentTo(writer, layout, cancellationToken);
        return writer.ToString();
    }
}
