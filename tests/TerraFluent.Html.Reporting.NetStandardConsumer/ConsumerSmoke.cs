using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.NetStandardConsumer;

public static class ConsumerSmoke
{
    public static string BuildReportHtml()
    {
        var tableStyle = TableStyle.Default.With(
            headerBackgroundColor: "#234",
            rowSplitBehavior: RowSplitBehavior.KeepRowIntact);

        var report = ReportDocument.Create(PageSize.Letter)
            .SetMargins(36)
            .Header(header => header.AddText("netstandard2.0 consumer").Bold())
            .Footer(footer => footer.AddPageNumber("Page {page} of {totalPages}"))
            .Content(content =>
            {
                content.AddHeading("Compatibility Smoke", HeadingLevel.H1);
                content.AddParagraph("This project compiles against the netstandard2.0 package asset.");
                content.AddTable(table =>
                {
                    table.AddColumns("Feature", "Result");
                    table.AddRow("Compile", "Passed");
                }, tableStyle);
            })
            .Build();

        return report.RenderHtml();
    }
}
