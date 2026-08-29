using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Model.Styling;

namespace TerraFluent.Html.Reporting.Sample.Scenarios;

/// <summary>
/// Showcases TextStyle.Direction: right-to-left paragraphs, a heading, a
/// list, and a table cell, each emitting the HTML dir attribute and CSS
/// direction property - contrasted with the same elements left-to-right.
/// </summary>
internal sealed class RtlTextScenario : ISampleScenario
{
    public string FileName => "17-rtl-text.html";

    public string Title => "Right-to-Left Text";

    public string Description => "TextStyle.Direction sets dir/direction on paragraphs, headings, lists, and table cells - independent of Alignment, which stays physical (Left/Right), not logical (Start/End).";

    public ReportDocument Build()
    {
        var rtl = TextStyle.Default.With(direction: TextDirection.Rtl);
        var rtlRightAligned = rtl.With(alignment: TextAlignment.Right);

        return ReportDocument.Create(PageSize.A4)
            .SetMargins(40)
            .Header(h => h.AddText("Right-to-Left Text").AlignCenter().Bold())
            .Footer(f => f.AddPageNumber().AlignCenter())
            .Content(c =>
            {
                c.AddHeading("Left-to-Right (Default)", HeadingLevel.H2);
                c.AddParagraph(
                    "This paragraph uses the default TextDirection.Ltr. Its text-align stays at " +
                    "the default Left, and the two naturally agree: left-to-right text that starts " +
                    "from the left edge.");
                c.AddSpacer(16);

                c.AddHeading("عربي (Right-to-Left)", HeadingLevel.H2, rtl);
                c.AddParagraph(
                    "يولد جميع الناس أحرارًا متساوين في الكرامة والحقوق. وقد وهبوا عقلاً وضميرًا " +
                    "وعليهم أن يعامل بعضهم بعضاً بروح الإخاء.",
                    rtl);
                c.AddParagraph(
                    "The heading and paragraph above set Direction: TextDirection.Rtl, which emits " +
                    "dir=\"rtl\" and direction:rtl on the rendered <h2>/<p> tags. Direction alone does " +
                    "not change text-align - it's still Left by default, which is why the text above " +
                    "starts flush against the left edge of its box even though it reads right-to-left. " +
                    "The next paragraph adds Alignment: TextAlignment.Right explicitly for a fully " +
                    "mirrored block:");
                c.AddParagraph(
                    "هذا نص تجريبي محاذى إلى اليمين، بالإضافة إلى اتجاهه من اليمين إلى اليسار.",
                    rtlRightAligned);
                c.AddSpacer(16);

                c.AddHeading("In a Table Cell", HeadingLevel.H3);
                c.AddTable(table =>
                {
                    table.AddColumns("Language", "Sample");
                    table.AddRow("English", "The quick brown fox jumps over the lazy dog.");
                    table.AddRow(new TableCell[]
                    {
                        "Arabic",
                        new TableCell("الثعلب البني السريع يقفز فوق الكلب الكسول.", rtlRightAligned),
                    });
                });
            })
            .Build();
    }
}
