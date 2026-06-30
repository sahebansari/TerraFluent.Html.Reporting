# Cookbook

Working recipes you can copy and adapt. Most are trimmed versions of the
scenarios in [samples/TerraFluent.Html.Reporting.Sample/Scenarios](../samples/TerraFluent.Html.Reporting.Sample/Scenarios) -
run `dotnet run --project samples/TerraFluent.Html.Reporting.Sample` to generate the
full versions as HTML and open them in a browser.

## A Multi-Page Report with Header, Footer, and a Table That Spans Pages

The "kitchen sink" example: a repeating header/footer with page numbers, a
heading, a paragraph, an image, a 45-row table that spans several pages
(header repeated, rows split correctly), and a numbered list.

```csharp
using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;

var products = new[] { "Widget A", "Widget B", "Gadget C", "Gizmo D" };
var random = new Random(42);

var report = ReportDocument.Create(PageSize.A4, PageOrientation.Portrait)
    .SetMargins(40, 40, 60, 60)
    .Header(h => h.AddText("Monthly Sales Report").AlignCenter().Bold().FontSize(16))
    .Footer(f => f.AddPageNumber("Page {page} of {totalPages}").AlignCenter())
    .Content(c =>
    {
        c.AddHeading("Sales Summary", HeadingLevel.H1);
        c.AddParagraph(
            "This report summarizes sales activity across all regions for the current " +
            "period. The table below spans multiple pages: the header row repeats on " +
            "every continuation page, and a row too tall to fit is split mid-row.");
        c.AddImage("logo.png", widthPx: 72);
        c.AddRule();

        c.AddHeading("Detailed Line Items", HeadingLevel.H2);
        c.AddTable(table =>
        {
            table.AddColumns("Product", "Qty", "Revenue");
            for (var i = 0; i < 45; i++)
            {
                var product = products[i % products.Length];
                var qty = random.Next(10, 500);
                var revenue = qty * (decimal)(5 + random.NextDouble() * 45);
                table.AddRow(product, qty.ToString(), revenue.ToString("C2"));
            }
        });

        c.AddSpacer(12);
        c.AddHeading("Notes", HeadingLevel.H2);
        c.AddList(ListStyle.Numbered, new[]
        {
            "Revenue figures are pre-tax.",
            "Contact the finance team for a region-level breakdown.",
        });
    })
    .Build();

string html = report.RenderHtml();
```

See [Tables](05-tables.md) for the column-width and row-split rules at play,
and [Pagination and Layout](07-pagination-and-layout.md) for why the header
row repeats automatically. Full source:
[`GettingStartedScenario.cs`](../samples/TerraFluent.Html.Reporting.Sample/Scenarios/GettingStartedScenario.cs).

## Comparing Table Row-Split Behaviors

Put the same long row through both `TableStyle.RowSplitBehavior` options
side by side, on a deliberately small custom page size so the row is forced
to overflow:

```csharp
using TerraFluent.Html.Reporting.Model.Styling;

const string longNote =
    "This note is deliberately long, and the column it sits in deliberately " +
    "narrow, so it cannot fit in the remaining space and must be handled by " +
    "the table's row-split behavior.";

var report = ReportDocument.Create(PageSize.FromPixels(650, 480))
    .SetMargins(20)
    .Content(c =>
    {
        c.AddHeading("AllowSplitWithContinuedHeader (the default)", HeadingLevel.H2);
        c.AddTable(
            table =>
            {
                table.AddColumn("Item", widthPx: 80);
                table.AddColumn("Notes", widthPx: 220);
                table.AddRow("Item 1", "Short note.");
                table.AddRow("Item 2", longNote);
                table.AddRow("Item 3", "Another short note.");
            },
            TableStyle.Default.With(rowSplitBehavior: RowSplitBehavior.AllowSplitWithContinuedHeader));

        c.AddPageBreak();
        c.AddHeading("KeepRowIntact", HeadingLevel.H2);
        c.AddTable(
            table =>
            {
                table.AddColumn("Item", widthPx: 80);
                table.AddColumn("Notes", widthPx: 220);
                table.AddRow("Item 1", "Short note.");
                table.AddRow("Item 2", longNote);
                table.AddRow("Item 3", "Another short note.");
            },
            TableStyle.Default.With(rowSplitBehavior: RowSplitBehavior.KeepRowIntact));
    })
    .Build();
```

