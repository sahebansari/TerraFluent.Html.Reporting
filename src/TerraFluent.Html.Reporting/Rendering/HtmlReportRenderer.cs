using System.Threading;
using TerraFluent.Html.Reporting.Layout;

namespace TerraFluent.Html.Reporting.Rendering;

/// <summary>
/// The default <see cref="IHtmlReportRenderer"/>. Targets browser print-to-PDF
/// as the primary consumer: each page is an absolutely-positioned, exact-pixel
/// <c>&lt;div&gt;</c> sized via <c>@page</c> CSS, with <c>page-break-after: always</c>
/// between pages as a redundant signal for browsers that do not fully honor
/// <c>@page</c> sizing. Element fragments produced by <c>IReportElement.Split</c>
/// are rendered exactly like whole elements - the renderer has no pagination
/// logic of its own, it only walks the already-resolved <see cref="LayoutResult"/>
/// and translates each element's section-relative placement into page-absolute
/// coordinates before delegating to <c>IReportElement.RenderHtml</c>.
/// </summary>
public sealed class HtmlReportRenderer : IHtmlReportRenderer
{
    /// <summary>A shared, stateless default instance.</summary>
    public static HtmlReportRenderer Default { get; } = new();

    /// <inheritdoc />
    public string RenderDocument(LayoutResult layout)
    {
        var writer = new StringWriter();
        RenderDocumentTo(writer, layout);
        return writer.ToString();
    }

    /// <inheritdoc />
    public string RenderFragment(LayoutResult layout)
    {
        var writer = new StringWriter();
        RenderFragmentTo(writer, layout);
        return writer.ToString();
    }

    /// <inheritdoc />
    public void RenderDocumentTo(TextWriter writer, LayoutResult layout, CancellationToken cancellationToken = default)
    {
        writer.Write("<!DOCTYPE html><html><head><meta charset=\"utf-8\" /><title>Report</title><style>");
        writer.Write(BuildStyles(layout));
        writer.Write("</style></head><body>");
        RenderPages(writer, layout, cancellationToken);
        writer.Write("</body></html>");
    }

    /// <inheritdoc />
    public void RenderFragmentTo(TextWriter writer, LayoutResult layout, CancellationToken cancellationToken = default)
    {
        writer.Write("<style>");
        writer.Write(BuildStyles(layout));
        writer.Write("</style>");
        RenderPages(writer, layout, cancellationToken);
    }

    private static string BuildStyles(LayoutResult layout)
    {
        var width = CssFormat.Number(layout.PageSize.WidthPx);
        var height = CssFormat.Number(layout.PageSize.HeightPx);

        return
            "@page { size: " + width + "px " + height + "px; margin: 0; } " +
            "html, body { margin: 0; padding: 0; } " +
            "body { background: #e8e8e8; font-family: Segoe UI, Arial, sans-serif; } " +
            ".fhr-page { position: relative; width: " + CssFormat.Px(layout.PageSize.WidthPx) +
                "; height: " + CssFormat.Px(layout.PageSize.HeightPx) +
                "; background: #ffffff; overflow: hidden; margin: 0 auto 16px auto; " +
                "box-shadow: 0 0 6px rgba(0,0,0,0.25); page-break-after: always; break-after: page; } " +
            ".fhr-page:last-child { page-break-after: auto; break-after: auto; margin-bottom: 0; } " +
            "@media print { body { background: none; } .fhr-page { margin: 0; box-shadow: none; } }";
    }

    private static void RenderPages(TextWriter writer, LayoutResult layout, CancellationToken cancellationToken)
    {
        var totalPages = layout.Pages.Count;

        foreach (var page in layout.Pages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var renderContext = new RenderContext(page.PageIndex + 1, totalPages);
            var headerHeightPx = SumHeight(page.HeaderElements);
            var footerHeightPx = SumHeight(page.FooterElements);
            var footerTopPx = layout.PageSize.HeightPx - layout.Margins.Bottom - footerHeightPx;

            writer.Write("<div class=\"fhr-page\">");
            RenderElements(writer, page.HeaderElements, layout.Margins.Left, layout.Margins.Top, renderContext);
            RenderElements(writer, page.ContentElements, layout.Margins.Left, layout.Margins.Top + headerHeightPx, renderContext);
            RenderElements(writer, page.FooterElements, layout.Margins.Left, footerTopPx, renderContext);
            writer.Write("</div>");
        }
    }

    private static double SumHeight(IReadOnlyList<PlacedElement> elements)
    {
        var total = 0.0;
        foreach (var element in elements) total += element.Placement.HeightPx;
        return total;
    }

    private static void RenderElements(TextWriter writer, IReadOnlyList<PlacedElement> elements, double offsetX, double offsetY, RenderContext context)
    {
        foreach (var placed in elements)
        {
            var absolutePlacement = placed.Placement.Translate(offsetX, offsetY);
            writer.Write(placed.Element.RenderHtml(absolutePlacement, context));
        }
    }
}
