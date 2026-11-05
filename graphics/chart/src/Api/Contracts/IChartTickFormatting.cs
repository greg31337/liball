namespace chart;
/// <summary>Defines substitutable chart tick formatter operations.</summary>
public interface IChartTickFormatting
{
    /// <summary>
    /// Formats number as deterministic display text.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <param name = "decimalPlaces">The decimal places.</param>
    /// <returns>The resulting string.</returns>
    string FormatNumber(double value, int decimalPlaces);
    /// <summary>Formats a number using an explicitly selected culture.</summary>
    /// <param name = "value">The finite numeric value.</param>
    /// <param name = "decimalPlaces">The maximum decimal places.</param>
    /// <param name = "cultureName">The explicit culture name.</param>
    /// <returns>The culture-formatted number.</returns>
    string FormatNumberWithCulture(double value, int decimalPlaces, string cultureName);
    /// <summary>Formats a currency value using an explicitly selected culture.</summary>
    /// <param name = "value">The finite currency value.</param>
    /// <param name = "decimalPlaces">The exact decimal places.</param>
    /// <param name = "cultureName">The explicit culture name that supplies currency conventions.</param>
    /// <returns>The culture-formatted currency value.</returns>
    string FormatCurrency(double value, int decimalPlaces, string cultureName);
    /// <summary>
    /// Formats percent as deterministic display text.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <param name = "decimalPlaces">The decimal places.</param>
    /// <returns>The resulting string.</returns>
    string FormatPercent(double value, int decimalPlaces);
    /// <summary>
    /// Formats scientific as deterministic display text.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <param name = "decimalPlaces">The decimal places.</param>
    /// <returns>The resulting string.</returns>
    string FormatScientific(double value, int decimalPlaces);
    /// <summary>
    /// Formats si prefix as deterministic display text.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <param name = "decimalPlaces">The decimal places.</param>
    /// <returns>The resulting string.</returns>
    string FormatSiPrefix(double value, int decimalPlaces);
    /// <summary>
    /// Formats date as deterministic display text.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting string.</returns>
    string FormatDate(System.DateTime value);
    /// <summary>
    /// Formats date as deterministic display text.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <param name = "pattern">The pattern.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    string FormatDateWithPattern(System.DateTime value, string pattern);
    /// <summary>Formats a date using an explicit pattern and culture.</summary>
    /// <param name = "value">The date and time.</param>
    /// <param name = "pattern">The date and time format pattern.</param>
    /// <param name = "cultureName">The explicit culture name.</param>
    /// <returns>The culture-formatted date.</returns>
    string FormatDateWithCulture(System.DateTime value, string pattern, string cultureName);
    /// <summary>
    /// Formats duration as deterministic display text.
    /// </summary>
    /// <param name = "duration">The duration.</param>
    /// <returns>The resulting string.</returns>
    string FormatDuration(System.TimeSpan duration);
    /// <summary>
    /// Formats category as deterministic display text.
    /// </summary>
    /// <param name = "category">The category.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    string FormatCategory(string category);
    /// <summary>
    /// Formats tick as deterministic display text.
    /// </summary>
    /// <param name = "tick">The tick.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    string FormatTick(ChartTick tick);
    /// <summary>
    /// Formats ticks as deterministic display text.
    /// </summary>
    /// <param name = "ticks">The ticks.</param>
    /// <returns>The resulting string array.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    string[] FormatTicks(ChartTick[] ticks);
}
