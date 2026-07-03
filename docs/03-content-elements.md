# Content Elements

This page is the reference for every element you can add to a document, and
exactly which `Add*` method produces it. "Content" below means the callback
passed to `ReportDocumentBuilder.Content(...)`; "Header"/"Footer" means the
callback passed to `.Header(...)`/`.Footer(...)`; "Row column" means a
callback passed to `RowBuilder.AddColumn(...)` (see
[Rows and Columns](06-rows-and-columns.md)).

## Three different builders, three different method sets

Content, headers/footers, and row columns are configured by three distinct
builder types, and they intentionally don't expose the same methods:

| Method | [`ContentBuilder`](../src/TerraFluent.Html.Reporting/Fluent/ContentBuilder.cs) (Content) | [`PageSectionBuilder`](../src/TerraFluent.Html.Reporting/Fluent/PageSectionBuilder.cs) (Header/Footer) | [`RowColumnBuilder`](../src/TerraFluent.Html.Reporting/Fluent/RowColumnBuilder.cs) (row column) |
|---|---|---|---|
| Plain text line | `AddParagraph` | `AddText` | `AddText` |
| Heading | `AddHeading` | - | `AddHeading` |
| Page number | - | `AddPageNumber` | `AddPageNumber` |
| Image (file/bytes) | `AddImage` | `AddImage` | `AddImage` |
| Image (base64) | `AddImageFromBase64` | - | - |
| Barcode (Code 128) | `AddBarcode` | `AddBarcode` | `AddBarcode` |
| Table | `AddTable` | - | - |
| List | `AddList` | - | - |
| Horizontal rule | `AddRule` | `AddRule` | `AddRule` |
| Row of columns | `AddRow` | `AddRow` | - (rows don't nest) |
| Page break | `AddPageBreak` | - | - |
| Spacer | `AddSpacer` | - | `AddSpacer` |
| Raw HTML | `AddRawHtml` | - | - |
| Custom `IReportElement` | `AddElement` | - | - |

Rationale: a header/footer is a small, fixed-height, repeated block - it has
no use for a page break or a multi-page table, and `AddText` (unlike
`AddParagraph`) defaults to **no trailing margin** since a header/footer line
typically shouldn't add bottom spacing the way body text does. A row column
stacks simple content vertically inside a fixed-width slot - it supports
headings/text/images/rules/spacers like a header/footer, but not a nested
row, table, list, or raw HTML. Text elements inside a column also default to
no trailing margin, for the same layout reason described in
[Rows and Columns: Column content defaults](06-rows-and-columns.md#column-content-defaults).

## Paragraph

A block of body text that word-wraps to the content width and may split
across pages at a line boundary, with widow/orphan control (see
[Pagination and Layout: Paragraph splitting](07-pagination-and-layout.md#paragraph-splitting-widoworphan-control)).

```csharp
c.AddParagraph("This report summarizes sales activity for the period.");
```

`AddParagraph(text, style?)` returns a `TextElementBuilder` - see
[Styling](04-styling.md) for every chainable modifier (`.Bold()`,
`.AlignCenter()`, `.FontSize(...)`, `.Margin(...)`, `.Padding(...)`, ...).

Model type: [`Paragraph`](../src/TerraFluent.Html.Reporting/Model/Elements/Paragraph.cs).

## Heading

A semantic heading (H1-H6), rendered as the matching HTML tag
(`<h1>`-`<h6>`). Unlike `Paragraph`, a heading never splits - one that
doesn't fit moves whole to the next page.

```csharp
c.AddHeading("Sales Summary", HeadingLevel.H1);
```

Each `HeadingLevel` has a default font size/weight/bottom-margin (see
[Styling: The heading scale](04-styling.md#the-heading-scale)); pass an
explicit `TextStyle` as the third argument to override it. `AddHeading`
returns a `TextElementBuilder`, same as `AddParagraph`.

Model type: [`Heading`](../src/TerraFluent.Html.Reporting/Model/Elements/Heading.cs).

## Image

An embedded image, base64-encoded inline as a data URI - the output HTML has
no external file references. Never splits across pages.

```csharp
c.AddImage("logo.png", widthPx: 120, heightPx: 60);   // from a file path
c.AddImage(imageBytes, "image/png", widthPx: 120);    // from bytes
c.AddImageFromBase64(dataUriOrBase64, widthPx: 120);  // from base64 (Content only)
```

- **If both `widthPx` and `heightPx` are given**, the image is stretched/shrunk
  to exactly that box.
- **If only one is given**, the other is derived from the image's intrinsic
  pixel dimensions (sniffed from the file's own header bytes - PNG, GIF, BMP,
  and baseline/progressive JPEG are supported; see
  [`ImageDimensionReader`](../src/TerraFluent.Html.Reporting/Model/Elements/ImageDimensionReader.cs))
  so the aspect ratio is preserved.
- **If neither is given**, the intrinsic dimensions are used directly.
- If the format isn't recognized and neither dimension was supplied, loading
  throws `InvalidOperationException` - specify at least one explicitly.

`AddImage`/`AddImageFromBase64` return an `ImageElementBuilder` -
`.AlignLeft()`/`.AlignCenter()`/`.AlignRight()` position the image within a
container wider than itself, and `.Margin(...)`/`.Padding(...)` work the same
as on text elements. See [Styling: Images](04-styling.md#images).

Model type: [`ReportImage`](../src/TerraFluent.Html.Reporting/Model/Elements/ReportImage.cs).

## Barcode

A Code 128 barcode, rendered natively as a generated PNG `ReportImage` - no
external barcode library or web service involved. Useful for an invoice
number, tracking number, or any other short identifier that needs to be both
human-readable and machine-scannable.

```csharp
c.AddBarcode("INV-1042");                                            // defaults below
c.AddBarcode("20264471", moduleWidthPx: 2, heightPx: 40, quietZoneModules: 10);
```

- `value` must be non-empty, printable ASCII (character codes 32-126) -
  letters, digits, and most punctuation, but no newlines or control
  characters; anything else throws `ArgumentException`.
- `moduleWidthPx` (default `2`) is the width of the narrowest bar/space; the
  generated image's total width is derived from the encoded value's module
  count, so it varies with `value`'s length.
- `heightPx` (default `60`) is the bar height in pixels.
- `quietZoneModules` (default `10`) is the blank margin on each side, in
  modules - scanners need this clear space to lock onto the barcode; most
  scanners are unreliable without it, so don't reduce it to `0` for a barcode
  meant to actually be scanned.

`AddBarcode` returns the same `ImageElementBuilder` as `AddImage` - chain
`.AlignRight()`/`.AlignCenter()`/`.Margin(...)`/`.Padding(...)` exactly as you
would on any other image, e.g. to pin it to the top-right corner of a header
column. See [Styling: Images](04-styling.md#images).

**Sizing a fixed-width column to hold the barcode.** Unlike text, a barcode
doesn't wrap or shrink to fit - its rendered width is fixed by the encoded
value's length, `moduleWidthPx`, and `quietZoneModules`. If you place it in a
`row.AddColumn(widthPx, ...)` that's narrower than the barcode itself (e.g.
to pin it to a header's top-right corner via `AlignRight()`), the barcode
overflows past the column - and past the page's right margin - instead of
stopping at it. Size the column to at least the barcode's rendered width: for
Code 128, that's `(quietZoneModules * 2 + sum of per-character module
widths) * moduleWidthPx`, where each encoded character (including the
start/checksum codes) is 11 modules wide and the stop character is 13; an
8-digit value at the defaults shown above (`moduleWidthPx: 2`,
`quietZoneModules: 10`) renders to 286px wide, so a 300px column leaves a
small safety margin. See [Cookbook: A Barcode in the Header](10-cookbook.md#a-barcode-in-the-header-invoice-number).

Model type: [`ReportImage`](../src/TerraFluent.Html.Reporting/Model/Elements/ReportImage.cs)
(generated by [`BarcodeImage`](../src/TerraFluent.Html.Reporting/Model/Elements/BarcodeImage.cs)).

## Table

A table with a repeated header row, optional zebra striping, two
configurable behaviors for what happens when a row doesn't fit on the
remaining space of a page, and cells that can span multiple columns/rows via
`ColSpan`/`RowSpan`.

```csharp
c.AddTable(table =>
{
    table.AddColumns("Product", "Qty", "Revenue");
    table.AddRow("Widget A", "120", "$2,400");
});
```

Tables are involved enough to warrant their own page - see
[Tables](05-tables.md) for column width resolution, `TableStyle`, per-cell
style overrides, [column and row spans](05-tables.md#column-and-row-spans),
and `RowSplitBehavior`.

Model type: [`Table`](../src/TerraFluent.Html.Reporting/Model/Elements/Table.cs).

## List

A bulleted or numbered list, rendered as a native `<ul>`/`<ol>` so the
browser handles marker layout and line wrapping itself. Splits at item
boundaries (an individual item's wrapped lines are never separated), and a
numbered list resumes counting correctly across a page break instead of
restarting at 1.

```csharp
c.AddList(ListStyle.Numbered, new[]
{
    "Revenue figures are pre-tax.",
    "Quantities reflect units shipped, not units ordered.",
});
```

`AddList(style, items, textStyle?)` returns the `ContentBuilder` itself (no
chainable modifiers today - pass `textStyle` directly for font/color
control).

Model type: [`ReportList`](../src/TerraFluent.Html.Reporting/Model/Elements/ReportList.cs).

## Row

Side-by-side columns - e.g. a logo next to a company name - laid out
horizontally within the content width. Unlike a table, a row never splits
across pages.

```csharp
c.AddRow(row =>
{
    row.AddColumn(48, col => col.AddImage("logo.png", widthPx: 40));
    row.AddColumn(col => col.AddText("Acme Corporation").Bold().FontSize(20));
});
```

Rows are covered in depth in [Rows and Columns](06-rows-and-columns.md).

Model type: [`Row`](../src/TerraFluent.Html.Reporting/Model/Elements/Row.cs) /
[`RowColumn`](../src/TerraFluent.Html.Reporting/Model/Elements/RowColumn.cs).

## Horizontal rule

A divider line spanning the content width.

```csharp
c.AddRule();                          // 1px, #d8dde0
c.AddRule(thicknessPx: 3, color: "#2f4858");
```

Has fixed default margins (8px top and bottom) baked into the model type
rather than the fluent call - there's no chainable modifier for a rule today;
construct [`HorizontalRule`](../src/TerraFluent.Html.Reporting/Model/Elements/HorizontalRule.cs)
directly and add it via a lower-level path if you need different margins.

## Spacer

An invisible, fixed-height gap - useful for manual vertical spacing between
elements that don't otherwise have a margin between them.

```csharp
c.AddSpacer(20); // 20px of blank vertical space
```

Throws `ArgumentOutOfRangeException` for a negative height. Model type:
[`Spacer`](../src/TerraFluent.Html.Reporting/Model/Elements/Spacer.cs).

## Page break

A structural marker that forces the next content element to start on a fresh
page, regardless of how much room is left on the current one. It has no
height and produces no visible output - the layout engine special-cases it
before the usual fit-or-split logic. A page break with nothing yet placed on
the current page is a no-op, so a *leading* page break never produces a
blank first page (and consecutive page breaks don't produce blank pages
between them either).

```csharp
c.AddHeading("Chapter 1", HeadingLevel.H1);
c.AddParagraph("...");
c.AddPageBreak();
c.AddHeading("Chapter 2", HeadingLevel.H1);
```

Content-only (no header/footer/row-column equivalent - a page break inside a
fixed-height repeated section wouldn't mean anything). Model type:
[`PageBreak`](../src/TerraFluent.Html.Reporting/Model/Elements/PageBreak.cs).

## Raw HTML

An escape hatch for markup the built-in elements don't cover - a styled
callout box, a QR code `<img>`, an inline SVG diagram. Since the layout
engine cannot measure arbitrary HTML it doesn't understand, **you must supply
the height it will occupy**; the engine treats it as an opaque, unsplittable
block at exactly that height (like an oversized image, it can produce a
`LayoutWarning` if it doesn't fit even on an empty page - see
[Pagination and Layout: Warnings](07-pagination-and-layout.md#layoutwarning-when-content-doesnt-fit)).

```csharp
c.AddRawHtml(
    "<div style=\"border:2px dashed #2f4858;padding:16px;\">Custom callout</div>",
    heightPx: 110);
```

The HTML is emitted **verbatim, with no encoding** - unlike text elements
(which HTML-encode their content automatically), this is your one place to
inject markup, so don't pass it unsanitized end-user input. Content-only.
Model type: [`RawHtml`](../src/TerraFluent.Html.Reporting/Model/Elements/RawHtml.cs).

## Page number text

Text containing `{page}`/`{totalPages}` tokens, resolved once the whole
document has been paginated.

```csharp
f.AddPageNumber("Page {page} of {totalPages}").AlignCenter();
f.AddPageNumber(); // same default template
```

Only available via `AddPageNumber` on a header/footer/row-column builder (not
directly in `Content`, since the main content area isn't a fixed, once-per-page
position). Returns a `TextElementBuilder`, same modifiers as `AddParagraph`.

Model type: [`PageNumberText`](../src/TerraFluent.Html.Reporting/Model/Elements/PageNumberText.cs).

## Where to go next

- [Styling](04-styling.md) for every fluent modifier available on text and
  image elements.
- [Tables](05-tables.md) and [Rows and Columns](06-rows-and-columns.md) for
  the two most involved element types.
- [Cookbook](10-cookbook.md) for these elements combined into complete,
  working reports.
