using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Rendering;
using TerraFluent.Html.Reporting.Compatibility;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>
/// An embedded image. Never splits across pages. If only one of width/height is
/// given, the other is derived from the image's intrinsic pixel dimensions
/// (sniffed from its header bytes, see <see cref="ImageDimensionReader"/>); if
/// neither is given, the intrinsic dimensions are used directly.
/// </summary>
public sealed class ReportImage : IReportElement
{
    private readonly byte[] _imageBytes;
    private double _marginTopPx;
    private double _marginRightPx;
    private double _marginBottomPx = 8;
    private double _marginLeftPx;
    private double _paddingTopPx;
    private double _paddingRightPx;
    private double _paddingBottomPx;
    private double _paddingLeftPx;

    /// <summary>The raw image bytes, embedded inline as a base64 data URI when rendered.</summary>
    public byte[] ImageBytes => (byte[])_imageBytes.Clone();

    /// <summary>The MIME type used for the data URI, e.g. "image/png".</summary>
    public string MimeType { get; }

    /// <summary>The resolved render width in pixels.</summary>
    public double WidthPx { get; }

    /// <summary>The resolved render height in pixels.</summary>
    public double HeightPx { get; }

    /// <summary>Space above the image, in pixels.</summary>
    public double MarginTopPx
    {
        get => _marginTopPx;
        init => _marginTopPx = Guard.NonNegative(value, nameof(MarginTopPx));
    }

    /// <summary>Space to the right of the image, in pixels - shrinks its box from the right edge of its container.</summary>
    public double MarginRightPx
    {
        get => _marginRightPx;
        init => _marginRightPx = Guard.NonNegative(value, nameof(MarginRightPx));
    }

    /// <summary>Space below the image, in pixels.</summary>
    public double MarginBottomPx
    {
        get => _marginBottomPx;
        init => _marginBottomPx = Guard.NonNegative(value, nameof(MarginBottomPx));
    }

    /// <summary>Space to the left of the image, in pixels - shrinks its box from the left edge of its container and shifts it right.</summary>
    public double MarginLeftPx
    {
        get => _marginLeftPx;
        init => _marginLeftPx = Guard.NonNegative(value, nameof(MarginLeftPx));
    }

    /// <summary>Inset, in pixels, between the image's box edge and the image itself, on each side.</summary>
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

    /// <summary>
    /// Horizontal position of the image within its container when narrower
    /// than the available width (after margin/padding). <see cref="TextAlignment.Justify"/>
    /// behaves the same as <see cref="TextAlignment.Left"/>.
    /// </summary>
    public TextAlignment Alignment { get; init; } = TextAlignment.Left;

    private ReportImage(byte[] imageBytes, string mimeType, double widthPx, double heightPx)
    {
        _imageBytes = (byte[])imageBytes.Clone();
        MimeType = string.IsNullOrWhiteSpace(mimeType)
            ? throw new ArgumentException("MIME type must be provided.", nameof(mimeType))
            : mimeType;
        WidthPx = Guard.Positive(widthPx, nameof(widthPx));
        HeightPx = Guard.Positive(heightPx, nameof(heightPx));
    }

    /// <summary>Loads an image from a local file path.</summary>
    public static ReportImage FromFile(string filePath, double? widthPx = null, double? heightPx = null) =>
        FromBytes(File.ReadAllBytes(filePath), MimeTypeFromExtension(filePath), widthPx, heightPx);

    /// <summary>Loads an image from an in-memory byte array.</summary>
    public static ReportImage FromBytes(byte[] imageBytes, string mimeType = "image/png", double? widthPx = null, double? heightPx = null)
    {
        if (imageBytes is null || imageBytes.Length == 0) throw new ArgumentException("Image bytes must not be empty.", nameof(imageBytes));
        var (naturalWidth, naturalHeight) = ImageDimensionReader.ReadDimensions(imageBytes);
        var (w, h) = ResolveDimensions(widthPx, heightPx, naturalWidth, naturalHeight);
        return new ReportImage(imageBytes, mimeType, w, h);
    }

