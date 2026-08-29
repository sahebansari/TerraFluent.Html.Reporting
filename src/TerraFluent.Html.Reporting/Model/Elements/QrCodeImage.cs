using System.Text;
using TerraFluent.Html.Reporting.Compatibility;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>
/// Generates a QR code (ISO/IEC 18004) as a PNG, natively - no external
/// barcode library or web service. Scoped deliberately narrow for a first
/// version: byte-mode encoding only (UTF-8), a single fixed error-correction
/// level ("M", ~15% recoverable), and an auto-selected minimum version
/// (1-40). Numeric/alphanumeric mode compaction and a consumer-selectable
/// error-correction level are not supported - see
/// <see href="https://www.iso.org/standard/83389.html">ISO/IEC 18004</see>
/// for the full specification this is a subset of.
/// </summary>
internal static class QrCodeImage
{
    private const int EccLevelM = 0; // ISO/IEC 18004 2-bit format-info encoding for ECC level M.
    private const int ByteModeIndicator = 0b0100;

    // BCH generator polynomials/mask for format (G15) and version (G18)
    // information, per ISO/IEC 18004 Annex C/D.
    private const int G15 = 1335;
    private const int G18 = 7973;
    private const int G15Mask = 21522;

    private static readonly int[] ExpTable = BuildExpTable();
    private static readonly int[] LogTable = BuildLogTable(ExpTable);

    // Alignment pattern center positions, indexed by version - 1. Empty for
    // version 1 (no alignment pattern). ISO/IEC 18004 Table E.1.
    private static readonly int[][] AlignmentPatternPositions =
    {
        Array.Empty<int>(),
        new[] { 6, 18 },
        new[] { 6, 22 },
        new[] { 6, 26 },
        new[] { 6, 30 },
        new[] { 6, 34 },
        new[] { 6, 22, 38 },
        new[] { 6, 24, 42 },
        new[] { 6, 26, 46 },
        new[] { 6, 28, 50 },
        new[] { 6, 30, 54 },
        new[] { 6, 32, 58 },
        new[] { 6, 34, 62 },
        new[] { 6, 26, 46, 66 },
        new[] { 6, 26, 48, 70 },
        new[] { 6, 26, 50, 74 },
        new[] { 6, 30, 54, 78 },
        new[] { 6, 30, 56, 82 },
        new[] { 6, 30, 58, 86 },
        new[] { 6, 34, 62, 90 },
        new[] { 6, 28, 50, 72, 94 },
        new[] { 6, 26, 50, 74, 98 },
        new[] { 6, 30, 54, 78, 102 },
        new[] { 6, 28, 54, 80, 106 },
        new[] { 6, 32, 58, 84, 110 },
        new[] { 6, 30, 58, 86, 114 },
        new[] { 6, 34, 62, 90, 118 },
        new[] { 6, 26, 50, 74, 98, 122 },
        new[] { 6, 30, 54, 78, 102, 126 },
        new[] { 6, 26, 52, 78, 104, 130 },
        new[] { 6, 30, 56, 82, 108, 134 },
        new[] { 6, 34, 60, 86, 112, 138 },
        new[] { 6, 30, 58, 86, 114, 142 },
        new[] { 6, 34, 62, 90, 118, 146 },
        new[] { 6, 30, 54, 78, 102, 126, 150 },
        new[] { 6, 24, 50, 76, 102, 128, 154 },
        new[] { 6, 28, 54, 80, 106, 132, 158 },
        new[] { 6, 32, 58, 84, 110, 136, 162 },
        new[] { 6, 26, 54, 82, 110, 138, 166 },
        new[] { 6, 30, 58, 86, 114, 142, 170 },
    };

