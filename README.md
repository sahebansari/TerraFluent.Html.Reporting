# TerraFluent.Html.Reporting

TerraFluent.Html.Reporting is a fluent, dependency-free .NET library for
building paginated, print-ready HTML reports. It gives you fixed page sizes,
headers and footers, measured content flow, table/list pagination, and
self-contained HTML/CSS that opens in a browser or can be printed to PDF.

Use it when you want PDF-style report layout without taking a dependency on a
PDF engine. The library targets `netstandard2.0` and `net10.0`.

## What's New in 1.1.0

- `AddBarcode(value, moduleWidthPx, heightPx, quietZoneModules)` on content,
  header/footer, and row-column builders: generates a Code 128 barcode
  natively as a PNG image - no external barcode library or web service
  involved. Returns the same image builder as `AddImage`, so it supports
  alignment, margin, and padding modifiers. See
  [Content Elements: Barcode](docs/03-content-elements.md#barcode) and the
  [cookbook recipe](docs/10-cookbook.md#a-barcode-in-the-header-invoice-number).

See [CHANGELOG.md](CHANGELOG.md#110---2026-07-03) for the full release notes.

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

## Samples

Run the sample project to generate twelve HTML reports:

```shell
dotnet run --project samples/TerraFluent.Html.Reporting.Sample
```

The sample output includes getting started, styling, images, tables, lists,
page breaks, raw HTML, warnings, invoices, certificates, row layouts, and an
invoice with a barcode in the header.

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

## Repository Layout

- [src/TerraFluent.Html.Reporting](src/TerraFluent.Html.Reporting) - the library.
- [samples/TerraFluent.Html.Reporting.Sample](samples/TerraFluent.Html.Reporting.Sample) - runnable examples.
- [tests/TerraFluent.Html.Reporting.Tests](tests/TerraFluent.Html.Reporting.Tests) - unit and pagination tests.
- [tests/TerraFluent.Html.Reporting.BrowserTests](tests/TerraFluent.Html.Reporting.BrowserTests) - browser print-layout checks.
- [tests/TerraFluent.Html.Reporting.NetStandardConsumer](tests/TerraFluent.Html.Reporting.NetStandardConsumer) - `netstandard2.0` consumer smoke project.
- [docs](docs/README.md) - full documentation.

## Status

This library is stable for public use. The current version is `1.1.0`. See
[CHANGELOG.md](CHANGELOG.md) for release history and
[known limitations](docs/12-faq-troubleshooting.md#known-limitations) for the
current boundaries.

## License

TerraFluent.Html.Reporting is licensed under the MIT License. You can use it
in personal, commercial, and open-source projects, modify it, and redistribute
it, provided the original license notice is included.

See [LICENSE](LICENSE) for the full license text.
