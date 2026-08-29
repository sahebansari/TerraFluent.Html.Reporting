# TerraFluent.Html.Reporting

TerraFluent.Html.Reporting is a fluent, dependency-free .NET library for
building paginated, print-ready HTML reports. It gives you fixed page sizes,
headers and footers, measured content flow, table/list pagination, and
self-contained HTML/CSS that opens in a browser or can be printed to PDF.

Use it when you want PDF-style report layout without taking a dependency on a
PDF engine. The library targets `netstandard2.0` and `net10.0`.

[![NuGet](https://img.shields.io/nuget/v/TerraFluent.Html.Reporting.svg)](https://www.nuget.org/packages/TerraFluent.Html.Reporting)
[![NuGet downloads](https://img.shields.io/nuget/dt/TerraFluent.Html.Reporting.svg)](https://www.nuget.org/packages/TerraFluent.Html.Reporting)
[![CI](https://github.com/sahebansari/TerraFluent.Html.Reporting/actions/workflows/ci.yml/badge.svg)](https://github.com/sahebansari/TerraFluent.Html.Reporting/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/sahebansari/TerraFluent.Html.Reporting/blob/master/LICENSE)

📚 **Full documentation, guides, and samples:** 🌐 [https://terrafluent.dev/html/](https://terrafluent.dev/html/)

[![View Sample Reports](https://img.shields.io/badge/View-Sample%20Reports-2f4858?style=for-the-badge)](https://sahebansari.github.io/TerraFluent.Html.Reporting/SampleReports/index.html)

## What's New in 1.2.0

- `AddColumns(columnCount, configure, columnGapPx)`: opt-in, conservative
  multi-column ("newspaper-style") content blocks - equal-width columns,
  fill-then-wrap, splitting across a column boundary just like content
  already splits across a page boundary. See
  [Rows and Columns: Multi-column sections](docs/06-rows-and-columns.md#multi-column-sections-addcolumns).
- `AddQrCode(value, moduleWidthPx, quietZoneModules)`: generates a QR code
  natively as a PNG, matching `AddBarcode`'s API shape - no external library
  or web service involved.
- `TextStyle.Direction` for right-to-left text (`dir`/CSS `direction`), and
  `ReportDocumentBuilder.EmbedFont(...)` to embed a custom font as a base64
  `@font-face` rule. See [Styling](docs/04-styling.md#text-direction).
- `ReportDocumentBuilder.Title(...)` sets the generated HTML's `<title>`.
- `LayoutWarning` gained structured `Reason`/`ElementType`/`ElementIndex`
  fields (filterable via the new `LayoutWarningReason` enum) instead of only
  a free-form message, and a table/row's auto-width column collapsing to
  0px is no longer silent - it now raises a `ColumnWidthCollapsed` warning,
  with an opt-in `UseStrictLayoutValidation()`/`ColumnWidthOverflowMode` to
  throw instead.
- A new [Supported Composition Patterns](docs/15-composition-patterns.md)
  reference, a reference Playwright-backed `ITextMeasurer` sample, and a
  minimal preview/debug console harness - see
  [Repository Layout](#repository-layout) below.

See [CHANGELOG.md](CHANGELOG.md#120---2026-08-30) for the full release notes.

## Install

```shell
dotnet add package TerraFluent.Html.Reporting
```

## Quick Start

```csharp
using TerraFluent.Html.Reporting.Model;

var report = ReportDocument.Create(PageSize.A4, PageOrientation.Portrait)
    .SetMargins(40, 40, 60, 60)
    .Header(header => header
        .AddText("Monthly Sales Report")
        .AlignCenter()
        .Bold())
    .Footer(footer => footer.AddPageNumber("Page {page} of {totalPages}"))
    .Content(content =>
    {
        content.AddHeading("Sales Summary", HeadingLevel.H1);
        content.AddParagraph("This report summarizes sales activity for the period.");
        content.AddImage("logo.png", widthPx: 120, heightPx: 60);

        content.AddTable(table =>
        {
            table.AddColumns("Product", "Qty", "Revenue");
            table.AddRow("Widget A", "120", "$2,400");
            table.AddRow("Widget B", "85", "$1,700");
        });
    })
    .Build();

var html = report.RenderHtml();
report.RenderHtmlDocument("monthly-sales.html");
```

Open the generated HTML in a browser, or use the browser's print dialog to
print or save it as PDF.

## What It Supports

- Fixed page geometry: A4, Letter, Legal, portrait/landscape, or custom sizes.
- Repeating headers and footers, including page number templates.
- Paragraphs, headings, images, tables, lists, rules, spacers, page breaks, raw
  HTML, side-by-side row layouts, and opt-in multi-column ("newspaper-style")
  sections.
- Natively generated Code 128 barcode and QR code images - no external
  library or web service - handy for an invoice number in the header or a
  tracking number on a label.
- Table cell `ColSpan`/`RowSpan` for merged header/summary cells or grouped
  categories.
- Right-to-left text direction and embedded custom fonts.
- Fluent styling for text, margins, padding, alignment, images, rows, and
  tables.
- Pagination with line-level paragraph splitting, table row splitting, repeated
  table headers, and numbered-list continuation.
- Layout warnings, categorized via `LayoutWarningReason`, for content that
  cannot fit on an empty page or an auto-width table/row column that collapsed
  to 0px.
- Document title, rendered into the generated HTML's `<title>`.
- Streaming render APIs and async file rendering for larger reports.
- Extension points for custom elements, renderers, and text measurement.

## Text Measurement

Pagination depends on measuring text before it is rendered. The built-in
`ApproximateTextMeasurer` keeps the core package zero-dependency by using
generic character-width tables. It is suitable for many reports, but it is not
a pixel-perfect browser text layout engine.

If page breaks must match a specific rendering engine exactly, implement
`ITextMeasurer` and pass it to `UseTextMeasurer(...)` when building the
document.

## Tested Against Real Browsers

Beyond unit tests for the pagination math itself, CI renders generated
reports in **real, headless Chromium, Firefox, and WebKit** (via
[Playwright](https://playwright.dev/dotnet/)) and asserts every page's
rendered geometry matches the requested page size exactly under
`@media print` - the same media browsers use for "Print to PDF". See
[docs/08-rendering.md#tested-against-real-browsers](docs/08-rendering.md#tested-against-real-browsers).

## Samples

Run the sample project to generate eighteen HTML reports:

```shell
dotnet run --project samples/TerraFluent.Html.Reporting.Sample
```

The sample output includes getting started, styling, images, tables, lists,
page breaks, raw HTML, warnings, invoices, certificates, row layouts, an
invoice with a barcode in the header, a table using column/row spans, a
multi-column newsletter layout, a shipping label combining a barcode and a
QR code, embedded custom fonts, right-to-left text, and column-width
diagnostics.

Two more small sample projects demonstrate specific extension points -
see [Repository Layout](#repository-layout) below.

## Documentation

- [Getting Started](docs/01-getting-started.md)
- [Core Concepts](docs/02-core-concepts.md)
- [Content Elements](docs/03-content-elements.md)
- [Styling](docs/04-styling.md)
- [Tables](docs/05-tables.md)
- [Rows and Columns](docs/06-rows-and-columns.md)
- [Pagination and Layout](docs/07-pagination-and-layout.md)
- [Rendering](docs/08-rendering.md)
- [Text Measurement](docs/09-text-measurement.md)
- [Cookbook](docs/10-cookbook.md)
- [Extending the Library](docs/11-extending.md)
- [FAQ / Troubleshooting](docs/12-faq-troubleshooting.md)
- [Release Checklist](docs/13-release-checklist.md)
- [Supported Composition Patterns](docs/15-composition-patterns.md)

## Repository Layout

- [src/TerraFluent.Html.Reporting](src/TerraFluent.Html.Reporting) - the library.
- [samples/TerraFluent.Html.Reporting.Sample](samples/TerraFluent.Html.Reporting.Sample) - runnable examples.
- [samples/TerraFluent.Html.Reporting.Sample.PlaywrightMeasurer](samples/TerraFluent.Html.Reporting.Sample.PlaywrightMeasurer) - a reference `ITextMeasurer` backed by headless Chromium.
- [samples/TerraFluent.Html.Reporting.Sample.Preview](samples/TerraFluent.Html.Reporting.Sample.Preview) - a minimal preview/debug console harness.
- [tests/TerraFluent.Html.Reporting.Tests](tests/TerraFluent.Html.Reporting.Tests) - unit and pagination tests.
- [tests/TerraFluent.Html.Reporting.BrowserTests](tests/TerraFluent.Html.Reporting.BrowserTests) - browser print-layout checks.
- [tests/TerraFluent.Html.Reporting.NetStandardConsumer](tests/TerraFluent.Html.Reporting.NetStandardConsumer) - `netstandard2.0` consumer smoke project.
- [docs](docs/README.md) - full documentation.

## Status

This library is stable for public use. The current version is `1.2.0`. See
[CHANGELOG.md](CHANGELOG.md) for release history and
[known limitations](docs/12-faq-troubleshooting.md#known-limitations) for the
current boundaries.

## License

TerraFluent.Html.Reporting is licensed under the MIT License. You can use it
in personal, commercial, and open-source projects, modify it, and redistribute
it, provided the original license notice is included.

See [LICENSE](LICENSE) for the full license text.
