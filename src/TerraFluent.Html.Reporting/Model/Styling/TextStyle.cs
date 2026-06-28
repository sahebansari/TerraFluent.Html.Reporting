using TerraFluent.Html.Reporting.Compatibility;

namespace TerraFluent.Html.Reporting.Model.Styling;

/// <summary>
/// Visual styling applied to text-bearing elements (paragraphs, headings, list items,
/// table cells). Immutable; use <see cref="With"/> to derive a modified copy.
/// </summary>
public sealed class TextStyle
{
    private double _fontSizePx = 14;
    private double _lineHeightMultiplier = 1.4;
    private double _marginTopPx;
    private double _marginRightPx;
    private double _marginBottomPx = 8;
    private double _marginLeftPx;
    private double _paddingTopPx;
    private double _paddingRightPx;
    private double _paddingBottomPx;
    private double _paddingLeftPx;

    /// <summary>The default style used when an element does not specify one.</summary>
    public static TextStyle Default { get; } = new();

    /// <summary>CSS font-family list, e.g. "Segoe UI, Arial, sans-serif".</summary>
    public string FontFamily { get; init; } = "Segoe UI, Arial, sans-serif";

    /// <summary>Font size in CSS pixels.</summary>
    public double FontSizePx
    {
        get => _fontSizePx;
        init => _fontSizePx = Guard.Positive(value, nameof(FontSizePx));
    }

    /// <summary>Font weight (normal/bold).</summary>
    public FontWeight FontWeight { get; init; } = FontWeight.Normal;

    /// <summary>Font style (normal/italic).</summary>
    public FontStyle FontStyle { get; init; } = FontStyle.Normal;

    /// <summary>Text color as a CSS color string (e.g. "#222222").</summary>
    public string Color { get; init; } = "#222222";

    /// <summary>Line height as a multiple of <see cref="FontSizePx"/> (e.g. 1.4).</summary>
    public double LineHeightMultiplier
    {
        get => _lineHeightMultiplier;
        init => _lineHeightMultiplier = Guard.Positive(value, nameof(LineHeightMultiplier));
    }

    /// <summary>Horizontal alignment of the text within its containing block.</summary>
    public TextAlignment Alignment { get; init; } = TextAlignment.Left;

    /// <summary>Space, in pixels, above the element.</summary>
    public double MarginTopPx
    {
        get => _marginTopPx;
        init => _marginTopPx = Guard.NonNegative(value, nameof(MarginTopPx));
    }

    /// <summary>Space, in pixels, to the right of the element - shrinks its box from the right edge of its container.</summary>
    public double MarginRightPx
    {
        get => _marginRightPx;
        init => _marginRightPx = Guard.NonNegative(value, nameof(MarginRightPx));
    }

    /// <summary>Space, in pixels, below the element before the next one begins.</summary>
    public double MarginBottomPx
    {
        get => _marginBottomPx;
        init => _marginBottomPx = Guard.NonNegative(value, nameof(MarginBottomPx));
    }

    /// <summary>Space, in pixels, to the left of the element - shrinks its box from the left edge of its container and shifts it right.</summary>
    public double MarginLeftPx
    {
        get => _marginLeftPx;
        init => _marginLeftPx = Guard.NonNegative(value, nameof(MarginLeftPx));
    }

    /// <summary>Inset, in pixels, between the element's box edge and its text on each side. Text wraps within the space left over after padding (and margin) are subtracted from the container width.</summary>
    public double PaddingTopPx
    {
        get => _paddingTopPx;
        init => _paddingTopPx = Guard.NonNegative(value, nameof(PaddingTopPx));
    }

    /// <summary>See <see cref="PaddingTopPx"/>.</summary>
    public double PaddingRightPx
    {
        get => _paddingRightPx;
        init => _paddingRightPx = Guard.NonNegative(value, nameof(PaddingRightPx));
    }

