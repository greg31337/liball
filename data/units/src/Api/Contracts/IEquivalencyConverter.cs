namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="EquivalencyConverter"/>.
/// </summary>
public interface IEquivalencyConverter
{
    /// <summary>
    /// Applies the selected equivalency after converting through each unit's system value.
    /// </summary>
    /// <param name="quantity">The quantity to use.</param>
    /// <param name="targetUnit">The target unit definition.</param>
    /// <param name="equivalency">The explicitly selected equivalency definition.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult Convert(QuantityValue quantity, UnitDefinition targetUnit, EquivalencyDefinition equivalency);
    /// <summary>
    /// Applies the selected equivalency after converting through each unit's system value.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <param name="equivalency">The explicitly selected equivalency definition.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult ConvertValue(double value, UnitDefinition fromUnit, UnitDefinition toUnit, EquivalencyDefinition equivalency);
    /// <summary>
    /// Applies the selected equivalency after converting through each unit's system value.
    /// </summary>
    /// <param name="quantity">The quantity to use.</param>
    /// <param name="targetUnit">The target unit definition.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <param name="equivalencyId">The stable identifier of the equivalency to use.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult ConvertById(QuantityValue quantity, UnitDefinition targetUnit, IUnitCatalog registry, string equivalencyId);
    /// <summary>
    /// Reports whether the supplied units can be converted with the selected equivalency.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <param name="equivalency">The explicitly selected equivalency definition.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool CanConvert(UnitDefinition? fromUnit, UnitDefinition? toUnit, EquivalencyDefinition? equivalency);
}
