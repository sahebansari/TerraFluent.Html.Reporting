using TerraFluent.Html.Reporting.Compatibility;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>A column definition: its header text and, optionally, a fixed width.</summary>
public sealed class TableColumn
{
    private double? _widthPx;
    /// <summary>The text shown in the header row for this column.</summary>
    public string Header { get; }

    /// <summary>
    /// A fixed width in pixels, or <see langword="null"/> to share the width
    /// remaining after fixed-width columns equally among the other columns.
    /// </summary>
    public double? WidthPx
    {
        get => _widthPx;
        init => _widthPx = Guard.NonNegative(value, nameof(WidthPx));
    }

    /// <summary>Creates a table column.</summary>
    public TableColumn(string header)
    {
        Header = header ?? string.Empty;
    }

    /// <summary>Allows a plain string to be used wherever a column is expected.</summary>
    public static implicit operator TableColumn(string header) => new(header);
}
