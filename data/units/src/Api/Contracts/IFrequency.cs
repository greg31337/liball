namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="Frequency"/>.
/// </summary>
public interface IFrequency
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
    /// Returns the stored frequency converted to hertz.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Hertz();
    /// <summary>
    /// Returns the stored frequency converted to kilohertz.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kilohertz();
    /// <summary>
    /// Returns the stored frequency converted to megahertz.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Megahertz();
    /// <summary>
    /// Converts this frequency to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting frequency.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Frequency ToUnit(UnitDefinition unit);
    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible frequency value to use.</param>
    /// <returns>The resulting frequency.</returns>
    public Frequency Add(Frequency value);
    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible frequency value to use.</param>
    /// <returns>The resulting frequency.</returns>
    public Frequency Subtract(Frequency value);
    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting frequency.</returns>
    public Frequency Scale(double factor);
    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting frequency.</returns>
    public Frequency Negate();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting frequency.</returns>
    public Frequency AbsoluteValue();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting frequency.</returns>
    public Frequency Abs();
    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue();
}
