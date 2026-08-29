namespace TerraFluent.Html.Reporting.Model.Styling;

/// <summary>
/// Behavioral settings for a <see cref="Elements.Row"/>, separate from its
/// layout constructor parameters (<c>ColumnGapPx</c>/<c>VerticalAlignment</c>)
/// so future row-level settings have a natural, additive home. Immutable; use
/// <see cref="With"/> to derive a modified copy.
/// </summary>
public sealed class RowStyle
{
    /// <summary>The default style used when a row does not specify one.</summary>
    public static RowStyle Default { get; } = new();

    /// <summary>
    /// How this row reacts when an auto-width column resolves to 0px. Also
    /// throws (regardless of this setting) when the document itself opts into
    /// strict layout validation via <c>ReportDocumentBuilder.UseStrictLayoutValidation</c>.
    /// </summary>
    public ColumnWidthOverflowMode ColumnWidthOverflowMode { get; init; } = ColumnWidthOverflowMode.Warn;

    /// <summary>
    /// Returns a copy of this style with the given properties overridden, leaving
    /// all others unchanged.
    /// </summary>
    public RowStyle With(ColumnWidthOverflowMode? columnWidthOverflowMode = null) => new()
    {
        ColumnWidthOverflowMode = columnWidthOverflowMode ?? ColumnWidthOverflowMode,
    };
}
