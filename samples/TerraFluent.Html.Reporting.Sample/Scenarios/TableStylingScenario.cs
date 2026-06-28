using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Sample.Scenarios;

/// <summary>Showcases custom column widths and the two row-split behaviors side by side.</summary>
internal sealed class TableStylingScenario : ISampleScenario
{
    public string FileName => "04-table-styling.html";

    public string Title => "Table Styling";

    public string Description => "Custom column widths, and AllowSplitWithContinuedHeader vs KeepRowIntact compared.";

    private const string LongNote =
        "This note is deliberately long, and the column it sits in deliberately narrow, so that " +
        "combined with the other rows on this small page it cannot fit in the remaining space and " +
        "must be handled by the table's row-split behavior. Depending on the setting below, it " +
        "either continues mid-sentence on the next page under a repeated header, or moves there " +
        "as a single, unbroken row.";

    public ReportDocument Build() =>
        ReportDocument.Create(PageSize.FromPixels(650, 480))
            .SetMargins(20)
            .Header(h => h.AddText("Table Styling Showcase").AlignCenter().Bold())
            .Footer(f => f.AddPageNumber().AlignCenter())
            .Content(c =>
            {
                c.AddHeading("Custom Column Widths", HeadingLevel.H2);
                c.AddParagraph("The first and third columns below have fixed widths; the middle column takes whatever space is left.");
                c.AddTable(table =>
                {
                    table.AddColumn("Code", widthPx: 70);
                    table.AddColumn("Description");
                    table.AddColumn("Price", widthPx: 90);
                    table.AddRow("A100", "Standard widget", "$19.99");
                    table.AddRow("B200", "Deluxe widget with extended warranty", "$49.99");
                    table.AddRow("C300", "Premium widget", "$89.99");
                });

                c.AddPageBreak();
                c.AddHeading("AllowSplitWithContinuedHeader (the default)", HeadingLevel.H2);
                c.AddParagraph("A row too tall for the remaining page is truncated mid-row; the header repeats, marked \"(continued)\".");
                c.AddTable(
                    table =>
                    {
                        table.AddColumn("Item", widthPx: 80);
                        table.AddColumn("Notes", widthPx: 220);
                        table.AddRow("Item 1", "Short note.");
                        table.AddRow("Item 2", LongNote);
                        table.AddRow("Item 3", "Another short note.");
                    },
                    TableStyle.Default.With(rowSplitBehavior: RowSplitBehavior.AllowSplitWithContinuedHeader));

                c.AddPageBreak();
                c.AddHeading("KeepRowIntact", HeadingLevel.H2);
                c.AddParagraph("With this setting, the same long row instead moves whole to the next page rather than splitting mid-row.");
                c.AddTable(
                    table =>
                    {
                        table.AddColumn("Item", widthPx: 80);
                        table.AddColumn("Notes", widthPx: 220);
                        table.AddRow("Item 1", "Short note.");
                        table.AddRow("Item 2", LongNote);
                        table.AddRow("Item 3", "Another short note.");
                    },
                    TableStyle.Default.With(rowSplitBehavior: RowSplitBehavior.KeepRowIntact));
            })
            .Build();
}
