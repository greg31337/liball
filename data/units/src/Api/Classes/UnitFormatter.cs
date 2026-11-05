namespace units;

/// <summary>
/// Formats unit definitions for display or canonical interchange.
/// </summary>
/// <remarks>
/// Canonical output uses registered unit identifiers, fully parenthesized multiplication, division, integer powers, and exact roots. Identifiers outside the canonical ASCII atom subset are escaped as uppercase UTF-16 code-unit hexadecimal text in brackets.
/// </remarks>
public class UnitFormatter : IUnitFormatter
{
    /// <summary>
    /// Initializes a unit formatter.
    /// </summary>
    public UnitFormatter()
    {
    }

    /// <summary>
    /// Formats the supplied unit according to the selected options.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The formatted text.</returns>
    public string Format(UnitDefinition unit)
    {
        return FormatWithOptions(unit, UnitFormatOptions.CreateDefault());
    }

    /// <summary>
    /// Formats the supplied unit according to the selected options.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The formatted text.</returns>
    public string FormatWithOptions(UnitDefinition unit, UnitFormatOptions options)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (options.IsSymbol())
        {
            return unit.GetSymbol();
        }

        if (options.IsName())
        {
            return unit.GetName();
        }

        if (options.IsCanonical())
        {
            return unit.GetCanonicalExpression();
        }

        return FormatDecomposed(unit.GetDimension());
    }

    internal string FormatDecomposed(DimensionVector dimension)
    {
        int numeratorCount = CountFactors(dimension, true);
        int denominatorCount = CountFactors(dimension, false);

        if (numeratorCount == 0 && denominatorCount == 0)
        {
            return "1";
        }

        string numerator = FormatFactors(dimension, true);
        if (numeratorCount == 0)
        {
            numerator = "1";
        }

        if (denominatorCount == 0)
        {
            return numerator;
        }

        string denominator = FormatFactors(dimension, false);
        if (denominatorCount > 1)
        {
            denominator = "(" + denominator + ")";
        }

        return numerator + "/" + denominator;
    }

    internal static int CountFactors(DimensionVector dimension, bool numerator)
    {
        int count = 0;
        int slot = 0;

        while (slot < DimensionVector.GetSlotCount())
        {
            int exponent = dimension.GetExponent(slot);
            if (numerator && exponent > 0)
            {
                count = count + 1;
            }

            if (!numerator && exponent < 0)
            {
                count = count + 1;
            }

            slot = slot + 1;
        }

        return count;
    }

    internal static string FormatFactors(DimensionVector dimension, bool numerator)
    {
        string text = "";
        int slot = 0;

        while (slot < DimensionVector.GetSlotCount())
        {
            int exponent = dimension.GetExponent(slot);
            if (ShouldFormatFactor(exponent, numerator))
            {
                if (text.Length > 0)
                {
                    text = text + "*";
                }

                text = text + FormatFactor(slot, FormatExponent(exponent, numerator));
            }

            slot = slot + 1;
        }

        return text;
    }

    internal static bool ShouldFormatFactor(int exponent, bool numerator)
    {
        if (numerator)
        {
            return exponent > 0;
        }

        return exponent < 0;
    }

    internal static int FormatExponent(int exponent, bool numerator)
    {
        if (numerator)
        {
            return exponent;
        }

        return 0 - exponent;
    }

    internal static string FormatFactor(int slot, int exponent)
    {
        string symbol = GetBaseSymbol(slot);

        if (exponent == 1)
        {
            return symbol;
        }

        return symbol + "^" + FormatInt(exponent);
    }

    internal static string GetBaseSymbol(int slot)
    {
        switch (slot)
        {
            case DimensionSlots.Length:
                return "m";
            case DimensionSlots.Mass:
                return "kg";
            case DimensionSlots.Time:
                return "s";
            case DimensionSlots.ElectricCurrent:
                return "A";
            case DimensionSlots.ThermodynamicTemperature:
                return "K";
            case DimensionSlots.AmountOfSubstance:
                return "mol";
            case DimensionSlots.LuminousIntensity:
                return "cd";
            case DimensionSlots.PlaneAngle:
                return "rad";
            case DimensionSlots.SolidAngle:
                return "sr";
            case DimensionSlots.Information:
                return "bit";
            case DimensionSlots.Currency:
                return "currency";
            case DimensionSlots.Count:
                return "count";
        }

        throw new System.ArgumentException("Unknown dimension slot.");
    }

    internal static string FormatInt(int value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
