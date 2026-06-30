using System.Reflection;
using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Measurement;
using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Rendering;
using Xunit;

namespace TerraFluent.Html.Reporting.Tests.Model;

public sealed class PublicApiSnapshotTests
{
    [Fact]
    public void ExportedTypes_MatchStableSnapshot()
    {
        var actual = typeof(ReportDocument).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedExportedTypes, actual);
    }

    [Fact]
    public void CoreStableEntryPoints_RemainAvailable()
    {
        Assert.NotNull(typeof(ReportDocument).GetMethod(
            nameof(ReportDocument.Create),
            BindingFlags.Public | BindingFlags.Static,
            new[] { typeof(PageSize), typeof(PageOrientation) }));

        Assert.NotNull(typeof(ReportDocument).GetMethod(
            nameof(ReportDocument.RenderHtml),
            BindingFlags.Public | BindingFlags.Instance,
            new[] { typeof(IHtmlReportRenderer), typeof(CancellationToken) }));

        Assert.NotNull(typeof(ReportDocument).GetMethod(
            nameof(ReportDocument.RenderFragment),
            BindingFlags.Public | BindingFlags.Instance,
            new[] { typeof(IHtmlReportRenderer), typeof(CancellationToken) }));

        Assert.NotNull(typeof(LayoutEngine).GetMethod(
            nameof(LayoutEngine.Paginate),
            BindingFlags.Public | BindingFlags.Static,
            new[] { typeof(ReportDocument), typeof(CancellationToken) }));

        Assert.NotNull(typeof(ITextMeasurer).GetMethod(
            nameof(ITextMeasurer.Measure),
            new[] { typeof(string), typeof(FontSpecification), typeof(double) }));
    }

    private static readonly string[] ExpectedExportedTypes =
    {
        "TerraFluent.Html.Reporting.Fluent.ContentBuilder",
        "TerraFluent.Html.Reporting.Fluent.ImageElementBuilder",
        "TerraFluent.Html.Reporting.Fluent.PageSectionBuilder",
        "TerraFluent.Html.Reporting.Fluent.ReportDocumentBuilder",
        "TerraFluent.Html.Reporting.Fluent.RowBuilder",
        "TerraFluent.Html.Reporting.Fluent.RowColumnBuilder",
        "TerraFluent.Html.Reporting.Fluent.RowColumnHandle",
        "TerraFluent.Html.Reporting.Fluent.RowHandle",
        "TerraFluent.Html.Reporting.Fluent.TableBuilder",
        "TerraFluent.Html.Reporting.Fluent.TextElementBuilder",
        "TerraFluent.Html.Reporting.Layout.ElementMeasurement",
        "TerraFluent.Html.Reporting.Layout.ElementPlacement",
        "TerraFluent.Html.Reporting.Layout.LayoutContext",
        "TerraFluent.Html.Reporting.Layout.LayoutEngine",
        "TerraFluent.Html.Reporting.Layout.LayoutResult",
        "TerraFluent.Html.Reporting.Layout.LayoutWarning",
        "TerraFluent.Html.Reporting.Layout.PageLayout",
        "TerraFluent.Html.Reporting.Layout.PlacedElement",
        "TerraFluent.Html.Reporting.Layout.SplitResult",
        "TerraFluent.Html.Reporting.Measurement.ApproximateTextMeasurer",
        "TerraFluent.Html.Reporting.Measurement.FontSpecification",
        "TerraFluent.Html.Reporting.Measurement.ITextMeasurer",
        "TerraFluent.Html.Reporting.Measurement.TextMeasurement",
        "TerraFluent.Html.Reporting.Model.Elements.Heading",
        "TerraFluent.Html.Reporting.Model.Elements.HorizontalRule",
        "TerraFluent.Html.Reporting.Model.Elements.PageBreak",
        "TerraFluent.Html.Reporting.Model.Elements.PageNumberText",
        "TerraFluent.Html.Reporting.Model.Elements.Paragraph",
        "TerraFluent.Html.Reporting.Model.Elements.RawHtml",
        "TerraFluent.Html.Reporting.Model.Elements.ReportImage",
        "TerraFluent.Html.Reporting.Model.Elements.ReportList",
        "TerraFluent.Html.Reporting.Model.Elements.Row",
        "TerraFluent.Html.Reporting.Model.Elements.RowColumn",
        "TerraFluent.Html.Reporting.Model.Elements.Spacer",
        "TerraFluent.Html.Reporting.Model.Elements.Table",
        "TerraFluent.Html.Reporting.Model.Elements.TableCell",
        "TerraFluent.Html.Reporting.Model.Elements.TableColumn",
        "TerraFluent.Html.Reporting.Model.Elements.TableRow",
        "TerraFluent.Html.Reporting.Model.FontStyle",
        "TerraFluent.Html.Reporting.Model.FontWeight",
        "TerraFluent.Html.Reporting.Model.HeadingLevel",
        "TerraFluent.Html.Reporting.Model.IReportElement",
        "TerraFluent.Html.Reporting.Model.ListStyle",
        "TerraFluent.Html.Reporting.Model.Margins",
        "TerraFluent.Html.Reporting.Model.PageOrientation",
        "TerraFluent.Html.Reporting.Model.PageSectionKind",
        "TerraFluent.Html.Reporting.Model.PageSize",
        "TerraFluent.Html.Reporting.Model.ReportDocument",
        "TerraFluent.Html.Reporting.Model.RowSplitBehavior",
        "TerraFluent.Html.Reporting.Model.RowVerticalAlignment",
        "TerraFluent.Html.Reporting.Model.Sections.IPageSection",
        "TerraFluent.Html.Reporting.Model.Sections.PageSection",
        "TerraFluent.Html.Reporting.Model.Styling.TableStyle",
        "TerraFluent.Html.Reporting.Model.Styling.TextStyle",
        "TerraFluent.Html.Reporting.Model.Styling.TextStyleExtensions",
        "TerraFluent.Html.Reporting.Model.TextAlignment",
        "TerraFluent.Html.Reporting.Rendering.HtmlReportRenderer",
        "TerraFluent.Html.Reporting.Rendering.IHtmlReportRenderer",
        "TerraFluent.Html.Reporting.Rendering.RenderContext",
    };
}
