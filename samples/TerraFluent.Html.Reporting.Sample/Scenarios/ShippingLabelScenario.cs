using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Sample.Scenarios;

/// <summary>Showcases a compact 4x6" shipping label template: a custom small page size combining a tracking barcode and a QR code.</summary>
internal sealed class ShippingLabelScenario : ISampleScenario
{
    public string FileName => "15-shipping-label.html";

    public string Title => "Shipping Label Template";

    public string Description => "A compact 4x6 inch shipping label - a custom small page size combining a Code 128 tracking barcode and a QR code for the tracking URL.";

    public ReportDocument Build()
    {
        const string trackingNumber = "1Z9998887764";
        var small = TextStyle.Default.With(fontSizePx: 11, marginBottomPx: 2);
        var bold = TextStyle.Default.With(fontSizePx: 13, fontWeight: FontWeight.Bold, marginBottomPx: 2);
        var center = TextStyle.Default.With(alignment: TextAlignment.Center, marginBottomPx: 0);

        return ReportDocument.Create(PageSize.FromPixels(384, 576)) // 4in x 6in at 96px/inch
            .SetMargins(16)
            .Title($"Shipping Label - {trackingNumber}")
            .Content(c =>
            {
                c.AddParagraph("FROM: Acme Fulfillment Co, 800 Industrial Pkwy, Springfield, USA", small);
                c.AddRule();
                c.AddSpacer(6);

                c.AddParagraph("SHIP TO", TextStyle.Default.With(fontSizePx: 11, color: "#5b6b73", marginBottomPx: 2));
                c.AddParagraph("Jane Doe", bold);
                c.AddParagraph("456 Oak Avenue\nSpringfield, USA 62704", small);
                c.AddSpacer(16);

                c.AddBarcode(trackingNumber, moduleWidthPx: 2, heightPx: 60).AlignCenter();
                c.AddParagraph(trackingNumber, center.With(fontSizePx: 12, marginBottomPx: 0)).MarginTop(4);
                c.AddSpacer(16);

                c.AddRow(row =>
                {
                    row.AddColumn(col => col.AddQrCode($"https://track.example/{trackingNumber}", moduleWidthPx: 4).AlignCenter());
                    row.AddColumn(col =>
                    {
                        col.AddText("Scan to track", small);
                        col.AddText("Ground - 2.4 lb", small);
                        col.AddText("Ref: PO-48213", small);
                    });
                });
            })
            .Build();
    }
}
