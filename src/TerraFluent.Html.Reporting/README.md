# TerraFluent.Html.Reporting

TerraFluent.Html.Reporting is a fluent, dependency-free .NET library for
building paginated, print-ready HTML reports. It gives you fixed page sizes,
headers and footers, measured content flow, table/list pagination, and
self-contained HTML/CSS that opens in a browser or can be printed to PDF.

Use it when you want PDF-style report layout without taking a dependency on a
PDF engine. The library targets `netstandard2.0` and `net10.0`.

[![View Sample Reports](https://img.shields.io/badge/View-Sample%20Reports-2f4858?style=for-the-badge)](https://sahebansari.github.io/TerraFluent.Html.Reporting/SampleReports/index.html)

## What's New in 1.1.1

- `AddBarcode(value, moduleWidthPx, heightPx, quietZoneModules)` on content,
  header/footer, and row-column builders: generates a Code 128 barcode
  natively as a PNG image - no external barcode library or web service
  involved. Returns the same image builder as `AddImage`, so it supports
  alignment, margin, and padding modifiers. See
  [Content Elements: Barcode](https://github.com/sahebansari/TerraFluent.Html.Reporting/blob/master/docs/03-content-elements.md#barcode)
  and the
  [cookbook recipe](https://github.com/sahebansari/TerraFluent.Html.Reporting/blob/master/docs/10-cookbook.md#a-barcode-in-the-header-invoice-number).
- Table cell `ColSpan`/`RowSpan`, for a merged header/summary cell or a
  category cell grouping several rows. A `RowSpan` group is treated as one
  atomic unit during pagination - see
  [Tables: Column and row spans](https://github.com/sahebansari/TerraFluent.Html.Reporting/blob/master/docs/05-tables.md#column-and-row-spans)
  and the
  [cookbook recipe](https://github.com/sahebansari/TerraFluent.Html.Reporting/blob/master/docs/10-cookbook.md#grouping-rows-with-rowspan).

See [CHANGELOG.md](https://github.com/sahebansari/TerraFluent.Html.Reporting/blob/master/CHANGELOG.md#111---2026-07-03)
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
  HTML, and side-by-side row layouts.
- Natively generated Code 128 barcode images - no external library or web
  service - handy for an invoice number in the header or a tracking number on
  a label.
- Table cell `ColSpan`/`RowSpan` for merged header/summary cells or grouped
  categories.
- Fluent styling for text, margins, padding, alignment, images, rows, and
  tables.
- Pagination with line-level paragraph splitting, table row splitting, repeated
  table headers, and numbered-list continuation.
- Layout warnings for content that cannot fit on an empty page.
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

## Documentation

Full documentation and runnable samples live in the GitHub repository:

- [Documentation index](https://github.com/sahebansari/TerraFluent.Html.Reporting/tree/master/docs)
- [Getting started](https://github.com/sahebansari/TerraFluent.Html.Reporting/blob/master/docs/01-getting-started.md)
- [Cookbook](https://github.com/sahebansari/TerraFluent.Html.Reporting/blob/master/docs/10-cookbook.md)
- [FAQ / Troubleshooting](https://github.com/sahebansari/TerraFluent.Html.Reporting/blob/master/docs/12-faq-troubleshooting.md)
- [Source repository](https://github.com/sahebansari/TerraFluent.Html.Reporting)

## Status

This library is stable for public use. The current version is `1.1.1`. The
default text measurer is approximate; supply a custom `ITextMeasurer` when
pagination must match a specific rendering engine pixel-for-pixel.

## License

TerraFluent.Html.Reporting is licensed under the MIT License. You can use it
in personal, commercial, and open-source projects, modify it, and redistribute
it, provided the original license notice is included.
