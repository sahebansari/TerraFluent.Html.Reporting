using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;

namespace TerraFluent.Html.Reporting.Fluent;

/// <summary>Builds the column list for a <see cref="Row"/>; see <see cref="ContentBuilder.AddRow"/>/<see cref="PageSectionBuilder.AddRow"/>.</summary>
public sealed class RowBuilder
{
    private readonly List<RowColumn> _columns = new();

    /// <summary>Adds a column that shares the width left over after fixed-width columns equally with other auto-width columns.</summary>
    public RowColumnHandle AddColumn(Action<RowColumnBuilder> configure) => AddColumnCore(null, configure);

    /// <summary>Adds a column with an explicit fixed width.</summary>
    public RowColumnHandle AddColumn(double widthPx, Action<RowColumnBuilder> configure) => AddColumnCore(widthPx, configure);

    private RowColumnHandle AddColumnCore(double? widthPx, Action<RowColumnBuilder> configure)
    {
        var builder = new RowColumnBuilder();
        configure(builder);
        var column = new RowColumn(builder.Elements, widthPx);
        var index = _columns.Count;
        _columns.Add(column);
        return new RowColumnHandle(column, c => _columns[index] = c);
    }

    internal Row Build(double columnGapPx, RowVerticalAlignment verticalAlignment) => new(_columns, columnGapPx, verticalAlignment);
}
