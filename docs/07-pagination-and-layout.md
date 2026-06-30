# Pagination and Layout

This page explains how [`LayoutEngine.Paginate`](../src/TerraFluent.Html.Reporting/Layout/LayoutEngine.cs)
turns a `ReportDocument`'s flat list of content elements into a sequence of
pages - useful if you want to understand exactly why a page breaks where it
does, what a `LayoutWarning` means, or you're implementing a custom
`IReportElement` (see [Extending the Library](11-extending.md)).

## Calling the engine directly

`ReportDocument.RenderHtml()` and friends call this for you, but you can call
it yourself to inspect the result before rendering:

```csharp
using TerraFluent.Html.Reporting.Layout;

LayoutResult layout = LayoutEngine.Paginate(document);
Console.WriteLine($"{layout.Pages.Count} page(s)");
foreach (var warning in layout.Warnings)
{
    Console.WriteLine(warning); // "Page 3: A ReportImage required ... "
}
```

## The `IReportElement` contract

Every content, header, and footer element implements
[`IReportElement`](../src/TerraFluent.Html.Reporting/Model/IReportElement.cs):

```csharp
public interface IReportElement
{
    ElementMeasurement Measure(LayoutContext context);
    SplitResult Split(double availableHeightPx, LayoutContext context);
    string RenderHtml(ElementPlacement placement, RenderContext context);
}
```

The engine drives every element through exactly this contract and never
inspects an element's concrete type:

1. Call **`Measure`**. If the result fits in the remaining content height on
   the current page, place it whole and move on.
2. Otherwise, if any space is left on the current page, call **`Split`** with
   that remaining height.
   - If it returns a non-null `Head`, place that fragment on the current
     page.
   - If `Tail` is also non-null, start a new page and process the tail next
     (a tail may itself need further splitting - e.g. a table spanning three
     or more pages repeats this step).
   - If `Head` is null (`SplitResult.Unsplittable`), nothing fit even
     partially - fall through to step 3.
3. If nothing fit at all, start a new page and retry the *same* element at
   the top of it.
4. **Termination guarantee:** if an element still doesn't fit even on a
   completely empty page, it's placed anyway (so it overflows visually
   rather than disappearing or looping forever), and a `LayoutWarning` is
   recorded.

