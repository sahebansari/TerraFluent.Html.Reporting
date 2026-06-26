# Tables

Tables are the most involved element in the library: they have their own
column-width algorithm, two different strategies for handling a row that
doesn't fit on a page, and a row-height cache that keeps pagination of a
large table fast. This page covers all of it.

## Building a table

```csharp
c.AddTable(table =>
{
    table.AddColumns("Product", "Qty", "Revenue");      // headers only, auto width
    table.AddRow("Widget A", "120", "$2,400");
    table.AddRow("Widget B", "85", "$1,275");
});
```

[`TableBuilder`](../src/FluentHtmlReport/Fluent/TableBuilder.cs) methods:

- **`AddColumns(params string[] headers)`** - one auto-width column per
  header string.
- **`AddColumn(string header, double? widthPx = null)`** - a single column,
  optionally with a fixed width.
- **`AddRow(params string[] cells)`** - a row from plain strings, one per
  column, implicitly converted to `TableCell`.
- **`AddRow(IReadOnlyList<TableCell> cells)`** - a row with explicit cells,
  letting you override style per cell.

Every row must supply exactly one cell per column - mismatched counts throw
`ArgumentException` from the `Table` constructor (e.g. `AddColumns("A", "B")`
followed by `AddRow("only one")`).

`AddTable(configure, style?)` returns the `ContentBuilder` itself - tables
have no chainable post-add modifiers; pass a `TableStyle` directly as the
second argument instead.

## Column width resolution

Each `TableColumn` either has an explicit `WidthPx` or is auto-width. Given
the table's resolved content width:

1. Sum the explicit widths of all fixed-width columns.
2. Whatever's left over (content width minus that sum, floored at 0) is
   divided **equally** among the auto-width columns.

```csharp
table.AddColumn("Code", widthPx: 70);   // fixed: 70px
table.AddColumn("Description");          // auto: shares what's left
table.AddColumn("Price", widthPx: 90);  // fixed: 90px
```

If the fixed widths alone exceed the content width, auto columns are pinned
to `0` rather than going negative - there's no shrink-to-fit or overflow
warning for this case today, so size fixed columns conservatively relative to
the page width you're targeting.

