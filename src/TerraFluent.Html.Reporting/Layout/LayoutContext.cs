using TerraFluent.Html.Reporting.Measurement;
using TerraFluent.Html.Reporting.Compatibility;

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

    /// <summary>
    /// The buffer <see cref="LayoutEngine"/> collects non-fatal diagnostics
    /// into (e.g. a collapsed auto-width column), or <see langword="null"/>
    /// for a <see cref="LayoutContext"/> not wired up for diagnostics (e.g.
    /// one built directly via the public constructor). Internal: elements
    /// report through it, but it is not part of the public surface.
    /// </summary>
    internal LayoutDiagnostics? Diagnostics { get; }

    /// <summary>
    /// When true, a condition that would otherwise only produce a
    /// <see cref="LayoutWarning"/> (via <see cref="Diagnostics"/>) throws
    /// instead. Defaults to <see langword="false"/>; there is no public
    /// opt-in for this yet.
    /// </summary>
    internal bool StrictMode { get; }

    /// <summary>Creates a layout context.</summary>
    public LayoutContext(ITextMeasurer textMeasurer, double contentWidthPx)
        : this(textMeasurer, contentWidthPx, diagnostics: null, strictMode: false)
    {
    }

    internal LayoutContext(ITextMeasurer textMeasurer, double contentWidthPx, LayoutDiagnostics? diagnostics, bool strictMode)
    {
        TextMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
        ContentWidthPx = Guard.Positive(contentWidthPx, nameof(contentWidthPx));
        Diagnostics = diagnostics;
        StrictMode = strictMode;
    }

    /// <summary>Returns a copy of this context narrowed/widened to <paramref name="contentWidthPx"/>.</summary>
    public LayoutContext WithContentWidth(double contentWidthPx) => new(TextMeasurer, contentWidthPx, Diagnostics, StrictMode);
}
