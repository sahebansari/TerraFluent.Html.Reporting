using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Fluent;

/// <summary>
/// Builds the flat element list for a <see cref="MultiColumnSection"/>; see
/// <see cref="ContentBuilder.AddColumns"/>. Mirrors <see cref="ContentBuilder"/>'s
/// method set except for <c>AddRow</c> (a row can't span multiple columns in v1)
/// and <c>AddPageBreak</c>/<c>AddColumns</c> (a page break and nested multi-column
/// sections aren't supported inside a column's content in v1).
/// </summary>
public sealed class ColumnsBuilder
{
    private readonly List<IReportElement> _elements = new();

    internal IReadOnlyList<IReportElement> Elements => _elements;

    /// <summary>
    /// Adds a custom report element. Use this with an <see cref="IReportElement"/>
    /// implementation when the built-in element methods do not cover the
    /// required content.
    /// </summary>
    public ColumnsBuilder AddElement(IReportElement element)
    {
        _elements.Add(element ?? throw new ArgumentNullException(nameof(element)));
        return this;
    }

    /// <summary>Adds a heading (H1-H6).</summary>
    public TextElementBuilder AddHeading(string text, HeadingLevel level, TextStyle? style = null)
    {
        var initial = style ?? TextStyle.ForHeading(level);
        var index = _elements.Count;
        _elements.Add(new Heading(text, level, initial));
        return new TextElementBuilder(initial, s => new Heading(text, level, s), e => _elements[index] = e);
    }

    /// <summary>Adds a paragraph of body text.</summary>
    public TextElementBuilder AddParagraph(string text, TextStyle? style = null)
    {
        var initial = style ?? TextStyle.Default;
        var index = _elements.Count;
        _elements.Add(new Paragraph(text, initial));
        return new TextElementBuilder(initial, s => new Paragraph(text, s), e => _elements[index] = e);
    }

    /// <summary>Adds an image loaded from a local file path.</summary>
    public ImageElementBuilder AddImage(string filePath, double? widthPx = null, double? heightPx = null) =>
        AddImageCore(ReportImage.FromFile(filePath, widthPx, heightPx));

    /// <summary>Adds an image from an in-memory byte array.</summary>
    public ImageElementBuilder AddImage(byte[] imageBytes, string mimeType = "image/png", double? widthPx = null, double? heightPx = null) =>
        AddImageCore(ReportImage.FromBytes(imageBytes, mimeType, widthPx, heightPx));

    /// <summary>Adds a Code 128 barcode as an image.</summary>
    public ImageElementBuilder AddBarcode(string value, double moduleWidthPx = 2, double heightPx = 60, int quietZoneModules = 10) =>
        AddImageCore(BarcodeImage.CreateCode128(value, moduleWidthPx, heightPx, quietZoneModules));

    /// <summary>Adds a QR code as an image. See <see cref="QrCodeImage"/> for encoding scope/limitations.</summary>
    public ImageElementBuilder AddQrCode(string value, double moduleWidthPx = 4, int quietZoneModules = 4) =>
        AddImageCore(QrCodeImage.CreateQrCode(value, moduleWidthPx, quietZoneModules));

    private ImageElementBuilder AddImageCore(ReportImage image)
    {
        var index = _elements.Count;
        _elements.Add(image);
        return new ImageElementBuilder(image, e => _elements[index] = e);
    }

    /// <summary>Adds an image from a base64 string (optionally a full <c>data:</c> URI).</summary>
    public ColumnsBuilder AddImageFromBase64(string base64OrDataUri, string mimeType = "image/png", double? widthPx = null, double? heightPx = null)
    {
        _elements.Add(ReportImage.FromBase64(base64OrDataUri, mimeType, widthPx, heightPx));
        return this;
    }

    /// <summary>Adds a table, configured via <paramref name="configure"/>.</summary>
    public ColumnsBuilder AddTable(Action<TableBuilder> configure, TableStyle? style = null)
    {
        var builder = new TableBuilder();
        configure(builder);
        _elements.Add(builder.Build(style));
        return this;
    }

    /// <summary>Adds a bulleted or numbered list.</summary>
    public ColumnsBuilder AddList(ListStyle style, IEnumerable<string> items, TextStyle? textStyle = null)
    {
        _elements.Add(new ReportList(style, items.ToList(), textStyle));
        return this;
    }

    /// <summary>Adds a horizontal divider line.</summary>
    public ColumnsBuilder AddRule(double thicknessPx = 1, string color = "#d8dde0")
    {
        _elements.Add(new HorizontalRule { ThicknessPx = thicknessPx, Color = color });
        return this;
    }

    /// <summary>Adds a fixed-height blank gap.</summary>
    public ColumnsBuilder AddSpacer(double heightPx)
    {
        _elements.Add(new Spacer(heightPx));
        return this;
    }

    /// <summary>Adds raw HTML with a caller-supplied height (the engine cannot measure opaque markup).</summary>
    public ColumnsBuilder AddRawHtml(string html, double heightPx)
    {
        _elements.Add(new RawHtml(html, heightPx));
        return this;
    }
}
