namespace FluentHtmlReport.Measurement;

/// <summary>
/// Per-character advance widths (in thousandths of an em) from the Helvetica
/// AFM core font metrics - the same public-domain numbers PDF/HTML renderers
/// have used for decades to approximate generic sans-serif text without
/// shaping a real font. Used by <see cref="ApproximateTextMeasurer"/> as a
/// stand-in for whatever sans-serif font the consumer actually renders with.
/// </summary>
internal static class HelveticaCharacterWidths
{
    private const int DefaultWidth = 556;

    private static readonly Dictionary<char, int> Widths = new()
    {
        [' '] = 278, ['!'] = 278, ['"'] = 355, ['#'] = 556, ['$'] = 556, ['%'] = 889, ['&'] = 667, ['\''] = 191,
        ['('] = 333, [')'] = 333, ['*'] = 389, ['+'] = 584, [','] = 278, ['-'] = 333, ['.'] = 278, ['/'] = 278,
        ['0'] = 556, ['1'] = 556, ['2'] = 556, ['3'] = 556, ['4'] = 556, ['5'] = 556, ['6'] = 556, ['7'] = 556,
        ['8'] = 556, ['9'] = 556, [':'] = 278, [';'] = 278, ['<'] = 584, ['='] = 584, ['>'] = 584, ['?'] = 556,
        ['@'] = 1015,
        ['A'] = 667, ['B'] = 667, ['C'] = 722, ['D'] = 722, ['E'] = 667, ['F'] = 611, ['G'] = 778, ['H'] = 722,
        ['I'] = 278, ['J'] = 500, ['K'] = 667, ['L'] = 556, ['M'] = 833, ['N'] = 722, ['O'] = 778, ['P'] = 667,
        ['Q'] = 778, ['R'] = 722, ['S'] = 667, ['T'] = 611, ['U'] = 722, ['V'] = 667, ['W'] = 944, ['X'] = 667,
        ['Y'] = 667, ['Z'] = 611,
        ['['] = 278, ['\\'] = 278, [']'] = 278, ['^'] = 469, ['_'] = 556, ['`'] = 333,
        ['a'] = 556, ['b'] = 556, ['c'] = 500, ['d'] = 556, ['e'] = 556, ['f'] = 278, ['g'] = 556, ['h'] = 556,
        ['i'] = 222, ['j'] = 222, ['k'] = 500, ['l'] = 222, ['m'] = 833, ['n'] = 556, ['o'] = 556, ['p'] = 556,
        ['q'] = 556, ['r'] = 333, ['s'] = 500, ['t'] = 278, ['u'] = 556, ['v'] = 500, ['w'] = 722, ['x'] = 500,
        ['y'] = 500, ['z'] = 500,
        ['{'] = 334, ['|'] = 260, ['}'] = 334, ['~'] = 584,
    };

    /// <summary>The advance width of <paramref name="c"/>, in thousandths of an em (falls back to an average width for unknown characters).</summary>
    public static int AdvanceWidthPerMille(char c) => Widths.TryGetValue(c, out var width) ? width : DefaultWidth;
}
