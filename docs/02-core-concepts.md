# Core Concepts

## The pipeline: model → layout → render

FluentHtmlReport is built as three distinct stages, each in its own
namespace, connected only by simple data:

```
Fluent (builders)  -->  Model (immutable elements)  -->  Layout (pagination)  -->  Rendering (HTML)
  ReportDocumentBuilder    ReportDocument                  LayoutEngine.Paginate    HtmlReportRenderer
  ContentBuilder           IReportElement implementations  -> LayoutResult           -> HTML string
  TableBuilder, RowBuilder ...                              (pages, placements)
```

- **`FluentHtmlReport.Fluent`** - the builders you actually call
  (`ReportDocumentBuilder`, `ContentBuilder`, `PageSectionBuilder`,
  `TableBuilder`, `RowBuilder`, ...). These exist purely to give you a nice
  chaining API; they hold mutable lists internally but produce immutable
  model objects.
- **`FluentHtmlReport.Model`** (and `Model.Elements`, `Model.Styling`,
  `Model.Sections`) - the immutable document: `ReportDocument`,
  `IReportElement` implementations (`Paragraph`, `Table`, `Row`, ...),
  `TextStyle`, `TableStyle`. Nothing here knows how to paginate or render
  itself in isolation - each element only knows how to measure and split
  *itself*.
- **`FluentHtmlReport.Layout`** - `LayoutEngine.Paginate` walks the document's
  content elements once, in order, deciding what goes on which page. The
  output, a `LayoutResult`, is a plain description of pages and placements -
  no HTML anywhere yet.
- **`FluentHtmlReport.Rendering`** - `HtmlReportRenderer` walks a
  `LayoutResult` and asks each placed element to render itself as HTML at its
  resolved position.

`ReportDocument.RenderHtml()` (and its siblings) just calls
`LayoutEngine.Paginate` followed by `HtmlReportRenderer.RenderDocumentTo` for
you. You can call both stages yourself - e.g. to inspect `LayoutResult.Warnings`
before deciding whether to render at all. See
[Pagination and Layout](07-pagination-and-layout.md) and
[Rendering](08-rendering.md).

## The document model

[`ReportDocument`](../src/FluentHtmlReport/Model/ReportDocument.cs) is the
root: page geometry (`PageSize`, `Orientation`, `Margins`), an optional
`Header`/`Footer` (`IPageSection`), the top-level `ContentElements`, and the
`TextMeasurer` to lay out with. It is immutable and can only be constructed
via `ReportDocument.Create(...)` followed by `ReportDocumentBuilder.Build()`.

Every piece of content - a paragraph, a table, a row of columns - implements
[`IReportElement`](../src/FluentHtmlReport/Model/IReportElement.cs):

```csharp
public interface IReportElement
{
    ElementMeasurement Measure(LayoutContext context);
    SplitResult Split(double availableHeightPx, LayoutContext context);
    string RenderHtml(ElementPlacement placement, RenderContext context);
}
```

The layout engine drives every element through exactly this contract and
never inspects an element's concrete type - which is what makes it possible
to add new element kinds (see [Extending the Library](11-extending.md))
without touching the engine. The full Measure/Split/Render contract,
including what head/tail mean and how splitting interacts with widow/orphan
control, is covered in [Pagination and Layout](07-pagination-and-layout.md).

## The rebuild-and-replace pattern

Model types like `TextStyle`, `ReportImage`, `Row`, and `RowColumn` are
immutable - every property is `init`-only, and each type exposes a `With(...)`
method that returns a *new* instance with the given properties overridden.

Fluent modifiers (`.Bold()`, `.AlignCenter()`, `.Margin(8)`, ...) are thin
wrappers around this: when you write

```csharp
c.AddParagraph("Note").Bold().FontSize(16)
```

`AddParagraph` adds an initial `Paragraph` to the content list and returns a
[`TextElementBuilder`](../src/FluentHtmlReport/Fluent/TextElementBuilder.cs)
that remembers *where* in that list the paragraph lives. Each chained call
(`.Bold()`, then `.FontSize(16)`) calls `Style.With(...)`, builds a brand new
`Paragraph` with the updated style, and replaces the old one at that same
index. The builder itself holds no state that outlives the chain - it's a
short-lived handle onto a slot in a list, not a long-lived wrapper around the
element.

