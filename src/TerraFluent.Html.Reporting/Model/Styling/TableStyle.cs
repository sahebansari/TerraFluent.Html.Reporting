using TerraFluent.Html.Reporting.Compatibility;

namespace TerraFluent.Html.Reporting.Model.Styling;

/// <summary>
/// Visual styling applied to a <see cref="Elements.Table"/>. Immutable; use
/// <see cref="With"/> to derive a modified copy.
/// </summary>
public sealed class TableStyle
{
    private double _cellPaddingPx = 6;
    private double _borderWidthPx = 1;

    /// <summary>The default style used when a table does not specify one.</summary>
    public static TableStyle Default { get; } = new();

    /// <summary>Style applied to header-row cell text.</summary>
    public TextStyle HeaderTextStyle { get; init; } =
        TextStyle.Default.With(fontWeight: FontWeight.Bold, color: "#ffffff", marginBottomPx: 0);

    /// <summary>Style applied to body-row cell text.</summary>
    public TextStyle CellTextStyle { get; init; } = TextStyle.Default.With(marginBottomPx: 0);

    /// <summary>Background color of the header row.</summary>
    public string HeaderBackgroundColor { get; init; } = "#2f4858";

    /// <summary>Background color of even-indexed body rows (0-based) when striping is enabled.</summary>
    public string EvenRowBackgroundColor { get; init; } = "#ffffff";

    /// <summary>Background color of odd-indexed body rows (0-based) when striping is enabled.</summary>
    public string OddRowBackgroundColor { get; init; } = "#f4f6f7";

    /// <summary>Whether alternating row backgrounds are applied.</summary>
    public bool StripedRows { get; init; } = true;

    /// <summary>Cell padding, in pixels, applied on all four sides of every cell.</summary>
    public double CellPaddingPx
    {
        get => _cellPaddingPx;
        init => _cellPaddingPx = Guard.NonNegative(value, nameof(CellPaddingPx));
    }

    /// <summary>Border color between cells/rows; null/empty means no visible border.</summary>
    public string BorderColor { get; init; } = "#d8dde0";

    /// <summary>Border thickness in pixels.</summary>
    public double BorderWidthPx
    {
        get => _borderWidthPx;
        init => _borderWidthPx = Guard.NonNegative(value, nameof(BorderWidthPx));
    }

    /// <summary>How the table behaves when a row would otherwise be cut across a page boundary.</summary>
    public RowSplitBehavior RowSplitBehavior { get; init; } = RowSplitBehavior.AllowSplitWithContinuedHeader;

    /// <summary>
    /// Suffix appended to the repeated header on continuation pages when
    /// <see cref="RowSplitBehavior"/> is <see cref="RowSplitBehavior.AllowSplitWithContinuedHeader"/>,
    /// e.g. " (continued)". Set to an empty string to suppress.
    /// </summary>
    public string ContinuedHeaderSuffix { get; init; } = " (continued)";

    /// <summary>
    /// Returns a copy of this style with the given properties overridden, leaving
    /// all others unchanged.
    /// </summary>
    public TableStyle With(
        TextStyle? headerTextStyle = null,
        TextStyle? cellTextStyle = null,
        string? headerBackgroundColor = null,
        string? evenRowBackgroundColor = null,
        string? oddRowBackgroundColor = null,
        bool? stripedRows = null,
        double? cellPaddingPx = null,
        string? borderColor = null,
        double? borderWidthPx = null,
        RowSplitBehavior? rowSplitBehavior = null,
        string? continuedHeaderSuffix = null) => new()
    {
        HeaderTextStyle = headerTextStyle ?? HeaderTextStyle,
        CellTextStyle = cellTextStyle ?? CellTextStyle,
        HeaderBackgroundColor = headerBackgroundColor ?? HeaderBackgroundColor,
        EvenRowBackgroundColor = evenRowBackgroundColor ?? EvenRowBackgroundColor,
        OddRowBackgroundColor = oddRowBackgroundColor ?? OddRowBackgroundColor,
        StripedRows = stripedRows ?? StripedRows,
        CellPaddingPx = cellPaddingPx ?? CellPaddingPx,
        BorderColor = borderColor ?? BorderColor,
        BorderWidthPx = borderWidthPx ?? BorderWidthPx,
        RowSplitBehavior = rowSplitBehavior ?? RowSplitBehavior,
        ContinuedHeaderSuffix = continuedHeaderSuffix ?? ContinuedHeaderSuffix,
    };
}