`Measure` must be a pure function of the element's own content/style and the
given `LayoutContext` - the engine may call it more than once for the same
element (e.g. once to decide whether to split, again on the resulting
fragments), so it must not have side effects that would change behavior
between calls. (`Table` and `Row` use an internal cache to avoid recomputing
expensive measurements repeatedly - see
[Tables: Performance](05-tables.md#performance-row-heights-are-cached-not-recomputed-per-page) -
but the cache is keyed by content width and is purely a performance detail
invisible to callers.)

## `LayoutContext`

[`LayoutContext`](../src/TerraFluent.Html.Reporting/Layout/LayoutContext.cs) is the
ambient information threaded through every `Measure`/`Split` call: the
`ITextMeasurer` to use, and the `ContentWidthPx` available to the element.
Elements that nest children at a narrower width (a table cell, a row column)
derive a child context via `context.WithContentWidth(narrowerWidth)` rather
than mutating the original - instances are immutable, so the same context is
safely reused across sibling elements.

## `SplitResult`

[`SplitResult`](../src/TerraFluent.Html.Reporting/Layout/SplitResult.cs) is the return
type of `Split`:

- **`SplitResult.Partial(head, tail)`** - the element was divided: `head`
  fits in the offered height and should be placed now; `tail` (nullable)
  continues on the next page.
- **`SplitResult.Unsplittable(original)`** - nothing fit, or the element
  doesn't support partial placement at all. `Head` is `null`; `Tail` is the
  original, unmodified element, to be retried elsewhere.

Elements that can never be partially placed - `Heading`, `ReportImage`,
`HorizontalRule`, `Spacer`, `PageBreak`, `RawHtml`, `Row`, `PageNumberText` -
always return `Unsplittable`. Only `Paragraph`, `ReportList`, and `Table`
support a true partial split.

## Paragraph splitting (widow/orphan control)

`Paragraph.Split` doesn't just cut at "however many lines fit" - it applies a
minimum of two lines kept together on either side of a break:

- If only **one line** would fit on the current page (an orphan), the whole
  paragraph defers to the next page instead, rather than stranding a single
  line.
- If splitting would leave only **one line** alone at the top of the next
  page (a widow), one additional line is pulled back onto the current page
  so at least two move together.

This only applies when the paragraph has more than two lines total - a
two-line paragraph either fits whole or doesn't split at all.

`Heading` never splits (a fragment of a heading would be a poor reading
experience), so a heading that doesn't fit always moves whole to the next
page, same as an image.

`ReportList` splits at **item boundaries** - an individual item's own wrapped
lines are never separated from each other, only entire items move to the
next page. A continuation fragment carries `StartIndex` forward so a numbered
list keeps counting correctly instead of restarting at 1.

`Table` splits at **row boundaries**, with an optional mid-row split - see
[Tables: Row splitting](05-tables.md#row-splitting-rowsplitbehavior) for the
full behavior, which is configurable per table via `TableStyle.RowSplitBehavior`.

## `LayoutWarning`: when content doesn't fit

A [`LayoutWarning`](../src/TerraFluent.Html.Reporting/Layout/LayoutWarning.cs) is
recorded only when an element couldn't be split (or split usefully) and
didn't fit even on a completely empty page - the engine places it anyway
(so it overflows visually) rather than silently dropping content or looping
forever:

```csharp
public sealed class LayoutWarning
{
    public int PageIndex { get; }   // 0-based
    public string Message { get; }  // human-readable
}
```

`LayoutResult.Warnings` is empty in the common case where everything fit.
Check it after pagination if you want to detect "my report clipped content"
programmatically instead of only finding out by eyeballing the rendered
output - e.g. log a warning, or reject a generated report before it reaches
a user, when a page comes back with warnings attached. See
[Cookbook: Detecting layout warnings](10-cookbook.md#detecting-content-that-doesnt-fit-layoutwarning)
for a runnable example (an oversized image that can't be split and is taller
than the entire page).

## Headers and footers

A header/footer's height is **not configured directly** - it's the sum of
its elements' measured heights, computed once (with the document's
`ITextMeasurer`) before pagination of the main content begins, then held
constant across every page. This is why the available content height per
page is `PageSize.HeightPx - Margins.Top - Margins.Bottom - headerHeight - footerHeight`.

Header/footer elements are **never split** across anything (there's only one
"page" of them per page, by definition) - `IPageSection.MeasureHeight` is a
plain sum, not a pagination pass. The engine does recompute header/footer
*placement geometry* once per page (cheap, since they typically hold only a
handful of elements) purely so each page's `ElementPlacement.PageIndex` is
accurate; the rendered content is otherwise identical on every page except
for page-number tokens, which are resolved later during rendering (see
[Rendering](08-rendering.md)) rather than during layout.

## `PageBreak`

A `PageBreak` is special-cased entirely outside the measure/split/place flow
described above: encountering one closes the current page immediately,
*unless* the current page is still empty (`usedHeightPx <= Epsilon`), in
which case it's a no-op. This is what makes a leading or repeated page break
never produce a blank page. See
[Content Elements: Page break](03-content-elements.md#page-break).

## The output: `LayoutResult`

[`LayoutResult`](../src/TerraFluent.Html.Reporting/Layout/LayoutResult.cs) is plain
data describing the paginated document - no HTML anywhere in it:

- **`Pages`** - an ordered list of [`PageLayout`](../src/TerraFluent.Html.Reporting/Layout/PageLayout.cs),
  each holding its `HeaderElements`/`ContentElements`/`FooterElements` as
  [`PlacedElement`](../src/TerraFluent.Html.Reporting/Layout/PlacedElement.cs)s (an
  element instance - possibly a `Split`-produced fragment, not the original -
  paired with an [`ElementPlacement`](../src/TerraFluent.Html.Reporting/Layout/ElementPlacement.cs)
  describing where).
- **`PageSize`**/**`Margins`** - copied from the document, already
  orientation-adjusted, so a renderer doesn't need the original document to
  emit correct page CSS.
- **`Warnings`** - see above.

`ElementPlacement.XPx`/`YPx` are relative to the section (header, content, or
footer) the element belongs to, not the page - `HtmlReportRenderer` calls
`ElementPlacement.Translate(offsetX, offsetY)` to convert to page-absolute
coordinates before rendering. `Pages.Count` is also the "totalPages" value
used to resolve `{totalPages}` tokens in `PageNumberText`.

## Thread safety

`LayoutEngine.Paginate` holds no static or shared mutable state, so
concurrent calls for different `ReportDocument` instances (or even the same
instance, since it's immutable) are safe. The default `ApproximateTextMeasurer`
is likewise stateless. `Table`/`Row` cache measurements internally as a
performance optimization; concurrently paginating the very same `Table`/`Row`
instance is safe but may redundantly recompute the cache once or twice (a
benign race, not a correctness issue).

## Where to go next

- [Rendering](08-rendering.md) for how a `LayoutResult` becomes HTML.
- [Text Measurement](09-text-measurement.md) for the `ITextMeasurer` seam
  that drives every height calculation above.
- [Extending the Library](11-extending.md) for implementing your own
  `IReportElement`.
