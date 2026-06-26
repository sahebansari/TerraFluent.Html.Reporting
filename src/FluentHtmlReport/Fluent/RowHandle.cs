using FluentHtmlReport.Model.Elements;

namespace FluentHtmlReport.Fluent;

/// <summary>
/// Returned by <c>AddRow</c> to let the caller chain margin modifiers directly
/// after configuring a row, e.g. <c>c.AddRow(row => ...).MarginTop(12)</c>.
/// Mirrors <see cref="TextElementBuilder"/>'s pattern: <see cref="Row"/> is
/// immutable, so each modifier rebuilds it via <see cref="Row.With"/> and
/// replaces it in the owning section/content list.
/// </summary>
public sealed class RowHandle
{
    private readonly Action<Row> _replace;
    private Row _current;

    internal RowHandle(Row initial, Action<Row> replace)
    {
        _current = initial;
        _replace = replace;
    }

    private RowHandle Apply(Func<Row, Row> mutate)
    {
        _current = mutate(_current);
        _replace(_current);
        return this;
    }

    /// <summary>Sets the space above the row, in pixels.</summary>
    public RowHandle MarginTop(double px) => Apply(r => r.With(marginTopPx: px));

    /// <summary>Sets the space to the right of the row, in pixels.</summary>
    public RowHandle MarginRight(double px) => Apply(r => r.With(marginRightPx: px));

    /// <summary>Sets the space below the row, in pixels.</summary>
    public RowHandle MarginBottom(double px) => Apply(r => r.With(marginBottomPx: px));

    /// <summary>Sets the space to the left of the row, in pixels.</summary>
    public RowHandle MarginLeft(double px) => Apply(r => r.With(marginLeftPx: px));

    /// <summary>Sets uniform margin on all four sides, in pixels.</summary>
    public RowHandle Margin(double px) => Apply(r => r.With(marginTopPx: px, marginRightPx: px, marginBottomPx: px, marginLeftPx: px));

    /// <summary>Sets margin per side, in pixels (CSS shorthand order: top, right, bottom, left).</summary>
    public RowHandle Margin(double topPx, double rightPx, double bottomPx, double leftPx) =>
        Apply(r => r.With(marginTopPx: topPx, marginRightPx: rightPx, marginBottomPx: bottomPx, marginLeftPx: leftPx));
}
