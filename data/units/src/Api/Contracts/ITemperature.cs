namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="Temperature"/>.
/// </summary>
public interface ITemperature
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
    /// Returns the stored temperature converted to kelvins.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kelvins();
    /// <summary>
    /// Returns the stored temperature converted to celsius.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Celsius();
    /// <summary>
    /// Returns the stored temperature converted to fahrenheit.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Fahrenheit();
    /// <summary>
    /// Returns the stored temperature converted to rankine.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Rankine();
    /// <summary>
    /// Converts this temperature to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting temperature.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Temperature ToUnit(UnitDefinition unit);
    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible temperature interval value to use.</param>
    /// <returns>The resulting temperature.</returns>
    public Temperature Add(TemperatureInterval value);
    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible temperature interval value to use.</param>
    /// <returns>The resulting temperature.</returns>
    public Temperature Subtract(TemperatureInterval value);
    /// <summary>
    /// Returns the temperature interval between this point and the supplied point.
    /// </summary>
    /// <param name="value">The compatible temperature value to use.</param>
    /// <returns>The resulting temperature interval.</returns>
    public TemperatureInterval Difference(Temperature value);
    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue();
}
