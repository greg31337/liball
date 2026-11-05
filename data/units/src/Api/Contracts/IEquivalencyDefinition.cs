namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="EquivalencyDefinition"/>.
/// </summary>
public interface IEquivalencyDefinition
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
    /// Gets the description.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetDescription();
    /// <summary>
    /// Gets the source quantity kind id.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetSourceQuantityKindId();
    /// <summary>
    /// Gets the source dimension.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector GetSourceDimension();
    /// <summary>
    /// Gets the target quantity kind id.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetTargetQuantityKindId();
    /// <summary>
    /// Gets the target dimension.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector GetTargetDimension();
    /// <summary>
    /// Gets the forward scale.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetForwardScale();
    /// <summary>
    /// Gets the forward power.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetForwardPower();
    /// <summary>
    /// Gets the reverse scale.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetReverseScale();
    /// <summary>
    /// Gets the reverse power.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetReversePower();
    /// <summary>
    /// Returns a copy of the aliases.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public string[] GetAliases();
    /// <summary>
    /// Reports whether the supplied text matches the definition's identity or aliases.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool MatchesText(string? text);
    /// <summary>
    /// Reports whether the supplied source and target units match the forward direction.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool CanConvertForward(UnitDefinition? fromUnit, UnitDefinition? toUnit);
    /// <summary>
    /// Reports whether the supplied source and target units match the reverse direction.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool CanConvertReverse(UnitDefinition? fromUnit, UnitDefinition? toUnit);
    /// <summary>
    /// Reports whether the supplied units can be converted with the selected equivalency.
    /// </summary>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool CanConvert(UnitDefinition? fromUnit, UnitDefinition? toUnit);
    /// <summary>
    /// Converts a source system value with the forward equivalency formula.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <returns>The requested numeric value.</returns>
    public double ConvertForwardSystemValue(double value);
    /// <summary>
    /// Converts a target system value with the reverse equivalency formula.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <returns>The requested numeric value.</returns>
    public double ConvertReverseSystemValue(double value);
    /// <summary>
    /// Selects and applies the forward or reverse equivalency formula for the supplied units.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="fromUnit">The source unit definition.</param>
    /// <param name="toUnit">The target unit definition.</param>
    /// <returns>The requested numeric value.</returns>
    public double ConvertSystemValue(double value, UnitDefinition fromUnit, UnitDefinition toUnit);
}
