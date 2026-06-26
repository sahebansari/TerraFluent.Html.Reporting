namespace FluentHtmlReport.Model;

/// <summary>
/// Page margins, in CSS pixels, measured inward from each edge of the page.
/// The content area available for header/content/footer is the page size
/// minus these margins.
/// </summary>
public readonly struct Margins : IEquatable<Margins>
{
    /// <summary>Margin from the top edge of the page, in pixels.</summary>
    public double Top { get; }

    /// <summary>Margin from the right edge of the page, in pixels.</summary>
    public double Right { get; }

    /// <summary>Margin from the bottom edge of the page, in pixels.</summary>
    public double Bottom { get; }

    /// <summary>Margin from the left edge of the page, in pixels.</summary>
    public double Left { get; }

    /// <summary>Creates margins with an independent value per edge.</summary>
    public Margins(double top, double right, double bottom, double left)
    {
        if (top < 0) throw new ArgumentOutOfRangeException(nameof(top));
        if (right < 0) throw new ArgumentOutOfRangeException(nameof(right));
        if (bottom < 0) throw new ArgumentOutOfRangeException(nameof(bottom));
        if (left < 0) throw new ArgumentOutOfRangeException(nameof(left));
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    /// <summary>Creates uniform margins applied to all four edges.</summary>
    public static Margins All(double value) => new(value, value, value, value);

    /// <summary>No margin on any edge.</summary>
    public static Margins None { get; } = new(0, 0, 0, 0);

    /// <inheritdoc />
    public bool Equals(Margins other) =>
        Top.Equals(other.Top) && Right.Equals(other.Right) && Bottom.Equals(other.Bottom) && Left.Equals(other.Left);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Margins other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = Top.GetHashCode();
        hash = hash * 397 ^ Right.GetHashCode();
        hash = hash * 397 ^ Bottom.GetHashCode();
        hash = hash * 397 ^ Left.GetHashCode();
        return hash;
    }

    /// <summary>Equality operator, see <see cref="Equals(Margins)"/>.</summary>
    public static bool operator ==(Margins left, Margins right) => left.Equals(right);

    /// <summary>Inequality operator, see <see cref="Equals(Margins)"/>.</summary>
    public static bool operator !=(Margins left, Margins right) => !left.Equals(right);
}
