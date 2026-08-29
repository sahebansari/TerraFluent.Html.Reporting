using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Tests.TestHelpers;
using Xunit;

namespace TerraFluent.Html.Reporting.Tests.Model;

public class ReportDocumentTests
{
    private static ReportDocument BuildSampleDocument() =>
        ReportDocument.Create(PageSize.FromPixels(400, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Footer(f => f.AddPageNumber())
            .Content(c => c.AddParagraph("Hello world"))
            .Build();

    [Fact]
    public void RenderHtml_NoTitleSet_FallsBackToReport()
    {
        var document = BuildSampleDocument();

        Assert.Contains("<title>Report</title>", document.RenderHtml());
    }

    [Fact]
    public void RenderHtml_TitleSet_IsHtmlEncodedIntoTitleTag()
    {
        var document = ReportDocument.Create(PageSize.FromPixels(400, 300))
            .SetMargins(0)
            .UseTextMeasurer(new FakeTextMeasurer())
            .Title("Q&A <Report>")
            .Content(c => c.AddParagraph("Hello world"))
            .Build();

        Assert.Equal("Q&A <Report>", document.Title);
        Assert.Contains("<title>Q&amp;A &lt;Report&gt;</title>", document.RenderHtml());
    }

    [Fact]
    public void RenderHtmlDocument_WritesTheSameContentAsRenderHtml()
    {
        var document = BuildSampleDocument();
        var expected = document.RenderHtml();

        var path = Path.GetTempFileName();
        try
        {
            document.RenderHtmlDocument(path);
            var actual = File.ReadAllText(path);
            Assert.Equal(expected, actual);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task RenderHtmlDocumentAsync_WritesTheSameContentAsRenderHtml()
    {
        var document = BuildSampleDocument();
        var expected = document.RenderHtml();

        var path = Path.GetTempFileName();
        try
        {
            await document.RenderHtmlDocumentAsync(path);
            var actual = await File.ReadAllTextAsync(path);
            Assert.Equal(expected, actual);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
