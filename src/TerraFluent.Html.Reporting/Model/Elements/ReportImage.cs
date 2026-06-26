using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Rendering;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>
/// An embedded image. Never splits across pages. If only one of width/height is
/// given, the other is derived from the image's intrinsic pixel dimensions
/// (sniffed from its header bytes, see <see cref="ImageDimensionReader"/>); if
/// neither is given, the intrinsic dimensions are used directly.
/// </summary>
public sealed class ReportImage : IReportElement
{
    /// <summary>The raw image bytes, embedded inline as a base64 data URI when rendered.</summary>
    public byte[] ImageBytes { get; }

    /// <summary>The MIME type used for the data URI, e.g. "image/png".</summary>
    public string MimeType { get; }

    /// <summary>The resolved render width in pixels.</summary>
    public double WidthPx { get; }

    /// <summary>The resolved render height in pixels.</summary>
    public double HeightPx { get; }

    /// <summary>Space above the image, in pixels.</summary>
    public double MarginTopPx { get; init; } = 0;

    /// <summary>Space to the right of the image, in pixels - shrinks its box from the right edge of its container.</summary>
    public double MarginRightPx { get; init; } = 0;

    /// <summary>Space below the image, in pixels.</summary>
    public double MarginBottomPx { get; init; } = 8;

    /// <summary>Space to the left of the image, in pixels - shrinks its box from the left edge of its container and shifts it right.</summary>
    public double MarginLeftPx { get; init; } = 0;

    /// <summary>Inset, in pixels, between the image's box edge and the image itself, on each side.</summary>
    public double PaddingTopPx { get; init; } = 0;

    /// <summary>See <see cref="PaddingTopPx"/>.</summary>
    public double PaddingRightPx { get; init; } = 0;

    /// <summary>See <see cref="PaddingTopPx"/>.</summary>
    public double PaddingBottomPx { get; init; } = 0;

    /// <summary>See <see cref="PaddingTopPx"/>.</summary>
    public double PaddingLeftPx { get; init; } = 0;

    /// <summary>
    /// Horizontal position of the image within its container when narrower
    /// than the available width (after margin/padding). <see cref="TextAlignment.Justify"/>
    /// behaves the same as <see cref="TextAlignment.Left"/>.
    /// </summary>
    public TextAlignment Alignment { get; init; } = TextAlignment.Left;

    private ReportImage(byte[] imageBytes, string mimeType, double widthPx, double heightPx)
    {
        ImageBytes = imageBytes;
        MimeType = mimeType;
        WidthPx = widthPx;
        HeightPx = heightPx;
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
        TextAlignment? alignment = null) => new ReportImage(ImageBytes, MimeType, WidthPx, HeightPx)
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
            "src=\"data:" + MimeType + ";base64," + Convert.ToBase64String(ImageBytes) + "\" alt=\"\" />";
    }
}
