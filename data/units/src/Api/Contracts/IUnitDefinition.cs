namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="UnitDefinition"/>.
/// </summary>
public interface IUnitDefinition
{
    /// <summary>
    /// Gets the stable identifier.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetId();
    /// <summary>
    /// Gets the display name.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetName();
    /// <summary>
    /// Gets the plural name.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetPluralName();
    /// <summary>
    /// Gets the symbol.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetSymbol();
    /// <summary>
    /// Returns a copy of the aliases.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public string[] GetAliases();
    /// <summary>
    /// Gets the dimension.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector GetDimension();
    /// <summary>
    /// Gets the scale to system unit.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetScaleToSystemUnit();
    /// <summary>
    /// Gets the offset to system unit.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetOffsetToSystemUnit();
    /// <summary>
    /// Reports whether registered prefixes may be applied to this unit.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsPrefixable();
    /// <summary>
    /// Gets the conversion kind.
    /// </summary>
    /// <returns>The resulting unit conversion kind.</returns>
    public UnitConversionKind GetConversionKind();
    /// <summary>
    /// Reports whether the supplied unit has the same dimension vector.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsCompatibleWith(UnitDefinition? unit);
    /// <summary>
    /// Reports whether the supplied unit has the same dimension, scale, offset, and conversion kind.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsEquivalentTo(UnitDefinition? unit);
    /// <summary>
    /// Multiplies the supplied unit definitions and adds their dimension exponents and exact scales.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Multiply(UnitDefinition unit);
    /// <summary>
    /// Divides by the supplied unit definition and subtracts its dimension exponents and exact scale.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Divide(UnitDefinition unit);
    /// <summary>
    /// Creates the reciprocal unit by negating its dimension exponents and inverting its scale.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Inverse();
    /// <summary>
    /// Raises the exact scale and every dimension exponent to the supplied integer power.
    /// </summary>
    /// <param name="power">The integer exponent to apply.</param>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Pow(int power);
    /// <summary>
    /// Attempts to take an exact root of both the rational scale and every dimension exponent.
    /// </summary>
    /// <param name="root">The positive integer root to calculate.</param>
    /// <returns>A result that describes success or failure.</returns>
    public UnitRootResult Root(int root);
}
