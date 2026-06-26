using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Fluent;

/// <summary>
/// Returned by <c>AddText</c>/<c>AddParagraph</c>/<c>AddHeading</c>/<c>AddPageNumber</c>
/// to let the caller chain style modifiers directly after adding an element,
/// e.g. <c>h.AddText("Title").AlignCenter().Bold()</c>. Model elements are
/// immutable, so each modifier rebuilds the element with an updated
/// <see cref="TextStyle"/> and replaces it in the owning section/content list -
/// this builder itself is just a thin, short-lived handle onto that slot.
/// </summary>
public sealed class TextElementBuilder
{
    private readonly Func<TextStyle, IReportElement> _rebuild;
    private readonly Action<IReportElement> _replace;
    private TextStyle _style;

    internal TextElementBuilder(TextStyle initialStyle, Func<TextStyle, IReportElement> rebuild, Action<IReportElement> replace)
    {
        _style = initialStyle;
        _rebuild = rebuild;
        _replace = replace;
    }

    private TextElementBuilder Apply(Func<TextStyle, TextStyle> mutate)
    {
        _style = mutate(_style);
        _replace(_rebuild(_style));
        return this;
    }

    /// <summary>Left-aligns the text.</summary>
    public TextElementBuilder AlignLeft() => Apply(s => s.With(alignment: TextAlignment.Left));

    /// <summary>Centers the text.</summary>
    public TextElementBuilder AlignCenter() => Apply(s => s.With(alignment: TextAlignment.Center));

    /// <summary>Right-aligns the text.</summary>
    public TextElementBuilder AlignRight() => Apply(s => s.With(alignment: TextAlignment.Right));

    /// <summary>Justifies the text.</summary>
    public TextElementBuilder AlignJustify() => Apply(s => s.With(alignment: TextAlignment.Justify));

    /// <summary>Makes the text bold.</summary>
    public TextElementBuilder Bold() => Apply(s => s.With(fontWeight: FontWeight.Bold));

    /// <summary>Makes the text italic.</summary>
    public TextElementBuilder Italic() => Apply(s => s.With(fontStyle: FontStyle.Italic));

    /// <summary>Sets the font size, in pixels.</summary>
    public TextElementBuilder FontSize(double px) => Apply(s => s.With(fontSizePx: px));

    /// <summary>Sets the text color (any CSS color string).</summary>
    public TextElementBuilder FontColor(string cssColor) => Apply(s => s.With(color: cssColor));

    /// <summary>Sets the CSS font-family list.</summary>
    public TextElementBuilder FontFamily(string cssFontFamily) => Apply(s => s.With(fontFamily: cssFontFamily));

    /// <summary>Sets the space above the element, in pixels.</summary>
    public TextElementBuilder MarginTop(double px) => Apply(s => s.With(marginTopPx: px));

    /// <summary>Sets the space to the right of the element, in pixels.</summary>
    public TextElementBuilder MarginRight(double px) => Apply(s => s.With(marginRightPx: px));

    /// <summary>Sets the space below the element, in pixels.</summary>
    public TextElementBuilder MarginBottom(double px) => Apply(s => s.With(marginBottomPx: px));

    /// <summary>Sets the space to the left of the element, in pixels.</summary>
    public TextElementBuilder MarginLeft(double px) => Apply(s => s.With(marginLeftPx: px));

    /// <summary>Sets uniform margin on all four sides, in pixels.</summary>
    public TextElementBuilder Margin(double px) => Apply(s => s.With(marginTopPx: px, marginRightPx: px, marginBottomPx: px, marginLeftPx: px));

    /// <summary>Sets margin per side, in pixels (CSS shorthand order: top, right, bottom, left).</summary>
    public TextElementBuilder Margin(double topPx, double rightPx, double bottomPx, double leftPx) =>
        Apply(s => s.With(marginTopPx: topPx, marginRightPx: rightPx, marginBottomPx: bottomPx, marginLeftPx: leftPx));

    /// <summary>Sets uniform padding (inset between the box edge and the text) on all four sides, in pixels.</summary>
    public TextElementBuilder Padding(double px) => Apply(s => s.With(paddingTopPx: px, paddingRightPx: px, paddingBottomPx: px, paddingLeftPx: px));

    /// <summary>Sets padding per side, in pixels (CSS shorthand order: top, right, bottom, left).</summary>
    public TextElementBuilder Padding(double topPx, double rightPx, double bottomPx, double leftPx) =>
        Apply(s => s.With(paddingTopPx: topPx, paddingRightPx: rightPx, paddingBottomPx: bottomPx, paddingLeftPx: leftPx));
}
