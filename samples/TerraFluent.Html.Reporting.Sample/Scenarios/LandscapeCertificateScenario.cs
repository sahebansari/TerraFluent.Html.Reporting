using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;

namespace TerraFluent.Html.Reporting.Sample.Scenarios;

/// <summary>Showcases a custom page size in landscape orientation - a single-page certificate layout.</summary>
internal sealed class LandscapeCertificateScenario : ISampleScenario
{
    public string FileName => "07-landscape-certificate.html";

    public string Title => "Landscape Certificate";

    public string Description => "PageSize.Letter rotated to landscape via PageOrientation, for a certificate-style layout.";

    public ReportDocument Build() =>
        ReportDocument.Create(PageSize.Letter, PageOrientation.Landscape)
            .SetMargins(50)
            .Content(c =>
            {
                c.AddImage(SampleImages.Resolve("logo_2.png"), widthPx: 48);
                c.AddSpacer(30);
                c.AddHeading("Certificate of Completion", HeadingLevel.H1).AlignCenter();
                c.AddSpacer(20);
                c.AddParagraph("This certifies that").AlignCenter();
                c.AddHeading("Jane Doe", HeadingLevel.H2).AlignCenter();
                c.AddParagraph("has successfully completed the TerraFluent.Html.Reporting advanced training course.").AlignCenter();
                c.AddSpacer(40);
                c.AddRule();
                c.AddParagraph("Issued June 23, 2026").AlignCenter();
            })
            .Build();
}