    /// <summary>
    /// Error-correction block layout for level M, indexed by version - 1.
    /// Each row is a flat list of (blockCount, totalCodewords, dataCodewords)
    /// triples - e.g. <c>{2, 60, 38, 2, 61, 39}</c> means 2 blocks of
    /// (60 total, 38 data) followed by 2 blocks of (61 total, 39 data).
    /// ISO/IEC 18004 Table 9 (level M column only).
    /// </summary>
    private static readonly int[][] RsBlockTableM =
    {
        new[] { 1, 26, 16 },
        new[] { 1, 44, 28 },
        new[] { 1, 70, 44 },
        new[] { 2, 50, 32 },
        new[] { 2, 67, 43 },
        new[] { 4, 43, 27 },
        new[] { 4, 49, 31 },
        new[] { 2, 60, 38, 2, 61, 39 },
        new[] { 3, 58, 36, 2, 59, 37 },
        new[] { 4, 69, 43, 1, 70, 44 },
        new[] { 1, 80, 50, 4, 81, 51 },
        new[] { 6, 58, 36, 2, 59, 37 },
        new[] { 8, 59, 37, 1, 60, 38 },
        new[] { 4, 64, 40, 5, 65, 41 },
        new[] { 5, 65, 41, 5, 66, 42 },
        new[] { 7, 73, 45, 3, 74, 46 },
        new[] { 10, 74, 46, 1, 75, 47 },
        new[] { 9, 69, 43, 4, 70, 44 },
        new[] { 3, 70, 44, 11, 71, 45 },
        new[] { 3, 67, 41, 13, 68, 42 },
        new[] { 17, 68, 42 },
        new[] { 17, 74, 46 },
        new[] { 4, 75, 47, 14, 76, 48 },
        new[] { 6, 73, 45, 14, 74, 46 },
        new[] { 8, 75, 47, 13, 76, 48 },
        new[] { 19, 74, 46, 4, 75, 47 },
        new[] { 22, 73, 45, 3, 74, 46 },
        new[] { 3, 73, 45, 23, 74, 46 },
        new[] { 21, 73, 45, 7, 74, 46 },
        new[] { 19, 75, 47, 10, 76, 48 },
        new[] { 2, 74, 46, 29, 75, 47 },
        new[] { 10, 74, 46, 23, 75, 47 },
        new[] { 14, 74, 46, 21, 75, 47 },
        new[] { 14, 74, 46, 23, 75, 47 },
        new[] { 12, 75, 47, 26, 76, 48 },
        new[] { 6, 75, 47, 34, 76, 48 },
        new[] { 29, 74, 46, 14, 75, 47 },
        new[] { 13, 74, 46, 32, 75, 47 },
        new[] { 40, 75, 47, 7, 76, 48 },
        new[] { 18, 75, 47, 31, 76, 48 },
    };

    private readonly record struct EccBlock(int TotalCount, int DataCount);

