using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Sample.Scenarios;

/// <summary>Showcases a Code 128 barcode of the 8-digit invoice number, placed in the top-right corner of the header.</summary>
internal sealed class InvoiceBarcodeScenario : ISampleScenario
{
    public string FileName => "12-invoice-with-barcode.html";

    public string Title => "Invoice With Header Barcode";

    public string Description => "An invoice with a Code 128 barcode of the 8-digit invoice number in the top-right corner of the header.";

    public ReportDocument Build()
    {
        const string invoiceNumber = "20264471";
        var rightAlign = TextStyle.Default.With(alignment: TextAlignment.Right, marginBottomPx: 0);

        return ReportDocument.Create(PageSize.A4)
            .SetMargins(40)
            .Header(h =>
            {
                h.AddRow(row =>
                {
                    row.AddColumn(col =>
                    {
                        col.AddText("Acme Corporation").Bold().FontSize(20);
                        col.AddText("123 Market Street, Springfield, USA - billing@acmecorp.example").FontSize(12);
                    });
                    row.AddColumn(300, col =>
                    {
                        col.AddBarcode(invoiceNumber, moduleWidthPx: 2, heightPx: 40).AlignRight();
                        col.AddText(invoiceNumber, rightAlign).FontSize(10);
                    });
                }, verticalAlignment: RowVerticalAlignment.Top);
                h.AddRule();
            })
            .Footer(f =>
            {
                f.AddRule();
                f.AddPageNumber().AlignCenter().FontSize(9);
            })
            .Content(c =>
            {
                c.AddHeading("INVOICE", HeadingLevel.H1).AlignCenter();
                c.AddParagraph($"Invoice #: {invoiceNumber}\nInvoice Date: June 30, 2026\nDue Date: July 30, 2026").AlignRight().FontSize(11);
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
                });
                c.AddSpacer(12);

                c.AddParagraph("Subtotal: $1,730.00").AlignRight();
                c.AddParagraph("Tax (8%): $138.40").AlignRight();
                c.AddRule();
                c.AddParagraph("Total: $1,868.40").AlignRight().Bold().FontSize(16);
            })
            .Build();
    }
}
