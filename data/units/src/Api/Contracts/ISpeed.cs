namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="Speed"/>.
/// </summary>
public interface ISpeed
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
    /// Returns the stored speed converted to meters per second.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double MetersPerSecond();
    /// <summary>
    /// Returns the stored speed converted to kilometers per hour.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double KilometersPerHour();
    /// <summary>
    /// Returns the stored speed converted to miles per hour.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double MilesPerHour();
    /// <summary>
    /// Converts this speed to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting speed.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Speed ToUnit(UnitDefinition unit);
    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible speed value to use.</param>
    /// <returns>The resulting speed.</returns>
    public Speed Add(Speed value);
    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible speed value to use.</param>
    /// <returns>The resulting speed.</returns>
    public Speed Subtract(Speed value);
    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting speed.</returns>
    public Speed Scale(double factor);
    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting speed.</returns>
    public Speed Negate();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting speed.</returns>
    public Speed AbsoluteValue();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting speed.</returns>
    public Speed Abs();
    /// <summary>
    /// Multiplies this speed by a duration.
    /// </summary>
    /// <param name="value">The duration to use.</param>
    /// <returns>The resulting length.</returns>
    public Length MultiplyByDuration(Duration value);
    /// <summary>
    /// Divides this speed by a duration.
    /// </summary>
    /// <param name="value">The duration to use.</param>
    /// <returns>The resulting acceleration.</returns>
    public Acceleration DivideByDuration(Duration value);
    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue();
}
