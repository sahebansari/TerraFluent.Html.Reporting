using TerraFluent.Html.Reporting.Measurement;
using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Sections;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Fluent;

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
    private string? _title;
    private bool _strictLayoutValidation;
    private readonly List<EmbeddedFont> _embeddedFonts = new();

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

    /// <summary>
    /// Sets the document's title, rendered as the generated HTML's
    /// <c>&lt;title&gt;</c> (HTML-encoded) and available to custom
    /// <c>IHtmlReportRenderer</c> implementations via <see cref="Layout.LayoutResult.Title"/>.
    /// Falls back to <c>"Report"</c> when never called.
    /// </summary>
    public ReportDocumentBuilder Title(string title)
    {
        _title = title ?? throw new ArgumentNullException(nameof(title));
        return this;
    }

    /// <summary>
    /// When <paramref name="strict"/> is true, a table/row whose auto-width
    /// column collapses to 0px throws an <see cref="InvalidOperationException"/>
    /// instead of only recording a <see cref="Layout.LayoutWarning"/> - see
    /// <see cref="ReportDocument.StrictLayoutValidation"/>. Off by default: an
    /// individual table/row can still opt in via its own style's
    /// <see cref="ColumnWidthOverflowMode"/> regardless of this document-level
    /// setting.
    /// </summary>
    public ReportDocumentBuilder UseStrictLayoutValidation(bool strict = true)
    {
        _strictLayoutValidation = strict;
        return this;
    }

    /// <summary>
    /// Embeds a font face directly into the generated HTML as a base64 data
    /// URI <c>@font-face</c> rule, from in-memory bytes. Reference
    /// <paramref name="fontFamily"/> from a <see cref="TextStyle.FontFamily"/>
    /// to use it - the generated report no longer depends on the font being
    /// installed wherever the HTML is opened or printed.
    /// </summary>
    public ReportDocumentBuilder EmbedFont(
        string fontFamily,
        byte[] fontBytes,
        FontWeight weight = FontWeight.Normal,
        FontStyle style = FontStyle.Normal,
        string mimeType = "font/woff2")
    {
        _embeddedFonts.Add(new EmbeddedFont(fontFamily, fontBytes, weight, style, mimeType));
        return this;
    }

    /// <summary>Embeds a font face loaded from a local file path - see <see cref="EmbedFont(string, byte[], FontWeight, FontStyle, string)"/>.</summary>
    public ReportDocumentBuilder EmbedFont(
        string fontFamily,
        string filePath,
        FontWeight weight = FontWeight.Normal,
        FontStyle style = FontStyle.Normal,
        string? mimeType = null) =>
        EmbedFont(fontFamily, File.ReadAllBytes(filePath), weight, style, mimeType ?? MimeTypeFromExtension(filePath));

    private static string MimeTypeFromExtension(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".woff2" => "font/woff2",
        ".woff" => "font/woff",
        ".ttf" => "font/ttf",
        ".otf" => "font/otf",
        _ => "font/woff2",
    };

    /// <summary>Produces the immutable <see cref="ReportDocument"/>.</summary>
    public ReportDocument Build()
    {
        var header = _header is null ? null : new PageSection(PageSectionKind.Header, _header.Elements.ToList());
        var footer = _footer is null ? null : new PageSection(PageSectionKind.Footer, _footer.Elements.ToList());
        return new ReportDocument(_pageSize, _orientation, _margins, header, footer, _content.Elements.ToList(), _textMeasurer, _title, _strictLayoutValidation, _embeddedFonts);
    }
}
