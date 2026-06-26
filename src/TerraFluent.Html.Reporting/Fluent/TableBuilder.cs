using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Fluent;

/// <summary>Builds a <see cref="Table"/>; see <see cref="ContentBuilder.AddTable"/>.</summary>
public sealed class TableBuilder
{
    private readonly List<TableColumn> _columns = new();
    private readonly List<TableRow> _rows = new();

    /// <summary>Adds one column per header string given.</summary>
    public TableBuilder AddColumns(params string[] headers)
    {
        foreach (var header in headers)
        {
            _columns.Add(header);
        }

        return this;
    }

    /// <summary>Adds a single column, allowing an explicit fixed width.</summary>
    public TableBuilder AddColumn(string header, double? widthPx = null)
    {
        _columns.Add(new TableColumn(header) { WidthPx = widthPx });
        return this;
    }

    /// <summary>Adds a row from plain cell strings, one per column.</summary>
    public TableBuilder AddRow(params string[] cells)
    {
        _rows.Add(new TableRow(cells.Select(c => (TableCell)c).ToList()));
        return this;
    }

    /// <summary>Adds a row from explicit cells, allowing per-cell style overrides.</summary>
    public TableBuilder AddRow(IReadOnlyList<TableCell> cells)
    {
        _rows.Add(new TableRow(cells));
        return this;
    }

    internal Table Build(TableStyle? style) => new(_columns, _rows, style);
}
