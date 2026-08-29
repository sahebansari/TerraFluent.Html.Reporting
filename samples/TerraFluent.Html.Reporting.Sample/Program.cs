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
    new InvoiceBarcodeScenario(),
    new TableSpansScenario(),
    new MultiColumnScenario(),
    new ShippingLabelScenario(),
    new CustomFontsScenario(),
    new RtlTextScenario(),
    new ColumnDiagnosticsScenario(),
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
        // LayoutWarning carries structured fields (Reason/ElementType/PageIndex/
        // ElementIndex), not just a free-form Message - printing them all here
        // instead of just `warning` (its ToString()) is what makes this usable
        // as a debugging aid, e.g. filtering/grepping by Reason in CI logs.
        Console.WriteLine(
            $"  -> warning [{warning.Reason}] page {warning.PageIndex + 1}, element #{warning.ElementIndex} " +
            $"({warning.ElementType}): {warning.Message}");
    }

    var path = Path.Combine(outputDir, scenario.FileName);
    await document.RenderHtmlDocumentAsync(path);
}

var indexPath = Path.Combine(outputDir, "index.html");
await File.WriteAllTextAsync(indexPath, SampleIndexPage.Build(scenarios));

Console.WriteLine();
Console.WriteLine($"Wrote {scenarios.Length} sample reports to {outputDir}");
Console.WriteLine($"Open {indexPath} to browse them.");
