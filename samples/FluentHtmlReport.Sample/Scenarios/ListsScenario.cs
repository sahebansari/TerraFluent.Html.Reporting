using FluentHtmlReport.Model;
using FluentHtmlReport.Model.Elements;

namespace FluentHtmlReport.Sample.Scenarios;

/// <summary>Showcases bulleted and numbered lists, including a numbered list long enough to span a page break.</summary>
internal sealed class ListsScenario : ISampleScenario
{
    public string FileName => "05-lists.html";

    public string Description => "Bulleted list, and a numbered list long enough to span pages with numbering intact.";

    public ReportDocument Build() =>
        ReportDocument.Create(PageSize.A4)
            .SetMargins(40)
            .Header(h => h.AddText("Lists").AlignCenter().Bold())
            .Footer(f => f.AddPageNumber().AlignCenter())
            .Content(c =>
            {
                c.AddHeading("Bulleted List", HeadingLevel.H2);
                c.AddList(ListStyle.Bulleted, new[]
                {
                    "First item",
                    "Second item",
                    "Third item, with enough extra text to show how a longer entry wraps across the available width",
                });

                c.AddSpacer(20);
                c.AddHeading("Numbered List Spanning Multiple Pages", HeadingLevel.H2);
                c.AddParagraph(
                    "This list has enough items to split across a page boundary; numbering resumes correctly " +
                    "on the next page (via ReportList.StartIndex) instead of restarting at 1.");
                c.AddList(ListStyle.Numbered, Enumerable.Range(1, 60).Select(i => $"Numbered list entry #{i}"));
            })
            .Build();
}
