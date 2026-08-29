using TerraFluent.Html.Reporting.Compatibility;

namespace TerraFluent.Html.Reporting.Model.Elements;

internal static class BarcodeImage
{
    private static readonly string[] Code128Patterns =
    {
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
        "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
        "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
        "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
        "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
        "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
        "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
        "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
        "114131", "311141", "411131", "211412", "211214", "211232", "2331112",
    };

    public static ReportImage CreateCode128(string value, double moduleWidthPx, double heightPx, int quietZoneModules)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("Barcode value must not be empty.", nameof(value));
        }

        Guard.Positive(moduleWidthPx, nameof(moduleWidthPx));
        Guard.Positive(heightPx, nameof(heightPx));
        if (quietZoneModules < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quietZoneModules), quietZoneModules, "Quiet zone must be zero or greater.");
        }

        var moduleWidth = CheckedRoundUp(moduleWidthPx, nameof(moduleWidthPx));
        var height = CheckedRoundUp(heightPx, nameof(heightPx));
        var modules = BuildCode128Modules(value, quietZoneModules);
        var width = checked(modules.Length * moduleWidth);
        var pixels = new byte[height * (1 + width * 3)];
        var pos = 0;

        for (var y = 0; y < height; y++)
        {
            pixels[pos++] = 0; // PNG filter type: None
            foreach (var isBar in modules)
            {
                for (var x = 0; x < moduleWidth; x++)
                {
                    var color = isBar ? (byte)0 : (byte)255;
                    pixels[pos++] = color;
                    pixels[pos++] = color;
                    pixels[pos++] = color;
                }
            }
        }

        var png = PngWriter.CreateRgb(width, height, pixels);
        return ReportImage.FromBytes(png, "image/png", width, height);
    }
    // PngWriter moved to its own file, shared with QrCodeImage - see PngWriter.cs.

    private static bool[] BuildCode128Modules(string value, int quietZoneModules)
    {
        const int startCodeB = 104;
        const int stopCode = 106;

        var codes = new List<int>(value.Length + 3) { startCodeB };
        var checksum = startCodeB;
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (ch < 32 || ch > 126)
            {
                throw new ArgumentException("Code 128 barcode values must contain printable ASCII characters only.", nameof(value));
            }

            var code = ch - 32;
            codes.Add(code);
            checksum += code * (i + 1);
        }

        codes.Add(checksum % 103);
        codes.Add(stopCode);

        var moduleCount = quietZoneModules * 2;
        foreach (var code in codes)
        {
            foreach (var width in Code128Patterns[code])
            {
                moduleCount += width - '0';
            }
        }

        var modules = new bool[moduleCount];
        var position = quietZoneModules;
        foreach (var code in codes)
        {
            var drawBar = true;
            foreach (var widthChar in Code128Patterns[code])
            {
                var width = widthChar - '0';
                if (drawBar)
                {
                    for (var i = 0; i < width; i++)
                    {
                        modules[position + i] = true;
                    }
                }

                position += width;
                drawBar = !drawBar;
            }
        }

        return modules;
    }

    private static int CheckedRoundUp(double value, string paramName)
    {
        if (value > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value is too large.");
        }

        return checked((int)Math.Ceiling(value));
    }

}
