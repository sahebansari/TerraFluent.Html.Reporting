namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>
/// Sniffs the intrinsic pixel dimensions of common image formats by reading
/// just their header bytes - no decoding, no <c>System.Drawing</c>/native
/// dependency, which keeps <see cref="ReportImage"/> usable on netstandard2.0.
/// Supports PNG, GIF, BMP, and baseline/progressive JPEG. Returns (0, 0) if the
/// format is unrecognized, signaling the caller to require explicit dimensions.
/// </summary>
internal static class ImageDimensionReader
{
    public static (int Width, int Height) ReadDimensions(byte[] bytes)
    {
        if (TryReadPng(bytes, out var png)) return png;
        if (TryReadGif(bytes, out var gif)) return gif;
        if (TryReadBmp(bytes, out var bmp)) return bmp;
        if (TryReadJpeg(bytes, out var jpeg)) return jpeg;
        return (0, 0);
    }

    private static bool TryReadPng(byte[] b, out (int Width, int Height) size)
    {
        size = default;
        if (b.Length < 24) return false;
        if (b[0] != 0x89 || b[1] != 0x50 || b[2] != 0x4E || b[3] != 0x47) return false;
        var width = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
        var height = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
        size = (width, height);
        return true;
    }

    private static bool TryReadGif(byte[] b, out (int Width, int Height) size)
    {
        size = default;
        if (b.Length < 10) return false;
        if (b[0] != 'G' || b[1] != 'I' || b[2] != 'F') return false;
        var width = b[6] | (b[7] << 8);
        var height = b[8] | (b[9] << 8);
        size = (width, height);
        return true;
    }

    private static bool TryReadBmp(byte[] b, out (int Width, int Height) size)
    {
        size = default;
        if (b.Length < 26) return false;
        if (b[0] != 'B' || b[1] != 'M') return false;
        var width = b[18] | (b[19] << 8) | (b[20] << 16) | (b[21] << 24);
        var height = Math.Abs(b[22] | (b[23] << 8) | (b[24] << 16) | (b[25] << 24));
        size = (width, height);
        return true;
    }

    private static bool TryReadJpeg(byte[] b, out (int Width, int Height) size)
    {
        size = default;
        if (b.Length < 4 || b[0] != 0xFF || b[1] != 0xD8) return false;

        var offset = 2;
        while (offset + 9 < b.Length)
        {
            if (b[offset] != 0xFF) { offset++; continue; }

            var marker = b[offset + 1];
            // SOF0-SOF15 (excluding DHT 0xC4, JPG 0xC8, DAC 0xCC) carry width/height.
            var isStartOfFrame = marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC;
            var segmentLength = (b[offset + 2] << 8) | b[offset + 3];

            if (isStartOfFrame)
            {
                var height = (b[offset + 5] << 8) | b[offset + 6];
                var width = (b[offset + 7] << 8) | b[offset + 8];
                size = (width, height);
                return true;
            }

            if (marker == 0xD8 || marker == 0xD9) break; // SOI/EOI carry no length
            offset += 2 + segmentLength;
        }

        return false;
    }
}
