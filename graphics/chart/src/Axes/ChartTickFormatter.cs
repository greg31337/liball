namespace chart;

/// <summary>
/// Formats chart values and labels using deterministic, culture-aware text rules.
/// </summary>
internal static class ChartTickFormatterOperations
{
    /// <summary>
    /// Formats number as deterministic display text.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="decimalPlaces">The decimal places.</param>
    /// <returns>The resulting string.</returns>
    public static string FormatNumber(double value, int decimalPlaces)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        ValidateDecimalPlaces(decimalPlaces);
        double rounded = System.Math.Round(value, decimalPlaces);
        if (rounded == 0.0)
        {
            return "0";
        }

        return rounded.ToString(BuildDecimalFormat(decimalPlaces), System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string FormatNumberWithCulture(double value, int decimalPlaces, string cultureName)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        ValidateDecimalPlaces(decimalPlaces);
        System.Globalization.CultureInfo culture = ResolveCulture(cultureName);
        double rounded = System.Math.Round(value, decimalPlaces);
        if (rounded == 0.0)
        {
            return "0";
        }

        return rounded.ToString(BuildDecimalFormat(decimalPlaces), culture);
    }

    internal static string FormatCurrency(double value, int decimalPlaces, string cultureName)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        ValidateDecimalPlaces(decimalPlaces);
        System.Globalization.CultureInfo culture = ResolveCulture(cultureName);
        string format = "C" + decimalPlaces.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return value.ToString(format, culture);
    }

    /// <summary>
    /// Formats percent as deterministic display text.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="decimalPlaces">The decimal places.</param>
    /// <returns>The resulting string.</returns>
    public static string FormatPercent(double value, int decimalPlaces)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        ValidateDecimalPlaces(decimalPlaces);
        return FormatNumber(value * 100.0, decimalPlaces) + "%";
    }

    /// <summary>
    /// Formats scientific as deterministic display text.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="decimalPlaces">The decimal places.</param>
    /// <returns>The resulting string.</returns>
    public static string FormatScientific(double value, int decimalPlaces)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        ValidateDecimalPlaces(decimalPlaces);
        if (value == 0.0)
        {
            return "0E+0";
        }

        return value.ToString(BuildScientificFormat(decimalPlaces), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats si prefix as deterministic display text.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="decimalPlaces">The decimal places.</param>
    /// <returns>The resulting string.</returns>
    public static string FormatSiPrefix(double value, int decimalPlaces)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        ValidateDecimalPlaces(decimalPlaces);
        if (value == 0.0)
        {
            return "0";
        }

        double absoluteValue = System.Math.Abs(value);
        int exponent = (int)System.Math.Floor(System.Math.Log10(absoluteValue) / 3.0) * 3;
        if (exponent < -12)
        {
            exponent = -12;
        }

        if (exponent > 12)
        {
            exponent = 12;
        }

        double scale = System.Math.Pow(10.0, exponent);
        return FormatNumber(value / scale, decimalPlaces) + SiSuffix(exponent);
    }

    /// <summary>
    /// Formats date as deterministic display text.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The resulting string.</returns>
    public static string FormatDate(System.DateTime value)
    {
        return FormatDate(value, "yyyy-MM-dd");
    }

    /// <summary>
    /// Formats date as deterministic display text.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="pattern">The pattern.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string FormatDate(System.DateTime value, string pattern)
    {
        if (pattern == null)
        {
            throw new System.ArgumentNullException("pattern");
        }

        if (pattern.Length == 0)
        {
            throw new System.ArgumentException("Date pattern cannot be empty.");
        }

        return value.ToString(pattern, System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string FormatDateWithCulture(System.DateTime value, string pattern, string cultureName)
    {
        if (pattern == null)
        {
            throw new System.ArgumentNullException("pattern");
        }

        if (pattern.Length == 0)
        {
            throw new System.ArgumentException("Date pattern cannot be empty.");
        }

        return value.ToString(pattern, ResolveCulture(cultureName));
    }

    /// <summary>
    /// Formats duration as deterministic display text.
    /// </summary>
    /// <param name="duration">The duration.</param>
    /// <returns>The resulting string.</returns>
    public static string FormatDuration(System.TimeSpan duration)
    {
        string sign = "";
        long ticks = duration.Ticks;
        if (ticks < 0)
        {
            sign = "-";
            ticks = -ticks;
        }

        System.TimeSpan positive = new System.TimeSpan(ticks);
        if (positive.Days > 0)
        {
            return sign + positive.Days.ToString(System.Globalization.CultureInfo.InvariantCulture) + "d " + positive.Hours.ToString(System.Globalization.CultureInfo.InvariantCulture) + "h";
        }

        if (positive.Hours > 0)
        {
            return sign + positive.Hours.ToString(System.Globalization.CultureInfo.InvariantCulture) + "h " + positive.Minutes.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m";
        }

        if (positive.Minutes > 0)
        {
            return sign + positive.Minutes.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m " + positive.Seconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + "s";
        }

        if (positive.Seconds > 0)
        {
            return sign + FormatNumber(positive.TotalSeconds, 3) + "s";
        }

        return sign + FormatNumber(positive.TotalMilliseconds, 3) + "ms";
    }

    /// <summary>
    /// Formats category as deterministic display text.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string FormatCategory(string category)
    {
        if (category == null)
        {
            throw new System.ArgumentNullException("category");
        }

        return category;
    }

    /// <summary>
    /// Formats tick as deterministic display text.
    /// </summary>
    /// <param name="tick">The tick.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string FormatTick(ChartTick tick)
    {
        if (tick == null)
        {
            throw new System.ArgumentNullException("tick");
        }

        if (tick.Kind == ChartTickKind.Time)
        {
            return FormatDate(tick.DateTimeValue);
        }

        if (tick.Kind == ChartTickKind.Category)
        {
            return FormatCategory(tick.Label);
        }

        if (tick.Label.Length > 0)
        {
            return tick.Label;
        }

        return FormatNumber(tick.Value, 3);
    }

    /// <summary>
    /// Formats ticks as deterministic display text.
    /// </summary>
    /// <param name="ticks">The ticks.</param>
    /// <returns>The resulting string array.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string[] FormatTicks(ChartTick[] ticks)
    {
        if (ticks == null)
        {
            throw new System.ArgumentNullException("ticks");
        }

        string[] labels = new string[ticks.Length];
        int index = 0;
        while (index < ticks.Length)
        {
            labels[index] = FormatTick(ticks[index]);
            index = index + 1;
        }

        return labels;
    }

    internal static void ValidateDecimalPlaces(int decimalPlaces)
    {
        if (decimalPlaces < 0 || decimalPlaces > 15)
        {
            throw new System.ArgumentOutOfRangeException("decimalPlaces");
        }
    }

    internal static string BuildDecimalFormat(int decimalPlaces)
    {
        ValidateDecimalPlaces(decimalPlaces);
        if (decimalPlaces == 0)
        {
            return "0";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append("0.");
        int index = 0;
        while (index < decimalPlaces)
        {
            builder.Append("#");
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string BuildScientificFormat(int decimalPlaces)
    {
        ValidateDecimalPlaces(decimalPlaces);
        if (decimalPlaces == 0)
        {
            return "0E+0";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append("0.");
        int index = 0;
        while (index < decimalPlaces)
        {
            builder.Append("#");
            index = index + 1;
        }

        builder.Append("E+0");
        return builder.ToString();
    }

    internal static string SiSuffix(int exponent)
    {
        if (exponent == -12)
        {
            return "p";
        }

        if (exponent == -9)
        {
            return "n";
        }

        if (exponent == -6)
        {
            return "u";
        }

        if (exponent == -3)
        {
            return "m";
        }

        if (exponent == 0)
        {
            return "";
        }

        if (exponent == 3)
        {
            return "k";
        }

        if (exponent == 6)
        {
            return "M";
        }

        if (exponent == 9)
        {
            return "G";
        }

        if (exponent == 12)
        {
            return "T";
        }

        throw new System.ArgumentOutOfRangeException("exponent");
    }

    internal static System.Globalization.CultureInfo ResolveCulture(string cultureName)
    {
        if (cultureName == null)
        {
            throw new System.ArgumentNullException("cultureName");
        }

        if (cultureName.Length == 0)
        {
            throw new System.ArgumentException("Culture name cannot be empty.");
        }

        return System.Globalization.CultureInfo.GetCultureInfo(cultureName);
    }
}
