using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;

namespace TerraFluent.Html.Reporting.Fluent;

/// <summary>
/// Returned by <c>AddImage</c> to let the caller chain margin/padding/alignment
/// modifiers directly after adding an image, e.g. <c>c.AddImage(path).AlignCenter()</c>.
/// Mirrors <see cref="TextElementBuilder"/>'s pattern: <see cref="ReportImage"/> is
/// immutable, so each modifier rebuilds it via <see cref="ReportImage.With"/> and
/// replaces it in the owning section/content list.
/// </summary>
public sealed class ImageElementBuilder
{
    private readonly Action<ReportImage> _replace;
    private ReportImage _current;

    internal ImageElementBuilder(ReportImage initial, Action<ReportImage> replace)
    {
        _current = initial;
        _replace = replace;
    }

    private ImageElementBuilder Apply(Func<ReportImage, ReportImage> mutate)
    {
        _current = mutate(_current);
        _replace(_current);
        return this;
    }

    /// <summary>Aligns the image to the left of its container (the default).</summary>
    public ImageElementBuilder AlignLeft() => Apply(i => i.With(alignment: TextAlignment.Left));

    /// <summary>Centers the image within its container.</summary>
    public ImageElementBuilder AlignCenter() => Apply(i => i.With(alignment: TextAlignment.Center));

    /// <summary>Aligns the image to the right of its container.</summary>
    public ImageElementBuilder AlignRight() => Apply(i => i.With(alignment: TextAlignment.Right));

    /// <summary>Sets the space above the image, in pixels.</summary>
    public ImageElementBuilder MarginTop(double px) => Apply(i => i.With(marginTopPx: px));

    /// <summary>Sets the space to the right of the image, in pixels.</summary>
    public ImageElementBuilder MarginRight(double px) => Apply(i => i.With(marginRightPx: px));

    /// <summary>Sets the space below the image, in pixels.</summary>
    public ImageElementBuilder MarginBottom(double px) => Apply(i => i.With(marginBottomPx: px));

    /// <summary>Sets the space to the left of the image, in pixels.</summary>
    public ImageElementBuilder MarginLeft(double px) => Apply(i => i.With(marginLeftPx: px));

    /// <summary>Sets uniform margin on all four sides, in pixels.</summary>
    public ImageElementBuilder Margin(double px) => Apply(i => i.With(marginTopPx: px, marginRightPx: px, marginBottomPx: px, marginLeftPx: px));

    /// <summary>Sets margin per side, in pixels (CSS shorthand order: top, right, bottom, left).</summary>
    public ImageElementBuilder Margin(double topPx, double rightPx, double bottomPx, double leftPx) =>
        Apply(i => i.With(marginTopPx: topPx, marginRightPx: rightPx, marginBottomPx: bottomPx, marginLeftPx: leftPx));

    /// <summary>Sets uniform padding (inset between the box edge and the image) on all four sides, in pixels.</summary>
    public ImageElementBuilder Padding(double px) => Apply(i => i.With(paddingTopPx: px, paddingRightPx: px, paddingBottomPx: px, paddingLeftPx: px));

    /// <summary>Sets padding per side, in pixels (CSS shorthand order: top, right, bottom, left).</summary>
    public ImageElementBuilder Padding(double topPx, double rightPx, double bottomPx, double leftPx) =>
        Apply(i => i.With(paddingTopPx: topPx, paddingRightPx: rightPx, paddingBottomPx: bottomPx, paddingLeftPx: leftPx));
}
