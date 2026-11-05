namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="Length"/>.
/// </summary>
public interface ILength
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
    /// Returns the stored length converted to meters.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Meters();
    /// <summary>
    /// Returns the stored length converted to kilometers.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kilometers();
    /// <summary>
    /// Returns the stored length converted to feet.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Feet();
    /// <summary>
    /// Returns the stored length converted to inches.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Inches();
    /// <summary>
    /// Returns the stored length converted to miles.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Miles();
    /// <summary>
    /// Converts this length to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting length.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Length ToUnit(UnitDefinition unit);
    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible length value to use.</param>
    /// <returns>The resulting length.</returns>
    public Length Add(Length value);
    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible length value to use.</param>
    /// <returns>The resulting length.</returns>
    public Length Subtract(Length value);
    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting length.</returns>
    public Length Scale(double factor);
    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting length.</returns>
    public Length Negate();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting length.</returns>
    public Length AbsoluteValue();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting length.</returns>
    public Length Abs();
    /// <summary>
    /// Divides this length by a duration.
    /// </summary>
    /// <param name="value">The duration to use.</param>
    /// <returns>The resulting speed.</returns>
    public Speed DivideByDuration(Duration value);
    /// <summary>
    /// Multiplies this length by another length.
    /// </summary>
    /// <param name="value">The length to use.</param>
    /// <returns>The resulting area.</returns>
    public Area MultiplyByLength(Length value);
    /// <summary>
    /// Raises this length to the second power.
    /// </summary>
    /// <returns>The resulting area.</returns>
    public Area Square();
    /// <summary>
    /// Raises this length to the third power.
    /// </summary>
    /// <returns>The resulting volume.</returns>
    public Volume Cube();
    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue();
}
