using System.Text;
using TerraFluent.Html.Reporting.Measurement;

namespace TerraFluent.Html.Reporting.Tests.TestHelpers;

/// <summary>
/// A deterministic <see cref="ITextMeasurer"/> for layout-engine tests: every
/// character has the same fixed width and every line the same fixed height,
/// regardless of the requested font. This lets tests predict exact line
/// counts/heights from character counts instead of depending on
/// <c>ApproximateTextMeasurer</c>'s font-metric approximations.
/// </summary>
public sealed class FakeTextMeasurer : ITextMeasurer
{
    public double CharWidthPx { get; init; } = 10;

    public double FixedLineHeightPx { get; init; } = 20;

    public TextMeasurement Measure(string text, FontSpecification font, double maxWidthPx)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new TextMeasurement(new[] { string.Empty }, FixedLineHeightPx, 0);
        }

        var maxCharsPerLine = Math.Max(1, (int)(maxWidthPx / CharWidthPx));
        var lines = new List<string>();
        var widest = 0.0;

        foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            WrapParagraph(paragraph, maxCharsPerLine, lines, ref widest);
        }

        return new TextMeasurement(lines, FixedLineHeightPx, widest);
    }

    private void WrapParagraph(string paragraph, int maxCharsPerLine, List<string> lines, ref double widest)
    {
        var current = new StringBuilder();

        foreach (var word in paragraph.Split(' '))
        {
            var candidateLength = current.Length == 0 ? word.Length : current.Length + 1 + word.Length;

            if (current.Length > 0 && candidateLength > maxCharsPerLine)
            {
                Flush(current, lines, ref widest);
                current.Clear();
            }

            if (current.Length > 0) current.Append(' ');
            current.Append(word);
        }

        Flush(current, lines, ref widest);
    }

    private void Flush(StringBuilder current, List<string> lines, ref double widest)
    {
        lines.Add(current.ToString());
        var width = current.Length * CharWidthPx;
        if (width > widest) widest = width;
    }
}
