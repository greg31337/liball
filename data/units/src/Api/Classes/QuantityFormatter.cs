namespace units;

/// <summary>
/// Formats a quantity's numeric value and unit together.
/// </summary>
public class QuantityFormatter : IQuantityFormatter
{
    internal UnitFormatter _unitFormatter;

    /// <summary>
    /// Initializes an empty quantity formatter with default values.
    /// </summary>
    public QuantityFormatter()
    {
        _unitFormatter = new UnitFormatter();
    }

    /// <summary>
    /// Formats the supplied quantity according to the selected options.
    /// </summary>
    /// <param name="quantity">The quantity to use.</param>
    /// <returns>The formatted text.</returns>
    public string Format(QuantityValue quantity)
    {
        return FormatWithOptions(quantity, QuantityFormatOptions.CreateDefault());
    }

    /// <summary>
    /// Formats the supplied quantity according to the selected options.
    /// </summary>
    /// <param name="quantity">The quantity to use.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The formatted text.</returns>
    public string FormatWithOptions(QuantityValue quantity, QuantityFormatOptions options)
    {
        if (quantity == null)
        {
            throw new System.ArgumentNullException("quantity");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        string valueText = quantity.GetValue().ToString(options.GetNumericFormat(), options.GetCulture());
        string unitText = FormatUnit(quantity, options.GetUnitFormatOptions());
        return valueText + " " + unitText;
    }

    internal string FormatUnit(QuantityValue quantity, UnitFormatOptions unitFormatOptions)
    {
        if (unitFormatOptions.IsName())
        {
            if (IsSingular(quantity.GetValue()))
            {
                return quantity.GetUnit().GetName();
            }

            return quantity.GetUnit().GetPluralName();
        }

        return _unitFormatter.FormatWithOptions(quantity.GetUnit(), unitFormatOptions);
    }

    internal static bool IsSingular(double value)
    {
        return value == 1.0d || value == -1.0d;
    }
}
