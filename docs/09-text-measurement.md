# Text Measurement

Pagination needs to know how tall a block of text will be *before* it's
rendered - which means measuring how it wraps at a given width. This is the
single seam, `ITextMeasurer`, that every height calculation in the layout
engine ultimately depends on.

## `ITextMeasurer`

[`ITextMeasurer`](../src/TerraFluent.Html.Reporting/Measurement/ITextMeasurer.cs) is a
one-method interface:

```csharp
public interface ITextMeasurer
{
    TextMeasurement Measure(string text, FontSpecification font, double maxWidthPx);
}
```

- **`text`** may contain explicit newlines, which must be treated as hard
  breaks (not wrapped through).
- **`font`** is a [`FontSpecification`](../src/TerraFluent.Html.Reporting/Measurement/FontSpecification.cs) -
  family, size, bold, italic, and line-height multiplier - deliberately
  decoupled from `Model.Styling.TextStyle` so an `ITextMeasurer` implementation
  (including one shipped in a separate package) doesn't need to depend on the
  document model at all.
- **`maxWidthPx`** is the width a single line may occupy.

The return type, [`TextMeasurement`](../src/TerraFluent.Html.Reporting/Measurement/TextMeasurement.cs),
holds the resulting `Lines` (each one guaranteed to fit within the measured
width), the resolved `LineHeightPx`, and `WidestLineWidthPx`. `TotalHeightPx`
(`Lines.Count * LineHeightPx`) is what the layout engine actually sums for
pagination; elements that support splitting (`Paragraph`, table cells)
slice `Lines` at a line boundary to build the head/tail fragments for
`IReportElement.Split`.

## The default: `ApproximateTextMeasurer`

[`ApproximateTextMeasurer`](../src/TerraFluent.Html.Reporting/Measurement/ApproximateTextMeasurer.cs)
(exposed as the stateless singleton `ApproximateTextMeasurer.Instance`, and
used automatically unless you override it) estimates wrapping using
per-character average-width tables for Helvetica
([`HelveticaCharacterWidths`](../src/TerraFluent.Html.Reporting/Measurement/HelveticaCharacterWidths.cs)),
scaled by font size, with a flat 1.08x multiplier approximating bold's extra
width. It has **zero runtime dependencies** - no `System.Drawing`, no native
text-shaping library - which is what keeps the whole core package usable on
`netstandard2.0` and trimmable/AOT-compatible on modern .NET.

What this buys you, and what it doesn't:

- It **will not** match a browser's actual layout engine pixel-for-pixel -
  particularly for proportional fonts other than a generic sans-serif, text
  with heavy kerning/ligatures, or any font where Helvetica's metrics are a
  poor stand-in.
- It **does not hyphenate**: a single word wider than the available width is
  placed alone on its own (overflowing) line rather than broken mid-word.
- `FontFamily`/`Bold`/`Italic` on a `TextStyle` still affect the *rendered*
  output normally (the CSS faithfully sets `font-family`, `font-weight`,
  `font-style`) - they just don't change which width table `ApproximateTextMeasurer`
  consults, since it only ever measures against the one Helvetica table.

In practice this means: page breaks chosen with the default measurer are
*close* to where a real browser would wrap the same text, but not
guaranteed exact - acceptable for most reports, but worth knowing about if
your report's pagination needs to be pixel-exact (e.g. a legally significant
multi-page contract where a line must never silently shift to the next page
in print).

## Supplying a precise measurer

```csharp
ReportDocument.Create(PageSize.A4)
    .UseTextMeasurer(myPreciseMeasurer)
    .Content(c => { ... })
    .Build();
```

`ReportDocumentBuilder.UseTextMeasurer(ITextMeasurer measurer)` overrides the
default for the whole document; it throws `ArgumentNullException` if you
pass `null`. There's no per-element override - one document, one measurer,
used consistently for every element so pagination is internally coherent.

Implement `ITextMeasurer` against a real rendering engine - a headless
browser (e.g. Playwright/Puppeteer measuring actual DOM layout),
`System.Drawing`/GDI+ on Windows, or any text-shaping library that can report
glyph advances for your target font - to get pagination that matches actual
rendering. **No ready-made precise measurer ships with the core package
today** - this is a documented extension point, not a plug-in registry. The
intended pattern is a separate companion package (e.g. a hypothetical
`TerraFluent.Html.Reporting.Measurement.Playwright`) that depends on the core package
and supplies one, keeping the core package itself free of native/runtime
dependencies. See [Extending the Library](11-extending.md#a-custom-itextmeasurer)
for a sketch of what implementing one looks like.

## Where to go next

- [Pagination and Layout](07-pagination-and-layout.md) for how `Measure`
  results feed into page-break decisions.
- [Extending the Library](11-extending.md) for a concrete starting point if
  you're writing a custom measurer.
- [FAQ § Why didn't my page break where I expected?](12-faq-troubleshooting.md#why-didnt-my-page-break-exactly-where-i-expected)
