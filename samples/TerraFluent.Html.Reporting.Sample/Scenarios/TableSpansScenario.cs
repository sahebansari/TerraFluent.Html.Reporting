using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Sample.Scenarios;

/// <summary>Showcases table cell ColSpan/RowSpan: a category grouped with RowSpan and a ColSpan-based Grand Total row.</summary>
internal sealed class TableSpansScenario : ISampleScenario
{
    public string FileName => "13-table-spans.html";

    public string Title => "Table Spans (ColSpan/RowSpan)";

    public string Description => "Grouping line items under a shared RowSpan category cell, and a ColSpan-based Grand Total row.";

    public ReportDocument Build()
    {
        var right = TextStyle.Default.With(alignment: TextAlignment.Right, marginBottomPx: 0);
        var boldRight = TextStyle.Default.With(alignment: TextAlignment.Right, marginBottomPx: 0, fontWeight: FontWeight.Bold);
        var bold = TextStyle.Default.With(marginBottomPx: 0, fontWeight: FontWeight.Bold);

        return ReportDocument.Create(PageSize.A4)
            .SetMargins(40)
            .Header(h => h.AddText("Purchase Order Summary").AlignCenter().Bold())
            .Footer(f => f.AddPageNumber().AlignCenter())
            .Content(c =>
            {
                c.AddParagraph(
                    "Line items grouped by category: the Category cell spans every row in its group via " +
                    "RowSpan, and rows underneath it simply omit that column. The closing row's label cell " +
                    "spans two columns via ColSpan.");

                c.AddTable(table =>
                {
                    table.AddColumn("Category", widthPx: 110);
                    table.AddColumn("Item");
                    table.AddColumn("Qty", widthPx: 50);
                    table.AddColumn("Price", widthPx: 90);

                    table.AddRow(new TableCell[]
                    {
                        new TableCell("Electronics") { RowSpan = 3 },
                        "Wireless Mouse",
                        new TableCell("2", right),
                        new TableCell("$39.98", right),
                    });
                    table.AddRow(new TableCell[] { "Mechanical Keyboard", new TableCell("1", right), new TableCell("$89.00", right) });
                    table.AddRow(new TableCell[] { "USB-C Dock", new TableCell("1", right), new TableCell("$64.50", right) });

                    table.AddRow(new TableCell[]
                    {
                        new TableCell("Furniture") { RowSpan = 2 },
                        "Standing Desk",
                        new TableCell("1", right),
                        new TableCell("$349.00", right),
                    });
                    table.AddRow(new TableCell[] { "Office Chair", new TableCell("1", right), new TableCell("$210.00", right) });

                    table.AddRow(new TableCell[]
                    {
                        new TableCell("Grand Total", bold) { ColSpan = 3 },
                        new TableCell("$752.48", boldRight),
                    });
                });
            })
            .Build();
    }
}