With `AllowSplitWithContinuedHeader`, "Item 2"'s note is truncated mid-sentence
and continues on the next page under a header marked "(continued)". With
`KeepRowIntact`, the whole row moves to the next page instead, leaving
trailing whitespace on the first page. See
[Tables: Row splitting](05-tables.md#row-splitting-rowsplitbehavior). Full
source: [`TableStylingScenario.cs`](../samples/TerraFluent.Html.Reporting.Sample/Scenarios/TableStylingScenario.cs).

## A Header and Footer with a Logo Row

A realistic invoice-style header (logo + company name side by side) and
footer (small logo + a line of fine print + page number), using `AddRow` in
both:

```csharp
var report = ReportDocument.Create(PageSize.A4)
    .SetMargins(40)
    .Header(h =>
    {
        h.AddRow(row =>
        {
            row.AddColumn(48, col => col.AddImage("logo.png", widthPx: 40))
                .Padding(topPx: 15, 0, 0, 0);
            row.AddColumn(col =>
            {
                col.AddText("Acme Corporation").Bold().FontSize(20);
                col.AddText("123 Market Street, Springfield, USA").FontSize(12);
            });
        });
        h.AddRule();
    })
    .Footer(f =>
    {
        f.AddRule();
        f.AddRow(row =>
        {
            row.AddColumn(32, col => col.AddImage("logo.png", widthPx: 24));
            row.AddColumn(col => col.AddText("Payment is due within 30 days.").FontSize(10));
        });
        f.AddPageNumber().AlignCenter().FontSize(9);
    })
    .Content(c => { /* ... */ })
    .Build();
```

The fixed-width logo column (`48px`/`32px`) leaves the company-name column to
auto-share the rest of the content width; `RowVerticalAlignment.Middle` (the
default) keeps the logo centered against the two-line text block next to it.
See [Rows and Columns](06-rows-and-columns.md). Full source:
[`SalesInvoiceScenario.cs`](../samples/TerraFluent.Html.Reporting.Sample/Scenarios/SalesInvoiceScenario.cs)
(also see [section A Complete Sales Invoice](#a-complete-sales-invoice) below for
the rest of this report).

## Detecting Content That Doesn't Fit (`LayoutWarning`)

An image taller than the entire page's content area, and unsplittable by
definition, triggers a `LayoutWarning` instead of silently clipping or
throwing:

```csharp
using TerraFluent.Html.Reporting.Layout;

var report = ReportDocument.Create(PageSize.FromPixels(400, 150))
    .SetMargins(10)
    .Content(c =>
    {
        c.AddHeading("Warnings", HeadingLevel.H2);
        c.AddParagraph("The image below is taller than this page's entire content area.");
        c.AddImage(oversizedImageBytes, "image/png", widthPx: 300, heightPx: 300);
    })
    .Build();

var layout = LayoutEngine.Paginate(report);
foreach (var warning in layout.Warnings)
{
    Console.WriteLine(warning); // "Page 1: A ReportImage required 300px but only ... was available ..."
}
```

Check `LayoutResult.Warnings` after pagination - e.g. to log a warning or
reject the report before it reaches a user - rather than only discovering
clipped content by eyeballing the rendered HTML. See
[Pagination and Layout: Warnings](07-pagination-and-layout.md#layoutwarning-when-content-doesnt-fit).
Full source: [`WarningsAndAsyncScenario.cs`](../samples/TerraFluent.Html.Reporting.Sample/Scenarios/WarningsAndAsyncScenario.cs).

## Streaming a Large Report to Disk Asynchronously

For a report large enough that holding the full HTML string in memory is
undesirable, render straight to a file with the async, streaming API instead
of `RenderHtml()`:

```csharp
await report.RenderHtmlDocumentAsync(
    Path.Combine(outputDir, "report.html"),
    cancellationToken: cancellationToken);
```

This writes one page's HTML at a time rather than building the entire
document as a single in-memory string, and the `async` signature exists so
the final flush/dispose doesn't block a thread-pool thread in an async call
chain (e.g. inside an ASP.NET request handler) - pagination and HTML
generation themselves are still synchronous, CPU-bound work. See
[Rendering: The APIs on `ReportDocument`](08-rendering.md#the-apis-on-reportdocument).
The sample project's [`Program.cs`](../samples/TerraFluent.Html.Reporting.Sample/Program.cs)
uses this API for every scenario it writes out.

## A Landscape Certificate

A single-page, landscape-oriented certificate, built entirely from centered
content with no header/footer:

```csharp
var report = ReportDocument.Create(PageSize.Letter, PageOrientation.Landscape)
    .SetMargins(50)
    .Content(c =>
    {
        c.AddImage("logo.png", widthPx: 48);
        c.AddSpacer(30);
        c.AddHeading("Certificate of Completion", HeadingLevel.H1).AlignCenter();
        c.AddSpacer(20);
        c.AddParagraph("This certifies that").AlignCenter();
        c.AddHeading("Jane Doe", HeadingLevel.H2).AlignCenter();
        c.AddParagraph("has successfully completed the TerraFluent.Html.Reporting advanced training course.").AlignCenter();
        c.AddSpacer(40);
        c.AddRule();
        c.AddParagraph("Issued June 23, 2026").AlignCenter();
    })
    .Build();
```

`PageOrientation.Landscape` swaps `PageSize.Letter`'s width/height (see
[Core Concepts: Page geometry](02-core-concepts.md#page-geometry-pagesize-margins-orientation)).
Full source: [`LandscapeCertificateScenario.cs`](../samples/TerraFluent.Html.Reporting.Sample/Scenarios/LandscapeCertificateScenario.cs).

## Images: File, Bytes, and Base64, with Aspect-Ratio Sizing

```csharp
// From a file path, explicit width and height (stretched to exactly 240x80):
c.AddImage("photo.png", widthPx: 240, heightPx: 80);

// From bytes, only width given - height derived from the source's aspect ratio:
c.AddImage(imageBytes, "image/png", widthPx: 360);

// From bytes, only height given - width derived from the source's aspect ratio:
c.AddImage(tallImageBytes, "image/png", heightPx: 200);

// From a data: URI or bare base64 payload (Content only):
c.AddImageFromBase64($"data:image/png;base64,{base64Source}", widthPx: 150, heightPx: 150);
```

See [Content Elements: Image](03-content-elements.md#image) for how the
missing dimension is derived (sniffed from the image's own header bytes) and
what happens if the format can't be recognized. Full source:
[`ImagesScenario.cs`](../samples/TerraFluent.Html.Reporting.Sample/Scenarios/ImagesScenario.cs).

## A Numbered List Spanning Multiple Pages

```csharp
c.AddParagraph(
    "This list has enough items to split across a page boundary; numbering " +
    "resumes correctly on the next page instead of restarting at 1.");
c.AddList(ListStyle.Numbered, Enumerable.Range(1, 60).Select(i => $"Numbered list entry #{i}"));
```

The list splits at item boundaries only (an item's own wrapped lines are
never separated), and the continuation fragment's `StartIndex` keeps the
`<ol start="...">` numbering correct. See
[Pagination and Layout: Paragraph splitting](07-pagination-and-layout.md#paragraph-splitting-widoworphan-control)
for the related text-splitting rules. Full source:
[`ListsScenario.cs`](../samples/TerraFluent.Html.Reporting.Sample/Scenarios/ListsScenario.cs).

## Forcing Chapter Breaks with `AddPageBreak`

```csharp
c.AddHeading("Chapter 1: Introduction", HeadingLevel.H1);
c.AddParagraph("...");
c.AddPageBreak();

c.AddHeading("Chapter 2: Methodology", HeadingLevel.H1);
c.AddParagraph("...");
c.AddPageBreak();
```

Each chapter starts on a fresh page regardless of how much room was left on
the previous one. A page break with nothing yet placed on the page is a
no-op, so this never produces a blank page between chapters even if a
chapter happens to end exactly at a page boundary already. See
[Content Elements: Page break](03-content-elements.md#page-break). Full
source: [`PageBreaksScenario.cs`](../samples/TerraFluent.Html.Reporting.Sample/Scenarios/PageBreaksScenario.cs).

## Injecting Raw HTML for Custom Markup

```csharp
c.AddRawHtml(
    "<div style=\"border:2px dashed #2f4858;border-radius:8px;padding:16px;background:#eef3f6;\">" +
    "<strong>Custom callout box</strong><br/>This entire block is raw HTML supplied by the " +
    "caller, including its own inline styles.</div>",
    heightPx: 110);
```

You supply the height because the layout engine cannot measure markup it
doesn't understand; it treats the block as opaque and unsplittable, exactly
like an oversized image (see
[section Detecting Content That Doesn't Fit](#detecting-content-that-doesnt-fit-layoutwarning)
above if it doesn't fit). The HTML is emitted **verbatim** with no encoding -
don't pass unsanitized end-user input here. See
[Content Elements: Raw HTML](03-content-elements.md#raw-html). Full source:
[`RawHtmlScenario.cs`](../samples/TerraFluent.Html.Reporting.Sample/Scenarios/RawHtmlScenario.cs).

## A Complete Sales Invoice

Putting headers/footers, rows, right-aligned table columns via per-cell
style overrides, and running totals together:

```csharp
using TerraFluent.Html.Reporting.Model.Styling;

var rightAlign = TextStyle.Default.With(alignment: TextAlignment.Right, marginBottomPx: 0);

var report = ReportDocument.Create(PageSize.A4)
    .SetMargins(40)
    .Header(h => { /* logo + company name row, see above */ })
    .Footer(f => { /* logo + fine print row + page number, see above */ })
    .Content(c =>
    {
        c.AddHeading("INVOICE", HeadingLevel.H1).AlignCenter();
        c.AddParagraph("Invoice #: INV-1042\nInvoice Date: June 23, 2026\nDue Date: July 23, 2026")
            .AlignRight().FontSize(11);
        c.AddSpacer(8);

        c.AddHeading("Bill To", HeadingLevel.H3);
        c.AddParagraph("Jane Doe\n456 Oak Avenue\nSpringfield, USA");
        c.AddSpacer(20);

        c.AddTable(table =>
        {
            table.AddColumn("Item");
            table.AddColumn("Qty", widthPx: 50);
            table.AddColumn("Unit Price", widthPx: 100);
            table.AddColumn("Amount", widthPx: 100);
            table.AddRow(new TableCell[]
            {
                "Website Redesign",
                new TableCell("1", rightAlign),
                new TableCell("$1,200.00", rightAlign),
                new TableCell("$1,200.00", rightAlign),
            });
            // ... more rows ...
        });
        c.AddSpacer(12);

        c.AddParagraph("Subtotal: $2,300.00").AlignRight();
        c.AddParagraph("Tax (8%): $184.00").AlignRight();
        c.AddRule();
        c.AddParagraph("Total: $2,484.00").AlignRight().Bold().FontSize(16);
    })
    .Build();
```

Note the `\n` inside `AddParagraph`'s text - `ITextMeasurer.Measure` treats
explicit newlines as hard breaks, so a single `Paragraph` can hold multiple
visually distinct lines (an address block, here) without needing several
separate elements. See [Tables: Per-cell style overrides](05-tables.md#per-cell-style-overrides)
for the `rightAlign` pattern used on numeric columns. Full source:
[`SalesInvoiceScenario.cs`](../samples/TerraFluent.Html.Reporting.Sample/Scenarios/SalesInvoiceScenario.cs).

## Where to go next

- [FAQ / Troubleshooting](12-faq-troubleshooting.md) if something in your
  own report isn't behaving like these recipes.
- [Extending the Library](11-extending.md) if you need a capability none of
  these cover.
