namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="QuantityFormatOptions"/>.
/// </summary>
public interface IQuantityFormatOptions
{
    /// <summary>
    /// Gets the numeric format.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetNumericFormat();
    /// <summary>
    /// Gets the culture.
    /// </summary>
    /// <returns>The resulting system.globalization.culture info.</returns>
    public System.Globalization.CultureInfo GetCulture();
    /// <summary>
    /// Gets the unit format options.
    /// </summary>
    /// <returns>The resulting unit format options.</returns>
    public UnitFormatOptions GetUnitFormatOptions();
    /// <summary>
    /// Creates a new options object with numeric format replaced.
    /// </summary>
    /// <param name="numericFormat">The standard or custom numeric format string.</param>
    /// <returns>The resulting quantity format options.</returns>
    public QuantityFormatOptions WithNumericFormat(string numericFormat);
    /// <summary>
    /// Creates a new options object with culture replaced.
    /// </summary>
    /// <param name="culture">The culture used to read or write numeric text.</param>
    /// <returns>The resulting quantity format options.</returns>
    public QuantityFormatOptions WithCulture(System.Globalization.CultureInfo culture);
    /// <summary>
    /// Creates a new options object with unit format options replaced.
    /// </summary>
    /// <param name="unitFormatOptions">The options used to format the unit portion.</param>
    /// <returns>The resulting quantity format options.</returns>
    public QuantityFormatOptions WithUnitFormatOptions(UnitFormatOptions unitFormatOptions);
}
