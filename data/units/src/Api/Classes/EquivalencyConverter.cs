namespace units;

/// <summary>
/// Applies an explicitly selected equivalency to a quantity conversion.
/// </summary>
public class EquivalencyConverter : IEquivalencyConverter
{
    /// <summary>
    /// Initializes a converter for explicitly selected equivalencies.
    /// </summary>
    public EquivalencyConverter()
    {
    }

    /// <summary>
    /// Applies the selected equivalency after converting through each unit's system value.
    /// </summary>
    /// <param name="quantity">The quantity to use.</param>
    /// <param name="targetUnit">The target unit definition.</param>
    /// <param name="equivalency">The explicitly selected equivalency definition.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult Convert(QuantityValue quantity, UnitDefinition targetUnit, EquivalencyDefinition equivalency)
    {
        ValidateRequiredValue(quantity, "quantity");
        return ConvertValue(quantity.GetValue(), quantity.GetUnit(), targetUnit, equivalency);
    }

    /// <summary>
    /// Applies the selected equivalency after converting through each unit's system value.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <param name="equivalency">The explicitly selected equivalency definition.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult ConvertValue(double value, UnitDefinition fromUnit, UnitDefinition toUnit, EquivalencyDefinition equivalency)
    {
        ValidateRequiredValue(fromUnit, "fromUnit");
        ValidateRequiredValue(toUnit, "toUnit");
        ValidateRequiredValue(equivalency, "equivalency");

        if (!equivalency.CanConvert(fromUnit, toUnit))
        {
            return ConversionResult.CreateFailure(UnitErrorCode.ConversionIncompatibleDimensions, "Equivalency does not support the requested unit dimensions.");
        }

        if (!CanUseEquivalencyUnit(fromUnit) || !CanUseEquivalencyUnit(toUnit))
        {
            return ConversionResult.CreateFailure(UnitErrorCode.ConversionUnsupported, "Equivalency conversions require scale-only units.");
        }

        double sourceSystemValue = ToSystemValue(value, fromUnit);
        if (UsesNegativePower(sourceSystemValue, fromUnit, toUnit, equivalency))
        {
            return ConversionResult.CreateFailure(UnitErrorCode.ConversionUnsupported, "Equivalency reciprocal conversion cannot use zero.");
        }

        double targetSystemValue = equivalency.ConvertSystemValue(sourceSystemValue, fromUnit, toUnit);
        double targetValue = FromSystemValue(targetSystemValue, toUnit);
        return ConversionResult.CreateSuccess(targetValue, toUnit);
    }

    /// <summary>
    /// Applies the selected equivalency after converting through each unit's system value.
    /// </summary>
    /// <param name="quantity">The quantity to use.</param>
    /// <param name="targetUnit">The target unit definition.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <param name="equivalencyId">The stable identifier of the equivalency to use.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult ConvertById(QuantityValue quantity, UnitDefinition targetUnit, IUnitCatalog registry, string equivalencyId)
    {
        ValidateRequiredValue(quantity, "quantity");
        ValidateRequiredValue(targetUnit, "targetUnit");
        ValidateRequiredValue(registry, "registry");
        ValidateRequiredText(equivalencyId, "equivalencyId");

        EquivalencyDefinition? equivalency = registry.FindEquivalency(equivalencyId);
        if (equivalency == null)
        {
            return ConversionResult.CreateFailure(UnitErrorCode.LookupNotFound, "Equivalency was not found.");
        }

        return Convert(quantity, targetUnit, equivalency);
    }

    /// <summary>
    /// Reports whether the supplied units can be converted with the selected equivalency.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <param name="equivalency">The explicitly selected equivalency definition.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool CanConvert(UnitDefinition? fromUnit, UnitDefinition? toUnit, EquivalencyDefinition? equivalency)
    {
        if (fromUnit == null || toUnit == null || equivalency == null)
        {
            return false;
        }

        return equivalency.CanConvert(fromUnit, toUnit) && CanUseEquivalencyUnit(fromUnit) && CanUseEquivalencyUnit(toUnit);
    }

    internal static bool UsesNegativePower(double systemValue, UnitDefinition fromUnit, UnitDefinition toUnit, EquivalencyDefinition equivalency)
    {
        if (systemValue != 0.0d)
        {
            return false;
        }

        if (equivalency.CanConvertForward(fromUnit, toUnit))
        {
            return equivalency.GetForwardPower() < 0;
        }

        return equivalency.GetReversePower() < 0;
    }

    internal static bool CanUseEquivalencyUnit(UnitDefinition unit)
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

    internal static void ValidateRequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new System.ArgumentException("Equivalency converter " + fieldName + " must not be empty.");
        }
    }

    internal static void ValidateRequiredValue(object value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }
}
