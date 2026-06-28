using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Elements;
using TerraFluent.Html.Reporting.Rendering;
using Xunit;

namespace TerraFluent.Html.Reporting.Tests.Model;

public class ReportImageTests
{
    private static byte[] BuildPng(int width, int height)
    {
        var bytes = new byte[24];
        byte[] signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        Array.Copy(signature, bytes, 8);
        WriteBigEndian(bytes, 16, width);
        WriteBigEndian(bytes, 20, height);
        return bytes;
    }

    private static byte[] BuildGif(int width, int height)
    {
        var bytes = new byte[10];
        byte[] signature = { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a' };
        Array.Copy(signature, bytes, 6);
        bytes[6] = (byte)(width & 0xFF);
        bytes[7] = (byte)((width >> 8) & 0xFF);
        bytes[8] = (byte)(height & 0xFF);
        bytes[9] = (byte)((height >> 8) & 0xFF);
        return bytes;
    }

    private static byte[] BuildBmp(int width, int height)
    {
        var bytes = new byte[26];
        bytes[0] = (byte)'B';
        bytes[1] = (byte)'M';
        WriteLittleEndian(bytes, 18, width);
        WriteLittleEndian(bytes, 22, height);
        return bytes;
    }

    private static byte[] BuildJpeg(int width, int height)
    {
        var bytes = new byte[17];
        bytes[0] = 0xFF; bytes[1] = 0xD8; // SOI
        bytes[2] = 0xFF; bytes[3] = 0xC0; // SOF0
        bytes[4] = 0x00; bytes[5] = 0x0B; // segment length = 11
        bytes[6] = 0x08; // precision
        bytes[7] = (byte)((height >> 8) & 0xFF);
        bytes[8] = (byte)(height & 0xFF);
        bytes[9] = (byte)((width >> 8) & 0xFF);
        bytes[10] = (byte)(width & 0xFF);
        bytes[11] = 0x01; // 1 component
        bytes[12] = 0x00; bytes[13] = 0x11; bytes[14] = 0x00;
        bytes[15] = 0xFF; bytes[16] = 0xD9; // EOI
        return bytes;
    }

    private static void WriteBigEndian(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 24) & 0xFF);
        bytes[offset + 1] = (byte)((value >> 16) & 0xFF);
        bytes[offset + 2] = (byte)((value >> 8) & 0xFF);
        bytes[offset + 3] = (byte)(value & 0xFF);
    }

    private static void WriteLittleEndian(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value & 0xFF);
        bytes[offset + 1] = (byte)((value >> 8) & 0xFF);
        bytes[offset + 2] = (byte)((value >> 16) & 0xFF);
        bytes[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    [Theory]
    [InlineData("png")]
    [InlineData("gif")]
    [InlineData("bmp")]
    [InlineData("jpeg")]
    public void FromBytes_NoExplicitDimensions_UsesIntrinsicSize(string format)
    {
        var bytes = format switch
        {
            "png" => BuildPng(200, 100),
            "gif" => BuildGif(200, 100),
            "bmp" => BuildBmp(200, 100),
            "jpeg" => BuildJpeg(200, 100),
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };

        var image = ReportImage.FromBytes(bytes);

        Assert.Equal(200, image.WidthPx);
        Assert.Equal(100, image.HeightPx);
    }

    [Fact]
    public void FromBytes_OnlyWidthGiven_DerivesHeightFromAspectRatio()
    {
        var bytes = BuildPng(200, 100); // 2:1 aspect ratio

        var image = ReportImage.FromBytes(bytes, widthPx: 60);

        Assert.Equal(60, image.WidthPx);
        Assert.Equal(30, image.HeightPx);
    }

    [Fact]
    public void FromBytes_OnlyHeightGiven_DerivesWidthFromAspectRatio()
    {
        var bytes = BuildPng(200, 100); // 2:1 aspect ratio

        var image = ReportImage.FromBytes(bytes, heightPx: 50);

        Assert.Equal(100, image.WidthPx);
        Assert.Equal(50, image.HeightPx);
    }

    [Fact]
    public void FromBytes_BothDimensionsGiven_OverridesIntrinsicSizeEvenIfDistorted()
    {
        var bytes = BuildPng(200, 100);

        var image = ReportImage.FromBytes(bytes, widthPx: 10, heightPx: 10);

        Assert.Equal(10, image.WidthPx);
        Assert.Equal(10, image.HeightPx);
    }

    [Fact]
    public void FromBase64_WithDataUriPrefix_DecodesPayloadOnly()
    {
        var bytes = BuildPng(40, 20);
        var dataUri = "data:image/png;base64," + Convert.ToBase64String(bytes);

        var image = ReportImage.FromBase64(dataUri);

        Assert.Equal(40, image.WidthPx);
        Assert.Equal(20, image.HeightPx);
    }

    [Fact]
    public void FromBytes_UnrecognizedFormatAndNoDimensions_Throws()
    {
        var bytes = new byte[] { 0, 1, 2, 3, 4, 5 };

        Assert.Throws<InvalidOperationException>(() => ReportImage.FromBytes(bytes));
    }

    [Fact]
    public void With_OverridesGivenPropertiesOnly_KeepsBytesAndDimensions()
    {
        var image = ReportImage.FromBytes(BuildPng(50, 20), widthPx: 50, heightPx: 20);

        var styled = image.With(marginTopPx: 1, paddingLeftPx: 2, alignment: TextAlignment.Center);

        Assert.Equal(50, styled.WidthPx);
        Assert.Equal(20, styled.HeightPx);
        Assert.Equal(image.ImageBytes, styled.ImageBytes);
        Assert.NotSame(image.ImageBytes, styled.ImageBytes);
        Assert.Equal(1, styled.MarginTopPx);
        Assert.Equal(2, styled.PaddingLeftPx);
        Assert.Equal(TextAlignment.Center, styled.Alignment);
        Assert.Equal(8, styled.MarginBottomPx); // untouched default preserved
    }

    [Fact]
    public void Measure_AddsFullMarginAndPadding()
    {
        var image = ReportImage.FromBytes(BuildPng(50, 20), widthPx: 50, heightPx: 20)
            .With(marginTopPx: 1, marginBottomPx: 2, paddingTopPx: 3, paddingBottomPx: 4);

        var measurement = image.Measure(new LayoutContext(TerraFluent.Html.Reporting.Measurement.ApproximateTextMeasurer.Instance, 200));

        Assert.Equal(20 + 1 + 2 + 3 + 4, measurement.HeightPx);
    }

    [Theory]
    [InlineData(TextAlignment.Left, 0)]
    [InlineData(TextAlignment.Center, 75)]
    [InlineData(TextAlignment.Right, 150)]
    public void RenderHtml_Alignment_PositionsImageWithinAvailableWidth(TextAlignment alignment, double expectedOffsetPx)
    {
        var image = ReportImage.FromBytes(BuildPng(50, 20), widthPx: 50, heightPx: 20).With(alignment: alignment);
        var placement = new ElementPlacement(10, 10, 200, 28, 0, PageSectionKind.Content);

        var html = image.RenderHtml(placement, new RenderContext(1, 1));

        Assert.Contains($"left:{10 + expectedOffsetPx:0.##}px;top:10px;width:50px;height:20px;", html);
    }

    [Fact]
    public void RenderHtml_MarginAndPadding_InsetTheImageWithinItsBox()
    {
        var image = ReportImage.FromBytes(BuildPng(50, 20), widthPx: 50, heightPx: 20)
            .With(marginTopPx: 4, marginLeftPx: 5, paddingTopPx: 1, paddingLeftPx: 2, alignment: TextAlignment.Center);
        // Box width = 50 + paddingLeft(2) + paddingRight(0) = 52; available = 200 - marginLeft(5) = 195;
        // center offset = (195 - 52) / 2 = 71.5; image left = X + marginLeft(5) + 71.5 + paddingLeft(2).
        var placement = new ElementPlacement(0, 0, 200, 28, 0, PageSectionKind.Content);

        var html = image.RenderHtml(placement, new RenderContext(1, 1));

        Assert.Contains("left:78.5px;top:5px;width:50px;height:20px;", html);
    }
}
