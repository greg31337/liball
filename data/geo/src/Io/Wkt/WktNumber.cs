using System.Globalization;

namespace geo;

internal static class WktNumber
{
    internal static string FormatOrdinate(double value, WktWriteOptions options)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidGeometryException("WKT ordinates must be finite.");
        }

        if (value == 0.0) value = 0.0;
        if (options.DecimalPrecision == 17 && options.TrimTrailingZeros)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        string format = $"F{options.DecimalPrecision}";
        string text = value.ToString(format, CultureInfo.InvariantCulture);
        if (!options.TrimTrailingZeros || options.DecimalPrecision == 0)
        {
            return text;
        }

        int end = text.Length - 1;
        while (end >= 0 && text[end] == '0')
        {
            end--;
        }
        if (end >= 0 && text[end] == '.')
        {
            end--;
        }
        return text.Substring(0, end + 1);
    }
}
