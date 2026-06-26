using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;

namespace TerraFluent.Html.Reporting.Sample.Scenarios;

/// <summary>
/// The "kitchen sink" intro sample: header/footer with page numbers, a
/// heading, a paragraph, an embedded image, a multi-page table that
/// continues correctly onto later pages, and a numbered list.
/// </summary>
internal sealed class GettingStartedScenario : ISampleScenario
{
    public string FileName => "01-getting-started.html";

    public string Description => "Header/footer page numbers, heading, paragraph, image, multi-page table, numbered list.";

    public ReportDocument Build()
    {
        var products = new[] { "Widget A", "Widget B", "Gadget C", "Gizmo D", "Doohickey E", "Thingamajig F" };
        var random = new Random(42);
        var logoPath = SampleImages.Resolve("microsoft.png");

        return ReportDocument.Create(PageSize.A4, PageOrientation.Portrait)
            .SetMargins(40, 40, 60, 60)
            .Header(h => h.AddText("Monthly Sales Report").AlignCenter().Bold().FontSize(16))
            .Footer(f => f.AddPageNumber("Page {page} of {totalPages}").AlignCenter())
            .Content(c =>
            {
                c.AddHeading("Sales Summary", HeadingLevel.H1);
                c.AddParagraph(
                    "This report summarizes sales activity across all regions for the current " +
                    "period. Figures below are broken out by product line, with quantity sold " +
                    "and total revenue for each. The table spans multiple pages and demonstrates " +
                    "this library's row-aware pagination: the header row repeats on every " +
                    "continuation page, and a row that is too tall to fit in the remaining space " +
                    "on a page is split mid-row rather than being pushed whole to the next page.");
                c.AddImage(logoPath, widthPx: 72);
                c.AddRule();

                c.AddHeading("Detailed Line Items", HeadingLevel.H2);
                c.AddTable(table =>
                {
                    table.AddColumns("Product", "Qty", "Revenue");
                    for (var i = 0; i < 45; i++)
                    {
                        var product = products[i % products.Length];
                        var qty = random.Next(10, 500);
                        var revenue = qty * (decimal)(5 + random.NextDouble() * 45);
                        table.AddRow(product, qty.ToString(), revenue.ToString("C2"));
                    }
                });

                c.AddSpacer(12);
                c.AddHeading("Notes", HeadingLevel.H2);
                c.AddList(ListStyle.Numbered, new[]
                {
                    "Revenue figures are pre-tax.",
                    "Quantities reflect units shipped, not units ordered.",
                    "Contact the finance team for a region-level breakdown.",
                });
            })
            .Build();
    }
}
