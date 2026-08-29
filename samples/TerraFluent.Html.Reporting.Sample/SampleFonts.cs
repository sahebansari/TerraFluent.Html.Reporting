namespace TerraFluent.Html.Reporting.Sample;

/// <summary>Resolves paths to the font files shipped in the fonts/ folder next to the built sample.</summary>
internal static class SampleFonts
{
    public static string Resolve(string fileName) => Path.Combine(AppContext.BaseDirectory, "fonts", fileName);
}
