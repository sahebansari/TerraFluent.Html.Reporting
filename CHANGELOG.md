# Changelog

All notable changes to this project are documented in this file.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

### Added
- `AddRow` on both `Header`/`Footer` builders and `Content`: lays out side-by-side columns (e.g. a logo next to a company name), each column stacking its own elements vertically. Supports fixed or auto-shared column widths, a configurable column gap, and top/middle/bottom vertical alignment (`RowVerticalAlignment`). Like an image, a row never splits across pages.
- Margin, padding, and alignment fluent modifiers across the element API, every `Add*` method now returns a builder/handle you can chain them on:
  - `AddParagraph`/`AddHeading`/`AddText`/`AddPageNumber`: `TextElementBuilder` gained `MarginTop/Right/Bottom/Left`, `Margin(...)`, and `Padding(...)` (padding insets the wrapped text from its own box; existing `Alignment`/`MarginBottom` unchanged).
  - `AddImage`: now returns `ImageElementBuilder` with `AlignLeft/Center/Right`, `Margin(...)`, and `Padding(...)` - images can finally be positioned within a container wider than themselves instead of always sitting flush-left.
  - `AddRow`: now returns `RowHandle` with `Margin(...)` for the row as a whole.
  - `RowBuilder.AddColumn`: now returns `RowColumnHandle` with `Padding(...)` to inset a column's stacked content from its own edges.

### Changed
- `TextStyle` gained `MarginTopPx`/`MarginRightPx`/`MarginLeftPx` and `PaddingTopPx/RightPx/BottomPx/LeftPx` (all default to `0`, so existing styles render unchanged). `ReportImage` and `Row` gained the equivalent margin properties; `RowColumn` gained padding properties.

### Fixed
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
