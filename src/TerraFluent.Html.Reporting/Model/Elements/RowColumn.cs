using TerraFluent.Html.Reporting.Compatibility;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>One column of a <see cref="Row"/>: a list of elements stacked vertically, given a share of the row's width.</summary>
public sealed class RowColumn
{
    private double _paddingTopPx;
    private double _paddingRightPx;
    private double _paddingBottomPx;
    private double _paddingLeftPx;

    /// <summary>
    /// A fixed width in pixels, or <see langword="null"/> to share the width
    /// remaining after fixed-width columns (and column gaps) equally among the
    /// other auto-width columns.
    /// </summary>
    public double? WidthPx { get; }

    /// <summary>The elements stacked top-to-bottom within this column.</summary>
    public IReadOnlyList<IReportElement> Elements { get; }

    /// <summary>Inset, in pixels, between the column's box edge and its elements, on each side.</summary>
    public double PaddingTopPx
    {
        get => _paddingTopPx;
        init => _paddingTopPx = Guard.NonNegative(value, nameof(PaddingTopPx));
    }

    /// <summary>See <see cref="PaddingTopPx"/>.</summary>
    public double PaddingRightPx
    {
        get => _paddingRightPx;
        init => _paddingRightPx = Guard.NonNegative(value, nameof(PaddingRightPx));
    }

    /// <summary>See <see cref="PaddingTopPx"/>.</summary>
    public double PaddingBottomPx
    {
        get => _paddingBottomPx;
        init => _paddingBottomPx = Guard.NonNegative(value, nameof(PaddingBottomPx));
    }

    /// <summary>See <see cref="PaddingTopPx"/>.</summary>
    public double PaddingLeftPx
    {
        get => _paddingLeftPx;
        init => _paddingLeftPx = Guard.NonNegative(value, nameof(PaddingLeftPx));
    }

    /// <summary>Creates a row column.</summary>
    public RowColumn(IReadOnlyList<IReportElement> elements, double? widthPx = null)
    {
        Elements = Guard.Snapshot(elements, nameof(elements));
        WidthPx = Guard.NonNegative(widthPx, nameof(widthPx));
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
