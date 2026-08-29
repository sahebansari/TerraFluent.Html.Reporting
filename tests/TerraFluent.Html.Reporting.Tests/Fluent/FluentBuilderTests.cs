using TerraFluent.Html.Reporting.Fluent;
using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Model.Styling;
using TerraFluent.Html.Reporting.Rendering;
using Xunit;

namespace TerraFluent.Html.Reporting.Tests.Fluent;

public class FluentBuilderTests
{
    [Fact]
    public void TextElementBuilder_StyleModifiers_ReplaceTheStoredElementInPlace()
    {
        var document = ReportDocument.Create(PageSize.A4)
            .Header(h => h.AddText("Title").AlignCenter().Bold().FontSize(20))
            .Build();

        var paragraph = Assert.IsType<Paragraph>(Assert.Single(document.Header!.Elements));
        Assert.Equal("Title", paragraph.Text);
        Assert.Equal(TextAlignment.Center, paragraph.Style.Alignment);
        Assert.Equal(FontWeight.Bold, paragraph.Style.FontWeight);
        Assert.Equal(20, paragraph.Style.FontSizePx);
    }

    [Fact]
    public void Header_CalledTwice_AppendsRatherThanReplacing()
    {
        var document = ReportDocument.Create(PageSize.A4)
            .Header(h => h.AddText("Title"))
            .Header(h => h.AddRule())
            .Build();

        Assert.Equal(2, document.Header!.Elements.Count);
        Assert.IsType<Paragraph>(document.Header.Elements[0]);
        Assert.IsType<HorizontalRule>(document.Header.Elements[1]);
    }

    [Fact]
    public void Footer_AddPageNumber_DefaultsToStandardFormat()
    {
        var document = ReportDocument.Create(PageSize.A4)
            .Footer(f => f.AddPageNumber())
            .Build();

        var pageNumber = Assert.IsType<PageNumberText>(Assert.Single(document.Footer!.Elements));
        Assert.Equal("Page {page} of {totalPages}", pageNumber.FormatTemplate);
    }

    [Fact]
    public void Content_PreservesElementOrder()
    {
        var document = ReportDocument.Create(PageSize.A4)
            .Content(c =>
            {
                c.AddHeading("Title", HeadingLevel.H1);
                c.AddParagraph("Body");
                c.AddRule();
                c.AddSpacer(10);
            })
            .Build();

        Assert.Collection(
            document.ContentElements,
            e => Assert.IsType<Heading>(e),
            e => Assert.IsType<Paragraph>(e),
            e => Assert.IsType<HorizontalRule>(e),
            e => Assert.IsType<Spacer>(e));
    }

    [Fact]
    public void AddElement_AddsCustomReportElementToContent()
    {
        var customElement = new StubReportElement();

        var document = ReportDocument.Create(PageSize.A4)
            .Content(c => c.AddElement(customElement))
            .Build();

        Assert.Same(customElement, Assert.Single(document.ContentElements));
    }

