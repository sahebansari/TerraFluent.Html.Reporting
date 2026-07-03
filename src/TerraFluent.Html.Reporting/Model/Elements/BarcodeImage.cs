using System.Text;
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

    private static class PngWriter
    {
        public static byte[] CreateRgb(int width, int height, byte[] filteredRgbRows)
        {
            using var png = new MemoryStream();
            png.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, 0, 8);
            WriteChunk(png, "IHDR", BuildIhdr(width, height));
            WriteChunk(png, "IDAT", ZlibStoreUncompressed(filteredRgbRows));
            WriteChunk(png, "IEND", Array.Empty<byte>());
            return png.ToArray();
        }

        private static byte[] BuildIhdr(int width, int height)
        {
            var data = new byte[13];
            WriteBigEndian(data, 0, width);
            WriteBigEndian(data, 4, height);
            data[8] = 8; // bit depth
            data[9] = 2; // truecolor RGB
            data[10] = 0; // compression method
            data[11] = 0; // filter method
            data[12] = 0; // interlace method
            return data;
        }

        private static byte[] ZlibStoreUncompressed(byte[] raw)
        {
            const int maxBlockLength = ushort.MaxValue;

            using var ms = new MemoryStream();
            ms.WriteByte(0x78);
            ms.WriteByte(0x01);

            var offset = 0;
            while (true)
            {
                var blockLength = Math.Min(maxBlockLength, raw.Length - offset);
                var isFinalBlock = offset + blockLength >= raw.Length;

                ms.WriteByte(isFinalBlock ? (byte)0x01 : (byte)0x00);
                var len = (ushort)blockLength;
                var nlen = (ushort)~len;
                ms.WriteByte((byte)(len & 0xFF));
                ms.WriteByte((byte)(len >> 8));
                ms.WriteByte((byte)(nlen & 0xFF));
                ms.WriteByte((byte)(nlen >> 8));
                ms.Write(raw, offset, blockLength);

                offset += blockLength;
                if (isFinalBlock) break;
            }

            var adler = Adler32(raw);
            ms.WriteByte((byte)(adler >> 24));
            ms.WriteByte((byte)(adler >> 16));
            ms.WriteByte((byte)(adler >> 8));
            ms.WriteByte((byte)adler);

            return ms.ToArray();
        }

        private static uint Adler32(byte[] data)
        {
            uint a = 1, b = 0;
            const uint mod = 65521;
            foreach (var by in data)
            {
                a = (a + by) % mod;
                b = (b + a) % mod;
            }

            return (b << 16) | a;
        }

        private static void WriteChunk(Stream stream, string type, byte[] data)
        {
            var lengthBytes = new byte[4];
            WriteBigEndian(lengthBytes, 0, data.Length);
            stream.Write(lengthBytes, 0, 4);

            var typeBytes = Encoding.ASCII.GetBytes(type);
            stream.Write(typeBytes, 0, 4);
            stream.Write(data, 0, data.Length);

            var crc = Crc32(typeBytes, data);
            var crcBytes = new byte[4];
            WriteBigEndian(crcBytes, 0, unchecked((int)crc));
            stream.Write(crcBytes, 0, 4);
        }

        private static void WriteBigEndian(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        private static uint[]? _crcTable;

        private static uint Crc32(byte[] typeBytes, byte[] data)
        {
            _crcTable ??= BuildCrcTable();

            var crc = 0xFFFFFFFFu;
            foreach (var b in typeBytes) crc = _crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            foreach (var b in data) crc = _crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFFu;
        }

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                var c = i;
                for (var k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
                }

                table[i] = c;
            }

            return table;
        }
    }
}
