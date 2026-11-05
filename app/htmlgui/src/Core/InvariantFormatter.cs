using System;
using System.Globalization;

namespace htmlgui;

internal static class InvariantFormatter
{
    internal static string FormatBoolean(bool value)
    {
        return value ? "true" : "false";
    }

    internal static string FormatInt32(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatInt64(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatDouble(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException("value", value, "The value must be finite.");
        }

        if (value == 0.0)
        {
            return "0";
        }

        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    internal static string FormatDecimal(decimal value)
    {
        if (value == 0m)
        {
            return "0";
        }

        return value.ToString("G29", CultureInfo.InvariantCulture);
    }

    internal static string NormalizeLineEndings(string? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return value.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}
