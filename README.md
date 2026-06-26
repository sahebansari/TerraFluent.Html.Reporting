# FluentHtmlReport

A fluent .NET library for generating paginated, print-ready HTML reports - the
same kind of fixed-page layout you get from a PDF report generator (TerraPDF, iText,
QuestPDF), but outputting HTML/CSS so the result opens in any browser and
prints (or "Print to PDF") with correct page breaks.

```csharp
var report = ReportDocument.Create(PageSize.A4, PageOrientation.Portrait)
    .SetMargins(40, 40, 60, 60)
    .Header(h => h.AddText("Monthly Sales Report").AlignCenter().Bold())
    .Footer(f => f.AddPageNumber("Page {page} of {totalPages}"))
    .Content(c =>
    {
        c.AddHeading("Sales Summary", HeadingLevel.H1);
        c.AddParagraph("This report summarizes...");
        c.AddImage("logo.png", widthPx: 120, heightPx: 60);
        c.AddTable(table =>
        {
            table.AddColumns("Product", "Qty", "Revenue");
            table.AddRow("Widget A", "120", "$2,400");
        });
    })
    .Build();

string html = report.RenderHtml();
```

## Documentation

Full documentation lives in [docs/](docs/README.md):

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

The [samples/FluentHtmlReport.Sample](samples/FluentHtmlReport.Sample) project
is a runnable companion to the docs - run it to generate eleven example
reports as HTML files you can open straight in a browser.

## Repository layout

- [src/FluentHtmlReport](src/FluentHtmlReport) - the library itself.
- [samples/FluentHtmlReport.Sample](samples/FluentHtmlReport.Sample) - runnable scenarios demonstrating most of the API.
- [tests/FluentHtmlReport.Tests](tests/FluentHtmlReport.Tests) - unit and pagination tests.
- [docs/](docs/README.md) - the documentation set linked above.
- [CHANGELOG.md](CHANGELOG.md) - notable changes per release.

## Status

This library is pre-1.0 and under active development. The public API may
still change between minor versions until 1.0. See [CHANGELOG.md](CHANGELOG.md)
for what's changed and [docs/12-faq-troubleshooting.md](docs/12-faq-troubleshooting.md#known-limitations)
for current limitations.

## License

MIT - see [LICENSE](LICENSE).
