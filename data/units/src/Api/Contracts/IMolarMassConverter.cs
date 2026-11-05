namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="MolarMassConverter"/>.
/// </summary>
public interface IMolarMassConverter
{
    /// <summary>
    /// Converts between mass and amount of substance using explicitly supplied molar-mass context.
    /// </summary>
    /// <param name="quantity">The quantity to use.</param>
    /// <param name="targetUnit">The target unit definition.</param>
    /// <param name="context">The caller-supplied molar-mass context.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult Convert(QuantityValue quantity, UnitDefinition targetUnit, MolarMassContext context);
    /// <summary>
    /// Converts between mass and amount of substance using explicitly supplied molar-mass context.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <param name="context">The caller-supplied molar-mass context.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult ConvertValue(double value, UnitDefinition fromUnit, UnitDefinition toUnit, MolarMassContext context);
    /// <summary>
    /// Reports whether the supplied units can be converted with the explicit context.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <param name="context">The caller-supplied molar-mass context.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool CanConvert(UnitDefinition? fromUnit, UnitDefinition? toUnit, MolarMassContext? context);
}
