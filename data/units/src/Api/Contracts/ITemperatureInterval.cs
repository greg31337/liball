namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="TemperatureInterval"/>.
/// </summary>
public interface ITemperatureInterval
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
    /// Returns the stored temperature interval converted to kelvins.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kelvins();
    /// <summary>
    /// Returns the stored temperature interval converted to celsius.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Celsius();
    /// <summary>
    /// Returns the stored temperature interval converted to fahrenheit.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Fahrenheit();
    /// <summary>
    /// Returns the stored temperature interval converted to rankine.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Rankine();
    /// <summary>
    /// Converts this temperature interval to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting temperature interval.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public TemperatureInterval ToUnit(UnitDefinition unit);
    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible temperature interval value to use.</param>
    /// <returns>The resulting temperature interval.</returns>
    public TemperatureInterval Add(TemperatureInterval value);
    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible temperature interval value to use.</param>
    /// <returns>The resulting temperature interval.</returns>
    public TemperatureInterval Subtract(TemperatureInterval value);
    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting temperature interval.</returns>
    public TemperatureInterval Scale(double factor);
    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting temperature interval.</returns>
    public TemperatureInterval Negate();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting temperature interval.</returns>
    public TemperatureInterval AbsoluteValue();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting temperature interval.</returns>
    public TemperatureInterval Abs();
    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue();
}
