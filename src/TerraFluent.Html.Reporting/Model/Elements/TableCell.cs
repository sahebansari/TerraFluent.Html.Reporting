using TerraFluent.Html.Reporting.Compatibility;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>A single table cell. Falls back to the table's header/body text style when <see cref="Style"/> is not set.</summary>
public sealed class TableCell
{
    private int _colSpan = 1;
    private int _rowSpan = 1;

    /// <summary>The cell's text content.</summary>
    public string Text { get; }

    /// <summary>A per-cell style override, or <see langword="null"/> to inherit from <see cref="Styling.TableStyle"/>.</summary>
    public TextStyle? Style { get; init; }

    /// <summary>
    /// How many columns this cell occupies, starting at its own column. Default <c>1</c>.
    /// A row's cells must account for exactly <see cref="Table.Columns"/>.Count columns once
    /// spans (and any carried-over <see cref="RowSpan"/> from earlier rows) are taken into
    /// account - see <see cref="Table"/>.
    /// </summary>
    public int ColSpan
    {
        get => _colSpan;
        init => _colSpan = Guard.Positive(value, nameof(ColSpan));
    }

    /// <summary>
    /// How many rows this cell occupies, starting at its own row. Default <c>1</c>. Rows this
    /// cell's span carries into must omit a cell for the column(s) it covers. A cell with
    /// <see cref="RowSpan"/> greater than <c>1</c> links its rows into an atomic group for
    /// pagination purposes - see <see cref="Table"/>.
    /// </summary>
    public int RowSpan
    {
        get => _rowSpan;
        init => _rowSpan = Guard.Positive(value, nameof(RowSpan));
    }

    /// <summary>Creates a table cell.</summary>
    public TableCell(string text, TextStyle? style = null)
    {
        Text = text ?? string.Empty;
        Style = style;
    }

    /// <summary>Allows a plain string to be used wherever a cell is expected.</summary>
    public static implicit operator TableCell(string text) => new(text);
}
