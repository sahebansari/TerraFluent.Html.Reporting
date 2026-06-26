using TerraFluent.Html.Reporting.Measurement;

namespace TerraFluent.Html.Reporting.Model.Styling;

/// <summary>Bridges the document model's <see cref="TextStyle"/> to the measurer-facing <see cref="FontSpecification"/>.</summary>
public static class TextStyleExtensions
{
    /// <summary>Projects the font-relevant parts of a <see cref="TextStyle"/> into a <see cref="FontSpecification"/>.</summary>
    public static FontSpecification ToFontSpecification(this TextStyle style) => new(
        style.FontFamily,
        style.FontSizePx,
        style.FontWeight == FontWeight.Bold,
        style.FontStyle == FontStyle.Italic,
        style.LineHeightMultiplier);
}
