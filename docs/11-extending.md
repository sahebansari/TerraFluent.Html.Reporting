# Extending the Library

FluentHtmlReport has three deliberate extension seams: the text measurer
used for pagination, the renderer used to turn a layout into HTML, and the
element contract itself. None of them require forking the library - each is
a small interface the engine talks to abstractly.

## A custom `ITextMeasurer`

Implement this when the bundled `ApproximateTextMeasurer` isn't precise
enough for your needs - see [Text Measurement](09-text-measurement.md) for
why it's only approximate in the first place.

```csharp
using FluentHtmlReport.Measurement;

public sealed class MyPreciseTextMeasurer : ITextMeasurer
{
    public TextMeasurement Measure(string text, FontSpecification font, double maxWidthPx)
    {
        // 1. Resolve a real font/glyph source matching font.FontFamily,
        //    font.FontSizePx, font.Bold, font.Italic - e.g. a headless
        //    browser's measureText(), System.Drawing.Graphics.MeasureString
        //    on Windows, or a text-shaping library.
        // 2. Word-wrap `text` (splitting on '\n' first - each segment is a
        //    hard break, never wrapped through) so each resulting line's
        //    measured width is <= maxWidthPx.
        // 3. Compute lineHeightPx = font.FontSizePx * font.LineHeightMultiplier
        //    (or your engine's own metric, if more accurate) and the widest
        //    line's width.
        var lines = WrapPreciselyWithYourEngine(text, font, maxWidthPx);
        var lineHeightPx = font.FontSizePx * font.LineHeightMultiplier;
        var widestLineWidthPx = lines.Count == 0 ? 0 : lines.Max(l => MeasureLineWidth(l, font));
        return new TextMeasurement(lines, lineHeightPx, widestLineWidthPx);
    }
}
```

Plug it in via `UseTextMeasurer`:

```csharp
ReportDocument.Create(PageSize.A4)
    .UseTextMeasurer(new MyPreciseTextMeasurer())
    .Content(c => { ... })
    .Build();
```

A few things to get right, since the layout engine trusts this contract
completely:

- **Determinism.** `Measure` may be called more than once for the same
  `(text, font, maxWidthPx)` triple - it must return the same result every
  time (no randomness, no mutable shared state that changes the outcome).
- **Hard breaks.** Explicit `\n`/`\r\n` in `text` must never be wrapped
  through - each line they delimit is wrapped independently.
- **`LineHeightPx` must be > 0** - `TextMeasurement`'s constructor throws
  `ArgumentOutOfRangeException` otherwise.
- **Match what actually renders.** The whole point of a custom measurer is
  that its measurements agree with however the HTML is eventually displayed
  or printed - if you measure against one font but the rendered HTML's
  `font-family` resolves to a different one in the consumer's browser,
  you've just moved the mismatch rather than removed it.
