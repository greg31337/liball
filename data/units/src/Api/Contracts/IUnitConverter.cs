namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="UnitConverter"/>.
/// </summary>
public interface IUnitConverter
{
    /// <summary>
    /// Converts the source value to the target unit when their dimensions and conversion kinds are compatible.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult Convert(double value, UnitDefinition fromUnit, UnitDefinition toUnit);
    /// <summary>
    /// Reports whether ordinary same-dimension conversion is available for the supplied units.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool CanConvert(UnitDefinition? fromUnit, UnitDefinition? toUnit);
    /// <summary>
    /// Builds the conversion plan for the supplied source and target units.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <returns>The resulting conversion plan.</returns>
    public ConversionPlan GetPlan(UnitDefinition fromUnit, UnitDefinition toUnit);
    /// <summary>
    /// Converts a value with a previously created conversion plan so the cached plan can be reused.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="plan">The reusable conversion plan.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult ConvertWithPlan(double value, ConversionPlan plan);
    /// <summary>
    /// Converts through an explicitly selected built-in special conversion strategy.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <param name="specialConversion">The explicit special conversion metadata to use.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult ConvertSpecial(double value, UnitDefinition fromUnit, UnitDefinition toUnit, SpecialConversionDefinition specialConversion);
    /// <summary>
    /// Reports whether conversion is available through the explicitly selected special strategy.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <param name="specialConversion">The explicit special conversion metadata to use.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool CanConvertSpecial(UnitDefinition? fromUnit, UnitDefinition? toUnit, SpecialConversionDefinition? specialConversion);
    /// <summary>
    /// Builds a conversion plan using explicitly selected special conversion metadata.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <param name="specialConversion">The explicit special conversion metadata to use.</param>
    /// <returns>The resulting conversion plan.</returns>
    public ConversionPlan GetSpecialPlan(UnitDefinition fromUnit, UnitDefinition toUnit, SpecialConversionDefinition specialConversion);
}
