using TerraFluent.Html.Reporting.Compatibility;

namespace TerraFluent.Html.Reporting.Measurement;

/// <summary>
/// The result of word-wrapping a run of text at a given width: the resulting
/// line breakdown plus the resolved line height. The layout engine sums
/// <see cref="TotalHeightPx"/> for pagination, and elements that support
/// splitting (e.g. <c>Paragraph</c>) slice <see cref="Lines"/> at a line
/// boundary to build the head/tail fragments for <c>IReportElement.Split</c>.
/// </summary>
public sealed class TextMeasurement
{
    /// <summary>The text, broken into lines that each fit within the measured width.</summary>
    public IReadOnlyList<string> Lines { get; }

    /// <summary>Resolved line height in pixels (font size x line-height multiplier).</summary>
    public double LineHeightPx { get; }

    /// <summary>The width, in pixels, of the widest line actually produced.</summary>
    public double WidestLineWidthPx { get; }

    /// <summary>Total block height: <see cref="Lines"/>.Count * <see cref="LineHeightPx"/>.</summary>
    public double TotalHeightPx => Lines.Count * LineHeightPx;

    /// <summary>Creates a text measurement result.</summary>
    public TextMeasurement(IReadOnlyList<string> lines, double lineHeightPx, double widestLineWidthPx)
    {
        Lines = Guard.Snapshot(lines, nameof(lines));
        LineHeightPx = Guard.Positive(lineHeightPx, nameof(lineHeightPx));
        WidestLineWidthPx = Guard.NonNegative(widestLineWidthPx, nameof(widestLineWidthPx));
    }
}
