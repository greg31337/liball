namespace units;

/// <summary>
/// Defines explicit built-in strategy metadata for one special unit.
/// </summary>
public interface ISpecialConversionDefinition
{
    /// <summary>
    /// Gets the special unit identifier.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetUnitId();
    /// <summary>
    /// Gets the built-in special conversion strategy.
    /// </summary>
    /// <returns>The resulting special conversion strategy.</returns>
    public SpecialConversionStrategy GetStrategy();
    /// <summary>
    /// Gets the positive reference value expressed in system units.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetReferenceSystemValue();
    /// <summary>
    /// Gets the nonzero logarithmic coefficient.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetCoefficient();
    /// <summary>
    /// Reports whether the supplied unit is the configured special unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool MatchesUnit(UnitDefinition? unit);
    /// <summary>
    /// Converts a special-unit value to its system value.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <returns>The requested numeric value.</returns>
    public double ConvertToSystemValue(double value);
    /// <summary>
    /// Converts a positive system value to its special-unit value.
    /// </summary>
    /// <param name="value">The positive system value to use.</param>
    /// <returns>The requested numeric value.</returns>
    /// <exception cref="System.ArgumentException">The system value is not positive.</exception>
    public double ConvertFromSystemValue(double value);
}
