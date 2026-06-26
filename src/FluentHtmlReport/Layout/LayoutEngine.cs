using FluentHtmlReport.Model;
using FluentHtmlReport.Model.Elements;
using FluentHtmlReport.Model.Sections;

namespace FluentHtmlReport.Layout;

/// <summary>
/// Paginates a <see cref="ReportDocument"/> into a fixed sequence of pages.
/// </summary>
/// <remarks>
/// Algorithm, per content element, processed in document order:
/// <list type="number">
/// <item><see cref="Model.Elements.PageBreak"/> is special-cased: it closes the current page (unless it's already empty) rather than going through measurement.</item>
/// <item>Otherwise, measure the element at the content width. If it fits in the space left on the current page, place it and continue.</item>
/// <item>Otherwise, if any space is left on the current page, ask the element to <c>Split</c> into a head that fits and a tail that doesn't. Place the head (if any) and re-queue the tail (if any) to be processed next.</item>
/// <item>If nothing fit at all (the element returned no head, e.g. an image, or there was no space left), close the current page and retry the same element at the top of a fresh page.</item>
/// <item>As a termination guarantee, an element that still doesn't fit on a completely empty page is force-placed there anyway and a <see cref="LayoutWarning"/> is recorded, rather than dropping content or looping forever.</item>
/// </list>
/// Header and footer geometry is recomputed per page (cheap - they never split,
/// and typically hold only a handful of elements) only so each page's
/// <see cref="ElementPlacement.PageIndex"/> is accurate; their rendered output
/// is otherwise identical on every page except for page-number tokens, which
/// are resolved later from <c>RenderContext</c>.
/// </remarks>
/// <threadsafety>
/// <see cref="Paginate"/> holds no static/shared mutable state of its own, so
/// concurrent calls for different <see cref="ReportDocument"/> instances (or
/// even the same instance, since it is immutable) are safe. The default
/// <c>ApproximateTextMeasurer</c> is likewise stateless. <c>Table</c> caches
/// row heights internally as a performance optimization; concurrent
/// pagination of the very same <c>Table</c> element is safe but may
/// redundantly recompute the cache (a benign race, not a correctness issue).
/// </threadsafety>
public static class LayoutEngine
{
    private const double Epsilon = 0.01;

    /// <summary>Paginates <paramref name="document"/> into pages.</summary>
    /// <param name="document">The document to paginate.</param>
    /// <param name="cancellationToken">
    /// Checked periodically (once per element processed) so pagination of a
    /// very large document can be aborted, e.g. when the originating web
    /// request was canceled.
    /// </param>
    public static LayoutResult Paginate(ReportDocument document, CancellationToken cancellationToken = default)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));

        var contentWidthPx = document.PageSize.WidthPx - document.Margins.Left - document.Margins.Right;
        if (contentWidthPx <= 0) throw new InvalidOperationException("Left/right margins leave no horizontal room for content.");

        var context = new LayoutContext(document.TextMeasurer, contentWidthPx);

        var headerHeightPx = document.Header?.MeasureHeight(context) ?? 0;
        var footerHeightPx = document.Footer?.MeasureHeight(context) ?? 0;

        var contentAreaHeightPx = document.PageSize.HeightPx - document.Margins.Top - document.Margins.Bottom - headerHeightPx - footerHeightPx;
        if (contentAreaHeightPx <= 0) throw new InvalidOperationException("Margins and header/footer leave no vertical room for content.");

        var pages = new List<PageLayout>();
        var warnings = new List<LayoutWarning>();
        var currentPage = new List<PlacedElement>();
        var usedHeightPx = 0.0;
        var pageIndex = 0;
        var pending = new LinkedList<IReportElement>(document.ContentElements);

        PageLayout BuildPage(int index, List<PlacedElement> contentElements) => new(
            index,
            BuildSectionPlacements(document.Header, context, PageSectionKind.Header, index),
            contentElements,
            BuildSectionPlacements(document.Footer, context, PageSectionKind.Footer, index));

        void Place(IReportElement element, double heightPx)
        {
            currentPage.Add(new PlacedElement(element, new ElementPlacement(0, usedHeightPx, contentWidthPx, heightPx, pageIndex, PageSectionKind.Content)));
            usedHeightPx += heightPx;
        }

        void StartNewPage()
        {
            pages.Add(BuildPage(pageIndex, currentPage));
            currentPage = new List<PlacedElement>();
            usedHeightPx = 0;
            pageIndex++;
        }

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var element = pending.First!.Value;
            pending.RemoveFirst();

            if (element is PageBreak)
            {
                if (usedHeightPx > Epsilon) StartNewPage();
                continue;
            }

            var measurement = element.Measure(context);
            var remainingPx = contentAreaHeightPx - usedHeightPx;

            if (measurement.HeightPx <= remainingPx + Epsilon)
            {
                Place(element, measurement.HeightPx);
                continue;
            }

            if (remainingPx > Epsilon)
            {
                var split = element.Split(remainingPx, context);

                if (split.Head is not null)
                {
                    Place(split.Head, split.Head.Measure(context).HeightPx);

                    if (split.Tail is not null)
                    {
                        StartNewPage();
                        pending.AddFirst(split.Tail);
                    }

                    continue;
                }

                // Head is null (Unsplittable): nothing fit, even partially.
                // split.Tail is just the original element again - fall through
                // to the same "nothing fit" handling below instead of
                // re-queuing it here too, which would place it twice and loop
                // forever for an element taller than a full empty page.
            }

            if (usedHeightPx <= Epsilon)
            {
                // Even a fully empty page can't fit or split this element - force it through so we make progress.
                warnings.Add(new LayoutWarning(
                    pageIndex,
                    $"A {element.GetType().Name} required {measurement.HeightPx:0.#}px but only {contentAreaHeightPx:0.#}px was available on an empty page; it was placed anyway and will overflow visually."));
                Place(element, measurement.HeightPx);
                StartNewPage();
                continue;
            }

            StartNewPage();
            pending.AddFirst(element);
        }

        if (currentPage.Count > 0 || pages.Count == 0)
        {
            pages.Add(BuildPage(pageIndex, currentPage));
        }

        return new LayoutResult(pages, document.PageSize, document.Margins, warnings);
    }

    private static IReadOnlyList<PlacedElement> BuildSectionPlacements(IPageSection? section, LayoutContext context, PageSectionKind kind, int pageIndex)
    {
        if (section is null) return Array.Empty<PlacedElement>();

        var placements = new List<PlacedElement>();
        var y = 0.0;
        foreach (var element in section.Elements)
        {
            var height = element.Measure(context).HeightPx;
            placements.Add(new PlacedElement(element, new ElementPlacement(0, y, context.ContentWidthPx, height, pageIndex, kind)));
            y += height;
        }

        return placements;
    }
}
