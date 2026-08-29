using TerraFluent.Html.Reporting.Model;
using TerraFluent.Html.Reporting.Model.Styling;
using Xunit;

namespace TerraFluent.Html.Reporting.Tests.Model;

public class EmbeddedFontTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_InvalidFontFamily_Throws(string? fontFamily)
    {
        Assert.Throws<ArgumentException>(() => new EmbeddedFont(fontFamily!, new byte[] { 1 }, FontWeight.Normal, FontStyle.Normal, "font/woff2"));
    }

    [Theory]
    [InlineData("</style>")]
    [InlineData("Font<Injected>")]
    public void Constructor_FontFamilyContainingAngleBrackets_Throws(string fontFamily)
    {
        Assert.Throws<ArgumentException>(() => new EmbeddedFont(fontFamily, new byte[] { 1 }, FontWeight.Normal, FontStyle.Normal, "font/woff2"));
    }

    [Fact]
    public void Constructor_EmptyFontBytes_Throws()
    {
        Assert.Throws<ArgumentException>(() => new EmbeddedFont("MyFont", Array.Empty<byte>(), FontWeight.Normal, FontStyle.Normal, "font/woff2"));
    }

    [Fact]
    public void Constructor_ValidInput_ClonesBytesDefensively()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var font = new EmbeddedFont("MyFont", bytes, FontWeight.Normal, FontStyle.Normal, "font/woff2");

        bytes[0] = 99;

        Assert.Equal(new byte[] { 1, 2, 3 }, font.FontBytes);
    }
}
