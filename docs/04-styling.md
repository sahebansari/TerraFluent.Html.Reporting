# Styling

## `TextStyle`

[`TextStyle`](../src/TerraFluent.Html.Reporting/Model/Styling/TextStyle.cs) is the
immutable style record behind every text-bearing element (`Paragraph`,
`Heading`, `PageNumberText`, list items, table cells). `TextStyle.Default` is
used when an element doesn't specify one:

| Property | Default | Notes |
|---|---|---|
| `FontFamily` | `"Segoe UI, Arial, sans-serif"` | A CSS font-family list, used verbatim in the rendered `font-family`. |
| `FontSizePx` | `14` | CSS pixels. |
| `FontWeight` | `FontWeight.Normal` | `Normal` or `Bold`. |
| `FontStyle` | `FontStyle.Normal` | `Normal` or `Italic`. |
| `Color` | `"#222222"` | Any CSS color string. |
| `LineHeightMultiplier` | `1.4` | Resolved line height = `FontSizePx * LineHeightMultiplier`; see `LineHeightPx`. |
| `Alignment` | `TextAlignment.Left` | `Left`, `Center`, `Right`, or `Justify`. |
| `MarginTopPx`/`MarginRightPx`/`MarginBottomPx`/`MarginLeftPx` | `0`/`0`/`8`/`0` | See [Margin vs. padding](#margin-vs-padding) below. |
| `PaddingTopPx`/`PaddingRightPx`/`PaddingBottomPx`/`PaddingLeftPx` | `0` each | See [Margin vs. padding](#margin-vs-padding) below. |

Derived, read-only properties:

- **`LineHeightPx`** = `FontSizePx * LineHeightMultiplier`.
- **`HorizontalInsetPx`** = left+right margin + left+right padding - what's
  subtracted from the container width before word-wrapping.
- **`VerticalInsetPx`** = top+bottom margin + top+bottom padding - what's
  added on top of the wrapped text's height when measuring the element.

Call `style.With(...)` to derive a modified copy - every property above has a
matching nullable parameter, so `style.With(fontSizePx: 18)` changes only the
font size.

## Fluent modifiers (`TextElementBuilder`)

`AddParagraph`/`AddHeading`/`AddText`/`AddPageNumber` all return a
[`TextElementBuilder`](../src/TerraFluent.Html.Reporting/Fluent/TextElementBuilder.cs),
which exposes:

```csharp
c.AddParagraph("Note")
    .AlignCenter()      // AlignLeft / AlignCenter / AlignRight / AlignJustify
    .Bold()              // FontWeight.Bold
    .Italic()            // FontStyle.Italic
    .FontSize(16)         // px
    .FontColor("#b23b3b") // any CSS color
    .FontFamily("Georgia, 'Times New Roman', serif")
    .MarginTop(8).MarginRight(0).MarginBottom(8).MarginLeft(0)
    .Margin(8)                    // uniform, all four sides
    .Margin(8, 0, 8, 0)           // top, right, bottom, left (CSS shorthand order)
    .Padding(12)                  // uniform
    .Padding(12, 8, 12, 8);       // top, right, bottom, left
```

Each call rebuilds the element with an updated `TextStyle` and replaces it in
the owning content/section list - see
[Core Concepts: The rebuild-and-replace pattern](02-core-concepts.md#the-rebuild-and-replace-pattern)
for why this is safe to chain repeatedly. Because each call only overrides
what you pass, calling the same modifier twice just has the later value win
(no accumulation).

## Margin vs. padding

Both shrink the space available to the text, but at different edges of the
element's box:

- **Margin** is space *outside* the element's own box - it pushes the box
  away from its container's edge (left/right margin) or from neighboring
  elements (top/bottom margin). `MarginBottomPx` defaults to `8`, which is
  why elements added via `AddParagraph`/`AddHeading` in `Content` get
  natural spacing between them with no extra effort.
- **Padding** is space *inside* the element's own box - it insets the text
  from the box's own edges without affecting where the box itself sits
  relative to its neighbors. Padding is what you'd reach for to inset text
  away from a border or a colored background you've drawn around it (e.g.
  with `AddRawHtml`, or a `RawHtml`-wrapped frame).

Text wraps within whatever width is left after *both* are subtracted from
the container width (`HorizontalInsetPx`), and the element's measured height
includes both (`VerticalInsetPx`).

**Default margins differ between builders.** `ContentBuilder.AddParagraph`/`AddHeading`
default to `TextStyle.Default`/`TextStyle.ForHeading(level)`, which both carry
the normal bottom margin. `PageSectionBuilder.AddText`/`AddPageNumber` and
`RowColumnBuilder.AddText`/`AddHeading`/`AddPageNumber` instead default to
`marginBottomPx: 0` - a header/footer line or a row column's stacked content
usually shouldn't have an implicit gap after the last line the way body
content does. Pass an explicit `TextStyle` (or chain `.MarginBottom(...)`) if
you want different spacing in those contexts.

## The heading scale

`TextStyle.ForHeading(HeadingLevel)` is the default style scale used by
`AddHeading` when you don't pass an explicit `TextStyle`:

| Level | Font size | Weight | Bottom margin |
|---|---|---|---|
| H1 | 28px | Bold | 16px |
| H2 | 24px | Bold | 14px |
| H3 | 20px | Bold | 12px |
| H4 | 18px | Bold | 10px |
| H5 | 16px | Bold | 8px |
| H6 | 14px | Bold | 8px |

Pass a `TextStyle` explicitly to `AddHeading(text, level, style)` to override
any of these while keeping the semantic `<h1>`-`<h6>` tag the level implies.

## Images

`AddImage`/`AddImageFromBase64` return an
[`ImageElementBuilder`](../src/TerraFluent.Html.Reporting/Fluent/ImageElementBuilder.cs):

```csharp
c.AddImage("logo.png", widthPx: 96)
    .AlignCenter()              // AlignLeft (default) / AlignCenter / AlignRight
    .Margin(8)
    .MarginBottom(16)
    .Padding(4);
```

- **Alignment** only has a visible effect when the image's container
  (content width, or a row column's width) is wider than the image itself -
  it positions the image's box within the leftover space. `Justify` behaves
  the same as `Left`.
- **Margin** shrinks the available width from the container's edges (left/right)
  and adds vertical space around the image (top/bottom), exactly like text
  margins. The default is `MarginBottomPx: 8` (and `0` elsewhere) on
  `ReportImage`.
- **Padding** insets the image itself from its own box, which only matters in
  combination with alignment (it shifts where centering/right-alignment
  measures from) since an image with no visible border or background
  otherwise has nothing to "pad away from".

Model type: [`ReportImage`](../src/TerraFluent.Html.Reporting/Model/Elements/ReportImage.cs).

## Rows and row columns

`AddRow` returns a [`RowHandle`](../src/TerraFluent.Html.Reporting/Fluent/RowHandle.cs)
with margin modifiers for the row as a whole:

```csharp
c.AddRow(row => { ... }).Margin(12).MarginTop(20);
```

`RowBuilder.AddColumn` returns a
[`RowColumnHandle`](../src/TerraFluent.Html.Reporting/Fluent/RowColumnHandle.cs) with
padding modifiers for that one column's inset from its own edges:

```csharp
row.AddColumn(col => col.AddText("Hi")).Padding(8);
```

See [Rows and Columns](06-rows-and-columns.md) for the full picture, including
how column width and vertical alignment interact with these.

## Table styling

`TableStyle` (header/cell text styles, striping, borders, cell padding, and
`RowSplitBehavior`) is its own topic - see
[Tables: `TableStyle`](05-tables.md#tablestyle).

## Colors and fonts: a caveat

Every color in this library is a plain CSS color string passed straight
through to inline styles - there's no validation, palette, or theme system.
Likewise, `FontFamily` is an opaque CSS font-family list: the library doesn't
ship or embed any fonts, so whatever you specify must be available in
whichever browser/engine ultimately renders the HTML (or it falls back per
normal CSS font-family rules).

This also matters for *pagination accuracy*: the bundled `ApproximateTextMeasurer`
estimates wrapping using a single generic sans-serif metrics table regardless
of which `FontFamily` you set - changing fonts changes how the text looks but
not how the engine predicts it wraps. See
[Text Measurement](09-text-measurement.md) for what this means in practice
and how to get pixel-exact pagination if you need it.

## Where to go next

- [Tables](05-tables.md) for `TableStyle` and per-cell overrides.
- [Rows and Columns](06-rows-and-columns.md) for `RowVerticalAlignment` and
  column width resolution.
- [Text Measurement](09-text-measurement.md) for how font choice interacts
  with pagination precision.
