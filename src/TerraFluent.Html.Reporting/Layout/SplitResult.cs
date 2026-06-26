using TerraFluent.Html.Reporting.Model;

namespace TerraFluent.Html.Reporting.Layout;

/// <summary>
/// The outcome of asking an element to split itself across a page boundary.
/// The layout engine only calls <c>IReportElement.Split</c> after confirming
/// via <c>Measure</c> that the element does not fit in full, so a result never
/// needs to represent "fits entirely" - only "split into head/tail" or
/// "cannot usefully split, defer the whole element".
/// </summary>
public sealed class SplitResult
{
    /// <summary>
    /// The portion of the element that fits in the available height and should
    /// be placed on the current page, or <see langword="null"/> if nothing fits
    /// (e.g. the available height is smaller than a single line/row, or the
    /// element does not support splitting at all, such as an image).
    /// </summary>
    public IReportElement? Head { get; }

    /// <summary>
    /// The remaining portion to re-measure and place starting on the next page,
    /// or <see langword="null"/> in the rare case that <see cref="Head"/> turned
    /// out to consume the element in full.
    /// </summary>
    public IReportElement? Tail { get; }

    private SplitResult(IReportElement? head, IReportElement? tail)
    {
        Head = head;
        Tail = tail;
    }

    /// <summary>The element was divided: <paramref name="head"/> fits now, <paramref name="tail"/> continues later.</summary>
    public static SplitResult Partial(IReportElement head, IReportElement? tail) => new(head, tail);

    /// <summary>
    /// The element cannot be split (or nothing of it fits in the offered height);
    /// the whole, unmodified <paramref name="original"/> element should be deferred
    /// to the next page.
    /// </summary>
    public static SplitResult Unsplittable(IReportElement original) => new(null, original);
}
