namespace units;

/// <summary>
/// Converts between mass and amount of substance only when molar-mass context is supplied explicitly.
/// </summary>
public class MolarMassConverter : IMolarMassConverter
{
    /// <summary>
    /// Initializes an opt-in molar-mass converter.
    /// </summary>
    public MolarMassConverter()
    {
    }

    /// <summary>
    /// Converts between mass and amount of substance using explicitly supplied molar-mass context.
    /// </summary>
    /// <param name="quantity">The quantity to use.</param>
    /// <param name="targetUnit">The target unit definition.</param>
    /// <param name="context">The caller-supplied molar-mass context.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult Convert(QuantityValue quantity, UnitDefinition targetUnit, MolarMassContext context)
    {
        ValidateRequiredValue(quantity, "quantity");
        return ConvertValue(quantity.GetValue(), quantity.GetUnit(), targetUnit, context);
    }

    /// <summary>
    /// Converts between mass and amount of substance using explicitly supplied molar-mass context.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <param name="context">The caller-supplied molar-mass context.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult ConvertValue(double value, UnitDefinition fromUnit, UnitDefinition toUnit, MolarMassContext context)
    {
        ValidateRequiredValue(fromUnit, "fromUnit");
        ValidateRequiredValue(toUnit, "toUnit");
        ValidateRequiredValue(context, "context");

        if (!HasSupportedDimensions(fromUnit, toUnit))
        {
            return ConversionResult.CreateFailure(UnitErrorCode.ConversionIncompatibleDimensions, "Molar-mass context converts only between mass and amount of substance.");
        }

        if (!CanUseContextualUnit(fromUnit) || !CanUseContextualUnit(toUnit))
        {
            return ConversionResult.CreateFailure(UnitErrorCode.ConversionUnsupported, "Molar-mass conversion requires scale-only source and target units.");
        }

        QuantityValue molarMass = context.GetMolarMass();
        double molarMassSystemValue = ToSystemValue(molarMass.GetValue(), molarMass.GetUnit());
        double sourceSystemValue = ToSystemValue(value, fromUnit);
        double targetSystemValue;
        if (IsAmountToMass(fromUnit, toUnit))
        {
            targetSystemValue = sourceSystemValue * molarMassSystemValue;
        }
        else
        {
            targetSystemValue = sourceSystemValue / molarMassSystemValue;
        }

        double targetValue = FromSystemValue(targetSystemValue, toUnit);
        return ConversionResult.CreateSuccess(targetValue, toUnit);
    }

    /// <summary>
    /// Reports whether the supplied units can be converted with the explicit context.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <param name="context">The caller-supplied molar-mass context.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool CanConvert(UnitDefinition? fromUnit, UnitDefinition? toUnit, MolarMassContext? context)
    {
        if (fromUnit == null || toUnit == null || context == null)
        {
            return false;
        }

        return HasSupportedDimensions(fromUnit, toUnit) && CanUseContextualUnit(fromUnit) && CanUseContextualUnit(toUnit);
    }

    internal static bool HasSupportedDimensions(UnitDefinition fromUnit, UnitDefinition toUnit)
    {
        return IsAmountToMass(fromUnit, toUnit) || IsMassToAmount(fromUnit, toUnit);
    }

    internal static bool IsAmountToMass(UnitDefinition fromUnit, UnitDefinition toUnit)
    {
        StandardDimensions dimensions = new StandardDimensions();
        return fromUnit.GetDimension().EqualsDimension(dimensions.AmountOfSubstance()) && toUnit.GetDimension().EqualsDimension(dimensions.Mass());
    }

    internal static bool IsMassToAmount(UnitDefinition fromUnit, UnitDefinition toUnit)
    {
        StandardDimensions dimensions = new StandardDimensions();
        return fromUnit.GetDimension().EqualsDimension(dimensions.Mass()) && toUnit.GetDimension().EqualsDimension(dimensions.AmountOfSubstance());
    }

    internal static bool CanUseContextualUnit(UnitDefinition unit)
    {
        return unit.GetConversionKind() == UnitConversionKind.Scale && unit.GetOffsetToSystemUnit().IsZero();
    }

    internal static double ToSystemValue(double value, UnitDefinition unit)
    {
        return value * unit.GetScaleToSystemUnit().ToDouble();
    }

    internal static double FromSystemValue(double value, UnitDefinition unit)
    {
        return value / unit.GetScaleToSystemUnit().ToDouble();
    }

    internal static void ValidateRequiredValue(object value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }
}
