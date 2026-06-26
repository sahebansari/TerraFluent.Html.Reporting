using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Model.Styling;
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
}