    public static ReportImage CreateQrCode(string value, double moduleWidthPx, int quietZoneModules)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("QR code value must not be empty.", nameof(value));
        }

        Guard.Positive(moduleWidthPx, nameof(moduleWidthPx));
        if (quietZoneModules < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quietZoneModules), quietZoneModules, "Quiet zone must be zero or greater.");
        }

        var valueBytes = Encoding.UTF8.GetBytes(value);
        var version = SelectVersion(valueBytes.Length);
        var blocks = GetBlocks(version);
        var dataCapacityBytes = blocks.Sum(b => b.DataCount);
        var dataCodewords = BuildDataCodewords(valueBytes, version, dataCapacityBytes);
        var codewords = InterleaveDataAndEcc(dataCodewords, blocks);

        var size = version * 4 + 17;
        var blank = BuildBlankMatrix(version, size);

        var bestMask = 0;
        var bestScore = int.MaxValue;
        for (var mask = 0; mask < 8; mask++)
        {
            var trial = CloneMatrix(blank);
            SetupTypeInfo(trial, size, mask, test: true);
            if (version >= 7) SetupTypeNumber(trial, size, version, test: true);
            MapData(trial, size, codewords, mask);

            var score = LostPoint(ResolveMatrix(trial, size), size);
            if (score < bestScore)
            {
                bestScore = score;
                bestMask = mask;
            }
        }

        var final = CloneMatrix(blank);
        SetupTypeInfo(final, size, bestMask, test: false);
        if (version >= 7) SetupTypeNumber(final, size, version, test: false);
        MapData(final, size, codewords, bestMask);
        var matrix = ResolveMatrix(final, size);

        return RenderToImage(matrix, size, moduleWidthPx, quietZoneModules);
    }

    // --- Version selection and data encoding -------------------------------

    private static int SelectVersion(int byteLength)
    {
        for (var version = 1; version <= 40; version++)
        {
            var countBits = version < 10 ? 8 : 16;
            var neededBits = 4 + countBits + byteLength * 8;
            var capacityBits = GetBlocks(version).Sum(b => b.DataCount) * 8;
            if (neededBits <= capacityBits) return version;
        }

        throw new ArgumentException(
            "QR code value is too long to encode, even at the largest supported version (40) with error-correction level M.",
            nameof(byteLength));
    }

    private static List<EccBlock> GetBlocks(int version)
    {
        var raw = RsBlockTableM[version - 1];
        var blocks = new List<EccBlock>();
        for (var i = 0; i < raw.Length; i += 3)
        {
            var count = raw[i];
            var total = raw[i + 1];
            var data = raw[i + 2];
            for (var k = 0; k < count; k++)
            {
                blocks.Add(new EccBlock(total, data));
            }
        }

        return blocks;
    }

    private static byte[] BuildDataCodewords(byte[] valueBytes, int version, int dataCapacityBytes)
    {
        var buffer = new BitBuffer();
        buffer.Put(ByteModeIndicator, 4);
        var countBits = version < 10 ? 8 : 16;
        buffer.Put(valueBytes.Length, countBits);
        foreach (var b in valueBytes) buffer.Put(b, 8);

        var bitLimit = dataCapacityBytes * 8;

        // Terminator: up to four 0 bits.
        var terminatorBits = Math.Min(bitLimit - buffer.LengthBits, 4);
        for (var i = 0; i < terminatorBits; i++) buffer.PutBit(false);

        // Pad to a byte boundary.
        var delimit = buffer.LengthBits % 8;
        if (delimit != 0)
        {
            for (var i = 0; i < 8 - delimit; i++) buffer.PutBit(false);
        }

        // Alternating pad bytes (0xEC, 0x11) until the capacity is filled.
        var bytesToFill = (bitLimit - buffer.LengthBits) / 8;
        for (var i = 0; i < bytesToFill; i++)
        {
            buffer.Put(i % 2 == 0 ? 0xEC : 0x11, 8);
        }

        return buffer.ToByteArray();
    }

    private static byte[] InterleaveDataAndEcc(byte[] allDataCodewords, List<EccBlock> blocks)
    {
        var dcBlocks = new List<int[]>();
        var ecBlocks = new List<int[]>();
        var offset = 0;
        var maxDc = 0;
        var maxEc = 0;

        foreach (var block in blocks)
        {
            var dc = new int[block.DataCount];
            for (var i = 0; i < block.DataCount; i++) dc[i] = allDataCodewords[offset + i];
            offset += block.DataCount;

            var ec = ComputeEcc(dc, block.TotalCount - block.DataCount);

            dcBlocks.Add(dc);
            ecBlocks.Add(ec);
            maxDc = Math.Max(maxDc, dc.Length);
            maxEc = Math.Max(maxEc, ec.Length);
        }

        var result = new List<byte>(allDataCodewords.Length + maxEc * ecBlocks.Count);
        for (var i = 0; i < maxDc; i++)
        {
            foreach (var dc in dcBlocks.Where(dc => i < dc.Length))
            {
                result.Add((byte)dc[i]);
            }
        }

        for (var i = 0; i < maxEc; i++)
        {
            foreach (var ec in ecBlocks.Where(ec => i < ec.Length))
            {
                result.Add((byte)ec[i]);
            }
        }

        return result.ToArray();
    }

    private sealed class BitBuffer
    {
        private readonly List<byte> _bytes = new();

        public int LengthBits { get; private set; }

        public void Put(int value, int length)
        {
            for (var i = 0; i < length; i++)
            {
                PutBit(((value >> (length - i - 1)) & 1) == 1);
            }
        }

        public void PutBit(bool bit)
        {
            var byteIndex = LengthBits / 8;
            if (_bytes.Count <= byteIndex) _bytes.Add(0);
            if (bit) _bytes[byteIndex] |= (byte)(0x80 >> (LengthBits % 8));
            LengthBits++;
        }

        public byte[] ToByteArray() => _bytes.ToArray();
    }

    // --- GF(256) arithmetic and Reed-Solomon error correction --------------
    // ISO/IEC 18004 Annex A: GF(256) with primitive polynomial x^8+x^4+x^3+x^2+1.

    private static int[] BuildExpTable()
    {
        var table = new int[256];
        for (var i = 0; i < 8; i++) table[i] = 1 << i;
        for (var i = 8; i < 256; i++) table[i] = table[i - 4] ^ table[i - 5] ^ table[i - 6] ^ table[i - 8];
        return table;
    }

    private static int[] BuildLogTable(int[] expTable)
    {
        var table = new int[256];
        for (var i = 0; i < 255; i++) table[expTable[i]] = i;
        return table;
    }

    private static int GExp(int n) => ExpTable[((n % 255) + 255) % 255];

    private static int[] BuildGeneratorPolynomial(int eccCount)
    {
        var poly = new[] { 1 };
        for (var i = 0; i < eccCount; i++)
        {
            poly = MultiplyPolynomials(poly, new[] { 1, GExp(i) });
        }

        return poly;
    }

    private static int[] MultiplyPolynomials(int[] a, int[] b)
    {
        var result = new int[a.Length + b.Length - 1];
        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] == 0) continue;
            var logA = LogTable[a[i]];
            for (var j = 0; j < b.Length; j++)
            {
                if (b[j] == 0) continue;
                result[i + j] ^= GExp(logA + LogTable[b[j]]);
            }
        }

        return result;
    }

    /// <summary>
    /// Computes Reed-Solomon error-correction codewords for one block via
    /// polynomial long division in GF(256): the data codewords, shifted left
    /// by <paramref name="eccCount"/> zero coefficients, divided by the
    /// degree-<paramref name="eccCount"/> generator polynomial - the
    /// remainder is the block's EC codewords.
    /// </summary>
    private static int[] ComputeEcc(int[] dataCodewords, int eccCount)
    {
        var generator = BuildGeneratorPolynomial(eccCount);
        var message = new int[dataCodewords.Length + eccCount];
        Array.Copy(dataCodewords, message, dataCodewords.Length);

        for (var i = 0; i < dataCodewords.Length; i++)
        {
            var coefficient = message[i];
            if (coefficient == 0) continue;
            var logCoefficient = LogTable[coefficient];
            for (var j = 0; j < generator.Length; j++)
            {
                if (generator[j] == 0) continue;
                message[i + j] ^= GExp(LogTable[generator[j]] + logCoefficient);
            }
        }

        var ecc = new int[eccCount];
        Array.Copy(message, dataCodewords.Length, ecc, 0, eccCount);
        return ecc;
    }

    // --- Matrix construction (finder/alignment/timing/format/version) -----

    private static bool?[,] BuildBlankMatrix(int version, int size)
    {
        var modules = new bool?[size, size];
        SetupFinderPattern(modules, 0, 0, size);
        SetupFinderPattern(modules, size - 7, 0, size);
        SetupFinderPattern(modules, 0, size - 7, size);
        SetupAlignmentPatterns(modules, version, size);
        SetupTimingPattern(modules, size);
        return modules;
    }

    private static void SetupFinderPattern(bool?[,] modules, int row, int col, int size)
    {
        for (var r = -1; r <= 7; r++)
        {
            if (row + r <= -1 || size <= row + r) continue;
            for (var c = -1; c <= 7; c++)
            {
                if (col + c <= -1 || size <= col + c) continue;

                modules[row + r, col + c] = IsFinderPatternDark(r, c);
            }
        }
    }

    /// <summary>A finder pattern's 7x7 ring-and-center-square: its outer border, plus the solid 3x3 center square.</summary>
    private static bool IsFinderPatternDark(int r, int c) =>
        (r >= 0 && r <= 6 && (c == 0 || c == 6))
        || (c >= 0 && c <= 6 && (r == 0 || r == 6))
        || (r >= 2 && r <= 4 && c >= 2 && c <= 4);

    private static void SetupAlignmentPatterns(bool?[,] modules, int version, int size)
    {
        var positions = AlignmentPatternPositions[version - 1];
        foreach (var row in positions)
        {
            foreach (var col in positions.Where(col => modules[row, col] is null))
            {
                for (var r = -2; r <= 2; r++)
                {
                    for (var c = -2; c <= 2; c++)
                    {
                        var dark = r == -2 || r == 2 || c == -2 || c == 2 || (r == 0 && c == 0);
                        modules[row + r, col + c] = dark;
                    }
                }
            }
        }
    }

    private static void SetupTimingPattern(bool?[,] modules, int size)
    {
        for (var r = 8; r < size - 8; r++)
        {
            if (modules[r, 6] is null) modules[r, 6] = r % 2 == 0;
        }

        for (var c = 8; c < size - 8; c++)
        {
            if (modules[6, c] is null) modules[6, c] = c % 2 == 0;
        }
    }

    private static void SetupTypeInfo(bool?[,] modules, int size, int maskPattern, bool test)
    {
        var bits = BchTypeInfo((EccLevelM << 3) | maskPattern);

        for (var i = 0; i < 15; i++)
        {
            var mod = !test && ((bits >> i) & 1) == 1;
            if (i < 6) modules[i, 8] = mod;
            else if (i < 8) modules[i + 1, 8] = mod;
            else modules[size - 15 + i, 8] = mod;
        }

        for (var i = 0; i < 15; i++)
        {
            var mod = !test && ((bits >> i) & 1) == 1;
            if (i < 8) modules[8, size - i - 1] = mod;
            else if (i < 9) modules[8, 15 - i - 1 + 1] = mod;
            else modules[8, 15 - i - 1] = mod;
        }

        modules[size - 8, 8] = !test; // fixed dark module
    }

    private static void SetupTypeNumber(bool?[,] modules, int size, int version, bool test)
    {
        var bits = BchTypeNumber(version);

        for (var i = 0; i < 18; i++)
        {
            var mod = !test && ((bits >> i) & 1) == 1;
            modules[i / 3, i % 3 + size - 8 - 3] = mod;
        }

        for (var i = 0; i < 18; i++)
        {
            var mod = !test && ((bits >> i) & 1) == 1;
            modules[i % 3 + size - 8 - 3, i / 3] = mod;
        }
    }

    private static int BchDigit(int data)
    {
        var digit = 0;
        while (data != 0)
        {
            digit++;
            data >>= 1;
        }

        return digit;
    }

    private static int BchTypeInfo(int data)
    {
        var d = data << 10;
        while (BchDigit(d) - BchDigit(G15) >= 0)
        {
            d ^= G15 << (BchDigit(d) - BchDigit(G15));
        }

        return ((data << 10) | d) ^ G15Mask;
    }

    private static int BchTypeNumber(int version)
    {
        var d = version << 12;
        while (BchDigit(d) - BchDigit(G18) >= 0)
        {
            d ^= G18 << (BchDigit(d) - BchDigit(G18));
        }

        return (version << 12) | d;
    }

    /// <summary>
    /// Places <paramref name="codewords"/> into the matrix's remaining (data)
    /// modules in the standard two-columns-at-a-time, bottom-to-top-then-
    /// top-to-bottom zigzag order, applying <paramref name="maskPattern"/>
    /// as each bit is placed. <c>colBase</c> (the outer loop's column pair
    /// start) is intentionally never mutated - only the derived <c>col</c>/
    /// <c>col2</c> are, mirroring the reference algorithm's independent
    /// column-pair sequence (the column-6 timing-pattern skip must not shift
    /// where the *next* pair starts).
    /// </summary>
    private static void MapData(bool?[,] modules, int size, byte[] codewords, int maskPattern)
    {
        var inc = -1;
        var row = size - 1;
        var bitIndex = 7;
        var byteIndex = 0;
        var maskFunc = GetMaskFunc(maskPattern);

        for (var colBase = size - 1; colBase > 0; colBase -= 2)
        {
            var col = colBase <= 6 ? colBase - 1 : colBase;
            var col2 = col - 1;

            while (true)
            {
                for (var pass = 0; pass < 2; pass++)
                {
                    var c = pass == 0 ? col : col2;
                    if (modules[row, c] is null)
                    {
                        var dark = byteIndex < codewords.Length && ((codewords[byteIndex] >> bitIndex) & 1) == 1;
                        if (maskFunc(row, c)) dark = !dark;
                        modules[row, c] = dark;

                        bitIndex--;
                        if (bitIndex == -1)
                        {
                            byteIndex++;
                            bitIndex = 7;
                        }
                    }
                }

                row += inc;
                if (row < 0 || size <= row)
                {
                    row -= inc;
                    inc = -inc;
                    break;
                }
            }
        }
    }

    private static Func<int, int, bool> GetMaskFunc(int pattern) => pattern switch
    {
        0 => (i, j) => (i + j) % 2 == 0,
        1 => (i, j) => i % 2 == 0,
        2 => (i, j) => j % 3 == 0,
        3 => (i, j) => (i + j) % 3 == 0,
        4 => (i, j) => (i / 2 + j / 3) % 2 == 0,
        5 => (i, j) => i * j % 2 + i * j % 3 == 0,
        6 => (i, j) => (i * j % 2 + i * j % 3) % 2 == 0,
        7 => (i, j) => (i * j % 3 + (i + j) % 2) % 2 == 0,
        _ => throw new ArgumentOutOfRangeException(nameof(pattern)),
    };

    private static bool?[,] CloneMatrix(bool?[,] source)
    {
        var clone = new bool?[source.GetLength(0), source.GetLength(1)];
        Array.Copy(source, clone, source.Length);
        return clone;
    }

    private static bool[,] ResolveMatrix(bool?[,] modules, int size)
    {
        var result = new bool[size, size];
        for (var r = 0; r < size; r++)
        {
            for (var c = 0; c < size; c++)
            {
                result[r, c] = modules[r, c] ?? false;
            }
        }

        return result;
    }

    // --- Mask-pattern penalty scoring (ISO/IEC 18004 Annex J) --------------

    private static int LostPoint(bool[,] modules, int size) =>
        LostPointLevel1(modules, size) + LostPointLevel2(modules, size) + LostPointLevel3(modules, size) + LostPointLevel4(modules, size);

    private static int LostPointLevel1(bool[,] modules, int size)
    {
        var lostPoint = 0;
        var container = new int[size + 1];

        for (var row = 0; row < size; row++)
        {
            var previousColor = modules[row, 0];
            var length = 0;
            for (var col = 0; col < size; col++)
            {
                if (modules[row, col] == previousColor)
                {
                    length++;
                }
                else
                {
                    if (length >= 5) container[length]++;
                    length = 1;
                    previousColor = modules[row, col];
                }
            }

            if (length >= 5) container[length]++;
        }

        for (var col = 0; col < size; col++)
        {
            var previousColor = modules[0, col];
            var length = 0;
            for (var row = 0; row < size; row++)
            {
                if (modules[row, col] == previousColor)
                {
                    length++;
                }
                else
                {
                    if (length >= 5) container[length]++;
                    length = 1;
                    previousColor = modules[row, col];
                }
            }

            if (length >= 5) container[length]++;
        }

        for (var length = 5; length <= size; length++)
        {
            lostPoint += container[length] * (length - 2);
        }

        return lostPoint;
    }

    private static int LostPointLevel2(bool[,] modules, int size)
    {
        var lostPoint = 0;
        for (var row = 0; row < size - 1; row++)
        {
            for (var col = 0; col < size - 1; col++)
            {
                var topRight = modules[row, col + 1];
                if (topRight == modules[row + 1, col + 1] && topRight == modules[row, col] && topRight == modules[row + 1, col])
                {
                    lostPoint += 3;
                }
            }
        }

        return lostPoint;
    }

    private static int LostPointLevel3(bool[,] modules, int size)
    {
        var lostPoint = 0;

        for (var row = 0; row < size; row++)
        {
            for (var col = 0; col <= size - 11; col++)
            {
                if (HasFinderLikeRatio(offset => modules[row, col + offset]))
                {
                    lostPoint += 40;
                }
            }
        }

        for (var col = 0; col < size; col++)
        {
            for (var row = 0; row <= size - 11; row++)
            {
                if (HasFinderLikeRatio(offset => modules[row + offset, col]))
                {
                    lostPoint += 40;
                }
            }
        }

        return lostPoint;
    }

    /// <summary>
    /// Detects the 1:1:3:1:1 dark:light:dark:light:dark ratio pattern (resembling
    /// a finder pattern, which real scanners can latch onto and misread) starting
    /// at offset 0 of <paramref name="at"/> - ISO/IEC 18004's third masking
    /// penalty rule, shared here between its row-wise and column-wise scans.
    /// </summary>
    private static bool HasFinderLikeRatio(Func<int, bool> at) =>
        !at(1) && at(4) && !at(5) && at(6) && !at(9)
        && ((at(0) && at(2) && at(3) && !at(7) && !at(8) && !at(10))
            || (!at(0) && !at(2) && !at(3) && at(7) && at(8) && at(10)));

    private static int LostPointLevel4(bool[,] modules, int size)
    {
        var darkCount = 0;
        for (var r = 0; r < size; r++)
        {
            for (var c = 0; c < size; c++)
            {
                if (modules[r, c]) darkCount++;
            }
        }

        var percent = darkCount / ((double)size * size);
        var rating = (int)(Math.Abs(percent * 100 - 50) / 5);
        return rating * 10;
    }

    // --- Rendering ----------------------------------------------------------

    private static ReportImage RenderToImage(bool[,] matrix, int size, double moduleWidthPx, int quietZoneModules)
    {
        var moduleWidth = CheckedRoundUp(moduleWidthPx);
        var totalModules = size + quietZoneModules * 2;
        var widthPx = checked(totalModules * moduleWidth);

        var pixels = new byte[widthPx * (1 + widthPx * 3)];
        var pos = 0;
        for (var y = 0; y < widthPx; y++)
        {
            pixels[pos++] = 0; // PNG filter type: None
            var moduleRow = y / moduleWidth - quietZoneModules;
            for (var x = 0; x < widthPx; x++)
            {
                var moduleCol = x / moduleWidth - quietZoneModules;
                var isDark = moduleRow >= 0 && moduleRow < size && moduleCol >= 0 && moduleCol < size && matrix[moduleRow, moduleCol];
                var color = isDark ? (byte)0 : (byte)255;
                pixels[pos++] = color;
                pixels[pos++] = color;
                pixels[pos++] = color;
            }
        }

        var png = PngWriter.CreateRgb(widthPx, widthPx, pixels);
        return ReportImage.FromBytes(png, "image/png", widthPx, widthPx);
    }

    private static int CheckedRoundUp(double value)
    {
        if (value > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Value is too large.");
        }

        return checked((int)Math.Ceiling(value));
    }
}
