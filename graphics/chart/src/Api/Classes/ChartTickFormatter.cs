namespace chart;
/// <summary>Provides the public chart tick formatter operations.</summary>
public class ChartTickFormatter : IChartTickFormatting
{
    /// <summary>Initializes a stateless chart tick formatter facade.</summary>
    public ChartTickFormatter()
    {
    }

    /// <summary>
    /// Formats number as deterministic display text.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <param name = "decimalPlaces">The decimal places.</param>
    /// <returns>The resulting string.</returns>
    public string FormatNumber(double value, int decimalPlaces)
    {
        return ChartTickFormatterOperations.FormatNumber(value, decimalPlaces);
    }

    /// <summary>Formats a number using an explicitly selected culture.</summary>
    /// <param name = "value">The finite numeric value.</param>
    /// <param name = "decimalPlaces">The maximum decimal places.</param>
    /// <param name = "cultureName">The explicit culture name.</param>
    /// <returns>The culture-formatted number.</returns>
    public string FormatNumberWithCulture(double value, int decimalPlaces, string cultureName)
    {
        return ChartTickFormatterOperations.FormatNumberWithCulture(value, decimalPlaces, cultureName);
    }

    /// <summary>Formats a currency value using an explicitly selected culture.</summary>
    /// <param name = "value">The finite currency value.</param>
    /// <param name = "decimalPlaces">The exact decimal places.</param>
    /// <param name = "cultureName">The explicit culture name that supplies currency conventions.</param>
    /// <returns>The culture-formatted currency value.</returns>
    public string FormatCurrency(double value, int decimalPlaces, string cultureName)
    {
        return ChartTickFormatterOperations.FormatCurrency(value, decimalPlaces, cultureName);
    }

    /// <summary>
    /// Formats percent as deterministic display text.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <param name = "decimalPlaces">The decimal places.</param>
    /// <returns>The resulting string.</returns>
    public string FormatPercent(double value, int decimalPlaces)
    {
        return ChartTickFormatterOperations.FormatPercent(value, decimalPlaces);
    }

    /// <summary>
    /// Formats scientific as deterministic display text.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <param name = "decimalPlaces">The decimal places.</param>
    /// <returns>The resulting string.</returns>
    public string FormatScientific(double value, int decimalPlaces)
    {
        return ChartTickFormatterOperations.FormatScientific(value, decimalPlaces);
    }

    /// <summary>
    /// Formats si prefix as deterministic display text.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <param name = "decimalPlaces">The decimal places.</param>
    /// <returns>The resulting string.</returns>
    public string FormatSiPrefix(double value, int decimalPlaces)
    {
        return ChartTickFormatterOperations.FormatSiPrefix(value, decimalPlaces);
    }

    /// <summary>
    /// Formats date as deterministic display text.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting string.</returns>
    public string FormatDate(System.DateTime value)
    {
        return ChartTickFormatterOperations.FormatDate(value);
    }

    /// <summary>
    /// Formats date as deterministic display text.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <param name = "pattern">The pattern.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string FormatDateWithPattern(System.DateTime value, string pattern)
    {
        return ChartTickFormatterOperations.FormatDate(value, pattern);
    }

    /// <summary>Formats a date using an explicit pattern and culture.</summary>
    /// <param name = "value">The date and time.</param>
    /// <param name = "pattern">The date and time format pattern.</param>
    /// <param name = "cultureName">The explicit culture name.</param>
    /// <returns>The culture-formatted date.</returns>
    public string FormatDateWithCulture(System.DateTime value, string pattern, string cultureName)
    {
        return ChartTickFormatterOperations.FormatDateWithCulture(value, pattern, cultureName);
    }

    /// <summary>
    /// Formats duration as deterministic display text.
    /// </summary>
    /// <param name = "duration">The duration.</param>
    /// <returns>The resulting string.</returns>
    public string FormatDuration(System.TimeSpan duration)
    {
        return ChartTickFormatterOperations.FormatDuration(duration);
    }

    /// <summary>
    /// Formats category as deterministic display text.
    /// </summary>
    /// <param name = "category">The category.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string FormatCategory(string category)
    {
        return ChartTickFormatterOperations.FormatCategory(category);
    }

    /// <summary>
    /// Formats tick as deterministic display text.
    /// </summary>
    /// <param name = "tick">The tick.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string FormatTick(ChartTick tick)
    {
        return ChartTickFormatterOperations.FormatTick(tick);
    }

    /// <summary>
    /// Formats ticks as deterministic display text.
    /// </summary>
    /// <param name = "ticks">The ticks.</param>
    /// <returns>The resulting string array.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string[] FormatTicks(ChartTick[] ticks)
    {
        return ChartTickFormatterOperations.FormatTicks(ticks);
    }
}
