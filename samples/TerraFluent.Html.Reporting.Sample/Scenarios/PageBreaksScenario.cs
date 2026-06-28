using TerraFluent.Html.Reporting.Fluent;
using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;

namespace TerraFluent.Html.Reporting.Sample.Scenarios;

/// <summary>Showcases AddPageBreak() forcing each chapter to start on its own fresh page.</summary>
internal sealed class PageBreaksScenario : ISampleScenario
{
    public string FileName => "06-page-breaks.html";

    public string Title => "Page Breaks";

    public string Description => "AddPageBreak() forces each chapter to start on a fresh page regardless of remaining space.";

    public ReportDocument Build() =>
        ReportDocument.Create(PageSize.A4)
            .SetMargins(40)
            .Header(h => h.AddText("Page Breaks").AlignCenter().Bold())
            .Footer(f => f.AddPageNumber().AlignCenter())
            .Content(c =>
            {
                AddChapter(c, "Chapter 1: Introduction",
                    "This document demonstrates explicit page breaks. Each chapter below starts on its own " +
                    "fresh page via AddPageBreak(), regardless of how much room was left on the previous page.");
                c.AddPageBreak();

                AddChapter(c, "Chapter 2: Methodology",
                    "A page break is a structural instruction, not a measured element - it has no height and " +
                    "produces no visible output. The layout engine special-cases it before the usual fit-or-split logic.");
                c.AddPageBreak();

                AddChapter(c, "Chapter 3: Results",
                    "A leading page break (one with nothing placed on the page yet) is a no-op, so chapters never " +
                    "accidentally produce a blank page before their content.");
                c.AddPageBreak();

                AddChapter(c, "Chapter 4: Conclusion",
                    "Combine AddPageBreak() with headings to give every major section of a long report its own " +
                    "page, the way a printed document's chapters typically work.");
            })
            .Build();

    private static void AddChapter(ContentBuilder c, string title, string body)
    {
        c.AddHeading(title, HeadingLevel.H1);
        c.AddParagraph(body);
    }
}
