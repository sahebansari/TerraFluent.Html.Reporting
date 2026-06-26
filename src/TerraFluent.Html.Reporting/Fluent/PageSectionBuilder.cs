using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Fluent;

/// <summary>Builds the element list for a header or footer; see <see cref="ReportDocumentBuilder.Header"/>/<see cref="ReportDocumentBuilder.Footer"/>.</summary>
public sealed class PageSectionBuilder
{
    private readonly List<IReportElement> _elements = new();

    internal IReadOnlyList<IReportElement> Elements => _elements;

    /// <summary>Adds a line of text.</summary>
    public TextElementBuilder AddText(string text, TextStyle? style = null)
    {
        var initial = style ?? TextStyle.Default.With(marginBottomPx: 0);
        var index = _elements.Count;
        _elements.Add(new Paragraph(text, initial));
        return new TextElementBuilder(initial, s => new Paragraph(text, s), e => _elements[index] = e);
    }

    /// <summary>
    /// Adds page-number text containing the <c>{page}</c>/<c>{totalPages}</c>
    /// tokens, e.g. <c>"Page {page} of {totalPages}"</c>.
    /// </summary>
    public TextElementBuilder AddPageNumber(string format = "Page {page} of {totalPages}", TextStyle? style = null)
    {
        var initial = style ?? TextStyle.Default.With(marginBottomPx: 0);
        var index = _elements.Count;
        _elements.Add(new PageNumberText(format, initial));
        return new TextElementBuilder(initial, s => new PageNumberText(format, s), e => _elements[index] = e);
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

    /// <summary>Adds a horizontal divider line.</summary>
    public PageSectionBuilder AddRule(double thicknessPx = 1, string color = "#d8dde0")
    {
        _elements.Add(new HorizontalRule { ThicknessPx = thicknessPx, Color = color });
        return this;
    }

    /// <summary>
    /// Adds a row of side-by-side columns - e.g. a logo next to the company
    /// name - configured via <paramref name="configure"/>.
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
}
