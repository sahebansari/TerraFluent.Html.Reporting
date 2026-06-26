using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Fluent;

/// <summary>Builds the top-level content element list; see <see cref="ReportDocumentBuilder.Content"/>.</summary>
public sealed class ContentBuilder
{
    private readonly List<IReportElement> _elements = new();

    internal IReadOnlyList<IReportElement> Elements => _elements;

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

    private ImageElementBuilder AddImageCore(ReportImage image)
    {
        var index = _elements.Count;
        _elements.Add(image);
        return new ImageElementBuilder(image, e => _elements[index] = e);
    }

    /// <summary>Adds an image from a base64 string (optionally a full <c>data:</c> URI).</summary>
    public ContentBuilder AddImageFromBase64(string base64OrDataUri, string mimeType = "image/png", double? widthPx = null, double? heightPx = null)
    {
        _elements.Add(ReportImage.FromBase64(base64OrDataUri, mimeType, widthPx, heightPx));
        return this;
    }

    /// <summary>Adds a table, configured via <paramref name="configure"/>.</summary>
    public ContentBuilder AddTable(Action<TableBuilder> configure, TableStyle? style = null)
    {
        var builder = new TableBuilder();
        configure(builder);
        _elements.Add(builder.Build(style));
        return this;
    }

    /// <summary>Adds a bulleted or numbered list.</summary>
    public ContentBuilder AddList(ListStyle style, IEnumerable<string> items, TextStyle? textStyle = null)
    {
        _elements.Add(new ReportList(style, items.ToList(), textStyle));
        return this;
    }

    /// <summary>Adds a horizontal divider line.</summary>
    public ContentBuilder AddRule(double thicknessPx = 1, string color = "#d8dde0")
    {
        _elements.Add(new HorizontalRule { ThicknessPx = thicknessPx, Color = color });
        return this;
    }

    /// <summary>
    /// Adds a row of side-by-side columns, configured via <paramref name="configure"/>.
    /// Like an image, a row never splits across pages - one too tall for the
    /// remaining space moves whole to the next page.
    /// </summary>
    public RowHandle AddRow(Action<RowBuilder> configure, double columnGapPx = 12, RowVerticalAlignment verticalAlignment = RowVerticalAlignment.Middle)
    {
        var builder = new RowBuilder();
        configure(builder);
        var row = builder.Build(columnGapPx, verticalAlignment);
        var index = _elements.Count;
        _elements.Add(row);
        return new RowHandle(row, e => _elements[index] = e);
    }

    /// <summary>Forces the next element to start on a fresh page.</summary>
    public ContentBuilder AddPageBreak()
    {
        _elements.Add(new PageBreak());
        return this;
    }

    /// <summary>Adds a fixed-height blank gap.</summary>
    public ContentBuilder AddSpacer(double heightPx)
    {
        _elements.Add(new Spacer(heightPx));
        return this;
    }

    /// <summary>Adds raw HTML with a caller-supplied height (the engine cannot measure opaque markup).</summary>
    public ContentBuilder AddRawHtml(string html, double heightPx)
    {
        _elements.Add(new RawHtml(html, heightPx));
        return this;
    }
}
