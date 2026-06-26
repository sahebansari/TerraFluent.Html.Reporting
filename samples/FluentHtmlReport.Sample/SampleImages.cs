namespace FluentHtmlReport.Sample;

/// <summary>Resolves paths to the logo images shipped in the images/ folder next to the built sample.</summary>
internal static class SampleImages
{
    public static string Resolve(string fileName) => Path.Combine(AppContext.BaseDirectory, "images", fileName);
}