- **Package it separately if it has native/runtime dependencies.** The
  intended pattern (see [Text Measurement § Supplying a precise measurer](09-text-measurement.md#supplying-a-precise-measurer))
  is a companion NuGet package depending on the core library, keeping the
  core package's own dependency footprint at zero.

## A custom `IHtmlReportRenderer`

Implement this when you need different output shape than the bundled
`HtmlReportRenderer` produces - a different page-wrapper structure, extra
metadata embedded in the markup, or PDF-engine-specific tweaks.

```csharp
using FluentHtmlReport.Layout;
using FluentHtmlReport.Rendering;

public sealed class MyCustomRenderer : IHtmlReportRenderer
{
    public string RenderDocument(LayoutResult layout)
    {
        var writer = new StringWriter();
        RenderDocumentTo(writer, layout);
        return writer.ToString();
    }

    public string RenderFragment(LayoutResult layout)
    {
        var writer = new StringWriter();
        RenderFragmentTo(writer, layout);
        return writer.ToString();
    }

    public void RenderDocumentTo(TextWriter writer, LayoutResult layout, CancellationToken cancellationToken = default)
    {
        // Emit your own <html>/<head>/<style>, then delegate per-page
        // rendering to RenderFragmentTo (or reimplement it) so both
        // entry points share one page-rendering code path.
    }

    public void RenderFragmentTo(TextWriter writer, LayoutResult layout, CancellationToken cancellationToken = default)
    {
        var totalPages = layout.Pages.Count;
        foreach (var page in layout.Pages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var renderContext = new RenderContext(page.PageIndex + 1, totalPages);

            // For each of page.HeaderElements / page.ContentElements / page.FooterElements:
            //   var absolute = placed.Placement.Translate(offsetX, offsetY);
            //   writer.Write(placed.Element.RenderHtml(absolute, renderContext));
            // matching how HtmlReportRenderer.RenderElements does it - the
            // offsets you choose determine where each section sits on the page.
        }
    }
}
```

Pass an instance to any of the five render methods on `ReportDocument`:

```csharp
string html = report.RenderHtml(new MyCustomRenderer());
```

The key insight a custom renderer needs to internalize: by the time it runs,
**all positioning decisions are already made** - `LayoutResult` is a plain
description of pages and placements (see
[Pagination and Layout § The output](07-pagination-and-layout.md#the-output-layoutresult)).
A renderer's job is purely translation - section-relative coordinates to
whatever coordinate system your output format wants - not pagination logic.
`RenderContext(pageNumber, totalPages)` is what lets `PageNumberText`
resolve its `{page}`/`{totalPages}` tokens; construct one per page with the
correct 1-based page number.

## Implementing a new `IReportElement`

Implement this when none of the built-in elements
(see [Content Elements](03-content-elements.md)) cover what you need, and
`AddRawHtml`'s caller-supplied-height escape hatch isn't precise enough
(e.g. you want the engine to measure and split your content automatically).

```csharp
using FluentHtmlReport.Layout;
using FluentHtmlReport.Model;
using FluentHtmlReport.Rendering;

public sealed class Watermark : IReportElement
{
    public string Text { get; }
    public double HeightPx { get; }

    public Watermark(string text, double heightPx)
    {
        Text = text;
        HeightPx = heightPx;
    }

    public ElementMeasurement Measure(LayoutContext context) => new(HeightPx);

    // Unsplittable, like ReportImage/HorizontalRule/Spacer - a watermark
    // band doesn't make sense torn across two pages.
    public SplitResult Split(double availableHeightPx, LayoutContext context) =>
        SplitResult.Unsplittable(this);

    public string RenderHtml(ElementPlacement placement, RenderContext context) =>
        "<div style=\"position:absolute;left:" + placement.XPx + "px;top:" + placement.YPx + "px;" +
        "width:" + placement.WidthPx + "px;height:" + placement.HeightPx + "px;" +
        "opacity:0.15;font-size:48px;text-align:center;\">" + Text + "</div>";
}
```

```csharp
// No ContentBuilder.AddWatermark() exists - construct and add it via
// whatever extension method or lower-level path your application defines.
// (ContentBuilder.Elements is internal, so a real integration typically
// adds a small extension method alongside ContentBuilder, or you assemble
// IReportElement lists yourself outside the fluent builders entirely.)
```

Re-read [Pagination and Layout § The `IReportElement` contract](07-pagination-and-layout.md#the-ireportelement-contract)
before writing one of these - in particular: `Measure` must be pure and
side-effect-free (the engine may call it repeatedly), `Split` is only ever
called *after* `Measure` reported more height than is available, and if your
element can be partially placed, the head/tail fragments your `Split`
returns must themselves satisfy this same contract (they're just more
`IReportElement` instances, often of the very same type with a trimmed-down
payload - see how `Paragraph.Split` and `Table.Split` build their head/tail
as new instances of themselves for the pattern to follow). If your content
truly can't be partially placed, always return `SplitResult.Unsplittable(this)` -
that's what makes the "force-place on an empty page + warn" fallback in the
layout engine kick in correctly instead of looping.

**HTML-encode any user-supplied text yourself** inside `RenderHtml` (mirror
[`CssFormat.Encode`](../src/FluentHtmlReport/Rendering/CssFormat.cs)'s use of
`WebUtility.HtmlEncode`) unless you specifically intend to emit raw markup -
nothing upstream of your element does this for you.

## Where to go next

- [Pagination and Layout](07-pagination-and-layout.md) for the full
  measure/split/place algorithm your element or renderer plugs into.
- [Text Measurement](09-text-measurement.md) for the measurer contract in
  more detail.
- [Rendering](08-rendering.md) for exactly what the bundled renderer emits,
  if you're building a variation on it rather than starting from scratch.
