# TerraFluent.Html.Reporting

TerraFluent.Html.Reporting is a fluent, dependency-free .NET library for
building paginated, print-ready HTML reports. It gives you fixed page sizes,
headers and footers, measured content flow, table/list pagination, and
self-contained HTML/CSS that opens in a browser or can be printed to PDF.

Use it when you want PDF-style report layout without taking a dependency on a
PDF engine. The library targets `netstandard2.0` and `net10.0`.

[![View Sample Reports](https://img.shields.io/badge/View-Sample%20Reports-2f4858?style=for-the-badge)](https://sahebansari.github.io/TerraFluent.Html.Reporting/SampleReports/index.html)

## What's New in 1.2.0

- `AddColumns(columnCount, configure, columnGapPx)`: opt-in, conservative
  multi-column ("newspaper-style") content blocks - equal-width columns,
  fill-then-wrap, splitting across a column boundary just like content
  already splits across a page boundary. See
  [Rows and Columns: Multi-column sections](https://github.com/sahebansari/TerraFluent.Html.Reporting/blob/master/docs/06-rows-and-columns.md#multi-column-sections-addcolumns).
- `AddQrCode(value, moduleWidthPx, quietZoneModules)`: generates a QR code
  natively as a PNG, matching `AddBarcode`'s API shape - no external library
  or web service involved.
- `TextStyle.Direction` for right-to-left text (`dir`/CSS `direction`), and
  `ReportDocumentBuilder.EmbedFont(...)` to embed a custom font as a base64
  `@font-face` rule.
- `ReportDocumentBuilder.Title(...)` sets the generated HTML's `<title>`.
- `LayoutWarning` gained structured `Reason`/`ElementType`/`ElementIndex`
  fields (filterable via the new `LayoutWarningReason` enum) instead of only
  a free-form message, and a table/row's auto-width column collapsing to
  0px is no longer silent - it now raises a `ColumnWidthCollapsed` warning,
  with an opt-in `UseStrictLayoutValidation()`/`ColumnWidthOverflowMode` to
  throw instead.

See [CHANGELOG.md](https://github.com/sahebansari/TerraFluent.Html.Reporting/blob/master/CHANGELOG.md#120---2026-08-30)
for the full release notes.

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
[docs/08-rendering.md](https://github.com/sahebansari/TerraFluent.Html.Reporting/blob/master/docs/08-rendering.md#tested-against-real-browsers).

## Documentation

Full documentation and runnable samples live in the GitHub repository:

- [Documentation index](https://github.com/sahebansari/TerraFluent.Html.Reporting/tree/master/docs)
- [Getting started](https://github.com/sahebansari/TerraFluent.Html.Reporting/blob/master/docs/01-getting-started.md)
- [Cookbook](https://github.com/sahebansari/TerraFluent.Html.Reporting/blob/master/docs/10-cookbook.md)
- [FAQ / Troubleshooting](https://github.com/sahebansari/TerraFluent.Html.Reporting/blob/master/docs/12-faq-troubleshooting.md)
- [Supported Composition Patterns](https://github.com/sahebansari/TerraFluent.Html.Reporting/blob/master/docs/15-composition-patterns.md)
- [Source repository](https://github.com/sahebansari/TerraFluent.Html.Reporting)

## Status

This library is stable for public use. The current version is `1.2.0`. The
default text measurer is approximate; supply a custom `ITextMeasurer` when
pagination must match a specific rendering engine pixel-for-pixel.

## License

TerraFluent.Html.Reporting is licensed under the MIT License. You can use it
in personal, commercial, and open-source projects, modify it, and redistribute
it, provided the original license notice is included.
