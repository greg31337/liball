namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="DataSize"/>.
/// </summary>
public interface IDataSize
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
    /// Returns the stored data size converted to bits.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Bits();
    /// <summary>
    /// Returns the stored data size converted to bytes.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Bytes();
    /// <summary>
    /// Returns the stored data size converted to kilobytes.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kilobytes();
    /// <summary>
    /// Returns the stored data size converted to kibibytes.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kibibytes();
    /// <summary>
    /// Returns the stored data size converted to megabytes.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Megabytes();
    /// <summary>
    /// Returns the stored data size converted to mebibytes.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Mebibytes();
    /// <summary>
    /// Converts this data size to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting data size.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public DataSize ToUnit(UnitDefinition unit);
    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible data size value to use.</param>
    /// <returns>The resulting data size.</returns>
    public DataSize Add(DataSize value);
    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible data size value to use.</param>
    /// <returns>The resulting data size.</returns>
    public DataSize Subtract(DataSize value);
    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting data size.</returns>
    public DataSize Scale(double factor);
    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting data size.</returns>
    public DataSize Negate();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting data size.</returns>
    public DataSize AbsoluteValue();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting data size.</returns>
    public DataSize Abs();
    /// <summary>
    /// Divides this data size by a duration.
    /// </summary>
    /// <param name="value">The duration to use.</param>
    /// <returns>The resulting data rate.</returns>
    public DataRate DivideByDuration(Duration value);
    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue();
}
