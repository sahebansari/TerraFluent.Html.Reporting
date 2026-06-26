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

## How it works

- Pages have a fixed size (A4/Letter/Legal/custom) and margins; header and
  footer are fixed-height sections repeated on every page.
- Content elements (paragraphs, headings, images, tables, lists, rules) are
  measured and laid out top-to-bottom; an element that doesn't fit on the
  current page is split - at a line boundary for text, at a row boundary for
  tables (with the header repeated and marked "(continued)") - rather than
  just being pushed whole to the next page where avoidable.
- `AddRow` lays out columns side by side (e.g. a logo next to a company name)
  in a header, footer, or the main content; each column stacks its own
  elements vertically. Like an image, a row never splits across pages.
- Every `Add*` method returns a builder you can chain margin/padding/alignment
  modifiers onto, e.g. `c.AddImage("logo.png").AlignCenter().Margin(8)` or
  `c.AddParagraph("Note").Padding(12)`.
- Output is a single self-contained HTML string: inline `<style>`, `@page`
  sizing, and one absolutely-positioned `<div>` per page, so the layout you
  see in a browser is the layout you get when printing.

## Text measurement

Pagination needs to know how tall a block of text will be before it's
rendered, which means measuring how it wraps. The bundled
`ApproximateTextMeasurer` does this with zero runtime dependencies, using
generic sans-serif character-width tables - it will not match a browser's
real font rendering pixel-for-pixel, particularly for proportional fonts other
than a plain sans-serif, or text with heavy kerning/ligatures.

For exact pagination, implement `ITextMeasurer` against a real rendering
engine (e.g. a headless browser, or `System.Drawing`/GDI+ on Windows) and pass
it to `UseTextMeasurer(...)` on the document builder. No ready-made precise
measurer ships today - this is a documented extension point, not a plug-in
registry.

## Status

This library is pre-1.0 and under active development. The public API may
still change between minor versions until 1.0.
