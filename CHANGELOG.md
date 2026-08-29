# Changelog

All notable changes to this project are documented in this file.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).


## [Unreleased]

## [1.2.0] - 2026-08-30

### Added
- `AddColumns(columnCount, configure, columnGapPx)` on `ContentBuilder`: an
  opt-in, conservative multi-column ("newspaper-style") content block -
  equal-width columns, fill-then-wrap only (no balancing), content
  distributed across all columns even when it would otherwise fit as a
  single column. A child that supports splitting (paragraph, table) splits
  across a column boundary exactly as it already splits across a page
  boundary; an oversized, unsplittable child force-places into its own
  column with a `LayoutWarning`. Nested multi-column sections and `AddRow`
  inside a column are not supported. See `MultiColumnSection` and
  [docs/06-rows-and-columns.md](docs/06-rows-and-columns.md#multi-column-sections-addcolumns).
- `TextStyle.Direction` (`TextDirection.Ltr`/`.Rtl`): sets the HTML `dir`
  attribute and CSS `direction` property on paragraphs, headings, lists,
  and table cells. Independent of `Alignment`/margin/padding, which remain
  physical (not logical) properties - see
  [docs/04-styling.md#text-direction](docs/04-styling.md#text-direction).
- `ReportDocumentBuilder.EmbedFont(fontFamily, fontBytesOrFilePath, weight, style, mimeType)`:
  embeds a font file directly into the generated HTML as a base64
  `@font-face` rule, so a report no longer depends on that font being
  installed wherever the HTML is opened or printed.
- [docs/15-composition-patterns.md](docs/15-composition-patterns.md): a new
  single reference for what can nest inside what across `Row`, `Table`, and
  `AddColumns`, and why each restriction exists.
- New sample scenarios: a shipping label template (`15-shipping-label.html`,
  a compact custom page size combining a barcode and a QR code), a
  multi-column newsletter layout (`14-multi-column.html`), embedded custom
  fonts (`16-custom-fonts.html`, using the OFL-licensed Pacifico font),
  right-to-left text (`17-rtl-text.html`), and column-width diagnostics
  (`18-column-diagnostics.html`). The sample runner now also prints each
  `LayoutWarning`'s structured `Reason`/`ElementType`/`PageIndex`/`ElementIndex`
  fields instead of only its free-form message, and a few realistic
  scenarios (getting started, sales invoice, shipping label) now set
  `.Title(...)`.
- A minimal preview/debug console harness at
  `samples/TerraFluent.Html.Reporting.Sample.Preview` - paginates a report,
  prints its warnings with their structured fields, and opens the generated
  HTML in the default browser.
- `AddQrCode(value, moduleWidthPx, quietZoneModules)` on `ContentBuilder`,
  `PageSectionBuilder` (header/footer), and `RowColumnBuilder`: generates a
  QR code (ISO/IEC 18004) natively as a PNG - no external library or web
  service - matching `AddBarcode`'s API shape (alignment/margin/padding via
  the same `ImageElementBuilder`). Scoped to byte-mode (UTF-8) encoding, a
  single fixed error-correction level ("M"), and an auto-selected version
  (1-40); numeric/alphanumeric mode compaction and a selectable error-correction
  level are not supported. Verified end-to-end against an independent QR
  decoder across multiple versions (including the 1-/2-byte count-indicator
  boundary at version 10) and UTF-8 multi-byte content.
- `ReportDocumentBuilder.UseStrictLayoutValidation(strict)`: opts the whole
  document into throwing `InvalidOperationException` (instead of only
  recording a `LayoutWarning`) when a table/row's auto-width column
  collapses to 0px. A single table/row can opt in individually instead via
  the new `TableStyle.ColumnWidthOverflowMode`/`RowStyle.ColumnWidthOverflowMode`
  (new `RowStyle` type, passable to `AddRow`), regardless of the document-level
  setting.
- A reference `ITextMeasurer` sample backed by headless Chromium (via
  Playwright), measuring word widths with the browser's actual installed
  font instead of the default's static Helvetica table - see
  `samples/TerraFluent.Html.Reporting.Sample.PlaywrightMeasurer` and its
  README for what it improves on and its limits.
- `ReportDocumentBuilder.Title(string)`: sets the document's title, rendered
  (HTML-encoded) as the generated HTML's `<title>` (falls back to `"Report"`
  when unset, unchanged from before) and exposed to custom `IHtmlReportRenderer`
  implementations via the new `LayoutResult.Title`.
