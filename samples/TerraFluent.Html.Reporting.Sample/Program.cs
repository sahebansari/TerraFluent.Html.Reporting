using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Sample;
using TerraFluent.Html.Reporting.Sample.Scenarios;

ISampleScenario[] scenarios =
[
    new GettingStartedScenario(),
    new TextStylingScenario(),
    new ImagesScenario(),
    new TableStylingScenario(),
    new ListsScenario(),
    new PageBreaksScenario(),
    new LandscapeCertificateScenario(),
    new RawHtmlScenario(),
    new WarningsAndAsyncScenario(),
    new SalesInvoiceScenario(),
    new RowLayoutScenario(),
];

var outputDir = AppContext.BaseDirectory;

foreach (var scenario in scenarios)
{
    var document = scenario.Build();
    var layout = LayoutEngine.Paginate(document);

    Console.WriteLine($"{scenario.FileName}");
    Console.WriteLine($"  {scenario.Description}");
    Console.WriteLine($"  -> {layout.Pages.Count} page(s)");
    foreach (var warning in layout.Warnings)
    {
        Console.WriteLine($"  -> warning: {warning}");
    }

    var path = Path.Combine(outputDir, scenario.FileName);
    await document.RenderHtmlDocumentAsync(path);
}

var indexPath = Path.Combine(outputDir, "index.html");
await File.WriteAllTextAsync(indexPath, SampleIndexPage.Build(scenarios));

Console.WriteLine();
Console.WriteLine($"Wrote {scenarios.Length} sample reports to {outputDir}");
Console.WriteLine($"Open {indexPath} to browse them.");
