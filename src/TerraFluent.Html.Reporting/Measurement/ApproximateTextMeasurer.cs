using System.Text;

namespace TerraFluent.Html.Reporting.Measurement;

/// <summary>
/// The default, zero-dependency <see cref="ITextMeasurer"/>. Estimates line
/// wrapping and height using per-character average-width tables (see
/// <see cref="HelveticaCharacterWidths"/>) rather than shaping the consumer's
/// actual font, so it will not match a browser pixel-for-pixel - particularly
/// for proportional fonts other than a generic sans-serif, or text with heavy
/// kerning/ligatures. It does not hyphenate: a single word wider than the
/// available width is placed alone on its own (overflowing) line rather than
/// broken mid-word. Consumers needing exact pagination should supply a
/// precise <see cref="ITextMeasurer"/> backed by a real rendering engine.
/// </summary>
public sealed class ApproximateTextMeasurer : ITextMeasurer
{
    /// <summary>A shared, stateless instance - this measurer holds no per-call state.</summary>
    public static ApproximateTextMeasurer Instance { get; } = new();

    // Helvetica-Bold widths run roughly 5-12% wider than Helvetica's regular
    // weight; a flat multiplier is close enough for an "approximate" measurer.
    private const double BoldWidthMultiplier = 1.08;

    /// <inheritdoc />
    public TextMeasurement Measure(string text, FontSpecification font, double maxWidthPx)
    {
        if (maxWidthPx <= 0) throw new ArgumentOutOfRangeException(nameof(maxWidthPx));
        var lineHeightPx = font.FontSizePx * font.LineHeightMultiplier;

        if (string.IsNullOrEmpty(text))
        {
            return new TextMeasurement(new[] { string.Empty }, lineHeightPx, 0);
        }

        var lines = new List<string>();
        var widest = 0.0;

        foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            WrapParagraph(paragraph, font, maxWidthPx, lines, ref widest);
        }

        return new TextMeasurement(lines, lineHeightPx, widest);
    }

    private static void WrapParagraph(string paragraph, FontSpecification font, double maxWidthPx, List<string> lines, ref double widest)
    {
        var current = new StringBuilder();
        var currentWidth = 0.0;

        foreach (var word in paragraph.Split(' '))
        {
            var wordWidth = MeasureWidth(word, font);
            var spaceWidth = current.Length > 0 ? MeasureWidth(" ", font) : 0;

            if (current.Length > 0 && currentWidth + spaceWidth + wordWidth > maxWidthPx)
            {
                lines.Add(current.ToString());
                if (currentWidth > widest) widest = currentWidth;
                current.Clear();
                currentWidth = 0;
            }

            if (current.Length > 0)
            {
                current.Append(' ');
                currentWidth += spaceWidth;
            }

            current.Append(word);
            currentWidth += wordWidth;
        }

        lines.Add(current.ToString());
        if (currentWidth > widest) widest = currentWidth;
    }

    private static double MeasureWidth(string text, FontSpecification font)
    {
        var totalPerMille = 0;
        foreach (var c in text)
        {
            totalPerMille += HelveticaCharacterWidths.AdvanceWidthPerMille(c);
        }

        var widthPx = totalPerMille / 1000.0 * font.FontSizePx;
        return font.Bold ? widthPx * BoldWidthMultiplier : widthPx;
    }
}
