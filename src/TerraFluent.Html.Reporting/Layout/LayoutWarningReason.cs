namespace TerraFluent.Html.Reporting.Layout;

/// <summary>
/// Categorizes why a <see cref="LayoutWarning"/> was raised, so a consumer can
/// filter/group warnings programmatically instead of parsing <see cref="LayoutWarning.Message"/>.
/// </summary>
public enum LayoutWarningReason
{
    /// <summary>
    /// An element didn't fit, and couldn't be split, even on a completely
    /// empty page - it was force-placed and will overflow visually.
    /// </summary>
    Overflow,

    /// <summary>
    /// A <see cref="Model.Elements.Table"/>/<see cref="Model.Elements.Row"/>'s
    /// auto-width column resolved to 0px because its fixed-width columns
    /// already consume the full width available to it - the column's content
    /// is not visible.
    /// </summary>
    ColumnWidthCollapsed,
}
