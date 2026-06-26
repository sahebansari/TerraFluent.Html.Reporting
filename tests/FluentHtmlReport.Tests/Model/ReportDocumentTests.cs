using FluentHtmlReport.Model;
using FluentHtmlReport.Tests.TestHelpers;
using Xunit;

namespace FluentHtmlReport.Tests.Model;

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
