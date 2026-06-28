using TerraFluent.Html.Reporting.Fluent;
using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;

namespace TerraFluent.Html.Reporting.Sample.Scenarios;

/// <summary>Showcases AddRow: side-by-side columns with fixed/auto widths, vertical alignment, and multi-element columns.</summary>
internal sealed class RowLayoutScenario : ISampleScenario
{
    public string FileName => "11-row-layout.html";

    public string Title => "Row Layout";

    public string Description => "AddRow lays out columns side by side - fixed vs auto widths, vertical alignment, and multi-element columns.";

    public ReportDocument Build() =>
        ReportDocument.Create(PageSize.A4)
            .SetMargins(40)
            .Header(h => h.AddRow(row =>
            {
                row.AddColumn(36, col => col.AddImage(SampleImages.Resolve("indesign.png"), widthPx: 28));
                row.AddColumn(col => col.AddText("Row Layout").Bold().FontSize(16));
            }))
            .Footer(f => f.AddPageNumber().AlignCenter())
            .Content(c =>
            {
                c.AddHeading("Fixed and Auto-Width Columns", HeadingLevel.H2);
                c.AddParagraph(
                    "The outer two columns below have a fixed 80px width; the middle column takes " +
                    "whatever space is left. Each column's rule spans exactly its column's resolved width:");
                c.AddRow(row =>
                {
                    row.AddColumn(80, col =>
                    {
                        col.AddText("80px fixed").FontSize(11);
                        col.AddRule(3, "#2f4858");
                    });
                    row.AddColumn(col =>
                    {
                        col.AddText("Auto (shares remaining space)").FontSize(11);
                        col.AddRule(3, "#8e2f4d");
                    });
                    row.AddColumn(80, col =>
                    {
                        col.AddText("80px fixed").FontSize(11);
                        col.AddRule(3, "#2f8e4d");
                    });
                });
                c.AddSpacer(20);

                c.AddHeading("Vertical Alignment", HeadingLevel.H2);
                c.AddParagraph("Each row below pairs a one-line column against a three-line column, with a different RowVerticalAlignment:");
                AddAlignmentExample(c, "Top", RowVerticalAlignment.Top);
                AddAlignmentExample(c, "Middle (the default)", RowVerticalAlignment.Middle);
                AddAlignmentExample(c, "Bottom", RowVerticalAlignment.Bottom);
                c.AddSpacer(8);

                c.AddHeading("Multi-Element Columns", HeadingLevel.H2);
                c.AddParagraph("A column can stack several elements vertically - here, a bold number heading and a label, in a typical stats-row layout:");
                c.AddRow(row =>
                {
                    row.AddColumn(col =>
                    {
                        col.AddHeading("128", HeadingLevel.H2).AlignCenter();
                        col.AddText("Reports Generated").AlignCenter().FontSize(11);
                    });
                    row.AddColumn(col =>
                    {
                        col.AddHeading("12", HeadingLevel.H2).AlignCenter();
                        col.AddText("Active Projects").AlignCenter().FontSize(11);
                    });
                    row.AddColumn(col =>
                    {
                        col.AddHeading("99.9%", HeadingLevel.H2).AlignCenter();
                        col.AddText("Uptime").AlignCenter().FontSize(11);
                    });
                });
                c.AddSpacer(20);

                c.AddHeading("Image Alignment", HeadingLevel.H2);
                c.AddParagraph("AddImage(...) now returns a builder too: the same logo, left/center/right-aligned within an identically sized column:");
                c.AddRow(row =>
                {
                    row.AddColumn(col =>
                    {
                        col.AddText("Left").FontSize(10);
                        col.AddImage(SampleImages.Resolve("indesign.png"), widthPx: 32).AlignLeft();
                        col.AddRule();
                    });
                    row.AddColumn(col =>
                    {
                        col.AddText("Center").FontSize(10);
                        col.AddImage(SampleImages.Resolve("indesign.png"), widthPx: 32).AlignCenter();
                        col.AddRule();
                    });
                    row.AddColumn(col =>
                    {
                        col.AddText("Right").FontSize(10);
                        col.AddImage(SampleImages.Resolve("indesign.png"), widthPx: 32).AlignRight();
                        col.AddRule();
                    });
                });
                c.AddSpacer(8);

                c.AddHeading("Padding", HeadingLevel.H2);
                c.AddParagraph("AddParagraph(...).Padding(...) insets the text from its own box - framed here with rules to make the inset visible:");
                c.AddRule();
                c.AddParagraph("This paragraph has 16px of padding on every side.").Padding(16);
                c.AddRule();
            })
            .Build();

    private static void AddAlignmentExample(ContentBuilder c, string label, RowVerticalAlignment alignment)
    {
        c.AddParagraph(label).Bold().FontSize(11);
        c.AddRow(row =>
        {
            row.AddColumn(120, col => col.AddText("One line"));
            row.AddColumn(col =>
            {
                col.AddText("Line 1");
                col.AddText("Line 2");
                col.AddText("Line 3");
            });
        }, verticalAlignment: alignment);
        c.AddSpacer(12);
    }
}
