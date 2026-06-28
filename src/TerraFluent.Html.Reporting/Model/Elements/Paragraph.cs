using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Model.Styling;
using TerraFluent.Html.Reporting.Rendering;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>A block of body text that word-wraps to the content width and may split across pages at a line boundary.</summary>
public sealed class Paragraph : IReportElement
{
    /// <summary>The paragraph's text content.</summary>
    public string Text { get; }

    /// <summary>The style the text is measured and rendered with.</summary>
    public TextStyle Style { get; }

    /// <summary>Creates a paragraph.</summary>
    public Paragraph(string text, TextStyle? style = null)
    {
        Text = text ?? string.Empty;
        Style = style ?? TextStyle.Default;
    }

    /// <inheritdoc />
    public ElementMeasurement Measure(LayoutContext context)
    {
        var wrapWidthPx = Math.Max(1, context.ContentWidthPx - Style.HorizontalInsetPx);
        var measured = context.TextMeasurer.Measure(Text, Style.ToFontSpecification(), wrapWidthPx);
        return new ElementMeasurement(measured.TotalHeightPx + Style.VerticalInsetPx);
    }

    /// <summary>
    /// The minimum number of lines that must stay together at either side of a
    /// split, so a paragraph never strands a single orphan line at the bottom
    /// of a page or a single widow line alone at the top of the next one.
    /// </summary>
    private const int MinLinesKeptTogether = 2;

    /// <inheritdoc />
    public SplitResult Split(double availableHeightPx, LayoutContext context)
    {
        var wrapWidthPx = Math.Max(1, context.ContentWidthPx - Style.HorizontalInsetPx);
        var measured = context.TextMeasurer.Measure(Text, Style.ToFontSpecification(), wrapWidthPx);

        // Margin/padding above and below the text itself must be carved out of
        // the available height before any lines can be budgeted.
        var availableForLinesPx = availableHeightPx - Style.MarginTopPx - Style.PaddingTopPx - Style.PaddingBottomPx - Style.MarginBottomPx;
        var lineBudget = (int)Math.Floor(availableForLinesPx / measured.LineHeightPx);

        if (lineBudget > 0 && lineBudget < measured.Lines.Count)
        {
            if (lineBudget == 1 && measured.Lines.Count > MinLinesKeptTogether)
            {
                // A lone orphan line would be left at the bottom of this page;
                // defer the whole paragraph to the next page instead.
                lineBudget = 0;
            }
            else if (measured.Lines.Count - lineBudget == 1 && lineBudget > MinLinesKeptTogether)
            {
                // A lone widow line would be left at the top of the next page;
                // carry one more line over so at least two move together.
                lineBudget -= 1;
            }
        }

        if (lineBudget <= 0 || lineBudget >= measured.Lines.Count)
        {
            return SplitResult.Unsplittable(this);
        }

        // The head fragment is not the paragraph's true end, so it must not carry
        // the trailing margin/padding; symmetrically the tail is not the true
        // start, so it must not repeat the leading margin/padding either - only
        // an unsplit paragraph (or the head/tail at the actual edge) keeps both.
        // Preserve the measurer's line boundaries as explicit line breaks.
        // Joining with spaces loses caller-supplied newlines and can also make
        // a fragment re-wrap differently when LayoutEngine measures it again.
        var head = new Paragraph(string.Join("\n", measured.Lines.Take(lineBudget)), Style.With(marginBottomPx: 0, paddingBottomPx: 0));
        var tail = new Paragraph(string.Join("\n", measured.Lines.Skip(lineBudget)), Style.With(marginTopPx: 0, paddingTopPx: 0));
        return SplitResult.Partial(head, tail);
    }

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
            ";font-family:" + CssFormat.Attribute(Style.FontFamily) + ";font-size:" + CssFormat.Px(Style.FontSizePx) +
            ";font-weight:" + CssFormat.FontWeightCss(Style.FontWeight) + ";font-style:" + CssFormat.FontStyleCss(Style.FontStyle) +
            ";color:" + CssFormat.Attribute(Style.Color) + ";line-height:" + CssFormat.Number(Style.LineHeightMultiplier) +
            ";text-align:" + CssFormat.TextAlign(Style.Alignment) + ";white-space:pre-wrap;\">" +
            CssFormat.Encode(Text) + "</p>";
    }
}
