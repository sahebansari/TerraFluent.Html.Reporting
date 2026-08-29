namespace TerraFluent.Html.Reporting.Model.Styling;

/// <summary>
/// A font face embedded directly into the generated HTML as a base64 data
/// URI <c>@font-face</c> rule - see <c>ReportDocumentBuilder.EmbedFont</c>.
/// Referencing this font from a <see cref="TextStyle.FontFamily"/> makes the
/// generated report self-contained (no dependency on the font being
/// installed wherever the HTML is opened/printed), the same reason
/// <see cref="Elements.ReportImage"/> embeds images inline rather than by
/// external reference.
/// </summary>
public sealed class EmbeddedFont
{
    private readonly byte[] _fontBytes;

    /// <summary>
    /// The CSS font-family name this face registers - reference it from
    /// <see cref="TextStyle.FontFamily"/> to use it. Must not contain
    /// <c>&lt;</c>/<c>&gt;</c> (this value is written into a shared
    /// <c>&lt;style&gt;</c> block, not an HTML attribute, so it cannot be
    /// safely HTML-encoded there without corrupting the CSS itself).
    /// </summary>
    public string FontFamily { get; }

    /// <summary>The raw font file bytes (e.g. a <c>.woff2</c>/<c>.ttf</c> file's contents).</summary>
    public byte[] FontBytes => (byte[])_fontBytes.Clone();

    /// <summary>The font weight this face applies to.</summary>
    public FontWeight Weight { get; }

    /// <summary>The font style (normal/italic) this face applies to.</summary>
    public FontStyle Style { get; }

    /// <summary>The MIME type used for the data URI and the <c>@font-face</c> <c>format()</c> hint, e.g. "font/woff2".</summary>
    public string MimeType { get; }

    /// <summary>Creates an embedded font face.</summary>
    public EmbeddedFont(string fontFamily, byte[] fontBytes, FontWeight weight, FontStyle style, string mimeType)
    {
        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            throw new ArgumentException("Font family must be provided.", nameof(fontFamily));
        }

        if (fontFamily.IndexOfAny(new[] { '<', '>' }) >= 0)
        {
            throw new ArgumentException("Font family must not contain '<' or '>'.", nameof(fontFamily));
        }

        if (fontBytes is null || fontBytes.Length == 0)
        {
            throw new ArgumentException("Font bytes must not be empty.", nameof(fontBytes));
        }

        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new ArgumentException("MIME type must be provided.", nameof(mimeType));
        }

        if (mimeType.IndexOfAny(new[] { '<', '>' }) >= 0)
        {
            throw new ArgumentException("MIME type must not contain '<' or '>'.", nameof(mimeType));
        }

        FontFamily = fontFamily;
        _fontBytes = (byte[])fontBytes.Clone();
        Weight = weight;
        Style = style;
        MimeType = mimeType;
    }
}
