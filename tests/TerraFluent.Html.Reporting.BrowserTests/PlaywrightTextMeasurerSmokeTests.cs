using TerraFluent.Html.Reporting.Measurement;
using TerraFluent.Html.Reporting.Sample.PlaywrightMeasurer;
using Xunit;

namespace TerraFluent.Html.Reporting.BrowserTests;

/// <summary>
/// A smoke test for the sample <c>PlaywrightTextMeasurer</c> reference
/// implementation (see samples/TerraFluent.Html.Reporting.Sample.PlaywrightMeasurer) -
/// not exhaustive, just enough to catch "it doesn't work at all" regressions.
/// Lives here (rather than in the main unit test project) because it needs
/// the same Playwright browser install CI already performs for
/// <see cref="PrintLayoutBrowserTests"/>.
/// </summary>
public sealed class PlaywrightTextMeasurerSmokeTests
{
    [Fact]
    public async Task Measure_WrapsKnownTextIntoASaneLineCount()
    {
        await using var measurer = await PlaywrightTextMeasurer.CreateAsync();

        var font = new FontSpecification("Arial", 20);
        var narrow = measurer.Measure("one two three four five six seven eight nine ten", font, maxWidthPx: 100);
        var wide = measurer.Measure("one two three four five six seven eight nine ten", font, maxWidthPx: 2000);

        Assert.True(narrow.Lines.Count > 1, "Expected the text to wrap across multiple lines at a narrow width.");
        Assert.Single(wide.Lines);
        Assert.Equal("one two three four five six seven eight nine ten", wide.Lines[0]);
        Assert.True(narrow.WidestLineWidthPx <= 100 + 1, "A wrapped line should not (meaningfully) exceed the requested max width.");
        Assert.Equal(20 * font.LineHeightMultiplier, narrow.LineHeightPx);
    }

    [Fact]
    public async Task Measure_EmptyString_ReturnsSingleEmptyLine()
    {
        await using var measurer = await PlaywrightTextMeasurer.CreateAsync();

        var result = measurer.Measure(string.Empty, new FontSpecification("Arial", 14), maxWidthPx: 200);

        Assert.Equal(new[] { string.Empty }, result.Lines);
        Assert.Equal(0, result.WidestLineWidthPx);
    }
}
