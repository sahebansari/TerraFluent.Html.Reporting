using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Sample.Scenarios;

/// <summary>Showcases a realistic single-page sales invoice, with the company logo repeated in both the header and footer.</summary>
internal sealed class SalesInvoiceScenario : ISampleScenario
{
    public string FileName => "10-sales-invoice.html";

    public string Title => "Sales Invoice";

    public string Description => "A single-page sales invoice with the company logo in both the header and footer.";

    public ReportDocument Build()
    {
        var logoPath = SampleImages.Resolve("logo_1.png");
        var rightAlign = TextStyle.Default.With(alignment: TextAlignment.Right, marginBottomPx: 0);

        return ReportDocument.Create(PageSize.A4)
            .SetMargins(40)
            .Header(h =>
            {
                h.AddRow(row =>
                {
                    row.AddColumn(48, col => col.AddImage(logoPath, widthPx: 40)).Padding(topPx: 15, 0, 0, 0);
                    row.AddColumn(col =>
                    {
                        col.AddText("Acme Corporation").Bold().FontSize(20);
                        col.AddText("123 Market Street, Springfield, USA - billing@acmecorp.example").FontSize(12);
                    });
                });
                h.AddRule();
            })
            .Footer(f =>
            {
                f.AddRule();
                f.AddRow(row =>
                {
                    row.AddColumn(32, col => col.AddImage(logoPath, widthPx: 24));
                    row.AddColumn(col => col.AddText("Thank you for your business! Payment is due within 30 days of the invoice date.").FontSize(10));
                });
                f.AddPageNumber().AlignCenter().FontSize(9);
            })
            .Content(c =>
            {
                c.AddHeading("INVOICE", HeadingLevel.H1).AlignCenter();
                c.AddParagraph("Invoice #: INV-1042\nInvoice Date: June 23, 2026\nDue Date: July 23, 2026").AlignRight().FontSize(11);
                c.AddSpacer(8);

                c.AddHeading("Bill To", HeadingLevel.H3);
                c.AddParagraph("Jane Doe\n456 Oak Avenue\nSpringfield, USA");
                c.AddSpacer(20);

                c.AddTable(table =>
                {
                    table.AddColumn("Item");
                    table.AddColumn("Qty", widthPx: 50);
                    table.AddColumn("Unit Price", widthPx: 100);
                    table.AddColumn("Amount", widthPx: 100);
                    table.AddRow(new TableCell[] { "Website Redesign", new TableCell("1", rightAlign), new TableCell("$1,200.00", rightAlign), new TableCell("$1,200.00", rightAlign) });
                    table.AddRow(new TableCell[] { "Logo Design", new TableCell("1", rightAlign), new TableCell("$350.00", rightAlign), new TableCell("$350.00", rightAlign) });
                    table.AddRow(new TableCell[] { "Hosting (Annual)", new TableCell("1", rightAlign), new TableCell("$180.00", rightAlign), new TableCell("$180.00", rightAlign) });
                    table.AddRow(new TableCell[] { "SEO Consultation", new TableCell("4", rightAlign), new TableCell("$75.00", rightAlign), new TableCell("$300.00", rightAlign) });
                    table.AddRow(new TableCell[] { "Content Writing (per article)", new TableCell("6", rightAlign), new TableCell("$45.00", rightAlign), new TableCell("$270.00", rightAlign) });
                });
                c.AddSpacer(12);

                c.AddParagraph("Subtotal: $2,300.00").AlignRight();
                c.AddParagraph("Tax (8%): $184.00").AlignRight();
                c.AddRule();
                c.AddParagraph("Total: $2,484.00").AlignRight().Bold().FontSize(16);
            })
            .Build();
    }
}