    [Fact]
    public void AddElement_NullElement_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ContentBuilder().AddElement(null!));
    }

    [Fact]
    public void AddTable_BuildsColumnsAndRowsFromStrings()
    {
        var document = ReportDocument.Create(PageSize.A4)
            .Content(c => c.AddTable(table =>
            {
                table.AddColumns("Product", "Qty");
                table.AddRow("Widget A", "120");
                table.AddRow("Widget B", "45");
            }))
            .Build();

        var table = Assert.IsType<Table>(Assert.Single(document.ContentElements));
        Assert.Equal(new[] { "Product", "Qty" }, table.Columns.Select(c => c.Header));
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("Widget A", table.Rows[0].Cells[0].Text);
        Assert.Equal("120", table.Rows[0].Cells[1].Text);
    }

    [Fact]
    public void AddRow_OnHeader_BuildsColumnsWithGivenWidthAndElements()
    {
        var document = ReportDocument.Create(PageSize.A4)
            .Header(h => h.AddRow(row =>
            {
                row.AddColumn(40, col => col.AddImage(new byte[] { 1 }, "image/png", widthPx: 40, heightPx: 40));
                row.AddColumn(col => col.AddText("Acme Corp").Bold());
            }))
            .Build();

        var rowElement = Assert.IsType<Row>(Assert.Single(document.Header!.Elements));
        Assert.Equal(2, rowElement.Columns.Count);

        Assert.Equal(40, rowElement.Columns[0].WidthPx);
        Assert.IsType<ReportImage>(Assert.Single(rowElement.Columns[0].Elements));

        Assert.Null(rowElement.Columns[1].WidthPx);
        var text = Assert.IsType<Paragraph>(Assert.Single(rowElement.Columns[1].Elements));
        Assert.Equal("Acme Corp", text.Text);
        Assert.Equal(FontWeight.Bold, text.Style.FontWeight);
    }

    [Fact]
    public void AddRow_DefaultsToTwelvePixelGapAndMiddleVerticalAlignment()
    {
        var document = ReportDocument.Create(PageSize.A4)
            .Content(c => c.AddRow(row => row.AddColumn(col => col.AddText("Solo"))))
            .Build();

        var rowElement = Assert.IsType<Row>(Assert.Single(document.ContentElements));
        Assert.Equal(12, rowElement.ColumnGapPx);
        Assert.Equal(RowVerticalAlignment.Middle, rowElement.VerticalAlignment);
    }

    [Fact]
    public void AddColumn_PaddingChainedOnReturnedHandle_AppliesToTheBuiltColumn()
    {
        var document = ReportDocument.Create(PageSize.A4)
            .Content(c => c.AddRow(row => row.AddColumn(col => col.AddText("Hi")).Padding(2, 4, 6, 8)))
            .Build();

        var rowElement = Assert.IsType<Row>(Assert.Single(document.ContentElements));
        var column = rowElement.Columns[0];
        Assert.Equal(2, column.PaddingTopPx);
        Assert.Equal(4, column.PaddingRightPx);
        Assert.Equal(6, column.PaddingBottomPx);
        Assert.Equal(8, column.PaddingLeftPx);
    }

    [Fact]
    public void AddRow_MarginChainedOnReturnedHandle_AppliesToTheBuiltRow()
    {
        var document = ReportDocument.Create(PageSize.A4)
            .Content(c => c.AddRow(row => row.AddColumn(col => col.AddText("Hi"))).Margin(1, 2, 3, 4))
            .Build();

        var rowElement = Assert.IsType<Row>(Assert.Single(document.ContentElements));
        Assert.Equal(1, rowElement.MarginTopPx);
        Assert.Equal(2, rowElement.MarginRightPx);
        Assert.Equal(3, rowElement.MarginBottomPx);
        Assert.Equal(4, rowElement.MarginLeftPx);
    }

    [Fact]
    public void AddImage_AlignCenterChainedOnReturnedHandle_AppliesToTheBuiltImage()
    {
        var document = ReportDocument.Create(PageSize.A4)
            .Content(c => c.AddImage(new byte[] { 1 }, "image/png", widthPx: 10, heightPx: 10).AlignCenter())
            .Build();

        var image = Assert.IsType<ReportImage>(Assert.Single(document.ContentElements));
        Assert.Equal(TextAlignment.Center, image.Alignment);
    }

    [Fact]
    public void AddBarcode_AddsCode128PngImageAndSupportsImageModifiers()
    {
        var document = ReportDocument.Create(PageSize.A4)
            .Content(c => c.AddBarcode("INV-1001", moduleWidthPx: 2, heightPx: 48).AlignRight().MarginBottom(12))
            .Build();

        var image = Assert.IsType<ReportImage>(Assert.Single(document.ContentElements));
        Assert.Equal("image/png", image.MimeType);
        Assert.Equal(48, image.HeightPx);
        Assert.Equal(TextAlignment.Right, image.Alignment);
        Assert.Equal(12, image.MarginBottomPx);

        var bytes = image.ImageBytes;
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes.Take(4).ToArray());
    }

    [Fact]
    public void AddBarcode_OnRowColumn_AddsImageToColumn()
    {
        var document = ReportDocument.Create(PageSize.A4)
            .Content(c => c.AddRow(row => row.AddColumn(col => col.AddBarcode("SHIP-42", heightPx: 30).AlignCenter())))
            .Build();

        var row = Assert.IsType<Row>(Assert.Single(document.ContentElements));
        var image = Assert.IsType<ReportImage>(Assert.Single(row.Columns[0].Elements));
        Assert.Equal(30, image.HeightPx);
        Assert.Equal(TextAlignment.Center, image.Alignment);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Contains\nNewline")]
    public void AddBarcode_InvalidValue_Throws(string value)
    {
        Assert.ThrowsAny<ArgumentException>(() => new ContentBuilder().AddBarcode(value));
    }

    [Fact]
    public void AddQrCode_AddsSquarePngImageAndSupportsImageModifiers()
    {
        var document = ReportDocument.Create(PageSize.A4)
            .Content(c => c.AddQrCode("INV-1001", moduleWidthPx: 4, quietZoneModules: 4).AlignRight().MarginBottom(12))
            .Build();

        var image = Assert.IsType<ReportImage>(Assert.Single(document.ContentElements));
        Assert.Equal("image/png", image.MimeType);
        Assert.Equal(image.WidthPx, image.HeightPx);
        Assert.Equal(TextAlignment.Right, image.Alignment);
        Assert.Equal(12, image.MarginBottomPx);

        var bytes = image.ImageBytes;
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes.Take(4).ToArray());
    }

    [Fact]
    public void AddQrCode_OnRowColumn_AddsImageToColumn()
    {
        var document = ReportDocument.Create(PageSize.A4)
            .Content(c => c.AddRow(row => row.AddColumn(col => col.AddQrCode("SHIP-42").AlignCenter())))
            .Build();

        var row = Assert.IsType<Row>(Assert.Single(document.ContentElements));
        var image = Assert.IsType<ReportImage>(Assert.Single(row.Columns[0].Elements));
        Assert.Equal(TextAlignment.Center, image.Alignment);
    }

    [Fact]
    public void AddQrCode_EmptyValue_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => new ContentBuilder().AddQrCode(string.Empty));
    }

    [Fact]
    public void AddQrCode_NegativeQuietZone_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentBuilder().AddQrCode("X", quietZoneModules: -1));
    }

    [Fact]
    public void AddQrCode_SameValueTwice_ProducesIdenticalBytes()
    {
        var firstDocument = ReportDocument.Create(PageSize.A4).Content(c => c.AddQrCode("REPEATABLE-42")).Build();
        var secondDocument = ReportDocument.Create(PageSize.A4).Content(c => c.AddQrCode("REPEATABLE-42")).Build();

        var firstImage = Assert.IsType<ReportImage>(Assert.Single(firstDocument.ContentElements));
        var secondImage = Assert.IsType<ReportImage>(Assert.Single(secondDocument.ContentElements));

        Assert.Equal(firstImage.ImageBytes, secondImage.ImageBytes);
    }

    [Fact]
    public void AddQrCode_DifferentValues_ProduceDifferentBytes()
    {
        var firstDocument = ReportDocument.Create(PageSize.A4).Content(c => c.AddQrCode("VALUE-ONE")).Build();
        var secondDocument = ReportDocument.Create(PageSize.A4).Content(c => c.AddQrCode("VALUE-TWO")).Build();

        var firstImage = Assert.IsType<ReportImage>(Assert.Single(firstDocument.ContentElements));
        var secondImage = Assert.IsType<ReportImage>(Assert.Single(secondDocument.ContentElements));

        Assert.NotEqual(firstImage.ImageBytes, secondImage.ImageBytes);
    }

    [Fact]
    public void AddQrCode_LongerValue_SelectsLargerVersionThanShorterValue()
    {
        var shortDocument = ReportDocument.Create(PageSize.A4)
            .Content(c => c.AddQrCode("short"))
            .Build();
        var longDocument = ReportDocument.Create(PageSize.A4)
            .Content(c => c.AddQrCode(new string('x', 500)))
            .Build();

        var shortImage = Assert.IsType<ReportImage>(Assert.Single(shortDocument.ContentElements));
        var longImage = Assert.IsType<ReportImage>(Assert.Single(longDocument.ContentElements));

        Assert.True(longImage.WidthPx > shortImage.WidthPx);
    }

    [Fact]
    public void AddQrCode_TooLongForLargestVersion_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => new ContentBuilder().AddQrCode(new string('x', 5000)));
    }

    [Fact]
    public void SetMargins_FourValues_MapsToTopRightBottomLeft()
    {
        var document = ReportDocument.Create(PageSize.A4)
            .SetMargins(10, 20, 30, 40)
            .Build();

        Assert.Equal(10, document.Margins.Top);
        Assert.Equal(20, document.Margins.Right);
        Assert.Equal(30, document.Margins.Bottom);
        Assert.Equal(40, document.Margins.Left);
    }

    private sealed class StubReportElement : IReportElement
    {
        public ElementMeasurement Measure(LayoutContext context) => new(1);

        public SplitResult Split(double availableHeightPx, LayoutContext context) =>
            SplitResult.Unsplittable(this);

        public string RenderHtml(ElementPlacement placement, RenderContext context) => string.Empty;
    }
}
