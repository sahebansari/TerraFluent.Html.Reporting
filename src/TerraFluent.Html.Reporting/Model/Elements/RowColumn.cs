namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>One column of a <see cref="Row"/>: a list of elements stacked vertically, given a share of the row's width.</summary>
public sealed class RowColumn
{
    /// <summary>
    /// A fixed width in pixels, or <see langword="null"/> to share the width
    /// remaining after fixed-width columns (and column gaps) equally among the
    /// other auto-width columns.
    /// </summary>
    public double? WidthPx { get; }

    /// <summary>The elements stacked top-to-bottom within this column.</summary>
    public IReadOnlyList<IReportElement> Elements { get; }

    /// <summary>Inset, in pixels, between the column's box edge and its elements, on each side.</summary>
    public double PaddingTopPx { get; init; } = 0;

    /// <summary>See <see cref="PaddingTopPx"/>.</summary>
    public double PaddingRightPx { get; init; } = 0;

    /// <summary>See <see cref="PaddingTopPx"/>.</summary>
    public double PaddingBottomPx { get; init; } = 0;

    /// <summary>See <see cref="PaddingTopPx"/>.</summary>
    public double PaddingLeftPx { get; init; } = 0;

    /// <summary>Creates a row column.</summary>
    public RowColumn(IReadOnlyList<IReportElement> elements, double? widthPx = null)
    {
        Elements = elements ?? throw new ArgumentNullException(nameof(elements));
        if (widthPx is < 0) throw new ArgumentOutOfRangeException(nameof(widthPx));
        WidthPx = widthPx;
    }

    /// <summary>Returns a copy of this column with the given padding overridden, leaving its width and elements unchanged.</summary>
    public RowColumn With(
        double? paddingTopPx = null,
        double? paddingRightPx = null,
        double? paddingBottomPx = null,
        double? paddingLeftPx = null) => new(Elements, WidthPx)
    {
        PaddingTopPx = paddingTopPx ?? PaddingTopPx,
        PaddingRightPx = paddingRightPx ?? PaddingRightPx,
        PaddingBottomPx = paddingBottomPx ?? PaddingBottomPx,
        PaddingLeftPx = paddingLeftPx ?? PaddingLeftPx,
    };
}
