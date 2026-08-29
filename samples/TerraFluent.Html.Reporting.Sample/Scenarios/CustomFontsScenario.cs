using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Sample.Scenarios;

/// <summary>
/// Showcases EmbedFont: a script font (Pacifico, SIL Open Font License - see
/// fonts/Pacifico-OFL.txt) embedded directly into the generated HTML as a
/// base64 @font-face rule, so the report renders correctly even somewhere
/// that font isn't installed.
/// </summary>
internal sealed class CustomFontsScenario : ISampleScenario
{
    public string FileName => "16-custom-fonts.html";

    public string Title => "Embedded Custom Fonts";

    public string Description => "EmbedFont embeds a font file (here, the OFL-licensed Pacifico) as a base64 @font-face rule, so the report doesn't depend on that font being installed wherever it's opened.";

    public ReportDocument Build()
    {
        const string brandFont = "Pacifico";
        var script = TextStyle.Default.With(fontFamily: brandFont, fontSizePx: 32, marginBottomPx: 4);
        var caption = TextStyle.Default.With(fontSizePx: 11, color: "#5b6b73");

        return ReportDocument.Create(PageSize.A4)
            .SetMargins(40)
            .Title("Embedded Custom Fonts Sample")
            .EmbedFont(brandFont, SampleFonts.Resolve("Pacifico-Regular.ttf"), mimeType: "font/ttf")
            .Header(h => h.AddText("Embedded Custom Fonts").AlignCenter().Bold())
            .Footer(f => f.AddPageNumber().AlignCenter())
            .Content(c =>
            {
                c.AddHeading("Default Font", HeadingLevel.H2);
                c.AddParagraph("This heading and this paragraph use the library's default font-family - whatever the viewing browser resolves \"Segoe UI, Arial, sans-serif\" to.");
                c.AddSpacer(20);

                c.AddHeading("Embedded Font", HeadingLevel.H2);
                c.AddParagraph($"Acme Corporation", script);
                c.AddParagraph(
                    "The text above uses TextStyle.FontFamily(\"Pacifico\") - a script font embedded " +
                    "into this document's own HTML via EmbedFont(...), not merely referenced by name. " +
                    "Open this file on a machine that has never had Pacifico installed and it still " +
                    "renders correctly, because the font data travels with the report.",
                    caption);
                c.AddSpacer(20);

                c.AddParagraph(
                    "Pacifico is licensed under the SIL Open Font License - see fonts/Pacifico-OFL.txt " +
                    "next to this sample project for the full license text and attribution.",
                    caption);
            })
            .Build();
    }
}
