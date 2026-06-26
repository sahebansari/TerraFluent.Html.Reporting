using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>A single table cell. Falls back to the table's header/body text style when <see cref="Style"/> is not set.</summary>
public sealed class TableCell
{
    /// <summary>The cell's text content.</summary>
    public string Text { get; }

    /// <summary>A per-cell style override, or <see langword="null"/> to inherit from <see cref="Styling.TableStyle"/>.</summary>
    public TextStyle? Style { get; init; }

    /// <summary>Creates a table cell.</summary>
    public TableCell(string text, TextStyle? style = null)
    {
        Text = text ?? string.Empty;
        Style = style;
    }

    /// <summary>Allows a plain string to be used wherever a cell is expected.</summary>
    public static implicit operator TableCell(string text) => new(text);
}
