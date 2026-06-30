# FAQ / Troubleshooting

## Why didn't my page break exactly where I expected?

The default `ApproximateTextMeasurer` estimates text wrapping using generic
Helvetica character-width tables, not the actual font/engine that will
eventually render the HTML - so its line-wrap decisions are *close* to a
real browser's but not guaranteed identical, especially for proportional
fonts other than a plain sans-serif, or text with heavy kerning/ligatures. If
your pagination needs to be pixel-exact, supply a custom `ITextMeasurer`
backed by a real rendering engine via `UseTextMeasurer(...)`. See
[Text Measurement](09-text-measurement.md) and
[Extending the Library: A custom `ITextMeasurer`](11-extending.md#a-custom-itextmeasurer).

## My header/footer text has different spacing than my content text

`PageSectionBuilder.AddText`/`AddPageNumber` (header/footer) and
`RowColumnBuilder.AddText`/`AddHeading`/`AddPageNumber` (row columns) default
to `marginBottomPx: 0`, while `ContentBuilder.AddParagraph`/`AddHeading`
default to the normal `TextStyle.Default`/`TextStyle.ForHeading(level)`
margins (`8px` bottom for body text, more for headings). This is
intentional, not a bug - see
[Styling: Margin vs. padding](04-styling.md#margin-vs-padding). Chain
`.MarginBottom(...)` explicitly if you want spacing that differs from the
context's default.

## My image/row is positioned differently than I expected

- **Alignment only matters when the container is wider than the content.**
  `AlignCenter()`/`AlignRight()` on an image position its box within
  whatever width is left over after margin/padding - if the image already
  fills the available width, alignment has no visible effect.
- **Left/right margin shrinks the available width**, it doesn't just shift
  the element - an image with `MarginLeftPx: 20` inside a 200px-wide
  container effectively centers/right-aligns within 180px, not 200px.
- **Row column padding doesn't change the column's resolved width** in the
  row's layout math - it only insets that column's own content within
  whatever width the column already got. See
  [Rows and Columns: Column padding](06-rows-and-columns.md#column-padding).

## Why does my row/image/heading move to the next page instead of splitting?

Only `Paragraph`, `ReportList`, and `Table` support partial splitting.
`Heading`, `ReportImage`, `Row`, `HorizontalRule`, `Spacer`, `PageBreak`,
`RawHtml`, and `PageNumberText` always move whole to the next page if they
don't fit - see
[Pagination and Layout: The `IReportElement` contract](07-pagination-and-layout.md#the-ireportelement-contract).
If one of these is also taller than an entire empty page's content area, it
gets force-placed (overflowing visually) and recorded in
`LayoutResult.Warnings` rather than dropped - see
[Pagination and Layout: Warnings](07-pagination-and-layout.md#layoutwarning-when-content-doesnt-fit).

## `AddTable`/`Table` throws `ArgumentException` about cell counts

Every row passed to a table must supply exactly one cell per column.
`table.AddColumns("A", "B")` followed by `table.AddRow("only one value")`
throws, naming the offending row index. Check that every `AddRow(...)` call
matches the column count from `AddColumns`/`AddColumn`.

## `LayoutEngine.Paginate` throws `InvalidOperationException`

This means the page geometry leaves no room for content at all:

- *"Left/right margins leave no horizontal room for content."* - `Margins.Left + Margins.Right >= PageSize.WidthPx`.
- *"Margins and header/footer leave no vertical room for content."* - `Margins.Top + Margins.Bottom` plus the header's and footer's combined *measured* height (see
  [Pagination and Layout: Headers and footers](07-pagination-and-layout.md#headers-and-footers))
  is `>= PageSize.HeightPx`.

Reduce margins, shrink the header/footer content, or use a larger page size.

## Does `PageOrientation.Landscape` work with a custom `PageSize.FromPixels(...)`?

Yes - orientation is a plain width/height swap applied uniformly, regardless
of how the `PageSize` was constructed. It is **not** a "force width > height"
coercion: a custom size built via `FromPixels` rotates exactly the same way
`A4`/`Letter`/`Legal` do. See
[Core Concepts: Page geometry](02-core-concepts.md#page-geometry-pagesize-margins-orientation).

## Can I nest a row inside a row, or a table inside a row column?

No. `RowColumnBuilder` (what configures one column's content) deliberately
exposes a smaller method set than `ContentBuilder` - no `AddRow`, `AddTable`,
`AddList`, `AddPageBreak`, or `AddRawHtml`. See the comparison table in
[Content Elements](03-content-elements.md#three-different-builders-three-different-method-sets).
If you need a table or list inside what's visually a row-like layout,
consider `AddRawHtml` with a manually computed height, or restructure the
content to avoid the nesting.

## Why does `RawHtml`/`Spacer` need an explicit height?

The layout engine measures every element by calling `Measure`, which for
ordinary elements computes height from the element's own content and style.
`RawHtml` wraps markup the engine doesn't understand (it can't run a browser
layout pass on it), and `Spacer` has no content to measure at all - both
simply report back whatever height you constructed them with. If the
content you put in `RawHtml` is actually taller than the height you supply,
the engine still treats it as that height for pagination purposes - the
`overflow:hidden` wrapper will clip anything taller for that block specifically,
but pagination decisions for surrounding content won't see the real height.

## Is this thread-safe?

`LayoutEngine.Paginate` and `ApproximateTextMeasurer` are stateless and safe
to call concurrently, including for the same `ReportDocument` (it's
immutable). `Table`/`Row` use an internal measurement cache that may
redundantly recompute under concurrent pagination of the very same instance -
a benign race, not a correctness issue. See
[Pagination and Layout: Thread safety](07-pagination-and-layout.md#thread-safety).

## Known limitations

As of the 1.0 release:

- **Text measurement is approximate by default.** Exact, pixel-perfect
  pagination requires supplying a custom `ITextMeasurer` (see
  [Text Measurement](09-text-measurement.md)) - none ships in the core
  package today.
- **No custom font embedding.** Fonts are referenced by CSS `font-family`
  only; the library never embeds font files into the generated HTML.
- **No multi-column page layout** (newspaper-style columns within a single
  page) - content flows in a single column per page.
- **No cell colspan/rowspan** in tables.
- **No right-to-left (RTL) text support.**
- **Rows don't nest, and row columns can't contain a table, list, nested
  row, page break, or raw HTML** - see
  [section Can I nest a row inside a row?](#can-i-nest-a-row-inside-a-row-or-a-table-inside-a-row-column)
  above.
- **No shrink-to-fit for table/row columns** when fixed-width columns
  already exceed the available width - auto columns are pinned to `0`
  rather than the table/row overflowing or warning about it.

Check [CHANGELOG.md](../CHANGELOG.md) for what's changed most recently.

## Where to go next

- [Pagination and Layout](07-pagination-and-layout.md) for the full
  algorithm behind most of the answers above.
- [Extending the Library](11-extending.md) if the built-in elements/measurer/renderer
  genuinely don't cover your case.
