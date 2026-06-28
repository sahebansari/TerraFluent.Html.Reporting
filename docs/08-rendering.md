# Rendering

Rendering is the second pass: it turns an already-paginated `LayoutResult`
(see [Pagination and Layout](07-pagination-and-layout.md)) into HTML. By the
time a renderer runs, every page's geometry - and therefore the total page
count - is already known, which is what lets header/footer elements resolve
`{page}`/`{totalPages}` tokens correctly.

## The APIs on `ReportDocument`

You usually don't call the layout/render stages separately - `ReportDocument`
exposes five convenience methods that do both:

| Method | Output | Use when... |
|---|---|---|
| `RenderHtml(renderer?, ct?)` | `string` (full document) | You want the complete HTML in memory, e.g. to return from a web endpoint. |
| `RenderHtmlDocument(filePath, renderer?, ct?)` | writes to a file | You want to stream straight to disk without holding the full string in memory. |
| `RenderHtmlDocumentAsync(filePath, renderer?, ct?)` | `Task`, writes to a file | Same as above, but doesn't block a thread-pool thread on the final flush/dispose - see below. |
| `RenderFragment(renderer?, ct?)` | `string` (page `<div>`s only) | You're embedding the report inside an existing HTML page. |

All five accept an optional `IHtmlReportRenderer` (defaulting to
`HtmlReportRenderer.Default`) and a `CancellationToken`, checked periodically
during both pagination and rendering so a very large document can be
aborted - e.g. when the originating web request is canceled.

```csharp
string html = report.RenderHtml();
await report.RenderHtmlDocumentAsync("report.html", cancellationToken: ct);
string fragment = report.RenderFragment(); // for embedding
```

**Why is `RenderHtmlDocumentAsync` async at all if pagination/rendering are
synchronous CPU work?** Because there's no I/O to `await` until the final
flush to disk - the method exists so the flush, and the writer's disposal, do
not block a thread-pool thread in an async call chain (e.g. inside an ASP.NET
request handler), not because layout or HTML generation themselves are
asynchronous.

## `IHtmlReportRenderer`

[`IHtmlReportRenderer`](../src/TerraFluent.Html.Reporting/Rendering/IHtmlReportRenderer.cs)
is the renderer contract:

```csharp
public interface IHtmlReportRenderer
{
    string RenderDocument(LayoutResult layout);
    string RenderFragment(LayoutResult layout);
    void RenderDocumentTo(TextWriter writer, LayoutResult layout, CancellationToken cancellationToken = default);
    void RenderFragmentTo(TextWriter writer, LayoutResult layout, CancellationToken cancellationToken = default);
}
```

[`HtmlReportRenderer`](../src/TerraFluent.Html.Reporting/Rendering/HtmlReportRenderer.cs)
(accessible as the shared, stateless `HtmlReportRenderer.Default`) is the
only implementation shipped today. The `...To(TextWriter, ...)` overloads
write one page at a time rather than building the whole document as a single
in-memory string first - for a report with many thousands of pages, this
bounds peak memory to roughly one page's HTML rather than the entire
document's. The non-streaming `RenderDocument`/`RenderFragment` overloads are
implemented as a thin wrapper: write to a `StringWriter`, then return its
contents.

