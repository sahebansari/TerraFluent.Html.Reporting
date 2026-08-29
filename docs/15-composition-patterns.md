# Supported Composition Patterns

The library has several container-like elements - `Row`/`RowColumn`,
`Table`, and `MultiColumnSection` (via `AddColumns`) - and each one
deliberately supports a *narrower* set of nested content than the top-level
`ContentBuilder` does. This page is the single reference for what's
supported where, so you don't have to piece it together from each
container's own doc page or discover a restriction by hitting an exception.

## Quick reference

| Container | Can contain | Cannot contain |
|---|---|---|
| `Content`/`Header`/`Footer` (`ContentBuilder`/`PageSectionBuilder`) | Everything: paragraphs, headings, images, barcodes, QR codes, tables, lists, rules, spacers, raw HTML, rows, multi-column sections, page breaks (content only) | - |
| `Row` column (`RowColumnBuilder`) | Text, headings, page-number text, images, barcodes, QR codes, rules, spacers | Tables, lists, raw HTML, nested rows, page breaks, multi-column sections |
| `Table` cell | Plain text only (`TableCell`) | Any element - a cell is text, not a container |
| `MultiColumnSection` column (`ColumnsBuilder`, via `AddColumns`) | Paragraphs, headings, images, barcodes, QR codes, tables, lists, rules, spacers, raw HTML | Rows, nested multi-column sections, page breaks |

Two patterns recur across every restricted container: **no page breaks**
(a page break is a page-level concept, not a within-container one) and **no
self-nesting** (`Row` can't contain another `Row`; `MultiColumnSection`
can't contain another `MultiColumnSection`).

## Why the restrictions exist

Each container's restrictions come from what it needs to guarantee about
its own layout, not an arbitrary limitation:

- **`Row` never splits across pages** (see
  [Rows and Columns](06-rows-and-columns.md)) - so it deliberately excludes
  content that *needs* to split (a long table, a long list) or that only
  makes sense at the page level (a page break). Nesting a row inside a row
  would also make "never splits" apply transitively to arbitrarily complex
  content, which stops being a useful guarantee.
- **A table cell is text, not a container** - `TableCell`'s whole
  measurement/splitting model (shared line budgets across a row, see
  [Tables: Column and row spans](05-tables.md#column-and-row-spans)) assumes
  plain wrapped text, not arbitrary nested layout.
- **`MultiColumnSection` fills columns dynamically based on remaining
  space** (see [Rows and Columns: Multi-column sections](06-rows-and-columns.md#multi-column-sections-addcolumns)) -
  a nested multi-column section would need to answer "how tall is a
  column's worth of *columns*," which has no well-defined answer; a `Row`
  inside a column would need to answer "what happens when a row that never
  splits doesn't fit in the remaining column space," which is exactly the
  kind of edge case the "no column-spanning elements" v1 scope avoids.

## Combining containers: what already works well

These are the patterns worth reaching for first, in order of how often
real reports need them:

- **A `Row` in the header/footer, ordinary content below it.** The
  most common pattern - a logo next to a company name in the header, plain
  paragraphs/tables in the body. See
  [Cookbook: A letterhead with a logo](10-cookbook.md).
- **A `Table` after a `Row`.** A row for a summary strip (e.g. three
  centered stat numbers), followed by an ordinary table for the detail
  data - two independent top-level elements, not nested.
- **A barcode/QR code inside a `Row` column** (e.g. an invoice number
  barcode next to the company name) - both are just images once created,
  so every image modifier (`AlignCenter`, `Margin`, ...) works on them
  inside a column exactly like it does at the top level. See
  [Cookbook: A barcode in the header](10-cookbook.md#a-barcode-in-the-header-invoice-number).
- **A `Table` inside a `MultiColumnSection` column** - a genuinely new
  capability multi-column sections add: unlike a `Row` column, a
  `MultiColumnSection` column can hold a table, and that table can even
  split across the column boundary the same way it splits across a page
  boundary. Useful for a two-column glossary/reference layout where one
  column happens to need a small table.

## If you need something not on this list

If your content doesn't fit one of the supported patterns above, two
escape hatches exist rather than trying to force a nested container to do
something it wasn't designed for:

- **`AddRawHtml(html, heightPx)`** - inject arbitrary markup at a
  caller-supplied height when you need a layout the built-in containers
  don't support. See
  [FAQ: Why does RawHtml/Spacer need an explicit height?](12-faq-troubleshooting.md#why-does-rawhtmlspacer-need-an-explicit-height).
- **Restructure as independent top-level elements.** Most "I want to nest
  X inside Y" needs are actually satisfied by two separate top-level
  elements placed next to each other in document order (e.g. a `Row` for a
  header strip, then a `Table` immediately after it) rather than true
  nesting - see the combining patterns above.

## Where to go next

- [Rows and Columns](06-rows-and-columns.md) for `Row`/`RowColumn` and
  `MultiColumnSection` in full detail.
- [Tables](05-tables.md) for what a `TableCell` supports.
- [FAQ: Can I nest a row inside a row?](12-faq-troubleshooting.md#can-i-nest-a-row-inside-a-row-or-a-table-inside-a-row-column)
  for the original version of this question, answered inline.