    /// <summary>Loads an image from a base64 string, optionally prefixed with a <c>data:</c> URI header.</summary>
    public static ReportImage FromBase64(string base64OrDataUri, string mimeType = "image/png", double? widthPx = null, double? heightPx = null)
    {
        if (base64OrDataUri is null) throw new ArgumentNullException(nameof(base64OrDataUri));
        var commaIndex = base64OrDataUri.IndexOf(',');
        var payload = base64OrDataUri.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0
            ? base64OrDataUri.Substring(commaIndex + 1)
            : base64OrDataUri;
        return FromBytes(Convert.FromBase64String(payload), mimeType, widthPx, heightPx);
    }

    private static (double Width, double Height) ResolveDimensions(double? widthPx, double? heightPx, int naturalWidth, int naturalHeight)
    {
        if (widthPx.HasValue && heightPx.HasValue) return (widthPx.Value, heightPx.Value);

        if (widthPx.HasValue)
        {
            return naturalWidth > 0 ? (widthPx.Value, widthPx.Value * naturalHeight / naturalWidth) : (widthPx.Value, widthPx.Value);
        }

        if (heightPx.HasValue)
        {
            return naturalHeight > 0 ? (heightPx.Value * naturalWidth / naturalHeight, heightPx.Value) : (heightPx.Value, heightPx.Value);
        }

        if (naturalWidth > 0 && naturalHeight > 0) return (naturalWidth, naturalHeight);

        throw new InvalidOperationException(
            "Could not determine the image's intrinsic dimensions; specify widthPx and/or heightPx explicitly.");
    }

    /// <summary>
    /// Returns a copy of this image with the given properties overridden,
    /// leaving the underlying bytes/MIME type/resolved dimensions unchanged.
    /// </summary>
    public ReportImage With(
        double? marginTopPx = null,
        double? marginRightPx = null,
        double? marginBottomPx = null,
        double? marginLeftPx = null,
        double? paddingTopPx = null,
        double? paddingRightPx = null,
        double? paddingBottomPx = null,
        double? paddingLeftPx = null,
        TextAlignment? alignment = null) => new ReportImage(_imageBytes, MimeType, WidthPx, HeightPx)
    {
        MarginTopPx = marginTopPx ?? MarginTopPx,
        MarginRightPx = marginRightPx ?? MarginRightPx,
        MarginBottomPx = marginBottomPx ?? MarginBottomPx,
        MarginLeftPx = marginLeftPx ?? MarginLeftPx,
        PaddingTopPx = paddingTopPx ?? PaddingTopPx,
        PaddingRightPx = paddingRightPx ?? PaddingRightPx,
        PaddingBottomPx = paddingBottomPx ?? PaddingBottomPx,
        PaddingLeftPx = paddingLeftPx ?? PaddingLeftPx,
        Alignment = alignment ?? Alignment,
    };

    private static string MimeTypeFromExtension(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        ".svg" => "image/svg+xml",
        _ => "image/png",
    };

    /// <inheritdoc />
    public ElementMeasurement Measure(LayoutContext context) =>
        new(HeightPx + PaddingTopPx + PaddingBottomPx + MarginTopPx + MarginBottomPx);

    /// <inheritdoc />
    public SplitResult Split(double availableHeightPx, LayoutContext context) => SplitResult.Unsplittable(this);

    /// <inheritdoc />
    public string RenderHtml(ElementPlacement placement, RenderContext context)
    {
        var availableWidthPx = Math.Max(0, placement.WidthPx - MarginLeftPx - MarginRightPx);
        var boxWidthPx = WidthPx + PaddingLeftPx + PaddingRightPx;
        var alignOffsetPx = Alignment switch
        {
            TextAlignment.Center => (availableWidthPx - boxWidthPx) / 2,
            TextAlignment.Right => availableWidthPx - boxWidthPx,
            _ => 0,
        };

        var left = placement.XPx + MarginLeftPx + Math.Max(0, alignOffsetPx) + PaddingLeftPx;
        var top = placement.YPx + MarginTopPx + PaddingTopPx;

        return "<img style=\"position:absolute;" +
            "left:" + CssFormat.Px(left) + ";top:" + CssFormat.Px(top) +
            ";width:" + CssFormat.Px(WidthPx) + ";height:" + CssFormat.Px(HeightPx) + ";\" " +
            "src=\"data:" + CssFormat.Attribute(MimeType) + ";base64," + Convert.ToBase64String(_imageBytes) + "\" alt=\"\" />";
    }
}
