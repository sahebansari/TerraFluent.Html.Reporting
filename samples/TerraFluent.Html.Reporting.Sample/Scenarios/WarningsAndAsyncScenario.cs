using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;

namespace TerraFluent.Html.Reporting.Sample.Scenarios;

/// <summary>
/// Showcases <c>LayoutResult.Warnings</c>: an image too tall to fit (or split)
/// even on an empty page is placed anyway and overflows visually, but the
/// engine records why instead of failing silently. This report is also
/// written out via the async, streaming <c>RenderHtmlDocumentAsync</c> API
/// rather than <c>RenderHtml()</c> - see Program.cs.
/// </summary>
internal sealed class WarningsAndAsyncScenario : ISampleScenario
{
    public string FileName => "09-warnings-and-async.html";

    public string Description => "An oversized, unsplittable image triggers a LayoutResult.Warnings entry instead of failing silently.";

    public ReportDocument Build()
    {
        // Deliberately taller (300px) than the entire page's content area, and
        // an image can never be split - this is the scenario LayoutWarning exists for.
        var oversizedImage = MinimalPngWriter.CreateSolidColor(100, 100, 0x8e, 0x2f, 0x2f);

        return ReportDocument.Create(PageSize.FromPixels(400, 150))
            .SetMargins(10)
            .Content(c =>
            {
                c.AddHeading("Warnings", HeadingLevel.H2);
                c.AddParagraph("The image below is taller than this page's entire content area and cannot be split.");
                c.AddImage(oversizedImage, "image/png", widthPx: 300, heightPx: 300);
            })
            .Build();
    }
}
