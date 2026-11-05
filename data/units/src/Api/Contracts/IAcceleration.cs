namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="Acceleration"/>.
/// </summary>
public interface IAcceleration
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
    /// Returns the stored acceleration converted to meters per second squared.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double MetersPerSecondSquared();
    /// <summary>
    /// Returns the stored acceleration converted to feet per second squared.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double FeetPerSecondSquared();
    /// <summary>
    /// Converts this acceleration to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting acceleration.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Acceleration ToUnit(UnitDefinition unit);
    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible acceleration value to use.</param>
    /// <returns>The resulting acceleration.</returns>
    public Acceleration Add(Acceleration value);
    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible acceleration value to use.</param>
    /// <returns>The resulting acceleration.</returns>
    public Acceleration Subtract(Acceleration value);
    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting acceleration.</returns>
    public Acceleration Scale(double factor);
    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting acceleration.</returns>
    public Acceleration Negate();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting acceleration.</returns>
    public Acceleration AbsoluteValue();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting acceleration.</returns>
    public Acceleration Abs();
    /// <summary>
    /// Multiplies this acceleration by a duration.
    /// </summary>
    /// <param name="value">The duration to use.</param>
    /// <returns>The resulting speed.</returns>
    public Speed MultiplyByDuration(Duration value);
    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue();
}
