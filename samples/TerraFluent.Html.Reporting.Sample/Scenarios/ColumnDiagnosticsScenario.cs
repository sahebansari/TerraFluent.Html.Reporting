using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Sample.Scenarios;

/// <summary>
/// Showcases the structured LayoutWarning fields (Reason/ElementType/
/// ElementIndex, not just a free-form Message) via a table whose fixed-width
/// columns leave nothing for its one auto-width column - see Program.cs for
/// where these fields get printed to the console.
/// </summary>
internal sealed class ColumnDiagnosticsScenario : ISampleScenario
{
    public string FileName => "18-column-diagnostics.html";

    public string Title => "Column Width Diagnostics";

    public string Description => "A table whose fixed-width columns leave nothing for its auto-width column triggers a LayoutWarningReason.ColumnWidthCollapsed warning - printed to the console with its structured fields, not just a message string.";

    public ReportDocument Build() =>
        ReportDocument.Create(PageSize.FromPixels(400, 300))
            .SetMargins(20)
            // Not used here (this scenario deliberately keeps the default Warn
            // behavior so the report still renders and the warning prints to
            // the console) - but either of these would turn the same condition
            // into a thrown InvalidOperationException instead:
            //   .UseStrictLayoutValidation()
            //   TableStyle.Default.With(columnWidthOverflowMode: ColumnWidthOverflowMode.Throw)
            .Content(c =>
            {
                c.AddHeading("Column Width Diagnostics", HeadingLevel.H3);
                c.AddParagraph(
                    "This table's two fixed-width columns (200px + 200px = 400px) already consume " +
                    "the entire 360px content width on their own, leaving the third, auto-width " +
                    "column nothing - it resolves to 0px and its content isn't visible.");

                c.AddTable(table =>
                {
                    table.AddColumn("Fixed A", widthPx: 200);
                    table.AddColumn("Fixed B", widthPx: 200);
                    table.AddColumn("Auto (collapses to 0px)");
                    table.AddRow("A", "B", "Not visible");
                });
            })
            .Build();
}
