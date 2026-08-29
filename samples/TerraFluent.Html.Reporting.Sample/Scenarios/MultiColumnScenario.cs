using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Sample.Scenarios;

/// <summary>Showcases AddColumns: a newsletter-style two-column body that splits a long article across the column boundary.</summary>
internal sealed class MultiColumnScenario : ISampleScenario
{
    public string FileName => "14-multi-column.html";

    public string Title => "Multi-Column Sections";

    public string Description => "AddColumns packs content into equal-width columns, fill-then-wrap, splitting a paragraph across the column boundary just like it would across a page boundary.";

    public ReportDocument Build()
    {
        var caption = TextStyle.Default.With(fontSizePx: 11, color: "#5b6b73", marginBottomPx: 12);

        return ReportDocument.Create(PageSize.A4)
            .SetMargins(48)
            .Header(h => h.AddText("The Quarterly Dispatch").AlignCenter().Bold())
            .Footer(f => f.AddPageNumber().AlignCenter())
            .Content(c =>
            {
                c.AddHeading("Two-Column Layout, Newsletter-Style", HeadingLevel.H1);
                c.AddParagraph(
                    "AddColumns(columnCount, configure) packs a flat list of elements into equal-width " +
                    "columns, filling one completely before moving to the next - not balanced, just " +
                    "fill-then-wrap. A paragraph that doesn't fit in the remaining space of one column " +
                    "splits across the column boundary exactly the way it already splits across a page " +
                    "boundary.", caption);

                c.AddColumns(3, columns =>
                {
                    columns.AddHeading("Section One", HeadingLevel.H3);
                    columns.AddParagraph(
                        "Reports generated this quarter grew steadily across every region, with the " +
                        "sharpest increase coming from automated invoice generation. Teams that " +
                        "previously exported data by hand now generate paginated, print-ready HTML " +
                        "directly from their own reporting pipelines.");
                    columns.AddRule();
                    columns.AddHeading("Section Two", HeadingLevel.H3);
                    columns.AddParagraph(
                        "Barcode and QR code generation shipped natively this release, with no external " +
                        "dependency required. Both are rendered as ordinary embedded images, so every " +
                        "existing alignment and margin modifier already works with them unchanged.");
                    columns.AddRule();
                    columns.AddHeading("Section Three", HeadingLevel.H3);
                    columns.AddParagraph(
                        "Right-to-left text direction and embedded custom fonts are both new this " +
                        "release as well, alongside this multi-column layout feature you're reading " +
                        "right now - itself an opt-in addition that leaves the default single-column " +
                        "flow completely unchanged for every report that doesn't use it.");
                }, columnGapPx: 20);
            })
            .Build();
    }
}
