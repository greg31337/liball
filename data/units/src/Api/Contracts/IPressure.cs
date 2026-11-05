namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="Pressure"/>.
/// </summary>
public interface IPressure
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
    /// Returns the stored pressure converted to pascals.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Pascals();
    /// <summary>
    /// Returns the stored pressure converted to kilopascals.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kilopascals();
    /// <summary>
    /// Returns the stored pressure converted to psi.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Psi();
    /// <summary>
    /// Converts this pressure to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting pressure.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Pressure ToUnit(UnitDefinition unit);
    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible pressure value to use.</param>
    /// <returns>The resulting pressure.</returns>
    public Pressure Add(Pressure value);
    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible pressure value to use.</param>
    /// <returns>The resulting pressure.</returns>
    public Pressure Subtract(Pressure value);
    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting pressure.</returns>
    public Pressure Scale(double factor);
    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting pressure.</returns>
    public Pressure Negate();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting pressure.</returns>
    public Pressure AbsoluteValue();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting pressure.</returns>
    public Pressure Abs();
    /// <summary>
    /// Multiplies this pressure by an area.
    /// </summary>
    /// <param name="value">The area to use.</param>
    /// <returns>The resulting force.</returns>
    public Force MultiplyByArea(Area value);
    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue();
}
