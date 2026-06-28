using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;

namespace TerraFluent.Html.Reporting.Sample.Scenarios;

/// <summary>Showcases AddRawHtml as an escape hatch for markup the built-in elements don't cover.</summary>
internal sealed class RawHtmlScenario : ISampleScenario
{
    public string FileName => "08-raw-html.html";

    public string Title => "Raw HTML";

    public string Description => "AddRawHtml lets you inject arbitrary markup at a caller-supplied height.";

    public ReportDocument Build() =>
        ReportDocument.Create(PageSize.A4)
            .SetMargins(40)
            .Header(h => h.AddText("Raw HTML Escape Hatch").AlignCenter().Bold())
            .Content(c =>
            {
                c.AddHeading("Custom Markup", HeadingLevel.H1);
                c.AddParagraph(
                    "For anything the built-in elements don't cover, AddRawHtml injects arbitrary markup " +
                    "directly. The engine treats it as an opaque block at a height you specify yourself, " +
                    "since it has no way to measure HTML it doesn't understand.");

                c.AddRawHtml(
                    "<div style=\"border:2px dashed #2f4858;border-radius:8px;padding:16px;background:#eef3f6;\">" +
                    "<strong>Custom callout box</strong><br/>This entire block is raw HTML supplied by the " +
                    "caller, including its own inline styles - useful for things like a styled alert, a " +
                    "QR code <img> tag, or an inline SVG diagram.</div>",
                    heightPx: 110);

                c.AddSpacer(16);
                c.AddParagraph("Content placed after the raw HTML block resumes normal pagination as usual.");
            })
            .Build();
}
