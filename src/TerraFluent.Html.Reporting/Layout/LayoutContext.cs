using TerraFluent.Html.Reporting.Measurement;

namespace TerraFluent.Html.Reporting.Layout;

/// <summary>
/// Ambient information threaded through every <c>IReportElement.Measure</c> and
/// <c>IReportElement.Split</c> call: the text measurer to use and the width
/// available to the element. Elements that nest children at a narrower width
/// (e.g. a table cell inside a column) derive a child context via
/// <see cref="WithContentWidth"/> rather than mutating this one - instances are
/// immutable so the same context can be safely reused across sibling elements.
/// </summary>
public sealed class LayoutContext
{
    /// <summary>The text measurer used to compute wrapped line counts and heights.</summary>
    public ITextMeasurer TextMeasurer { get; }

    /// <summary>The width, in pixels, available to the element being measured/split.</summary>
    public double ContentWidthPx { get; }

    /// <summary>Creates a layout context.</summary>
    public LayoutContext(ITextMeasurer textMeasurer, double contentWidthPx)
    {
        TextMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
        if (contentWidthPx <= 0) throw new ArgumentOutOfRangeException(nameof(contentWidthPx));
        ContentWidthPx = contentWidthPx;
    }

    /// <summary>Returns a copy of this context narrowed/widened to <paramref name="contentWidthPx"/>.</summary>
    public LayoutContext WithContentWidth(double contentWidthPx) => new(TextMeasurer, contentWidthPx);
}
