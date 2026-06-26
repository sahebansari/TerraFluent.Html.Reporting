using FluentHtmlReport.Measurement;
using FluentHtmlReport.Model;
using FluentHtmlReport.Model.Sections;

namespace FluentHtmlReport.Fluent;

/// <summary>
/// The fluent entry point for assembling a <see cref="ReportDocument"/>. Obtain
/// one via <see cref="ReportDocument.Create"/>, configure it, then call
/// <see cref="Build"/> to produce the immutable document.
/// </summary>
public sealed class ReportDocumentBuilder
{
    private readonly PageSize _pageSize;
    private readonly PageOrientation _orientation;
    private readonly ContentBuilder _content = new();
    private Margins _margins = Margins.All(40);
    private PageSectionBuilder? _header;
    private PageSectionBuilder? _footer;
    private ITextMeasurer _textMeasurer = ApproximateTextMeasurer.Instance;

    internal ReportDocumentBuilder(PageSize pageSize, PageOrientation orientation)
    {
        _pageSize = pageSize;
        _orientation = orientation;
    }

    /// <summary>Sets independent margins for each edge, in pixels.</summary>
    public ReportDocumentBuilder SetMargins(double topPx, double rightPx, double bottomPx, double leftPx)
    {
        _margins = new Margins(topPx, rightPx, bottomPx, leftPx);
        return this;
    }

    /// <summary>Sets the same margin on all four edges, in pixels.</summary>
    public ReportDocumentBuilder SetMargins(double allEdgesPx)
    {
        _margins = Margins.All(allEdgesPx);
        return this;
    }

    /// <summary>
    /// Configures the repeating header. Calling this more than once appends to
    /// the same header rather than replacing it, matching <see cref="Content"/>'s
    /// behavior.
    /// </summary>
    public ReportDocumentBuilder Header(Action<PageSectionBuilder> configure)
    {
        _header ??= new PageSectionBuilder();
        configure(_header);
        return this;
    }

    /// <summary>
    /// Configures the repeating footer. Calling this more than once appends to
    /// the same footer rather than replacing it, matching <see cref="Content"/>'s
    /// behavior.
    /// </summary>
    public ReportDocumentBuilder Footer(Action<PageSectionBuilder> configure)
    {
        _footer ??= new PageSectionBuilder();
        configure(_footer);
        return this;
    }

    /// <summary>Configures the top-level content elements.</summary>
    public ReportDocumentBuilder Content(Action<ContentBuilder> configure)
    {
        configure(_content);
        return this;
    }

    /// <summary>
    /// Overrides the default <see cref="ApproximateTextMeasurer"/> with a
    /// different <see cref="ITextMeasurer"/>, e.g. a precise, headless-renderer-backed
    /// implementation supplied by a separate measurement package.
    /// </summary>
    public ReportDocumentBuilder UseTextMeasurer(ITextMeasurer measurer)
    {
        _textMeasurer = measurer ?? throw new ArgumentNullException(nameof(measurer));
        return this;
    }

    /// <summary>Produces the immutable <see cref="ReportDocument"/>.</summary>
    public ReportDocument Build()
    {
        var header = _header is null ? null : new PageSection(PageSectionKind.Header, _header.Elements.ToList());
        var footer = _footer is null ? null : new PageSection(PageSectionKind.Footer, _footer.Elements.ToList());
        return new ReportDocument(_pageSize, _orientation, _margins, header, footer, _content.Elements.ToList(), _textMeasurer);
    }
}
