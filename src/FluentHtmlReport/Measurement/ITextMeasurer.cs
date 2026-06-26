namespace FluentHtmlReport.Measurement;

/// <summary>
/// Computes how a run of text wraps and how tall it becomes at a given width.
/// This is the single seam the layout engine uses for all text height
/// calculations, so swapping it is enough to change pagination precision.
/// </summary>
/// <remarks>
/// The library ships <c>ApproximateTextMeasurer</c>, a zero-dependency
/// implementation backed by per-character average-width tables. It is
/// documented as approximate: it will not match a browser's layout engine
/// pixel-for-pixel, particularly for proportional fonts, kerning, or ligatures.
/// Consumers that need exact pagination can implement this interface against
/// a real rendering engine (e.g. a headless Chromium instance) and supply it
/// via <c>ReportDocumentBuilder.UseTextMeasurer</c>; such implementations are
/// expected to ship as separate packages (e.g. <c>FluentHtmlReport.Measurement.Playwright</c>)
/// to keep the core package free of native/runtime dependencies.
/// </remarks>
public interface ITextMeasurer
{
    /// <summary>
    /// Word-wraps <paramref name="text"/> so each line fits within
    /// <paramref name="maxWidthPx"/> when rendered with <paramref name="font"/>,
    /// and returns the resulting line breakdown and line height.
    /// </summary>
    /// <param name="text">The text to measure. May contain explicit newlines, which are treated as hard breaks.</param>
    /// <param name="font">The font to measure with.</param>
    /// <param name="maxWidthPx">The maximum width, in pixels, a single line may occupy.</param>
    TextMeasurement Measure(string text, FontSpecification font, double maxWidthPx);
}
