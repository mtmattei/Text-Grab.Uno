using System.Globalization;

namespace TextGrab.Shared;

public static class NumericUtilities
{
    public static double CalculateMedian(List<double> numbers)
    {
        if (numbers.Count == 0)
            return 0;

        List<double> sorted = [.. numbers.OrderBy(n => n)];
        int count = sorted.Count;

        if (count % 2 == 0)
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
        else
            return sorted[count / 2];
    }

    public static string FormatNumber(double value)
    {
        if (double.IsNaN(value)) return "NaN";
        if (double.IsPositiveInfinity(value)) return "\u221e";
        if (double.IsNegativeInfinity(value)) return "-\u221e";

        double absValue = Math.Abs(value);

        if (absValue >= 1e15 || (absValue < 1e-4 && absValue > 0))
            return value.ToString("E6", CultureInfo.CurrentCulture);

        double fractionalPart = Math.Abs(value - Math.Round(value));
        bool isEffectivelyInteger = fractionalPart < 1e-10 && absValue < 1e10;

        return isEffectivelyInteger
            ? Math.Round(value).ToString("N0", CultureInfo.CurrentCulture)
            : value.ToString("N", CultureInfo.CurrentCulture);
    }

    public static bool AreClose(double a, double b, double epsilon = 0.25)
    {
        return Math.Abs(a - b) < epsilon;
    }
}
