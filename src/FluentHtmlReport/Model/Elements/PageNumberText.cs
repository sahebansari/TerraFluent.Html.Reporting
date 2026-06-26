using FluentHtmlReport.Layout;
using FluentHtmlReport.Model.Styling;
using FluentHtmlReport.Rendering;

namespace FluentHtmlReport.Model.Elements;

/// <summary>
/// Header/footer text containing the <c>{page}</c> / <c>{totalPages}</c> tokens.
/// These can only be resolved once the whole document has been paginated, so
/// this element is rendered in the renderer's per-page second pass rather than
/// during layout - <see cref="Measure"/> substitutes a representative value so
/// pagination is unaffected by the actual page count.
/// </summary>
public sealed class PageNumberText : IReportElement
{
    /// <summary>The format string, e.g. "Page {page} of {totalPages}".</summary>
    public string FormatTemplate { get; }

    /// <summary>The style the text is measured and rendered with.</summary>
    public TextStyle Style { get; }

    /// <summary>Creates a page-number element.</summary>
    public PageNumberText(string formatTemplate, TextStyle? style = null)
    {
        FormatTemplate = formatTemplate ?? throw new ArgumentNullException(nameof(formatTemplate));
        Style = style ?? TextStyle.Default;
    }

    /// <inheritdoc />
    public ElementMeasurement Measure(LayoutContext context)
    {
        var sample = Resolve(FormatTemplate, 1, 1);
        var wrapWidthPx = Math.Max(1, context.ContentWidthPx - Style.HorizontalInsetPx);
        var measured = context.TextMeasurer.Measure(sample, Style.ToFontSpecification(), wrapWidthPx);
        return new ElementMeasurement(measured.TotalHeightPx + Style.VerticalInsetPx);
    }

    /// <inheritdoc />
    public SplitResult Split(double availableHeightPx, LayoutContext context) => SplitResult.Unsplittable(this);

    /// <inheritdoc />
    public string RenderHtml(ElementPlacement placement, RenderContext context)
    {
        var left = placement.XPx + Style.MarginLeftPx;
        var top = placement.YPx + Style.MarginTopPx;
        var width = Math.Max(0, placement.WidthPx - Style.MarginLeftPx - Style.MarginRightPx);
        var height = Math.Max(0, placement.HeightPx - Style.MarginTopPx - Style.MarginBottomPx);

        return "<p style=\"position:absolute;margin:0;box-sizing:border-box;" +
            "left:" + CssFormat.Px(left) + ";top:" + CssFormat.Px(top) +
            ";width:" + CssFormat.Px(width) + ";height:" + CssFormat.Px(height) +
            ";padding:" + CssFormat.Box(Style.PaddingTopPx, Style.PaddingRightPx, Style.PaddingBottomPx, Style.PaddingLeftPx) +
            ";font-family:" + Style.FontFamily + ";font-size:" + CssFormat.Px(Style.FontSizePx) +
            ";font-weight:" + CssFormat.FontWeightCss(Style.FontWeight) + ";font-style:" + CssFormat.FontStyleCss(Style.FontStyle) +
            ";color:" + Style.Color + ";line-height:" + CssFormat.Number(Style.LineHeightMultiplier) +
            ";text-align:" + CssFormat.TextAlign(Style.Alignment) + ";white-space:pre-wrap;\">" +
            CssFormat.Encode(Resolve(FormatTemplate, context.PageNumber, context.TotalPages)) + "</p>";
    }

    /// <summary>Substitutes <c>{page}</c> and <c>{totalPages}</c> in <paramref name="template"/>.</summary>
    public static string Resolve(string template, int page, int totalPages) =>
        template.Replace("{page}", page.ToString()).Replace("{totalPages}", totalPages.ToString());
}