See [Extending the Library § Custom renderer](11-extending.md#a-custom-ihtmlreportrenderer)
if you want to implement this interface yourself (e.g. to emit a different
page-wrapper structure, or PDF-specific markup).

## What the HTML looks like

`RenderDocumentTo` produces a single self-contained document - inline
`<style>`, no external CSS or JS, no external image references (every
`ReportImage` is base64-encoded inline). The example below is for
`PageSize.A4` (210mm x 297mm, converted to pixels at 96px/inch):

```html
<!DOCTYPE html><html><head><meta charset="utf-8" /><title>Report</title>
<style>
  @page { size: 793.701px 1122.52px; margin: 0; }
  html, body { margin: 0; padding: 0; }
  body { background: #e8e8e8; font-family: Segoe UI, Arial, sans-serif; }
  .fhr-page { position: relative; width: 793.7px; height: 1122.52px; background: #ffffff;
    overflow: hidden; margin: 0 auto 16px auto; box-shadow: 0 0 6px rgba(0,0,0,0.25);
    page-break-after: always; break-after: page; }
  .fhr-page:last-child { page-break-after: auto; break-after: auto; margin-bottom: 0; }
  @media print { body { background: none; } .fhr-page { margin: 0; box-shadow: none; } }
</style>
</head><body>
  <div class="fhr-page"> ... one absolutely-positioned element per placed fragment ... </div>
  <div class="fhr-page"> ... </div>
</body></html>
```

Key points:

- **`@page { size: ...; margin: 0; }`** sets the print page size to exactly
  match the document's `PageSize`. Page margins are *not* expressed as
  `@page margin` - they're baked into each element's absolute `left`/`top`
  position instead, so what you see on screen (a white page with a drop
  shadow) is pixel-identical to what prints.
- **One `.fhr-page` `<div>` per page**, sized exactly to `PageSize`, with
  `overflow:hidden` so any force-placed, overflowing content (see
  [`LayoutWarning`](07-pagination-and-layout.md#layoutwarning-when-content-doesnt-fit))
  is clipped visually rather than spilling into the next page's div.
- **`page-break-after: always`** (plus the modern `break-after: page`) on
  every page except the last is a redundant signal for browsers that don't
  fully honor `@page` sizing during print - belt-and-suspenders, since the
  exact-pixel page divs are normally enough on their own.
- **Every element renders as its own absolutely-positioned tag** (`<p>`,
  `<h1>`-`<h6>`, `<img>`, `<table>`, `<ul>`/`<ol>`, or a styled `<div>`) at the
  `left`/`top`/`width`/`height` the layout engine computed - the renderer has
  no pagination logic of its own; it only translates each `ElementPlacement`
  from section-relative to page-absolute coordinates (see
  [Pagination and Layout § The output](07-pagination-and-layout.md#the-output-layoutresult))
  and calls `IReportElement.RenderHtml`.
- **All CSS pixel values use the invariant culture** (`123.45px`, never
  `123,45px`) - see [`CssFormat`](../src/TerraFluent.Html.Reporting/Rendering/CssFormat.cs) -
  so generated reports are correct regardless of the server's locale.
- **All user-supplied text is HTML-encoded** before being written (again via
  `CssFormat.Encode`, i.e. `WebUtility.HtmlEncode`) - the one exception is
  `RawHtml`, which is emitted verbatim by design (see
  [Content Elements § Raw HTML](03-content-elements.md#raw-html)).

`RenderFragmentTo` emits the page styles and page `<div>`s without the
`<html>`/`<head>`/`<body>` wrapper. Its stylesheet omits the document-level
`html`/`body` reset and background rules, so embedding a report does not
change the host page's margins, background, or font.

## Printing to PDF

Because the HTML is built around exact-pixel `@page` sizing and one `<div>`
per page, a browser's native "Print to PDF" (or a headless-browser print API,
e.g. Playwright/Puppeteer) reproduces the same page breaks you see when
viewing the HTML directly - there's no separate PDF-specific code path in
this library. If you need exact pixel-perfect text wrapping in that PDF, see
[Text Measurement](09-text-measurement.md) for why the bundled measurer alone
may not guarantee that.

## Where to go next

- [Text Measurement](09-text-measurement.md) for the seam that determines
  how accurately layout matches real browser rendering.
- [Extending the Library](11-extending.md) for writing a custom
  `IHtmlReportRenderer`.
- [Cookbook § Streaming a large report](10-cookbook.md#streaming-a-large-report-to-disk-asynchronously)
  for a runnable example of the async file API.
