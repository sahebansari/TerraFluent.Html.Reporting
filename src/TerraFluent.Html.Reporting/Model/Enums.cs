namespace TerraFluent.Html.Reporting.Model;

/// <summary>
/// The orientation in which a page is laid out.
/// </summary>
public enum PageOrientation
{
    /// <summary>Page height is greater than its width.</summary>
    Portrait,

    /// <summary>Page width is greater than its height.</summary>
    Landscape,
}

/// <summary>
/// Semantic heading levels, mirroring HTML's H1-H6, each mapped to a default
/// font size/weight in the style scale unless overridden.
/// </summary>
public enum HeadingLevel
{
    /// <summary>Largest heading level, typically used once per document or section.</summary>
    H1 = 1,

    /// <summary>Second-largest heading level.</summary>
    H2 = 2,

    /// <summary>Third heading level.</summary>
    H3 = 3,

    /// <summary>Fourth heading level.</summary>
    H4 = 4,

    /// <summary>Fifth heading level.</summary>
    H5 = 5,

    /// <summary>Smallest heading level.</summary>
    H6 = 6,
}

/// <summary>
/// Horizontal alignment for text and block-level elements.
/// </summary>
public enum TextAlignment
{
    /// <summary>Aligned to the left edge of the content area.</summary>
    Left,

    /// <summary>Centered within the content area.</summary>
    Center,

    /// <summary>Aligned to the right edge of the content area.</summary>
    Right,

    /// <summary>Stretched so both edges align, with extra space distributed between words.</summary>
    Justify,
}

/// <summary>
/// Font weight for text styling.
/// </summary>
public enum FontWeight
{
    /// <summary>Normal weight (CSS 400).</summary>
    Normal,

    /// <summary>Bold weight (CSS 700).</summary>
    Bold,
}

/// <summary>
/// Font style/slant for text styling.
/// </summary>
public enum FontStyle
{
    /// <summary>Upright glyphs.</summary>
    Normal,

    /// <summary>Slanted/italic glyphs.</summary>
    Italic,
}

/// <summary>
/// Numbering style for <see cref="Model.Elements.ReportList"/>.
/// </summary>
public enum ListStyle
{
    /// <summary>Unordered list, rendered with bullet markers.</summary>
    Bulleted,

    /// <summary>Ordered list, rendered with sequential numbers.</summary>
    Numbered,
}

/// <summary>
/// Controls how a <see cref="Model.Elements.Table"/> behaves when a row would otherwise
/// be cut across a page boundary.
/// </summary>
public enum RowSplitBehavior
{
    /// <summary>
    /// Never split a row's content. If a row does not fully fit in the remaining
    /// content height, the whole row moves to the next page.
    /// </summary>
    KeepRowIntact,

    /// <summary>
    /// Allow a tall row's cell content to split across pages. The header row is
    /// repeated at the top of the continuation page, optionally annotated as
    /// "(continued)".
    /// </summary>
    AllowSplitWithContinuedHeader,
}

/// <summary>
/// Controls how a <see cref="Model.Elements.RowColumn"/>'s content is positioned
/// vertically within its <see cref="Model.Elements.Row"/>'s height, when other
/// columns in the same row are taller.
/// </summary>
public enum RowVerticalAlignment
{
    /// <summary>Aligned to the top of the row.</summary>
    Top,

    /// <summary>Centered within the row's height.</summary>
    Middle,

    /// <summary>Aligned to the bottom of the row.</summary>
    Bottom,
}

/// <summary>
/// Controls how a <see cref="Model.Elements.Table"/>/<see cref="Model.Elements.Row"/>
/// reacts when an auto-width column resolves to 0px because its fixed-width
/// columns already consume the full width available to it.
/// </summary>
public enum ColumnWidthOverflowMode
{
    /// <summary>
    /// Record a <see cref="Layout.LayoutWarning"/> with
    /// <see cref="Layout.LayoutWarningReason.ColumnWidthCollapsed"/> and
    /// continue - the collapsed column's content will not be visible.
    /// </summary>
    Warn,

    /// <summary>Throw an <see cref="InvalidOperationException"/> instead of warning.</summary>
    Throw,
}

/// <summary>
/// Text direction for a text-bearing element - emitted as both the HTML
/// <c>dir</c> attribute and the CSS <c>direction</c> property. Independent
/// of <see cref="TextAlignment"/>: setting <see cref="Rtl"/> does not itself
/// change <c>Left</c>/<c>Right</c> alignment to a "logical" start/end -
/// margin/padding/alignment on <c>TextStyle</c> remain physical properties,
/// same as plain CSS <c>left</c>/<c>right</c> (as opposed to logical
/// <c>start</c>/<c>end</c>) values - set both explicitly for a fully
/// mirrored right-to-left layout.
/// </summary>
public enum TextDirection
{
    /// <summary>Left-to-right (the default).</summary>
    Ltr,

    /// <summary>Right-to-left.</summary>
    Rtl,
}

/// <summary>
/// Identifies which repeating section of a page an element belongs to.
/// </summary>
public enum PageSectionKind
{
    /// <summary>The fixed-height section repeated at the top of every page.</summary>
    Header,

    /// <summary>The paginated flow area that content is laid out into.</summary>
    Content,

    /// <summary>The fixed-height section repeated at the bottom of every page.</summary>
    Footer,
}
