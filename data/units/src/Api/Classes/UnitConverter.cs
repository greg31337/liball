namespace units;

/// <summary>
/// Converts numeric values between compatible unit definitions.
/// </summary>
public class UnitConverter : IUnitConverter
{
    /// <summary>
    /// Initializes a converter for ordinary same-dimension unit conversions.
    /// </summary>
    public UnitConverter()
    {
    }

    /// <summary>
    /// Converts the source value to the target unit when their dimensions and conversion kinds are compatible.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult Convert(double value, UnitDefinition fromUnit, UnitDefinition toUnit)
    {
        return ConvertWithPlan(value, GetPlan(fromUnit, toUnit));
    }

    /// <summary>
    /// Converts a value with a previously created conversion plan so the cached plan can be reused.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="plan">The reusable conversion plan.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult ConvertWithPlan(double value, ConversionPlan plan)
    {
        if (plan == null)
        {
            throw new System.ArgumentNullException("plan");
        }

        if (plan.IsUnsupported())
        {
            return ConversionResult.CreateFailure(plan.GetErrorCode(), plan.GetMessage());
        }

        if (plan.GetConversionKind() == UnitConversionKind.Scale || plan.GetConversionKind() == UnitConversionKind.Affine)
        {
            double converted = value * plan.GetValueScale().ToDouble() + plan.GetValueOffset().ToDouble();
            return ConversionResult.CreateSuccess(converted, plan.GetToUnit());
        }

        if (plan.GetConversionKind() == UnitConversionKind.Special)
        {
            return ConvertWithSpecialPlan(value, plan);
        }

        return ConversionResult.CreateFailure(UnitErrorCode.ConversionUnsupported, "Conversion kind is not supported.");
    }

    /// <summary>
    /// Reports whether ordinary same-dimension conversion is available for the supplied units.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool CanConvert(UnitDefinition? fromUnit, UnitDefinition? toUnit)
    {
        if (fromUnit == null || toUnit == null)
        {
            return false;
        }

        ConversionPlan plan = GetPlan(fromUnit, toUnit);
        return plan.IsSupported() && (plan.GetConversionKind() == UnitConversionKind.Scale || plan.GetConversionKind() == UnitConversionKind.Affine);
    }

    /// <summary>
    /// Builds the conversion plan for the supplied source and target units.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <returns>The resulting conversion plan.</returns>
    public ConversionPlan GetPlan(UnitDefinition fromUnit, UnitDefinition toUnit)
    {
        return ConversionPlan.Create(fromUnit, toUnit);
    }

    /// <summary>
    /// Converts through an explicitly selected built-in special conversion strategy.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <param name="specialConversion">The explicit special conversion metadata to use.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult ConvertSpecial(double value, UnitDefinition fromUnit, UnitDefinition toUnit, SpecialConversionDefinition specialConversion)
    {
        return ConvertWithPlan(value, GetSpecialPlan(fromUnit, toUnit, specialConversion));
    }

    internal ConversionResult ConvertWithSpecialPlan(double value, ConversionPlan plan)
    {
        SpecialConversionDefinition specialConversion = plan.GetSpecialConversion();
        UnitDefinition fromUnit = plan.GetFromUnit();
        UnitDefinition toUnit = plan.GetToUnit();
        if (specialConversion.MatchesUnit(fromUnit) && specialConversion.MatchesUnit(toUnit))
        {
            return ConversionResult.CreateSuccess(value, toUnit);
        }

        double systemValue;
        if (specialConversion.MatchesUnit(fromUnit))
        {
            systemValue = specialConversion.ConvertToSystemValue(value);
        }
        else
        {
            systemValue = value * fromUnit.GetScaleToSystemUnit().ToDouble();
        }

        if (specialConversion.MatchesUnit(toUnit))
        {
            if (systemValue <= 0.0d || double.IsNaN(systemValue))
            {
                return ConversionResult.CreateFailure(UnitErrorCode.ConversionUnsupported, "Special logarithmic conversion requires a positive system value.");
            }

            return ConversionResult.CreateSuccess(specialConversion.ConvertFromSystemValue(systemValue), toUnit);
        }

        return ConversionResult.CreateSuccess(systemValue / toUnit.GetScaleToSystemUnit().ToDouble(), toUnit);
    }

    /// <summary>
    /// Reports whether conversion is available through the explicitly selected special strategy.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <param name="specialConversion">The explicit special conversion metadata to use.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool CanConvertSpecial(UnitDefinition? fromUnit, UnitDefinition? toUnit, SpecialConversionDefinition? specialConversion)
    {
        if (fromUnit == null || toUnit == null || specialConversion == null)
        {
            return false;
        }

        return GetSpecialPlan(fromUnit, toUnit, specialConversion).IsSupported();
    }

    /// <summary>
    /// Builds a conversion plan using explicitly selected special conversion metadata.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <param name="specialConversion">The explicit special conversion metadata to use.</param>
    /// <returns>The resulting conversion plan.</returns>
    public ConversionPlan GetSpecialPlan(UnitDefinition fromUnit, UnitDefinition toUnit, SpecialConversionDefinition specialConversion)
    {
        return ConversionPlan.CreateSpecial(fromUnit, toUnit, specialConversion);
    }
}
