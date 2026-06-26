using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;

namespace TerraFluent.Html.Reporting.Sample.Scenarios;

/// <summary>Showcases the heading style scale, inline text modifiers, and the four text alignments.</summary>
internal sealed class TextStylingScenario : ISampleScenario
{
    public string FileName => "02-text-styling.html";

    public string Description => "Heading levels H1-H6, bold/italic/color/font/size modifiers, and text alignment.";

    private const string Sentence =
        "The quick brown fox jumps over the lazy dog, demonstrating how this paragraph wraps across the available width.";

    public ReportDocument Build() =>
        ReportDocument.Create(PageSize.A4)
            .SetMargins(40)
            .Header(h => h.AddText("Text Styling Showcase").AlignCenter().Bold())
            .Footer(f => f.AddPageNumber().AlignCenter())
            .Content(c =>
            {
                c.AddHeading("Heading Levels", HeadingLevel.H1);
                c.AddHeading("This is an H1 heading", HeadingLevel.H1);
                c.AddHeading("This is an H2 heading", HeadingLevel.H2);
                c.AddHeading("This is an H3 heading", HeadingLevel.H3);
                c.AddHeading("This is an H4 heading", HeadingLevel.H4);
                c.AddHeading("This is an H5 heading", HeadingLevel.H5);
                c.AddHeading("This is an H6 heading", HeadingLevel.H6);
                c.AddRule();

                c.AddHeading("Inline Style Modifiers", HeadingLevel.H2);
                c.AddParagraph("This paragraph is bold.").Bold();
                c.AddParagraph("This paragraph is italic.").Italic();
                c.AddParagraph("This paragraph has a custom font color.").FontColor("#b23b3b");
                c.AddParagraph("This paragraph uses a serif font family.").FontFamily("Georgia, 'Times New Roman', serif");
                c.AddParagraph("This paragraph has a larger font size.").FontSize(20);
                c.AddRule();

                c.AddHeading("Text Alignment", HeadingLevel.H2);
                c.AddParagraph("Left-aligned (the default). " + Sentence).AlignLeft();
                c.AddParagraph("Center-aligned. " + Sentence).AlignCenter();
                c.AddParagraph("Right-aligned. " + Sentence).AlignRight();
                c.AddParagraph("Justified, so both edges line up evenly. " + Sentence + " " + Sentence).AlignJustify();
            })
            .Build();
}
