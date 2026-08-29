using TerraFluent.Html.Reporting.Measurement;
using Xunit;

namespace TerraFluent.Html.Reporting.Tests.Measurement;

/// <summary>
/// Direct tests of the shipped default <see cref="ApproximateTextMeasurer"/> -
/// every other test in this suite deliberately substitutes a fake/counting
/// measurer for determinism, which is correct for those tests but had left
/// this actual production measurement path untested in isolation. Font size
/// 1000px with a 1.0 line-height multiplier is used throughout so a
/// character's Helvetica per-mille advance width (see
/// <see cref="HelveticaCharacterWidths"/>) equals its measured pixel width
/// directly, keeping expected values exact rather than approximated.
/// </summary>
public class ApproximateTextMeasurerTests
{
    private static readonly FontSpecification Font1000 = new("Arial", 1000, lineHeightMultiplier: 1.0);

    [Fact]
    public void Measure_EmptyString_ReturnsSingleEmptyLineWithZeroWidth()
    {
        var result = ApproximateTextMeasurer.Instance.Measure(string.Empty, Font1000, maxWidthPx: 500);

        Assert.Equal(new[] { string.Empty }, result.Lines);
        Assert.Equal(0, result.WidestLineWidthPx);
        Assert.Equal(1000, result.LineHeightPx);
    }

    [Fact]
    public void Measure_WordsFitExactly_StaysOnOneLine()
    {
        // "ab" = 556+556 = 1112px, space = 278px, "cd" = 500+556 = 1056px.
        // 1112 + 278 + 1056 = 2446, so a maxWidthPx of exactly 2446 must fit both words.
        var result = ApproximateTextMeasurer.Instance.Measure("ab cd", Font1000, maxWidthPx: 2446);

        Assert.Equal(new[] { "ab cd" }, result.Lines);
        Assert.Equal(2446, result.WidestLineWidthPx);
    }

    [Fact]
    public void Measure_WordsExceedWidthByOnePixel_WrapsToNextLine()
    {
        var result = ApproximateTextMeasurer.Instance.Measure("ab cd", Font1000, maxWidthPx: 2445);

        Assert.Equal(new[] { "ab", "cd" }, result.Lines);
        Assert.Equal(1112, result.WidestLineWidthPx);
    }

    [Fact]
    public void Measure_ExplicitNewlines_ProducesOneWrappedBlockPerParagraph()
    {
        // Each "ab cd" pair only just fits on one line at maxWidthPx 2446 (see
        // above) - a \n must start a fresh line rather than being treated as
        // just another word boundary within the same wrap.
        var result = ApproximateTextMeasurer.Instance.Measure("ab cd\nab cd", Font1000, maxWidthPx: 2446);

        Assert.Equal(new[] { "ab cd", "ab cd" }, result.Lines);
    }

    [Fact]
    public void Measure_CarriageReturnNewline_IsTreatedSameAsPlainNewline()
    {
        var withCrlf = ApproximateTextMeasurer.Instance.Measure("ab cd\r\nab cd", Font1000, maxWidthPx: 2446);
        var withLf = ApproximateTextMeasurer.Instance.Measure("ab cd\nab cd", Font1000, maxWidthPx: 2446);

        Assert.Equal(withLf.Lines, withCrlf.Lines);
    }

    [Fact]
    public void Measure_SingleWordWiderThanMaxWidth_IsPlacedAloneOnOverflowingLineWithoutHyphenation()
    {
        // "Mmmmm" = 833 + 833*4 = 4165px ('M' and 'm' are both 833 per-mille
        // in the Helvetica metrics table), far wider than a 100px max width.
        var result = ApproximateTextMeasurer.Instance.Measure("Mmmmm", Font1000, maxWidthPx: 100);

        Assert.Equal(new[] { "Mmmmm" }, result.Lines);
        Assert.Equal(4165, result.WidestLineWidthPx);
    }

    [Fact]
    public void Measure_Bold_IsWiderThanNonBoldByTheDocumentedMultiplier()
    {
        var normal = new FontSpecification("Arial", 1000, bold: false);
        var bold = new FontSpecification("Arial", 1000, bold: true);

        // 'W' = 944 per-mille; ApproximateTextMeasurer documents a flat 1.08x
        // bold-width multiplier as "close enough for an approximate measurer".
        var normalResult = ApproximateTextMeasurer.Instance.Measure("W", normal, maxWidthPx: 10000);
        var boldResult = ApproximateTextMeasurer.Instance.Measure("W", bold, maxWidthPx: 10000);

        Assert.Equal(944, normalResult.WidestLineWidthPx, precision: 6);
        Assert.Equal(944 * 1.08, boldResult.WidestLineWidthPx, precision: 6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Measure_NonPositiveMaxWidth_Throws(double maxWidthPx)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ApproximateTextMeasurer.Instance.Measure("hi", Font1000, maxWidthPx));
    }
}
