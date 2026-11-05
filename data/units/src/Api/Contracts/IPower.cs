namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="Power"/>.
/// </summary>
public interface IPower
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
    /// Returns the stored power converted to watts.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Watts();
    /// <summary>
    /// Returns the stored power converted to kilowatts.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kilowatts();
    /// <summary>
    /// Returns the stored power converted to horsepower.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Horsepower();
    /// <summary>
    /// Converts this power to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting power.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Power ToUnit(UnitDefinition unit);
    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible power value to use.</param>
    /// <returns>The resulting power.</returns>
    public Power Add(Power value);
    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible power value to use.</param>
    /// <returns>The resulting power.</returns>
    public Power Subtract(Power value);
    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting power.</returns>
    public Power Scale(double factor);
    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting power.</returns>
    public Power Negate();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting power.</returns>
    public Power AbsoluteValue();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting power.</returns>
    public Power Abs();
    /// <summary>
    /// Multiplies this power by a duration.
    /// </summary>
    /// <param name="value">The duration to use.</param>
    /// <returns>The resulting energy.</returns>
    public Energy MultiplyByDuration(Duration value);
    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue();
}
