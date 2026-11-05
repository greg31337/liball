namespace units;

/// <summary>
/// Specifies numeric, culture, and unit formatting for quantities.
/// </summary>
public class QuantityFormatOptions : IQuantityFormatOptions
{
    internal string _numericFormat;
    internal System.Globalization.CultureInfo _culture;
    internal UnitFormatOptions _unitFormatOptions;

    /// <summary>
    /// Initializes a new quantity format options from the supplied metadata.
    /// </summary>
    /// <param name="numericFormat">The standard or custom numeric format string.</param>
    /// <param name="culture">The culture used to read or write numeric text.</param>
    /// <param name="unitFormatOptions">The options used to format the unit portion.</param>
    public QuantityFormatOptions(string numericFormat, System.Globalization.CultureInfo culture, UnitFormatOptions unitFormatOptions)
    {
        ValidateRequiredText(numericFormat, "numericFormat");

        if (culture == null)
        {
            throw new System.ArgumentNullException("culture");
        }

        if (unitFormatOptions == null)
        {
            throw new System.ArgumentNullException("unitFormatOptions");
        }

        _numericFormat = numericFormat;
        _culture = culture;
        _unitFormatOptions = unitFormatOptions;
    }

    internal static QuantityFormatOptions CreateDefault()
    {
        return new QuantityFormatOptions("G", System.Globalization.CultureInfo.InvariantCulture, UnitFormatOptions.CreateDefault());
    }

    /// <summary>
    /// Gets the numeric format.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetNumericFormat()
    {
        return _numericFormat;
    }

    /// <summary>
    /// Gets the culture.
    /// </summary>
    /// <returns>The resulting system.globalization.culture info.</returns>
    public System.Globalization.CultureInfo GetCulture()
    {
        return _culture;
    }

    /// <summary>
    /// Gets the unit format options.
    /// </summary>
    /// <returns>The resulting unit format options.</returns>
    public UnitFormatOptions GetUnitFormatOptions()
    {
        return _unitFormatOptions;
    }

    /// <summary>
    /// Creates a new options object with numeric format replaced.
    /// </summary>
    /// <param name="numericFormat">The standard or custom numeric format string.</param>
    /// <returns>The resulting quantity format options.</returns>
    public QuantityFormatOptions WithNumericFormat(string numericFormat)
    {
        ValidateRequiredText(numericFormat, "numericFormat");
        return new QuantityFormatOptions(numericFormat, _culture, _unitFormatOptions);
    }

    /// <summary>
    /// Creates a new options object with culture replaced.
    /// </summary>
    /// <param name="culture">The culture used to read or write numeric text.</param>
    /// <returns>The resulting quantity format options.</returns>
    public QuantityFormatOptions WithCulture(System.Globalization.CultureInfo culture)
    {
        if (culture == null)
        {
            throw new System.ArgumentNullException("culture");
        }

        return new QuantityFormatOptions(_numericFormat, culture, _unitFormatOptions);
    }

    /// <summary>
    /// Creates a new options object with unit format options replaced.
    /// </summary>
    /// <param name="unitFormatOptions">The options used to format the unit portion.</param>
    /// <returns>The resulting quantity format options.</returns>
    public QuantityFormatOptions WithUnitFormatOptions(UnitFormatOptions unitFormatOptions)
    {
        if (unitFormatOptions == null)
        {
            throw new System.ArgumentNullException("unitFormatOptions");
        }

        return new QuantityFormatOptions(_numericFormat, _culture, unitFormatOptions);
    }

    internal static void ValidateRequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new System.ArgumentException("Quantity format " + fieldName + " must not be empty.");
        }
    }
}
