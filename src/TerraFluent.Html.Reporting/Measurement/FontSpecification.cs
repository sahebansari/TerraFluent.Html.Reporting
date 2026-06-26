namespace TerraFluent.Html.Reporting.Measurement;

/// <summary>
/// A minimal description of the font a piece of text should be measured with.
/// Deliberately decoupled from <see cref="TerraFluent.Html.Reporting.Model.Styling.TextStyle"/>
/// so that <see cref="ITextMeasurer"/> implementations (including ones shipped in
/// separate measurement packages) do not need to depend on the document model.
/// </summary>
public readonly struct FontSpecification : IEquatable<FontSpecification>
{
    /// <summary>CSS font-family list, e.g. "Segoe UI, Arial, sans-serif".</summary>
    public string FontFamily { get; }

    /// <summary>Font size in CSS pixels.</summary>
    public double FontSizePx { get; }

    /// <summary>Whether the text is bold.</summary>
    public bool Bold { get; }

    /// <summary>Whether the text is italic.</summary>
    public bool Italic { get; }

    /// <summary>
    /// Line height as a multiple of <see cref="FontSizePx"/>. Carried here (rather
    /// than left to the measurer to assume a fixed default) so that a
    /// <see cref="TerraFluent.Html.Reporting.Model.Styling.TextStyle.LineHeightMultiplier"/>
    /// override actually affects the height the layout engine computes.
    /// </summary>
    public double LineHeightMultiplier { get; }

    /// <summary>Creates a font specification.</summary>
    public FontSpecification(string fontFamily, double fontSizePx, bool bold = false, bool italic = false, double lineHeightMultiplier = 1.2)
    {
        if (string.IsNullOrWhiteSpace(fontFamily)) throw new ArgumentException("Font family must be provided.", nameof(fontFamily));
        if (fontSizePx <= 0) throw new ArgumentOutOfRangeException(nameof(fontSizePx));
        if (lineHeightMultiplier <= 0) throw new ArgumentOutOfRangeException(nameof(lineHeightMultiplier));
        FontFamily = fontFamily;
        FontSizePx = fontSizePx;
        Bold = bold;
        Italic = italic;
        LineHeightMultiplier = lineHeightMultiplier;
    }

    /// <inheritdoc />
    public bool Equals(FontSpecification other) =>
        FontFamily == other.FontFamily && FontSizePx.Equals(other.FontSizePx) && Bold == other.Bold && Italic == other.Italic &&
        LineHeightMultiplier.Equals(other.LineHeightMultiplier);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is FontSpecification other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = FontFamily.GetHashCode();
        hash = hash * 397 ^ FontSizePx.GetHashCode();
        hash = hash * 397 ^ Bold.GetHashCode();
        hash = hash * 397 ^ Italic.GetHashCode();
        hash = hash * 397 ^ LineHeightMultiplier.GetHashCode();
        return hash;
    }

    /// <summary>Equality operator, see <see cref="Equals(FontSpecification)"/>.</summary>
    public static bool operator ==(FontSpecification left, FontSpecification right) => left.Equals(right);

    /// <summary>Inequality operator, see <see cref="Equals(FontSpecification)"/>.</summary>
    public static bool operator !=(FontSpecification left, FontSpecification right) => !left.Equals(right);
}
