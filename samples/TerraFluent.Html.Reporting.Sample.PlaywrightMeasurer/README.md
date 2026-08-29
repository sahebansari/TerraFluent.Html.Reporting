# Playwright-Backed `ITextMeasurer` Sample

This sample project is a **reference implementation**, not a shipped part of
`TerraFluent.Html.Reporting` - it demonstrates how to plug in a text measurer
backed by a real browser, for consumers who need pagination closer to actual
browser rendering than the core package's default `ApproximateTextMeasurer`
(a generic Helvetica-metrics table) provides. See
[docs/09-text-measurement.md](../../docs/09-text-measurement.md) and
[docs/11-extending.md](../../docs/11-extending.md#a-custom-itextmeasurer).

## Why this isn't in the core package

The core package is intentionally zero-dependency, and a browser-backed
measurer needs one (here, [Playwright](https://playwright.dev/dotnet/) plus
a downloaded browser binary). Bundling that into the base package would force
every consumer - including ones perfectly happy with the approximate default -
to accept that weight. Shipping it as a separate sample/reference instead
keeps the core package's zero-dependency promise while still giving consumers
who need it a working starting point to copy and adapt.

## What it actually does

`PlaywrightTextMeasurer` measures each word's width with the Canvas 2D
`measureText` API against a real, headless Chromium instance's installed
font - genuine font-metric data, not a static table - then wraps those words
into lines using the same greedy word-wrap algorithm
`ApproximateTextMeasurer` uses.

**What this improves over the default:** word widths reflect the actual font
being measured (including any OS/browser-specific font substitution), not a
generic sans-serif approximation - this is the single biggest source of
wrap divergence from a real browser for most reports.

**What this does *not* do:**

- It is not a full DOM layout pass. It doesn't walk actual rendered line
  boxes (e.g. via `Range.getClientRects()`), so it can't capture
  browser-specific line-breaking rules for punctuation, kerning between
  arbitrary adjacent character pairs across a word boundary, or
  ligatures/shaping for complex scripts.
- It still uses `ApproximateTextMeasurer`'s greedy, single-word-per-overflow
  wrap decision - a genuinely different (e.g. Knuth-Plass) line-breaking
  algorithm would produce different results even with identical word widths.
- If your reports need to match a real browser's rendering *exactly*, the
  only guaranteed-exact approach is rendering the actual generated HTML in a
  browser and reading back real layout metrics - a heavier, slower approach
  than word-width sampling. This sample is a middle ground: meaningfully
  closer than the default, without that full cost.

## Performance and lifetime

Launching a browser is expensive (hundreds of milliseconds to seconds).
`PlaywrightTextMeasurer.CreateAsync()` launches headless Chromium **once**;
create a single instance and reuse it for an entire document's pagination
(ideally across many documents in a long-lived process), then dispose it via
`DisposeAsync()` when done. Constructing a new instance per `Measure` call
would make pagination dramatically slower than the zero-dependency default.

## Threading note

`ITextMeasurer.Measure` is a synchronous method - `LayoutEngine` calls it
inline, never awaited - but Playwright's .NET API is entirely async.
`PlaywrightTextMeasurer.Measure` bridges this by blocking on the async call
(`.GetAwaiter().GetResult()`). This is safe from a console app, a background
worker, or an ASP.NET Core request (none of these capture a
`SynchronizationContext`), but avoid this pattern - or adapt it - if you're
calling into pagination from a UI thread that does.

## Running this sample

```shell
dotnet run --project samples/TerraFluent.Html.Reporting.Sample.PlaywrightMeasurer
```

The first run may need Playwright's browser binaries installed:

```shell
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

(or the platform equivalent - see
[Playwright's .NET installation docs](https://playwright.dev/dotnet/docs/intro)).
