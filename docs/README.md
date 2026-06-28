# TerraFluent.Html.Reporting Documentation

TerraFluent.Html.Reporting is a fluent, zero-dependency .NET library for generating
paginated, print-ready HTML reports: fixed page sizes, repeating headers and
footers, and content (text, images, tables, lists) that is measured and split
across pages the way a PDF report generator would, but rendered as a single
self-contained HTML document instead.

## Where to start

If you're new to the library, read these in order:

1. **[Getting Started](01-getting-started.md)** - install the package, build
   your first report, understand the four-step pipeline (`Create` →
   `Header`/`Footer`/`Content` → `Build` → `RenderHtml`).
2. **[Core Concepts](02-core-concepts.md)** - the document model, the
   immutable "rebuild and replace" pattern behind every fluent modifier, units
   (everything is a CSS pixel), and how a document goes from model → layout →
   HTML.
3. **[Content Elements](03-content-elements.md)** - the full reference for
   every `Add*` method and what it produces: paragraphs, headings, images,
   lists, rules, spacers, page breaks, raw HTML, and page-number text.

## Reference by topic

| Topic | Read this when... |
|---|---|
| **[Styling](04-styling.md)** | You want to control fonts, colors, alignment, margins, or padding on text and images. |
| **[Tables](05-tables.md)** | You're building a table and need to understand column widths, striping, borders, per-cell styles, or what happens when a row doesn't fit on a page. |
| **[Rows and Columns](06-rows-and-columns.md)** | You want side-by-side content (e.g. a logo next to a title) in a header, footer, or the body. |
| **[Pagination and Layout](07-pagination-and-layout.md)** | You want to understand *why* the engine breaks pages where it does, what `LayoutWarning` means, or you're implementing a custom element. |
| **[Rendering](08-rendering.md)** | You need streaming output for a large report, an HTML fragment to embed in an existing page, or you want to know exactly what HTML/CSS gets produced. |
| **[Text Measurement](09-text-measurement.md)** | Pagination needs to be pixel-exact and the bundled approximate measurer isn't good enough. |
| **[Cookbook](10-cookbook.md)** | You want a working recipe to copy - invoices, certificates, multi-page tables, raw HTML, async rendering, and more. |
| **[Extending the Library](11-extending.md)** | You want to plug in a custom text measurer, a custom renderer, or a brand-new element type. |
| **[FAQ / Troubleshooting](12-faq-troubleshooting.md)** | Something isn't behaving the way you expected. |
| **[Release Checklist](13-release-checklist.md)** | You're preparing, validating, or publishing a NuGet release. |

## Conventions used throughout

- **Units.** Every length in this library - page size, margins, font size,
  padding - is a CSS pixel (`px`), at 96px = 1 inch. There are no points,
  ems, or percentages anywhere in the public API; see
  [Core Concepts § Units](02-core-concepts.md#units-everything-is-a-css-pixel).
- **Immutability.** Document model types (`TextStyle`, `ReportImage`, `Row`,
  `RowColumn`, ...) are immutable. Every fluent modifier (`.Bold()`,
  `.Margin(8)`, ...) produces a new instance and swaps it into the owning
  builder's element list; see
  [Core Concepts § The rebuild-and-replace pattern](02-core-concepts.md#the-rebuild-and-replace-pattern).
- **`Add*` returns a builder.** Every `Add*` method on a content/section
  builder returns a small, short-lived builder or handle (`TextElementBuilder`,
  `ImageElementBuilder`, `RowHandle`, `RowColumnHandle`, ...) so you can chain
  modifiers directly onto the call that added the element, e.g.
  `c.AddImage("logo.png").AlignCenter().Margin(8)`.
- **Code samples are runnable.** Most snippets in this documentation are
  adapted from the [samples/TerraFluent.Html.Reporting.Sample](../samples/TerraFluent.Html.Reporting.Sample)
  project. Run it (`dotnet run --project samples/TerraFluent.Html.Reporting.Sample`) to
  generate the corresponding HTML files and open them in a browser.
