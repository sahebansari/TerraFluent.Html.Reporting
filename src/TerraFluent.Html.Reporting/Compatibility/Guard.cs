namespace TerraFluent.Html.Reporting.Compatibility;

/// <summary>Shared argument validation and snapshot helpers for public model boundaries.</summary>
internal static class Guard
{
    public static double Finite(double value, string paramName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be finite.");
        }

        return value;
    }

    public static double NonNegative(double value, string paramName)
    {
        Finite(value, paramName);
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be zero or greater.");
        }

        return value;
    }

    public static double Positive(double value, string paramName)
    {
        Finite(value, paramName);
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be greater than zero.");
        }

        return value;
    }

    public static double? NonNegative(double? value, string paramName) =>
        value.HasValue ? NonNegative(value.Value, paramName) : null;

    public static IReadOnlyList<T> Snapshot<T>(IEnumerable<T> source, string paramName)
    {
        if (source is null) throw new ArgumentNullException(paramName);
        return Array.AsReadOnly(source.ToArray());
    }
}
