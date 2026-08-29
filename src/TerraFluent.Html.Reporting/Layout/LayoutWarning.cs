namespace TerraFluent.Html.Reporting.Layout;

/// <summary>
/// A non-fatal problem noticed during pagination - either an element that
/// could not be split and did not fit even on a completely empty page (so
/// <see cref="LayoutEngine"/> placed it anyway and let it overflow visually
/// rather than dropping content or looping forever), or a
/// <see cref="Model.Elements.Table"/>/<see cref="Model.Elements.Row"/> whose
/// auto-width column collapsed to 0px. Surfacing this lets callers detect
/// "my report clipped content" instead of only finding out by eyeballing the
/// rendered output.
/// </summary>
public sealed class LayoutWarning
{
    /// <summary>The zero-based index of the page the problem occurred on.</summary>
    public int PageIndex { get; }

    /// <summary>A human-readable description of the problem.</summary>
    public string Message { get; }

    /// <summary>The category of problem - see <see cref="LayoutWarningReason"/>.</summary>
    public LayoutWarningReason Reason { get; }

    /// <summary>
    /// The simple type name of the element the warning is about (e.g. <c>"Table"</c>),
    /// or <see langword="null"/> if not known.
    /// </summary>
    public string? ElementType { get; }

    /// <summary>
    /// The zero-based index of the element within the content placed on
    /// <see cref="PageIndex"/> at the time the warning was raised, or <c>-1</c>
    /// if not known.
    /// </summary>
    public int ElementIndex { get; }

    /// <summary>Creates a layout warning with <see cref="Reason"/> defaulted to <see cref="LayoutWarningReason.Overflow"/>.</summary>
    public LayoutWarning(int pageIndex, string message)
        : this(pageIndex, message, LayoutWarningReason.Overflow, elementType: null, elementIndex: -1)
    {
    }

    /// <summary>Creates a layout warning.</summary>
    public LayoutWarning(int pageIndex, string message, LayoutWarningReason reason, string? elementType = null, int elementIndex = -1)
    {
        if (pageIndex < 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        PageIndex = pageIndex;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Reason = reason;
        ElementType = elementType;
        ElementIndex = elementIndex;
    }

    /// <inheritdoc />
    public override string ToString() => $"Page {PageIndex + 1}: {Message}";
}