- `LayoutWarningReason` enum (`Overflow`, `ColumnWidthCollapsed`) and three new
  `LayoutWarning` properties - `Reason`, `ElementType`, `ElementIndex` - so
  warnings can be filtered/grouped programmatically instead of parsing
  `Message`. The existing two-argument `LayoutWarning(pageIndex, message)`
  constructor is unchanged and defaults `Reason` to `Overflow`.
- `Table`/`Row` now report a `LayoutWarningReason.ColumnWidthCollapsed`
  warning when an auto-width column resolves to 0px because the table/row's
  fixed-width columns already consume the full available width - previously
  this collapsed silently with no signal (see
  [12-faq-troubleshooting.md#known-limitations](docs/12-faq-troubleshooting.md#known-limitations)).

### Changed
- README and [docs/08-rendering.md](docs/08-rendering.md#tested-against-real-browsers)
  now call out that CI renders generated reports in real, headless Chromium,
  Firefox, and WebKit (via Playwright) under `@media print` and asserts on
  their rendered geometry - this was previously only visible in a CI config
  comment.

### Testing
- Added direct unit tests for the default `ApproximateTextMeasurer` (word-wrap
  boundaries, bold width multiplier, multi-paragraph/`\n` handling, and the
  documented non-hyphenation behavior for an overflowing word) - previously
  every test substituted a fake measurer for determinism, leaving the actual
  shipped default measurement code untested in isolation.

## [1.1.1] - 2026-07-03

### Added
- `AddBarcode(value, moduleWidthPx, heightPx, quietZoneModules)` on `ContentBuilder`, `PageSectionBuilder` (header/footer), and `RowColumnBuilder`: generates a Code 128 barcode natively as a PNG `ReportImage` - no external barcode library or web service involved. Returns the same `ImageElementBuilder` as `AddImage`, so it supports `AlignLeft/Center/Right`, `Margin(...)`, and `Padding(...)` like any other image. `value` must be non-empty printable ASCII (32-126); anything else throws `ArgumentException`.
- `TableCell.ColSpan`/`RowSpan` (both default `1`): a cell can now span multiple columns and/or rows, for merged header/summary cells or a category cell grouping several rows. A row must supply exactly enough cells to account for every column once spans (and any `RowSpan` carried over from an earlier row) are taken into account; a mismatch, or a span extending past the table's last column/row, throws `ArgumentException`. Rows linked by an active `RowSpan` are treated as one atomic group for pagination - they either fit together on a page or move to the next page as a whole, even under `AllowSplitWithContinuedHeader`.

### Changed
- Reworked the root and package `README.md` with a clearer quick start, a "What It Supports" summary, and an explicit Code 128 barcode callout.
- Added a "View Sample Reports" badge to both `README.md`s, linking to the GitHub Pages-hosted sample output.

### Fixed
- CI's `publish` job had lost its `if:` gate and its tag/package version verification step; both are restored, so publishing again only runs for a pushed `v*` tag and only after confirming the tag matches `<Version>` in `Directory.Build.props`.

## [1.0.0] - 2026-06-30

### Added
- Stable-release package metadata, including a NuGet package icon.
- A `netstandard2.0` consumer smoke project so CI validates the asset used by
  non-`net10.0` consumers.
- A public API snapshot guard for the exported type surface.

### Changed
- Public documentation and release checklist now describe the stable package
  rather than the alpha prerelease.
- README wording now distinguishes fixed page geometry/computed pagination from
  pixel-perfect text wrapping, which still requires a custom `ITextMeasurer`.

## [0.2.0-alpha.1] - 2026-06-28

### Added
- `ContentBuilder.AddElement(IReportElement)` makes the documented custom-element extension point usable through the fluent API.
- `AddRow` on both `Header`/`Footer` builders and `Content`: lays out side-by-side columns (e.g. a logo next to a company name), each column stacking its own elements vertically. Supports fixed or auto-shared column widths, a configurable column gap, and top/middle/bottom vertical alignment (`RowVerticalAlignment`). Like an image, a row never splits across pages.
- Margin, padding, and alignment fluent modifiers across the element API, every `Add*` method now returns a builder/handle you can chain them on:
  - `AddParagraph`/`AddHeading`/`AddText`/`AddPageNumber`: `TextElementBuilder` gained `MarginTop/Right/Bottom/Left`, `Margin(...)`, and `Padding(...)` (padding insets the wrapped text from its own box; existing `Alignment`/`MarginBottom` unchanged).
  - `AddImage`: now returns `ImageElementBuilder` with `AlignLeft/Center/Right`, `Margin(...)`, and `Padding(...)` - images can finally be positioned within a container wider than themselves instead of always sitting flush-left.
  - `AddRow`: now returns `RowHandle` with `Margin(...)` for the row as a whole.
  - `RowBuilder.AddColumn`: now returns `RowColumnHandle` with `Padding(...)` to inset a column's stacked content from its own edges.

### Changed
- `TextStyle` gained `MarginTopPx`/`MarginRightPx`/`MarginLeftPx` and `PaddingTopPx/RightPx/BottomPx/LeftPx` (all default to `0`, so existing styles render unchanged). `ReportImage` and `Row` gained the equivalent margin properties; `RowColumn` gained padding properties.

### Fixed
- Paragraph and split-table fragments preserve measured line boundaries instead of replacing them with spaces, so explicit newlines survive pagination and fragments re-measure consistently.
- Table row splitting now budgets against the tallest cell line (and reserves the row border), preventing mixed-font-size rows from producing a head fragment taller than the available page space.
- Dynamic CSS and image MIME values are HTML-attribute encoded, preventing quotes in those values from breaking out of generated `style`/`src` attributes.
- `RenderFragment`/`RenderFragmentTo` no longer emit global `html`/`body` reset, background, or font rules that mutate the host page.
- `ReportImage` rendered every image stretched to the full content width (only height respected the requested/derived size). The `<img>` tag now uses the image's own resolved width and height.
- `Table` under-measured its own rendered height by the table's border width: `Measure`/`Split` summed cell text + padding per row but never accounted for `TableStyle.BorderWidthPx`, so the `overflow:hidden` container `RenderHtml` wraps the `<table>` in was sized slightly too short, silently clipping the last row's bottom border (and, on a split table, the continuation banner's). Row/header/banner heights now each include one border-line's worth of height, plus one extra for the table's outermost top edge.
- `Table` also pinned its `<table>` to `width:100%`, which (verified in a real browser) left the fixed-table-layout algorithm zero leftover space to draw the table's own outer border, silently dropping the rightmost column's right border (and, less visibly, the leftmost column's left border) regardless of `BorderWidthPx`. The table now sizes itself from its `<colgroup>` widths (which already sum to the intended content width) instead, leaving room for both outer borders without changing any column's rendered position.

## [0.1.0-alpha] - 2026-06-23

Initial pre-release. Core document model, pagination engine, and HTML renderer.

### Added
- Fluent builder API: `ReportDocument.Create(...).SetMargins(...).Header(...).Footer(...).Content(...).Build()`.
- Page sizes (A4/Letter/Legal/custom px/mm/inches) with portrait/landscape orientation.
- Content elements: `Paragraph`, `Heading`, `ReportImage`, `Table`, `ReportList`, `HorizontalRule`, `Spacer`, `PageNumberText`, `RawHtml`, `PageBreak`.
- Pagination engine (`LayoutEngine`) that measures and splits elements across pages: paragraphs split at line boundaries with widow/orphan control, tables split at row boundaries with an optional mid-row split and a repeated, "(continued)"-annotated header, numbered lists resume numbering correctly across a page break.
- `LayoutResult.Warnings` surfaces non-fatal pagination problems (e.g. an unsplittable element that overflowed an empty page) instead of failing silently.
- Zero-dependency `ApproximateTextMeasurer` (Helvetica-metrics-based word wrapping); `ITextMeasurer` is a public extension point for precise, renderer-backed measurement.
- `HtmlReportRenderer` emits a self-contained HTML document (inline `<style>`, `@page` sizing, one absolutely-positioned page `<div>` per page) targeting browser print-to-PDF.
- Streaming render APIs (`RenderDocumentTo`/`RenderFragmentTo` write to a `TextWriter` page-by-page) and `RenderHtmlDocumentAsync` for large documents, plus `CancellationToken` support throughout pagination and rendering.
- Multi-targets `netstandard2.0` and `net10.0`.

### Known limitations
- Text measurement is approximate by default; exact pagination requires supplying a custom `ITextMeasurer`.
- No custom font embedding, multi-column layout, cell colspan/rowspan, or RTL text support yet.
