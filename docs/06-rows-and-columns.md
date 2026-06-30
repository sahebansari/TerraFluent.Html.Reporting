# Rows and Columns

`AddRow` lays out columns side by side - e.g. a logo next to a company name,
or a stat strip with three centered numbers - within the content width. It's
available in `Content`, `Header`, and `Footer`, but columns themselves can't
nest another row inside them.

## Building a row

```csharp
c.AddRow(row =>
{
    row.AddColumn(48, col => col.AddImage("logo.png", widthPx: 40));
    row.AddColumn(col =>
    {
        col.AddText("Acme Corporation").Bold().FontSize(20);
        col.AddText("123 Market Street").FontSize(12);
    });
});
```

[`RowBuilder`](../src/TerraFluent.Html.Reporting/Fluent/RowBuilder.cs) has two
`AddColumn` overloads:

- **`AddColumn(Action<RowColumnBuilder> configure)`** - an auto-width column
  that shares the leftover width equally with other auto-width columns.
- **`AddColumn(double widthPx, Action<RowColumnBuilder> configure)`** - a
  column with an explicit fixed width.

`ContentBuilder.AddRow`/`PageSectionBuilder.AddRow` both take the same
signature:

```csharp
AddRow(Action<RowBuilder> configure, double columnGapPx = 12, RowVerticalAlignment verticalAlignment = RowVerticalAlignment.Middle)
```

and return a `RowHandle` for chaining margin modifiers (see
[Styling: Rows and row columns](04-styling.md#rows-and-row-columns)).

## Column width resolution

Same algorithm as table columns (see
[Tables: Column width resolution](05-tables.md#column-width-resolution)):
fixed-width columns get exactly what you specified; whatever's left after
subtracting fixed widths *and* the gaps between columns
(`columnGapPx * (columnCount - 1)`) is divided equally among the auto-width
columns. If fixed widths and gaps alone exceed the available width, auto
columns are pinned to `0` rather than going negative.

## Vertical alignment

When columns in the same row have different content heights, `RowVerticalAlignment`
controls how the shorter ones line up against the row's tallest column:

| Value | Behavior |
|---|---|
| `Top` | Aligned to the top of the row. |
| `Middle` (default) | Centered within the row's height. |
| `Bottom` | Aligned to the bottom of the row. |

```csharp
c.AddRow(row =>
{
    row.AddColumn(120, col => col.AddText("One line"));
    row.AddColumn(col =>
    {
        col.AddText("Line 1");
        col.AddText("Line 2");
        col.AddText("Line 3");
    });
}, verticalAlignment: RowVerticalAlignment.Top);
```

The row's overall height is the **tallest column's** measured height (plus
the row's own margins) - shorter columns don't stretch to fill it, they're
just positioned within it per `VerticalAlignment`.

## Column content defaults

A `RowColumnBuilder`'s text elements (`AddText`/`AddHeading`/`AddPageNumber`)
default to `marginBottomPx: 0`, unlike their `ContentBuilder` counterparts.
This is deliberate: a column's *measured* height needs to match its
*visible* content for `RowVerticalAlignment` to line columns up correctly -
if the last line in a column carried an invisible trailing margin, the
column would measure taller than it looks, and vertical centering would be
visibly off. Chain `.MarginBottom(...)` explicitly if you want deliberate
spacing between elements stacked within the same column.

`RowColumnBuilder` supports `AddText`, `AddHeading`, `AddPageNumber`,
`AddImage` (file or bytes), `AddRule`, and `AddSpacer` - see the comparison
table in [Content Elements](03-content-elements.md#three-different-builders-three-different-method-sets)
for what's deliberately *not* available inside a column (tables, lists,
nested rows, page breaks, raw HTML).

## Column padding

`RowBuilder.AddColumn` returns a `RowColumnHandle` with `Padding(...)` to
inset that column's stacked content from its own box edges, independent of
the column's width:

```csharp
row.AddColumn(col => col.AddText("Hi")).Padding(8);
row.AddColumn(48, col => col.AddImage("logo.png", widthPx: 40))
    .Padding(topPx: 15, 0, 0, 0);
```

Padding reduces the *inner* width available to the column's own elements
(`column width - left padding - right padding`) without changing the
column's resolved width in the row's layout - so padding never throws off
the row's overall column-width math, it only affects how the column's
content sits within its allotted space.

## Rows never split

Like an image, a row is always placed whole. If a row doesn't fit even on a
completely empty page, it's force-placed and recorded as a `LayoutWarning` -
the same fallback described in
[Pagination and Layout: Warnings](07-pagination-and-layout.md#layoutwarning-when-content-doesnt-fit).
In practice this matters for rows with a lot of stacked content in one
column (e.g. many lines of wrapped text) on a very short page - keep tall
row content modest, or split it into separate non-row elements if it might
not fit.

## Margins

A `Row` has the same four margin properties as a `TextStyle`
(`MarginTopPx`/`MarginRightPx`/`MarginBottomPx`/`MarginLeftPx`, defaulting to
`0`/`0`/`8`/`0`), set via the `RowHandle` returned by `AddRow`:

```csharp
c.AddRow(row => { ... }).Margin(12).MarginTop(20);
```

Left/right margin shrinks the width available to the row's columns from that
edge (and shifts the row right, for left margin); top/bottom margin adds
vertical space exactly like a text element's margin does.

## Where to go next

- [Content Elements](03-content-elements.md) for how `Row` compares to the
  library's other elements.
- [Styling](04-styling.md) for the full list of `RowHandle`/`RowColumnHandle`
  modifiers.
- [Cookbook: Row layouts](10-cookbook.md#a-header-and-footer-with-a-logo-row)
  for a complete invoice-style header/footer built from rows.
