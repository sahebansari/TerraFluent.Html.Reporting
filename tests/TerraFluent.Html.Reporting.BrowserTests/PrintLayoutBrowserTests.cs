using Microsoft.Playwright;
using TerraFluent.Html.Reporting.Model;
using Xunit;

namespace TerraFluent.Html.Reporting.BrowserTests;

public sealed class PrintLayoutBrowserTests
{
    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    [InlineData("webkit")]
    public async Task GeneratedReport_PreservesFixedPageGeometryUnderPrintMedia(string browserName)
    {
        var report = ReportDocument.Create(PageSize.FromPixels(300, 200))
            .SetMargins(20)
            .Header(header => header.AddText("Browser print check"))
            .Footer(footer => footer.AddPageNumber())
            .Content(content =>
            {
                content.AddParagraph("Page one content.");
                content.AddPageBreak();
                content.AddTable(table =>
                {
                    table.AddColumns("Name", "Value");
                    table.AddRow("Engine", browserName);
                });
            })
            .Build();

        using var playwright = await Playwright.CreateAsync();
        var browserType = browserName switch
        {
            "chromium" => playwright.Chromium,
            "firefox" => playwright.Firefox,
            "webkit" => playwright.Webkit,
            _ => throw new ArgumentOutOfRangeException(nameof(browserName)),
        };

        await using var browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync(report.RenderHtml(), new PageSetContentOptions { WaitUntil = WaitUntilState.Load });
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Print });

        var audit = await page.EvaluateAsync<PrintLayoutAudit>("""
            () => {
                const pages = [...document.querySelectorAll('.fhr-page')];
                const tolerance = 0.5;
                return {
                    pageCount: pages.length,
                    exactGeometry: pages.every(page => {
                        const rect = page.getBoundingClientRect();
                        return Math.abs(rect.width - 300) <= tolerance && Math.abs(rect.height - 200) <= tolerance;
                    }),
                    contentContained: pages.every(page => {
                        const pageRect = page.getBoundingClientRect();
                        return [...page.querySelectorAll('*')].every(element => {
                            const rect = element.getBoundingClientRect();
                            return rect.left >= pageRect.left - tolerance &&
                                rect.top >= pageRect.top - tolerance &&
                                rect.right <= pageRect.right + tolerance &&
                                rect.bottom <= pageRect.bottom + tolerance;
                        });
                    }),
                    printMarginBottom: getComputedStyle(pages[0]).marginBottom,
                    printBoxShadow: getComputedStyle(pages[0]).boxShadow,
                    hasPageRule: [...document.styleSheets].some(sheet =>
                        [...sheet.cssRules].some(rule => rule.type === CSSRule.PAGE_RULE)),
                };
            }
            """);

        Assert.Equal(2, audit.PageCount);
        Assert.True(audit.ExactGeometry);
        Assert.True(audit.ContentContained);
        Assert.Equal("0px", audit.PrintMarginBottom);
        Assert.Equal("none", audit.PrintBoxShadow);
        Assert.True(audit.HasPageRule);
    }

    private sealed class PrintLayoutAudit
    {
        public int PageCount { get; set; }

        public bool ExactGeometry { get; set; }

        public bool ContentContained { get; set; }

        public string PrintMarginBottom { get; set; } = string.Empty;

        public string PrintBoxShadow { get; set; } = string.Empty;

        public bool HasPageRule { get; set; }
    }
}
