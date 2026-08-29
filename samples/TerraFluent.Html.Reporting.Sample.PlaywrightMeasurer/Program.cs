using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Sample.PlaywrightMeasurer;

await using var measurer = await PlaywrightTextMeasurer.CreateAsync();

var report = ReportDocument.Create(PageSize.A4)
    .SetMargins(40)
    .UseTextMeasurer(measurer)
    .Header(header => header.AddText("Playwright-measured report").AlignCenter().Bold())
    .Footer(footer => footer.AddPageNumber())
    .Content(content =>
    {
        content.AddHeading("Pixel-closer pagination", HeadingLevel.H1);
        content.AddParagraph(
            "This report was paginated using PlaywrightTextMeasurer instead of the " +
            "core package's default ApproximateTextMeasurer. Word widths here come " +
            "from a real, headless Chromium instance's Canvas 2D measureText API - " +
            "the browser's actual installed font metrics - rather than a generic " +
            "Helvetica advance-width table, so line wrapping should track a real " +
            "browser's rendering more closely, especially for fonts other than a " +
            "plain sans-serif.");
        content.AddParagraph(
            "See this folder's README for what this measurer does and does not " +
            "account for, and why it isn't the library's default.");
    })
    .Build();

var outputPath = Path.Combine(AppContext.BaseDirectory, "playwright-measured-report.html");
report.RenderHtmlDocument(outputPath);
Console.WriteLine($"Wrote {outputPath}");
