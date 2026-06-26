using System.Text;

namespace TerraFluent.Html.Reporting.Sample;

/// <summary>
/// Encodes a tiny, valid, solid-color PNG with zero dependencies (no
/// System.Drawing, no image library) - just enough PNG/zlib/deflate framing
/// to produce a real image a browser can render, so the image-related
/// samples show actual colored rectangles instead of a 1x1 placeholder.
/// </summary>
internal static class MinimalPngWriter
{
    public static byte[] CreateSolidColor(int width, int height, byte r, byte g, byte b)
    {
        var raw = new byte[height * (1 + width * 3)];
        var pos = 0;
        for (var y = 0; y < height; y++)
        {
            raw[pos++] = 0; // filter type: None
            for (var x = 0; x < width; x++)
            {
                raw[pos++] = r;
                raw[pos++] = g;
                raw[pos++] = b;
            }
        }

        using var png = new MemoryStream();
        png.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, 0, 8);
        WriteChunk(png, "IHDR", BuildIhdr(width, height));
        WriteChunk(png, "IDAT", ZlibStoreUncompressed(raw));
        WriteChunk(png, "IEND", Array.Empty<byte>());
        return png.ToArray();
    }

    private static byte[] BuildIhdr(int width, int height)
    {
        var data = new byte[13];
        WriteBigEndian(data, 0, width);
        WriteBigEndian(data, 4, height);
        data[8] = 8; // bit depth
        data[9] = 2; // color type: truecolor RGB, no alpha
        data[10] = 0; // compression method
        data[11] = 0; // filter method
        data[12] = 0; // interlace method
        return data;
    }

    /// <summary>
    /// Wraps <paramref name="raw"/> in a zlib stream containing one or more
    /// uncompressed ("stored") deflate blocks, each up to 65535 bytes (the
    /// format's per-block limit) - valid per RFC 1950/1951, and far simpler
    /// than implementing real deflate compression for a few kilobytes of demo
    /// image data.
    /// </summary>
    private static byte[] ZlibStoreUncompressed(byte[] raw)
    {
        const int maxBlockLength = ushort.MaxValue;

        using var ms = new MemoryStream();
        ms.WriteByte(0x78); // zlib CMF: deflate, 32K window
        ms.WriteByte(0x01); // zlib FLG: level 0, valid check bits for CMF=0x78

        var offset = 0;
        while (true)
        {
            var blockLength = Math.Min(maxBlockLength, raw.Length - offset);
            var isFinalBlock = offset + blockLength >= raw.Length;

            ms.WriteByte(isFinalBlock ? (byte)0x01 : (byte)0x00); // BFINAL, BTYPE=00 (stored), padded to a byte
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

    private static void WriteChunk(MemoryStream stream, string type, byte[] data)
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
