namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="Energy"/>.
/// </summary>
public interface IEnergy
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
    /// Returns the stored energy converted to joules.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Joules();
    /// <summary>
    /// Returns the stored energy converted to kilojoules.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kilojoules();
    /// <summary>
    /// Returns the stored energy converted to kilowatt hours.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double KilowattHours();
    /// <summary>
    /// Returns the stored energy converted to british thermal units.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double BritishThermalUnits();
    /// <summary>
    /// Returns the stored energy converted to electronvolts.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Electronvolts();
    /// <summary>
    /// Converts this energy to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting energy.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Energy ToUnit(UnitDefinition unit);
    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible energy value to use.</param>
    /// <returns>The resulting energy.</returns>
    public Energy Add(Energy value);
    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible energy value to use.</param>
    /// <returns>The resulting energy.</returns>
    public Energy Subtract(Energy value);
    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting energy.</returns>
    public Energy Scale(double factor);
    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting energy.</returns>
    public Energy Negate();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting energy.</returns>
    public Energy AbsoluteValue();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting energy.</returns>
    public Energy Abs();
    /// <summary>
    /// Divides this energy by a duration.
    /// </summary>
    /// <param name="value">The duration to use.</param>
    /// <returns>The resulting power.</returns>
    public Power DivideByDuration(Duration value);
    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue();
}
