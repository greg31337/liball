namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="Duration"/>.
/// </summary>
public interface IDuration
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
    /// Returns the stored duration converted to seconds.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Seconds();
    /// <summary>
    /// Returns the stored duration converted to minutes.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Minutes();
    /// <summary>
    /// Returns the stored duration converted to hours.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Hours();
    /// <summary>
    /// Returns the stored duration converted to days.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Days();
    /// <summary>
    /// Converts this duration to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting duration.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Duration ToUnit(UnitDefinition unit);
    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible duration value to use.</param>
    /// <returns>The resulting duration.</returns>
    public Duration Add(Duration value);
    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible duration value to use.</param>
    /// <returns>The resulting duration.</returns>
    public Duration Subtract(Duration value);
    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting duration.</returns>
    public Duration Scale(double factor);
    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting duration.</returns>
    public Duration Negate();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting duration.</returns>
    public Duration AbsoluteValue();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting duration.</returns>
    public Duration Abs();
    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue();
}
