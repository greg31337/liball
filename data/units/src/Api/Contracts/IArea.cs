namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="Area"/>.
/// </summary>
public interface IArea
{
    /// <summary>
    /// Gets the numeric value expressed in the stored unit.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double GetValue();
    /// <summary>
    /// Gets the unit paired with the stored numeric value.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition GetUnit();
    /// <summary>
    /// Returns the stored area converted to square meters.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double SquareMeters();
    /// <summary>
    /// Returns the stored area converted to hectares.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Hectares();
    /// <summary>
    /// Returns the stored area converted to acres.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Acres();
    /// <summary>
    /// Converts this area to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting area.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Area ToUnit(UnitDefinition unit);
    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible area value to use.</param>
    /// <returns>The resulting area.</returns>
    public Area Add(Area value);
    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible area value to use.</param>
    /// <returns>The resulting area.</returns>
    public Area Subtract(Area value);
    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting area.</returns>
    public Area Scale(double factor);
    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting area.</returns>
    public Area Negate();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting area.</returns>
    public Area AbsoluteValue();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting area.</returns>
    public Area Abs();
    /// <summary>
    /// Multiplies this area by a length.
    /// </summary>
    /// <param name="value">The length to use.</param>
    /// <returns>The resulting volume.</returns>
    public Volume MultiplyByLength(Length value);
    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue();
}
