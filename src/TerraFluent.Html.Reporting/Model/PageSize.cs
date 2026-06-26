namespace TerraFluent.Html.Reporting.Model;

/// <summary>
/// The physical dimensions of a page, stored internally as CSS pixels (96px = 1in)
/// so the layout engine and renderer share a single unit. Use the factory methods
/// to construct from millimeters or inches; orientation is applied separately via
/// <see cref="WithOrientation"/>.
/// </summary>
public readonly struct PageSize : IEquatable<PageSize>
{
    private const double PixelsPerInch = 96.0;
    private const double MillimetersPerInch = 25.4;

    /// <summary>Page width, in CSS pixels, before any orientation swap.</summary>
    public double WidthPx { get; }

    /// <summary>Page height, in CSS pixels, before any orientation swap.</summary>
    public double HeightPx { get; }

    private PageSize(double widthPx, double heightPx)
    {
        if (widthPx <= 0) throw new ArgumentOutOfRangeException(nameof(widthPx));
        if (heightPx <= 0) throw new ArgumentOutOfRangeException(nameof(heightPx));
        WidthPx = widthPx;
        HeightPx = heightPx;
    }

    /// <summary>ISO A4: 210mm x 297mm.</summary>
    public static PageSize A4 { get; } = FromMillimeters(210, 297);

    /// <summary>US Letter: 8.5in x 11in.</summary>
    public static PageSize Letter { get; } = FromInches(8.5, 11);

    /// <summary>US Legal: 8.5in x 14in.</summary>
    public static PageSize Legal { get; } = FromInches(8.5, 14);

    /// <summary>Creates a custom page size from a width/height given in millimeters.</summary>
    public static PageSize FromMillimeters(double widthMm, double heightMm) =>
        new(MillimetersToPixels(widthMm), MillimetersToPixels(heightMm));

    /// <summary>Creates a custom page size from a width/height given in inches.</summary>
    public static PageSize FromInches(double widthIn, double heightIn) =>
        new(widthIn * PixelsPerInch, heightIn * PixelsPerInch);

    /// <summary>Creates a custom page size directly from CSS pixels.</summary>
    public static PageSize FromPixels(double widthPx, double heightPx) =>
        new(widthPx, heightPx);

    /// <summary>
    /// Returns this page size as-is for <see cref="PageOrientation.Portrait"/>,
    /// or with width/height swapped for <see cref="PageOrientation.Landscape"/>.
    /// This is a plain 90-degree rotation of whatever dimensions were given, not
    /// a "make width &gt; height" coercion - <see cref="A4"/>/<see cref="Letter"/>/<see cref="Legal"/>
    /// are defined portrait-shaped so this rotates them correctly, but a custom
    /// size from <see cref="FromPixels"/> is never silently reshaped just because
    /// the default orientation is Portrait.
    /// </summary>
    public PageSize WithOrientation(PageOrientation orientation) =>
        orientation == PageOrientation.Portrait ? this : new PageSize(HeightPx, WidthPx);

    private static double MillimetersToPixels(double mm) => mm / MillimetersPerInch * PixelsPerInch;

    /// <inheritdoc />
    public bool Equals(PageSize other) => WidthPx.Equals(other.WidthPx) && HeightPx.Equals(other.HeightPx);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is PageSize other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => WidthPx.GetHashCode() * 397 ^ HeightPx.GetHashCode();

    /// <summary>Equality operator, see <see cref="Equals(PageSize)"/>.</summary>
    public static bool operator ==(PageSize left, PageSize right) => left.Equals(right);

    /// <summary>Inequality operator, see <see cref="Equals(PageSize)"/>.</summary>
    public static bool operator !=(PageSize left, PageSize right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => $"{WidthPx:0.##}px x {HeightPx:0.##}px";
}
