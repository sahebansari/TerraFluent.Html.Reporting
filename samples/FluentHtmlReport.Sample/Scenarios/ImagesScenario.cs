using FluentHtmlReport.Model;
using FluentHtmlReport.Model.Elements;

namespace FluentHtmlReport.Sample.Scenarios;

/// <summary>Showcases loading images from bytes/base64/a file path, and deriving the missing dimension from aspect ratio.</summary>
internal sealed class ImagesScenario : ISampleScenario
{
    public string FileName => "03-images.html";

    public string Description => "Images from bytes/base64/file path, explicit dimensions vs aspect-ratio-derived sizing.";

    public ReportDocument Build() =>
        ReportDocument.Create(PageSize.A4)
            .SetMargins(40)
            .Header(h => h.AddText("Images").AlignCenter().Bold())
            .Content(c =>
            {
                c.AddHeading("Explicit Width and Height", HeadingLevel.H2);
                c.AddParagraph("A 240x80 source image, rendered at its native size:");
                c.AddImage(MinimalPngWriter.CreateSolidColor(240, 80, 0x2f, 0x4d, 0x8e), "image/png", widthPx: 240, heightPx: 80);
                c.AddSpacer(16);

                c.AddHeading("Only Width Given", HeadingLevel.H2);
                c.AddParagraph("The same 3:1 source, with only widthPx: 360 specified - height (120) is derived from its aspect ratio:");
                c.AddImage(MinimalPngWriter.CreateSolidColor(240, 80, 0x2f, 0x8e, 0x4d), "image/png", widthPx: 360);
                c.AddSpacer(16);

                c.AddHeading("Only Height Given", HeadingLevel.H2);
                c.AddParagraph("A tall 80x160 (1:2) source, with only heightPx: 200 specified - width (100) is derived from its aspect ratio:");
                c.AddImage(MinimalPngWriter.CreateSolidColor(80, 160, 0x8e, 0x2f, 0x4d), "image/png", heightPx: 200);
                c.AddSpacer(16);

                c.AddHeading("Loaded via a data: URI", HeadingLevel.H2);
                c.AddParagraph("AddImageFromBase64 accepts a full data: URI or a bare base64 payload:");
                var base64Source = Convert.ToBase64String(MinimalPngWriter.CreateSolidColor(150, 150, 0x8e, 0x6b, 0x2f));
                c.AddImageFromBase64($"data:image/png;base64,{base64Source}", widthPx: 150, heightPx: 150);
                c.AddSpacer(16);

                c.AddHeading("Loaded From a File Path", HeadingLevel.H2);
                c.AddParagraph("AddImage(filePath, ...) reads straight from disk - handy for a logo or other static asset shipped alongside the report generator. This 512x512 source keeps its 1:1 aspect ratio with only widthPx given:");
                c.AddImage(SampleImages.Resolve("spotify.png"), widthPx: 96);
            })
            .Build();
}
