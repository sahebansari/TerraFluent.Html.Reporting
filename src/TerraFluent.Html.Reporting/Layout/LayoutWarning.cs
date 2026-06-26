namespace TerraFluent.Html.Reporting.Layout;

/// <summary>
/// A non-fatal problem noticed during pagination - currently raised only when
/// an element could not be split and did not fit even on a completely empty
/// page, so <see cref="LayoutEngine"/> placed it anyway and let it overflow
/// visually rather than dropping content or looping forever. Surfacing this
/// lets callers detect "my report clipped content" instead of only finding
/// out by eyeballing the rendered output.
/// </summary>
public sealed class LayoutWarning
{
    /// <summary>The zero-based index of the page the problem occurred on.</summary>
    public int PageIndex { get; }

    /// <summary>A human-readable description of the problem.</summary>
    public string Message { get; }

    /// <summary>Creates a layout warning.</summary>
    public LayoutWarning(int pageIndex, string message)
    {
        if (pageIndex < 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        PageIndex = pageIndex;
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <inheritdoc />
    public override string ToString() => $"Page {PageIndex + 1}: {Message}";
}
