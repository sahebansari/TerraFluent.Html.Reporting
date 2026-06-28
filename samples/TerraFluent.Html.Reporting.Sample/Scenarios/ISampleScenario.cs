using TerraFluent.Html.Reporting.Model;

namespace TerraFluent.Html.Reporting.Sample.Scenarios;

/// <summary>One self-contained sample report, focused on showcasing a specific set of library features.</summary>
internal interface ISampleScenario
{
    /// <summary>The output HTML file name, without a directory.</summary>
    string FileName { get; }

    /// <summary>A short display title, used in the sample index page.</summary>
    string Title { get; }

    /// <summary>A one-line description of what this sample demonstrates, printed to the console.</summary>
    string Description { get; }

    /// <summary>Builds the report document.</summary>
    ReportDocument Build();
}
