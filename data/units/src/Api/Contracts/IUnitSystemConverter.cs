namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="UnitSystemConverter"/>.
/// </summary>
public interface IUnitSystemConverter
{
    /// <summary>
    /// Converts the value to the unit preferred by the selected system for the quantity kind.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="quantityKind">The quantity kind used to validate or narrow the operation.</param>
    /// <param name="system">The target unit-system definition.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult ConvertToPreferredUnit(double value, UnitDefinition fromUnit, QuantityKindDefinition quantityKind, UnitSystemDefinition system, IUnitCatalog registry);
    /// <summary>
    /// Converts a quantity to the unit preferred by the selected system for the quantity kind.
    /// </summary>
    /// <param name="quantity">The quantity to convert.</param>
    /// <param name="quantityKind">The quantity kind used to validate or narrow the operation.</param>
    /// <param name="system">The target unit-system definition.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult ConvertQuantityToPreferredUnit(QuantityValue quantity, QuantityKindDefinition quantityKind, UnitSystemDefinition system, IUnitCatalog registry);
    /// <summary>
    /// Converts a quantity to a compound expression built from the selected system's registered base units.
    /// </summary>
    /// <param name="quantity">The quantity to decompose.</param>
    /// <param name="system">The target unit-system definition.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>A result containing the decomposed quantity or structured failure details.</returns>
    public ConversionResult DecomposeToBaseUnits(QuantityValue quantity, UnitSystemDefinition system, IUnitCatalog registry);
    /// <summary>
    /// Converts an equivalent expression to one registered named derived unit preferred by the selected system.
    /// </summary>
    /// <param name="quantity">The quantity expression to compose.</param>
    /// <param name="system">The target unit-system definition.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>A result containing the named quantity or structured failure details.</returns>
    public ConversionResult ComposeToNamedUnit(QuantityValue quantity, UnitSystemDefinition system, IUnitCatalog registry);
}
