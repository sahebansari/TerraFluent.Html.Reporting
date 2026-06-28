using TerraFluent.Html.Reporting.Compatibility;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>One row of a <see cref="Table"/>: a fixed-order list of cells, one per column.</summary>
public sealed class TableRow
{
    /// <summary>The row's cells, in column order.</summary>
    public IReadOnlyList<TableCell> Cells { get; }

    /// <summary>Creates a table row.</summary>
    public TableRow(IReadOnlyList<TableCell> cells)
    {
        Cells = Guard.Snapshot(cells, nameof(cells));
    }
}