The same pattern repeats for every element that supports chained modifiers:

| Element | Builder/handle returned by `Add*` | Backing `With(...)` |
|---|---|---|
| `Paragraph` / `Heading` / `PageNumberText` | `TextElementBuilder` | `TextStyle.With(...)` |
| `ReportImage` | `ImageElementBuilder` | `ReportImage.With(...)` |
| `Row` | `RowHandle` | `Row.With(...)` |
| `RowColumn` | `RowColumnHandle` | `RowColumn.With(...)` |

Elements with no modifiers to chain (`HorizontalRule`, `Spacer`, `PageBreak`,
`RawHtml`, `Table`, `ReportList`) have their `Add*` method return the owning
builder itself (`ContentBuilder`/`PageSectionBuilder`) for plain method
chaining instead.

One practical consequence: because each `.With(...)` call only overrides the
properties you pass, calling the *same* modifier twice in a chain just has
the later call win - there's no accumulation (e.g. `.Margin(8).Margin(4)`
ends at `4`, not `12`).

## Units: everything is a CSS pixel

Every length in the public API - page dimensions, margins, font sizes,
padding, column widths, image dimensions - is expressed in CSS pixels
(`px`), using the standard 96px = 1 inch conversion. There are no points,
ems, or percentages anywhere.

- `PageSize.FromMillimeters(...)` and `PageSize.FromInches(...)` are
  convenience constructors that convert to pixels internally; `PageSize`
  itself always stores `WidthPx`/`HeightPx`.
- `PageSize.FromPixels(...)` skips the conversion entirely.
- The renderer emits these pixel values directly into inline CSS
  (`left:123.45px`, `@page { size: 794px 1123px; }`), so what the layout
  engine measured is exactly what the browser lays out - there's no unit
  translation step that could introduce drift between pagination and
  rendering.

This matters most when picking a `TextMeasurer`: pagination correctness
depends on the measurer's pixel measurements agreeing with however the
browser eventually renders the same font at the same pixel size. See
[Text Measurement](09-text-measurement.md).

## Page geometry: `PageSize`, `Margins`, `Orientation`

- **`PageSize`** is a `readonly struct` storing `WidthPx`/`HeightPx`. Built-in
  sizes - `PageSize.A4`, `PageSize.Letter`, `PageSize.Legal` - are defined
  portrait-shaped (width < height). `WithOrientation(PageOrientation.Landscape)`
  swaps width and height; `ReportDocument`'s constructor calls this for you
  based on the `orientation` argument to `ReportDocument.Create`, so you
  never call it directly. A size built via `PageSize.FromPixels(...)` is
  *not* assumed portrait - landscape still swaps whatever width/height you
  gave it, it's a plain 90-degree rotation either way.
- **`Margins`** is a `readonly struct` with independent `Top`/`Right`/`Bottom`/`Left`
  values. `Margins.All(value)` sets all four; `Margins.None` is all zero.
  `SetMargins(...)` on the document builder has both a 4-argument
  (top/right/bottom/left, matching CSS shorthand order) and a 1-argument
  (uniform) overload.
- **Content width** is `PageSize.WidthPx - Margins.Left - Margins.Right`,
  computed once by `LayoutEngine.Paginate` and threaded through every
  element's `Measure`/`Split` call via `LayoutContext.ContentWidthPx`.
- **Content height per page** is `PageSize.HeightPx - Margins.Top - Margins.Bottom`
  minus the header's and footer's *measured* height (header/footer height is
  not configured directly - see [Pagination and Layout](07-pagination-and-layout.md#headers-and-footers)).

If margins (plus header/footer) leave zero or negative room for content,
`LayoutEngine.Paginate` throws `InvalidOperationException` immediately rather
than producing a nonsensical layout.

## Where to go next

- [Content Elements](03-content-elements.md) for what you can actually put
  inside `Header`/`Footer`/`Content`.
- [Pagination and Layout](07-pagination-and-layout.md) for exactly how the
  engine decides where pages break.
- [Extending the Library](11-extending.md) if you want to implement
  `IReportElement` yourself.