The table renders with `table-layout:fixed` and an explicit `<colgroup>`
matching these resolved widths - see
[Rendering § What the HTML looks like](08-rendering.md#what-the-html-looks-like)
for why the `<table>` itself is deliberately *not* pinned to `width:100%`.

## `TableStyle`

[`TableStyle`](../src/FluentHtmlReport/Model/Styling/TableStyle.cs) controls
everything visual about a table:

| Property | Default | Notes |
|---|---|---|
| `HeaderTextStyle` | Bold, white text, no bottom margin | Style for header-row cell text. |
| `CellTextStyle` | `TextStyle.Default` with no bottom margin | Style for body-row cell text (per-cell overridable, see below). |
| `HeaderBackgroundColor` | `"#2f4858"` | Header row background. |
| `EvenRowBackgroundColor` / `OddRowBackgroundColor` | `"#ffffff"` / `"#f4f6f7"` | Body row backgrounds when `StripedRows` is on; 0-based, so row 0 is "even". |
| `StripedRows` | `true` | Whether alternating backgrounds are applied. |
| `CellPaddingPx` | `6` | Applied on all four sides of every cell. |
| `BorderColor` | `"#d8dde0"` | Set empty for no visible border. |
| `BorderWidthPx` | `1` | Border thickness; also factored into row-height measurement (see below). |
| `RowSplitBehavior` | `AllowSplitWithContinuedHeader` | See [Row splitting](#row-splitting-rowsplitbehavior) below. |
| `ContinuedHeaderSuffix` | `" (continued)"` | Appended to the header on a continuation page; empty string suppresses it. |

```csharp
c.AddTable(
    table => { ... },
    TableStyle.Default.With(stripedRows: false, headerBackgroundColor: "#1c2b33"));
```

`TableStyle.With(...)` follows the same override-only-what-you-pass pattern
as `TextStyle.With(...)`.

## Per-cell style overrides

A `TableCell` carries an optional `Style` that overrides `CellTextStyle` for
that one cell - useful for right-aligning numeric columns, for example:

```csharp
var rightAlign = TextStyle.Default.With(alignment: TextAlignment.Right, marginBottomPx: 0);

table.AddRow(new TableCell[]
{
    "Website Redesign",
    new TableCell("1", rightAlign),
    new TableCell("$1,200.00", rightAlign),
    new TableCell("$1,200.00", rightAlign),
});
```

Plain strings implicitly convert to a `TableCell` with no override
(`Style = null`), which is why `AddRow("a", "b", "c")` works without
constructing `TableCell` instances yourself.

## Row splitting (`RowSplitBehavior`)

When a table doesn't entirely fit in the space remaining on a page, the
engine places as many whole rows as fit, then has to decide what to do with
the row that's too tall for what's left. `TableStyle.RowSplitBehavior`
controls this:

- **`KeepRowIntact`** - the row that doesn't fit moves whole to the next
  page (under a repeated header). Simple, but a single very tall row can
  waste a lot of trailing whitespace on the page it didn't fit on.
- **`AllowSplitWithContinuedHeader`** (the default) - every cell in the
  offending row is truncated at a **shared line budget** (the narrowest
  cell's line height determines how many lines fit across all cells in that
  row, so the visual split lines up across columns), and the remainder
  continues as the first row on the next page, under a freshly repeated
  header annotated with `ContinuedHeaderSuffix`.

```csharp
c.AddTable(
    table => { ... },
    TableStyle.Default.With(rowSplitBehavior: RowSplitBehavior.KeepRowIntact));
```

If a table's header alone is taller than the entire available space (a
degenerate case - an oversized header style on a very small page), the table
is reported `Unsplittable` and the usual "doesn't fit anywhere" handling in
[Pagination and Layout](07-pagination-and-layout.md#layoutwarning-when-content-doesnt-fit)
takes over instead.

The continuation banner (the "(continued)" text above the repeated header) is
rendered as its own full-width row above the header on continuation pages,
and its height is included in measurement - omitting `ContinuedHeaderSuffix`
entirely (empty string) removes both the banner and its height contribution.

## How a row's height is measured

A row's height is the tallest of its cells' wrapped-text heights (each cell
measured independently at its column's resolved width, minus
`2 * CellPaddingPx`), plus one `BorderWidthPx` for that row's own bottom
border line. The table's total measured height adds one extra `BorderWidthPx`
for the outermost top edge that no row otherwise accounts for. This border
accounting exists specifically so the `overflow:hidden` container the table
renders inside isn't sized a hair too short - see the inline remarks on
[`Table.MeasureRowHeight`](../src/FluentHtmlReport/Model/Elements/Table.cs)
if you're curious about the exact reasoning (it was a real, since-fixed bug:
omitting it silently clipped the last row's bottom border).

## Performance: row heights are cached, not recomputed per page

A large table split across many pages does **not** re-measure its entire
remaining row list on every page transition. `Table` caches each row's
measured height (keyed by content width) the first time it's needed, and
`Split` slices that cached array into the head/tail fragments it produces
rather than recomputing - so an N-row table is measured in O(N) total work
across its entire pagination, however many pages it ends up spanning, not
O(N²). You don't need to do anything to get this - it's automatic - but it's
worth knowing if you're profiling a very large table (tens of thousands of
rows) and wondering where the cache lives.

## Where to go next

- [Content Elements § Table](03-content-elements.md#table) for where
  `AddTable` fits among the other element types.
- [Pagination and Layout](07-pagination-and-layout.md) for the general
  measure/split contract every element (including `Table`) implements.
- [Cookbook § Multi-page table](10-cookbook.md#a-multi-page-report-with-header-footer-and-a-table-that-spans-pages)
  and [Cookbook § Table styling comparison](10-cookbook.md#comparing-table-row-split-behaviors)
  for runnable examples.
