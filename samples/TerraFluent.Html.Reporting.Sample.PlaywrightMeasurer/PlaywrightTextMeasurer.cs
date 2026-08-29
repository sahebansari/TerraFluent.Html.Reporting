using Microsoft.Playwright;
using TerraFluent.Html.Reporting.Measurement;

namespace TerraFluent.Html.Reporting.Sample.PlaywrightMeasurer;

/// <summary>
/// A reference <see cref="ITextMeasurer"/> backed by a real, headless
/// Chromium instance (via Playwright), for consumers who need pagination
/// closer to what an actual browser renders than the core package's default
/// <c>ApproximateTextMeasurer</c> (a generic Helvetica-metrics table).
/// </summary>
/// <remarks>
/// Word widths are measured with the Canvas 2D <c>measureText</c> API
/// against the browser's actual installed font - this is real font-metric
/// data, not a static table - but lines are still assembled with the same
/// greedy word-wrap algorithm <c>ApproximateTextMeasurer</c> uses. This is
/// <em>not</em> a full DOM layout pass (it doesn't account for kerning
/// between arbitrary character pairs, complex script shaping, or
/// browser-specific line-breaking rules for punctuation) - see this
/// folder's README for the exact tradeoffs and when a fuller approach
/// (e.g. rendering into a real page and walking DOM <c>Range</c> boundaries)
/// might be worth the added complexity instead.
/// </remarks>
public sealed class PlaywrightTextMeasurer : ITextMeasurer, IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;
    private readonly IPage _page;

    private PlaywrightTextMeasurer(IPlaywright playwright, IBrowser browser, IPage page)
    {
        _playwright = playwright;
        _browser = browser;
        _page = page;
    }

    /// <summary>
    /// Launches headless Chromium and prepares a blank page for measurement.
    /// Create <b>one</b> instance and reuse it for an entire document's
    /// pagination (and, ideally, across documents) - launching a browser is
    /// expensive, and that cost is exactly what this class avoids paying more
    /// than once. Dispose it (via <see cref="DisposeAsync"/>) when done.
    /// </summary>
    public static async Task<PlaywrightTextMeasurer> CreateAsync()
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        return new PlaywrightTextMeasurer(playwright, browser, page);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="ITextMeasurer.Measure"/> is a synchronous method - the core
    /// package's pagination engine calls it inline, never awaited - but
    /// Playwright's .NET API is async-only. Blocking on the async call here
    /// is the deliberate bridge a browser-backed measurer has to accept; it's
    /// safe from a console app or background worker (no captured
    /// <see cref="SynchronizationContext"/>), but avoid this pattern from a
    /// UI thread, where it can deadlock.
    /// </remarks>
    public TextMeasurement Measure(string text, FontSpecification font, double maxWidthPx) =>
        MeasureAsync(text, font, maxWidthPx).GetAwaiter().GetResult();

    private async Task<TextMeasurement> MeasureAsync(string text, FontSpecification font, double maxWidthPx)
    {
        var lineHeightPx = font.FontSizePx * font.LineHeightMultiplier;

        if (string.IsNullOrEmpty(text))
        {
            return new TextMeasurement(new[] { string.Empty }, lineHeightPx, 0);
        }

        var lines = new List<string>();
        var widest = 0.0;

        foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            widest = Math.Max(widest, await WrapParagraphAsync(paragraph, font, maxWidthPx, lines));
        }

        return new TextMeasurement(lines, lineHeightPx, widest);
    }

    private async Task<double> WrapParagraphAsync(string paragraph, FontSpecification font, double maxWidthPx, List<string> lines)
    {
        var words = paragraph.Split(' ');
        var widths = await MeasureWidthsAsync(words, font);
        var spaceWidthPx = (await MeasureWidthsAsync(new[] { " " }, font))[0];

        var current = new System.Text.StringBuilder();
        var currentWidth = 0.0;
        var widest = 0.0;

        for (var i = 0; i < words.Length; i++)
        {
            var wordWidth = widths[i];
            var addedSpaceWidth = current.Length > 0 ? spaceWidthPx : 0;

            if (current.Length > 0 && currentWidth + addedSpaceWidth + wordWidth > maxWidthPx)
            {
                lines.Add(current.ToString());
                widest = Math.Max(widest, currentWidth);
                current.Clear();
                currentWidth = 0;
            }

            if (current.Length > 0)
            {
                current.Append(' ');
                currentWidth += spaceWidthPx;
            }

            current.Append(words[i]);
            currentWidth += wordWidth;
        }

        lines.Add(current.ToString());
        widest = Math.Max(widest, currentWidth);
        return widest;
    }

    private async Task<double[]> MeasureWidthsAsync(string[] words, FontSpecification font)
    {
        var fontCss = $"{(font.Italic ? "italic " : string.Empty)}{(font.Bold ? "bold " : string.Empty)}{font.FontSizePx}px {font.FontFamily}";
        return await _page.EvaluateAsync<double[]>(
            """
            ([words, fontCss]) => {
                const canvas = document.createElement('canvas');
                const ctx = canvas.getContext('2d');
                ctx.font = fontCss;
                return words.map(w => ctx.measureText(w).width);
            }
            """,
            new object[] { words, fontCss });
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _page.CloseAsync();
        await _browser.CloseAsync();
        _playwright.Dispose();
    }
}
