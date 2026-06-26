using FluentHtmlReport.Model.Elements;

namespace FluentHtmlReport.Fluent;

/// <summary>
/// Returned by <see cref="RowBuilder.AddColumn(Action{RowColumnBuilder})"/> to let
/// the caller chain padding modifiers directly after configuring a column's
/// content, e.g. <c>row.AddColumn(col => col.AddText("Hi")).Padding(8)</c>.
/// Mirrors <see cref="TextElementBuilder"/>'s pattern: <see cref="RowColumn"/> is
/// immutable, so each modifier rebuilds it via <see cref="RowColumn.With"/> and
/// replaces it in the owning row's column list.
/// </summary>
public sealed class RowColumnHandle
{
    private readonly Action<RowColumn> _replace;
    private RowColumn _current;

    internal RowColumnHandle(RowColumn initial, Action<RowColumn> replace)
    {
        _current = initial;
        _replace = replace;
    }

    private RowColumnHandle Apply(Func<RowColumn, RowColumn> mutate)
    {
        _current = mutate(_current);
        _replace(_current);
        return this;
    }

    /// <summary>Sets uniform padding (inset between the column's box edge and its elements) on all four sides, in pixels.</summary>
    public RowColumnHandle Padding(double px) => Apply(c => c.With(paddingTopPx: px, paddingRightPx: px, paddingBottomPx: px, paddingLeftPx: px));

    /// <summary>Sets padding per side, in pixels (CSS shorthand order: top, right, bottom, left).</summary>
    public RowColumnHandle Padding(double topPx, double rightPx, double bottomPx, double leftPx) =>
        Apply(c => c.With(paddingTopPx: topPx, paddingRightPx: rightPx, paddingBottomPx: bottomPx, paddingLeftPx: leftPx));
}
