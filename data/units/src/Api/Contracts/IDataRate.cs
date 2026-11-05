namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="DataRate"/>.
/// </summary>
public interface IDataRate
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
    /// Returns the stored data rate converted to bits per second.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double BitsPerSecond();
    /// <summary>
    /// Returns the stored data rate converted to bytes per second.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double BytesPerSecond();
    /// <summary>
    /// Returns the stored data rate converted to kilobytes per second.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double KilobytesPerSecond();
    /// <summary>
    /// Returns the stored data rate converted to kibibytes per second.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double KibibytesPerSecond();
    /// <summary>
    /// Returns the stored data rate converted to megabytes per second.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double MegabytesPerSecond();
    /// <summary>
    /// Returns the stored data rate converted to mebibytes per second.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double MebibytesPerSecond();
    /// <summary>
    /// Converts this data rate to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting data rate.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public DataRate ToUnit(UnitDefinition unit);
    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible data rate value to use.</param>
    /// <returns>The resulting data rate.</returns>
    public DataRate Add(DataRate value);
    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible data rate value to use.</param>
    /// <returns>The resulting data rate.</returns>
    public DataRate Subtract(DataRate value);
    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting data rate.</returns>
    public DataRate Scale(double factor);
    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting data rate.</returns>
    public DataRate Negate();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting data rate.</returns>
    public DataRate AbsoluteValue();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting data rate.</returns>
    public DataRate Abs();
    /// <summary>
    /// Multiplies this data rate by a duration.
    /// </summary>
    /// <param name="value">The duration to use.</param>
    /// <returns>The resulting data size.</returns>
    public DataSize MultiplyByDuration(Duration value);
    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue();
}
