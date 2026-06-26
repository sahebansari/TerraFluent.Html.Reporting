using FluentHtmlReport.Measurement;

namespace FluentHtmlReport.Tests.TestHelpers;

/// <summary>Wraps another <see cref="ITextMeasurer"/> and counts how many times <see cref="Measure"/> is called, for performance regression tests.</summary>
public sealed class CountingTextMeasurer : ITextMeasurer
{
    private readonly ITextMeasurer _inner;

    public CountingTextMeasurer(ITextMeasurer inner)
    {
        _inner = inner;
    }

    public int CallCount { get; private set; }

    public TextMeasurement Measure(string text, FontSpecification font, double maxWidthPx)
    {
        CallCount++;
        return _inner.Measure(text, font, maxWidthPx);
    }
}
