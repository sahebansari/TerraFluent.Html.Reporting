using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Measurement;
using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Model.Sections;
using TerraFluent.Html.Reporting.Model.Styling;
using TerraFluent.Html.Reporting.Tests.TestHelpers;
using Xunit;

namespace TerraFluent.Html.Reporting.Tests.Model;

public sealed class PublicBoundaryTests
{
    public static TheoryData<double> InvalidNonNegativeValues => new()
    {
        -1,
        double.NaN,
        double.PositiveInfinity,
        double.NegativeInfinity,
    };

    public static TheoryData<double> InvalidPositiveValues => new()
    {
        0,
        -1,
        double.NaN,
        double.PositiveInfinity,
        double.NegativeInfinity,
    };

    [Theory]
    [MemberData(nameof(InvalidNonNegativeValues))]
    public void NonNegativePublicDimensions_RejectNegativeAndNonFiniteValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Margins(value, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextStyle { MarginTopPx = value });
        Assert.Throws<ArgumentOutOfRangeException>(() => new TableStyle { CellPaddingPx = value });
        Assert.Throws<ArgumentOutOfRangeException>(() => new HorizontalRule { ThicknessPx = value });
        Assert.Throws<ArgumentOutOfRangeException>(() => new Spacer(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RawHtml("<b>x</b>", value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TableColumn("A") { WidthPx = value });
        Assert.Throws<ArgumentOutOfRangeException>(() => new RowColumn(Array.Empty<IReportElement>(), value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Row(new[] { new RowColumn(Array.Empty<IReportElement>()) }, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReportList(ListStyle.Bulleted, new[] { "A" }) { IndentPx = value });
        Assert.Throws<ArgumentOutOfRangeException>(() => new ElementMeasurement(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ElementPlacement(0, 0, value, 0, 0, PageSectionKind.Content));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextMeasurement(new[] { "A" }, 1, value));

        var image = ReportImage.FromBytes(new byte[] { 1 }, widthPx: 10, heightPx: 10);
        Assert.Throws<ArgumentOutOfRangeException>(() => image.With(marginTopPx: value));
    }

    [Theory]
    [MemberData(nameof(InvalidPositiveValues))]
    public void PositivePublicDimensions_RejectZeroNegativeAndNonFiniteValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PageSize.FromPixels(value, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextStyle { FontSizePx = value });
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextStyle { LineHeightMultiplier = value });
        Assert.Throws<ArgumentOutOfRangeException>(() => new FontSpecification("Arial", value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FontSpecification("Arial", 12, lineHeightMultiplier: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LayoutContext(new FakeTextMeasurer(), value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextMeasurement(new[] { "A" }, value, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ReportImage.FromBytes(new byte[] { 1 }, widthPx: value, heightPx: 10));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PlacementCoordinates_RejectNonFiniteValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ElementPlacement(value, 0, 1, 1, 0, PageSectionKind.Content));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ElementPlacement(0, value, 1, 1, 0, PageSectionKind.Content));
    }

    [Fact]
    public void ReportList_StartIndexRejectsNegativeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReportList(ListStyle.Numbered, new[] { "A" }) { StartIndex = -1 });
    }

    [Fact]
    public void PublicCollections_AreSnapshotsOfCallerOwnedLists()
    {
        var cells = new List<TableCell> { new("A") };
        var row = new TableRow(cells);
        cells.Add(new TableCell("B"));
        Assert.Single(row.Cells);

        var rows = new List<TableRow> { row };
        var columns = new List<TableColumn> { new("Column") };
        var table = new Table(columns, rows);
        rows.Clear();
        columns.Clear();
        Assert.Single(table.Rows);
        Assert.Single(table.Columns);

        var items = new List<string> { "One" };
        var list = new ReportList(ListStyle.Bulleted, items);
        items.Add("Two");
        Assert.Single(list.Items);

        var elements = new List<IReportElement> { new Spacer(1) };
        var column = new RowColumn(elements);
        var section = new PageSection(PageSectionKind.Header, elements);
        elements.Clear();
        Assert.Single(column.Elements);
        Assert.Single(section.Elements);
    }

    [Fact]
    public void LayoutAndMeasurementCollections_AreSnapshotsOfCallerOwnedLists()
    {
        var lines = new List<string> { "A" };
        var measurement = new TextMeasurement(lines, 10, 1);
        lines.Add("B");
        Assert.Single(measurement.Lines);

        var placements = new List<PlacedElement>();
        var page = new PageLayout(0, placements, placements, placements);
        placements.Add(new PlacedElement(new Spacer(1), new ElementPlacement(0, 0, 1, 1, 0, PageSectionKind.Content)));
        Assert.Empty(page.ContentElements);

        var pages = new List<PageLayout> { page };
        var warnings = new List<LayoutWarning> { new(0, "warning") };
        var result = new LayoutResult(pages, PageSize.A4, Margins.None, warnings);
        pages.Clear();
        warnings.Clear();
        Assert.Single(result.Pages);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void ReportImage_CopiesInputAndDoesNotExposeItsInternalByteArray()
    {
        var source = new byte[] { 1, 2, 3 };
        var image = ReportImage.FromBytes(source, widthPx: 10, heightPx: 10);

        source[0] = 9;
        Assert.Equal(1, image.ImageBytes[0]);

        var exposedCopy = image.ImageBytes;
        exposedCopy[0] = 8;
        Assert.Equal(1, image.ImageBytes[0]);
    }
}
