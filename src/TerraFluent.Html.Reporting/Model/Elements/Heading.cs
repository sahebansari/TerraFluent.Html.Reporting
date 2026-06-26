using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Model.Styling;
using TerraFluent.Html.Reporting.Rendering;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>A semantic heading (H1-H6). Unlike <see cref="Paragraph"/>, headings never split - one that doesn't fit moves whole to the next page.</summary>
public sealed class Heading : IReportElement
{
    /// <summary>The heading's text content.</summary>
    public string Text { get; }

    /// <summary>The heading level, used for its default style scale and for the rendered HTML tag.</summary>
    public HeadingLevel Level { get; }

    /// <summary>The style the text is measured and rendered with; defaults to the scale for <see cref="Level"/>.</summary>
    public TextStyle Style { get; }

    /// <summary>Creates a heading.</summary>
    public Heading(string text, HeadingLevel level, TextStyle? style = null)
    {
        Text = text ?? string.Empty;
        Level = level;
        Style = style ?? TextStyle.ForHeading(level);
    }

    /// <inheritdoc />
    public ElementMeasurement Measure(LayoutContext context)
    {
        var wrapWidthPx = Math.Max(1, context.ContentWidthPx - Style.HorizontalInsetPx);
        var measured = context.TextMeasurer.Measure(Text, Style.ToFontSpecification(), wrapWidthPx);
        return new ElementMeasurement(measured.TotalHeightPx + Style.VerticalInsetPx);
    }

    /// <inheritdoc />
    public SplitResult Split(double availableHeightPx, LayoutContext context) => SplitResult.Unsplittable(this);

    /// <inheritdoc />
    public string RenderHtml(ElementPlacement placement, RenderContext context)
    {
        var tag = "h" + (int)Level;
        var left = placement.XPx + Style.MarginLeftPx;
        var top = placement.YPx + Style.MarginTopPx;
        var width = Math.Max(0, placement.WidthPx - Style.MarginLeftPx - Style.MarginRightPx);
        var height = Math.Max(0, placement.HeightPx - Style.MarginTopPx - Style.MarginBottomPx);

        return "<" + tag + " style=\"position:absolute;margin:0;box-sizing:border-box;" +
            "left:" + CssFormat.Px(left) + ";top:" + CssFormat.Px(top) +
            ";width:" + CssFormat.Px(width) + ";height:" + CssFormat.Px(height) +
            ";padding:" + CssFormat.Box(Style.PaddingTopPx, Style.PaddingRightPx, Style.PaddingBottomPx, Style.PaddingLeftPx) +
            ";font-family:" + Style.FontFamily + ";font-size:" + CssFormat.Px(Style.FontSizePx) +
            ";font-weight:" + CssFormat.FontWeightCss(Style.FontWeight) + ";font-style:" + CssFormat.FontStyleCss(Style.FontStyle) +
            ";color:" + Style.Color + ";line-height:" + CssFormat.Number(Style.LineHeightMultiplier) +
            ";text-align:" + CssFormat.TextAlign(Style.Alignment) + ";white-space:pre-wrap;\">" +
            CssFormat.Encode(Text) + "</" + tag + ">";
    }
}
