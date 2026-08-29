# Roadmap: Next Features and Product Gaps

This roadmap is based on a direct read of the source (`src/TerraFluent.Html.Reporting`,
~4,740 lines), the test suite (`tests/`, ~2,450 lines across unit, public-API-snapshot,
netstandard2.0-consumer-smoke, and real-browser Playwright tests), the CI pipeline, and
the documented known limitations in [12-faq-troubleshooting.md](12-faq-troubleshooting.md).
Every gap below is cited against a specific file so it can be re-verified quickly rather
than taken on faith.

## Executive summary

The library is in excellent shape for what it claims to be: a disciplined, single-column,
fixed-page HTML reporting engine. There is no architectural debt to pay down. The gaps
that remain are narrow and mostly fall into three buckets:

1. Two small, concrete correctness/diagnostics gaps (silent auto-width clamping, thin
   `LayoutWarning`) that are cheap to fix and directly improve trust.
2. One real test-coverage gap: the shipped default text measurer has no direct unit tests.
3. A short list of already-well-documented, intentionally-deferred features (multi-column
   layout, RTL, font embedding, deeper row/table nesting) that are genuine feature work,
   not bugs.

## Progress

**Release N+1 (P0, items 1-5) and Release N+2 (P1, items 6-8) below are
implemented** - see [CHANGELOG.md#unreleased](../CHANGELOG.md#unreleased) for
the full list.

P0: auto-width column collapse now raises a `LayoutWarningReason.ColumnWidthCollapsed`
warning instead of silently zeroing the column, `LayoutWarning` gained
structured `Reason`/`ElementType`/`ElementIndex` fields, the default
`ApproximateTextMeasurer` has direct unit tests, `ReportDocumentBuilder.Title(...)`
sets the generated HTML's `<title>`, and the existing Playwright cross-browser
CI testing is now documented in the README/rendering docs rather than only
visible in a CI config comment.

P1: `AddQrCode` generates QR codes natively (byte-mode, ECC level M,
auto-selected version), verified end-to-end against an independent QR decoder;
`ReportDocumentBuilder.UseStrictLayoutValidation`/`TableStyle`/`RowStyle`'s
`ColumnWidthOverflowMode` let a consumer opt into throwing instead of warning
on a collapsed column, document-wide or per-element; and a reference
Playwright-backed `ITextMeasurer` sample (measuring real font metrics via
Canvas 2D `measureText`) lives at
`samples/TerraFluent.Html.Reporting.Sample.PlaywrightMeasurer`.

P2: `AddColumns` adds opt-in, conservative multi-column ("newspaper-style")
sections (equal-width, fill-then-wrap, no nesting) - visually verified with
real-browser screenshots, not just unit tests, after an initial design bug
(content collapsing into column 1 instead of spreading across columns) was
caught exactly that way; `TextStyle.Direction` adds RTL `dir`/`direction`
support (independent of `Alignment`, which stays physical); `ReportDocumentBuilder.EmbedFont`
embeds a font as a base64 `@font-face` rule; a new
[docs/15-composition-patterns.md](../docs/15-composition-patterns.md)
catalogues what nests inside what across `Row`/`Table`/`AddColumns`; new
sample scenarios cover a shipping label template and a multi-column
newsletter layout; and a minimal preview/debug console harness lives at
`samples/TerraFluent.Html.Reporting.Sample.Preview`.

All P0-P2 items from this roadmap are now implemented.

## Current strengths (verified, not just claimed)

- Immutable document/model with a real fluent builder API; `ReportDocument` is
  constructed only through `ReportDocumentBuilder.Build()`
  ([ReportDocument.cs](../src/TerraFluent.Html.Reporting/Model/ReportDocument.cs)).
- `LayoutEngine.Paginate` is a single, well-commented ~155-line state machine with an
  explicit termination guarantee for unsplittable, over-tall elements
  ([LayoutEngine.cs:133-146](../src/TerraFluent.Html.Reporting/Layout/LayoutEngine.cs#L133-L146)) -
  no silent content drop, ever.
- `Table` correctly treats a `RowSpan` group as an atomic pagination unit and caches row
  heights so a large table doesn't re-measure itself O(n²) times across page breaks
  ([Table.cs:293-347](../src/TerraFluent.Html.Reporting/Model/Elements/Table.cs#L293-L347)),
  and this is actually asserted by a dedicated performance test
  ([TablePaginationPerformanceTests.cs](../tests/TerraFluent.Html.Reporting.Tests/Layout/TablePaginationPerformanceTests.cs)).
- `Paragraph.Split` implements real widow/orphan control (never strands one line alone at
  a page boundary) ([Paragraph.cs:31-63](../src/TerraFluent.Html.Reporting/Model/Elements/Paragraph.cs#L31-L63)).
- CI doesn't just unit-test the pagination math - it renders real documents in headless
  **Chromium, Firefox, and WebKit via Playwright** and asserts on `getBoundingClientRect()`
  geometry under `@media print`
  ([PrintLayoutBrowserTests.cs](../tests/TerraFluent.Html.Reporting.BrowserTests/PrintLayoutBrowserTests.cs),
  wired into [ci.yml:39-49](../.github/workflows/ci.yml#L39-L49)). This is a materially
  stronger trust story than most libraries in this space have, and should be advertised
  more - see P0 item 3 below.
- A public API snapshot test (`PublicApiSnapshotTests.cs`) and a dedicated
  `netstandard2.0` consumer smoke project guard against accidental breaking changes and
  TFM-specific regressions.
- Barcode generation (Code 128) and table `ColSpan`/`RowSpan` are new in 1.1.1 and are
  both well tested through the fluent API
  ([FluentBuilderTests.cs:185-219](../tests/TerraFluent.Html.Reporting.Tests/Fluent/FluentBuilderTests.cs#L185-L219)).

## Confirmed gaps

### 1. Auto-width columns silently collapse to 0px instead of warning

Both `Table.ResolveColumnWidths` and `Row.ResolveColumnWidths` compute an auto column's
width as `Math.Max(0, contentWidthPx - explicitTotal) / autoCount`
([Table.cs:223-246](../src/TerraFluent.Html.Reporting/Model/Elements/Table.cs#L223-L246),
[Row.cs:95-121](../src/TerraFluent.Html.Reporting/Model/Elements/Row.cs#L95-L121)). When a
table/row's fixed-width columns already exceed the available width, every auto column is
silently pinned to `0` - the column (and its content) effectively disappears with no
signal. This is already named as a known limitation in the FAQ
("No shrink-to-fit... auto columns are pinned to 0 rather than the table/row overflowing
or warning about it") but it's worth promoting: the library already has a
`LayoutResult.Warnings` mechanism built for exactly this class of problem ("content
doesn't fit, and we didn't fail loudly") - this case just isn't wired into it. There is
also no test exercising this path today (confirmed: no test in `TableTests.cs`/`RowTests.cs`
constructs fixed widths that exceed content width).

**Fix is small and self-contained**: thread a warning sink (or return value) from
`Measure`/`Split` into `LayoutEngine`, emit a `LayoutWarning` when `autoWidth` resolves to
`0` while an auto column actually has content, and add the missing test case.

### 2. `LayoutWarning` is too thin for automated diagnostics

`LayoutWarning` is `{ PageIndex, Message }` only
([LayoutWarning.cs](../src/TerraFluent.Html.Reporting/Layout/LayoutWarning.cs)). The
message is a free-form string built with `element.GetType().Name`
([LayoutEngine.cs:138](../src/TerraFluent.Html.Reporting/Layout/LayoutEngine.cs#L138)).
That's fine for a human reading logs, but there's no structured `ElementType`, no reason
code, and no reference back to the source element/index - so a consumer can't group,
filter, or assert on warnings in an automated pipeline without string-parsing the message.
Given warnings are the library's primary "something's wrong" signal (there's no other
diagnostic channel), this is worth strengthening before adding more producers of warnings
(like item 1 above) - otherwise every new warning source adds another slightly-different
free-form string.

### 3. The default (shipped) text measurer has no dedicated unit tests

`ApproximateTextMeasurer`/`HelveticaCharacterWidths` is what every consumer runs unless
they supply a custom `ITextMeasurer` - it's the actual production code path, not a test
double. Yet virtually all layout/table/paragraph/list tests deliberately substitute
`FakeTextMeasurer` or `CountingTextMeasurer` for determinism (by design - that's the right
call for *those* tests). Searching the test tree turns up exactly one direct usage of
`ApproximateTextMeasurer.Instance`, and it's just to call `.Measure()` once inside an
unrelated image test ([ReportImageTests.cs:174](../tests/TerraFluent.Html.Reporting.Tests/Model/ReportImageTests.cs#L174)).
There is no test asserting the Helvetica-width-table wrapping decisions themselves: word
wrap at a given width, bold-multiplier behavior, explicit `\n` handling, empty-string
handling, or a single overflowing word not being hyphenated. The Playwright browser tests
validate the *renderer's* geometry, not whether the *approximate measurer's* line-wrap
decisions are reasonable relative to a real browser - so today, nothing in CI would catch
a regression in the one measurement path most users actually depend on.

**Fix**: add `Measurement/ApproximateTextMeasurerTests.cs` covering wrap boundaries, bold
width, multi-paragraph (`\n`-separated) input, and the documented non-hyphenation
behavior for a single too-wide word.

### 4. Hardcoded document title, no report metadata

`HtmlReportRenderer.RenderDocumentTo` writes a literal `<title>Report</title>` and no
other `<head>` metadata
([HtmlReportRenderer.cs:41](../src/TerraFluent.Html.Reporting/Rendering/HtmlReportRenderer.cs#L41)).
There's no way to set a document title, author, subject, or custom `<meta>` tags through
the fluent API - every generated report has the same browser tab title and nothing
machine-readable identifying what it is. For anything treated as a deliverable (an
invoice, a statement, an archived report) this matters more than it looks: the generated
file's title bar/PDF metadata is often the only identifying text once it's saved outside
its originating system.

**Fix**: add an optional `.Title(...)`/`.Metadata(...)` step to `ReportDocumentBuilder`,
thread it into `RenderContext` or a small `DocumentMetadata` record, and have
`HtmlReportRenderer` emit it. Low risk, additive, easy to test.

### 5. Barcode support is Code 128 only

`BarcodeImage` ([BarcodeImage.cs](../src/TerraFluent.Html.Reporting/Model/Elements/BarcodeImage.cs))
hand-rolls a Code 128 encoder plus a minimal uncompressed-PNG writer - a genuinely nice
zero-dependency implementation. Given barcodes were the headline feature of 1.1.1 and the
library already positions itself for invoices/labels, a QR code generator (needed for
payment references, tracking links, and ticket/label scenarios Code 128 can't cover) is
the natural next addition in the same style: no external dependency, same
`AddBarcode`-shaped API surface (`AddQrCode(value, ...)` returning `ImageElementBuilder`).

### 6. Already-documented, intentionally-deferred feature gaps

These are not discoveries - they're already listed accurately in
[12-faq-troubleshooting.md#known-limitations](12-faq-troubleshooting.md#known-limitations) -
but they are the real feature-shaped work still ahead, so they belong in this roadmap's
prioritization:

- No multi-column (newspaper-style) page layout - single content flow per page only.
- No RTL text support.
- No custom font embedding (CSS `font-family` reference only).
- Row columns can't contain a table, list, nested row, page break, or raw HTML; rows
  don't nest. `RowSpan` groups can't split across a page break.
- No exact/pixel-perfect measurer ships in the core package - `ITextMeasurer` is the
  extension point, but there's no reference implementation (e.g. a Playwright- or
  headless-Chromium-backed measurer) for consumers who need production parity, even
  though the repo *already* has a Playwright dependency in `BrowserTests` that could be
  adapted into a documented sample measurer.

## Priority roadmap

### P0 - next patch/minor release (cheap, concrete, high trust-per-effort) - done, see [Progress](#progress)

1. **Warn on auto-width collapse** (gap #1) - wire a `LayoutWarning` into the existing
   warnings pipeline instead of silently zeroing columns. Small, testable, no API break.
2. **Strengthen `LayoutWarning`** (gap #2) - add a structured `ElementType`/`ElementIndex`
   (or similar) before more warning producers are added, so gap #1's new warning and any
   future ones share one useful shape from the start.
3. **Test the default measurer directly** (gap #3) - close the one real coverage hole in
   an otherwise well-tested codebase.
4. **Document title/metadata** (gap #4) - small, additive, unblocks "this is a real
   deliverable" scenarios (invoices, statements) cheaply.
5. **Promote the Playwright cross-browser CI story** in the README/docs - this is a
   genuine differentiator that undersells itself currently; it's mentioned only in a CI
   comment, not in user-facing docs.

### P1 - next feature release - done, see [Progress](#progress)

6. **QR code generation** (gap #5), matching the existing `AddBarcode` API shape.
7. **Reference exact-measurement sample** - adapt the existing Playwright dependency
   (already present in `BrowserTests`) into a documented, sample `ITextMeasurer`
   implementation consumers can copy for pixel-parity needs, rather than asking every
   consumer to build one from scratch.
8. **Table/row explicit-width overflow policy** - once gap #1 emits a warning, consider
   an opt-in stricter mode (throw, or shrink proportionally) for consumers who want fail-
   fast behavior instead of a warning-and-continue default.

### P2 - larger, deliberate feature work (unchanged in spirit from before, now explicitly framed as "intentional, deferred" rather than "missing") - done, see [Progress](#progress)

9. Multi-column page sections (behind a conservative, opt-in feature flag; default
   single-column engine stays as-is).
10. Deeper, explicitly-supported composition patterns for rows/tables (not full nesting
    generality - specific, tested, documented combinations).
11. RTL text support.
12. Custom font embedding.
13. Template packs (invoice/statement/label recipes) and a richer sample gallery.
14. Preview/debug tooling (a local HTML preview harness that also surfaces
    `LayoutResult.Warnings` visually).

## Suggested sequencing

- **Release N+1 (patch/minor)**: P0 items 1-5. All are additive, low-risk, and each is
  independently shippable.
- **Release N+2**: P1 items 6-8.
- **Release N+3+**: P2, sequenced by consumer demand once P0/P1 land.

## Success metrics

- No pagination path silently drops or hides content without a `LayoutWarning` -
  including the auto-width case, which is the one remaining silent case today.
- The default measurer's wrapping behavior is protected by its own tests, not only
  exercised incidentally through other elements' tests.
- A consumer can identify a report's provenance (title/metadata) without reading its
  HTML source.
- The library's existing real-browser CI validation is visible to prospective adopters,
  not just to contributors reading the CI config.