    /// <summary>See <see cref="PaddingTopPx"/>.</summary>
    public double PaddingBottomPx
    {
        get => _paddingBottomPx;
        init => _paddingBottomPx = Guard.NonNegative(value, nameof(PaddingBottomPx));
    }

    /// <summary>See <see cref="PaddingTopPx"/>.</summary>
    public double PaddingLeftPx
    {
        get => _paddingLeftPx;
        init => _paddingLeftPx = Guard.NonNegative(value, nameof(PaddingLeftPx));
    }

    /// <summary>Resolved line height in pixels: <see cref="FontSizePx"/> * <see cref="LineHeightMultiplier"/>.</summary>
    public double LineHeightPx => FontSizePx * LineHeightMultiplier;

    /// <summary>Total margin + padding eaten from the container width on the left and right combined.</summary>
    public double HorizontalInsetPx => MarginLeftPx + MarginRightPx + PaddingLeftPx + PaddingRightPx;

    /// <summary>Total margin + padding added to the element's height on top and bottom combined.</summary>
    public double VerticalInsetPx => MarginTopPx + MarginBottomPx + PaddingTopPx + PaddingBottomPx;

    /// <summary>
    /// Returns a copy of this style with the given properties overridden, leaving
    /// all others unchanged.
    /// </summary>
    public TextStyle With(
        string? fontFamily = null,
        double? fontSizePx = null,
        FontWeight? fontWeight = null,
        Model.FontStyle? fontStyle = null,
        string? color = null,
        double? lineHeightMultiplier = null,
        TextAlignment? alignment = null,
        double? marginTopPx = null,
        double? marginRightPx = null,
        double? marginBottomPx = null,
        double? marginLeftPx = null,
        double? paddingTopPx = null,
        double? paddingRightPx = null,
        double? paddingBottomPx = null,
        double? paddingLeftPx = null) => new()
    {
        FontFamily = fontFamily ?? FontFamily,
        FontSizePx = fontSizePx ?? FontSizePx,
        FontWeight = fontWeight ?? FontWeight,
        FontStyle = fontStyle ?? FontStyle,
        Color = color ?? Color,
        LineHeightMultiplier = lineHeightMultiplier ?? LineHeightMultiplier,
        Alignment = alignment ?? Alignment,
        MarginTopPx = marginTopPx ?? MarginTopPx,
        MarginRightPx = marginRightPx ?? MarginRightPx,
        MarginBottomPx = marginBottomPx ?? MarginBottomPx,
        MarginLeftPx = marginLeftPx ?? MarginLeftPx,
        PaddingTopPx = paddingTopPx ?? PaddingTopPx,
        PaddingRightPx = paddingRightPx ?? PaddingRightPx,
        PaddingBottomPx = paddingBottomPx ?? PaddingBottomPx,
        PaddingLeftPx = paddingLeftPx ?? PaddingLeftPx,
    };

    /// <summary>The built-in font size/weight scale used for <see cref="Elements.Heading"/> levels H1-H6.</summary>
    public static TextStyle ForHeading(HeadingLevel level) => level switch
    {
        HeadingLevel.H1 => Default.With(fontSizePx: 28, fontWeight: Model.FontWeight.Bold, marginBottomPx: 16),
        HeadingLevel.H2 => Default.With(fontSizePx: 24, fontWeight: Model.FontWeight.Bold, marginBottomPx: 14),
        HeadingLevel.H3 => Default.With(fontSizePx: 20, fontWeight: Model.FontWeight.Bold, marginBottomPx: 12),
        HeadingLevel.H4 => Default.With(fontSizePx: 18, fontWeight: Model.FontWeight.Bold, marginBottomPx: 10),
        HeadingLevel.H5 => Default.With(fontSizePx: 16, fontWeight: Model.FontWeight.Bold, marginBottomPx: 8),
        HeadingLevel.H6 => Default.With(fontSizePx: 14, fontWeight: Model.FontWeight.Bold, marginBottomPx: 8),
        _ => throw new ArgumentOutOfRangeException(nameof(level)),
    };
}
