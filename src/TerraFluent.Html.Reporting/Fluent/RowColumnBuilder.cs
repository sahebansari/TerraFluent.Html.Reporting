using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Fluent;

/// <summary>
/// Builds the element list for one column of a <see cref="Row"/>; see
/// <see cref="RowBuilder.AddColumn(Action{RowColumnBuilder})"/>. Text elements
/// default to no trailing margin (unlike their <see cref="ContentBuilder"/>
/// counterparts) so a column's measured height matches its visible content,
/// which matters for <see cref="RowVerticalAlignment"/> to line up correctly;
/// chain <c>.MarginBottom(...)</c> to add deliberate spacing between elements
/// stacked within the same column.
/// </summary>
public sealed class RowColumnBuilder
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

    /// <summary>Adds a heading (H1-H6).</summary>
    public TextElementBuilder AddHeading(string text, HeadingLevel level, TextStyle? style = null)
    {
        var initial = style ?? TextStyle.ForHeading(level).With(marginBottomPx: 0);
        var index = _elements.Count;
        _elements.Add(new Heading(text, level, initial));
        return new TextElementBuilder(initial, s => new Heading(text, level, s), e => _elements[index] = e);
    }

    /// <summary>
    /// Adds page-number text containing the <c>{page}</c>/<c>{totalPages}</c> tokens.
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
    public RowColumnBuilder AddRule(double thicknessPx = 1, string color = "#d8dde0")
    {
        _elements.Add(new HorizontalRule { ThicknessPx = thicknessPx, Color = color });
        return this;
    }

    /// <summary>Adds a fixed-height blank gap.</summary>
    public RowColumnBuilder AddSpacer(double heightPx)
    {
        _elements.Add(new Spacer(heightPx));
        return this;
    }
}
