namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="Force"/>.
/// </summary>
public interface IForce
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
    /// Returns the stored force converted to newtons.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Newtons();
    /// <summary>
    /// Returns the stored force converted to kilonewtons.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kilonewtons();
    /// <summary>
    /// Converts this force to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting force.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Force ToUnit(UnitDefinition unit);
    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible force value to use.</param>
    /// <returns>The resulting force.</returns>
    public Force Add(Force value);
    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible force value to use.</param>
    /// <returns>The resulting force.</returns>
    public Force Subtract(Force value);
    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting force.</returns>
    public Force Scale(double factor);
    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting force.</returns>
    public Force Negate();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting force.</returns>
    public Force AbsoluteValue();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting force.</returns>
    public Force Abs();
    /// <summary>
    /// Multiplies this force by a length.
    /// </summary>
    /// <param name="value">The length to use.</param>
    /// <returns>The resulting energy.</returns>
    public Energy MultiplyByLength(Length value);
    /// <summary>
    /// Divides this force by an area.
    /// </summary>
    /// <param name="value">The area to use.</param>
    /// <returns>The resulting pressure.</returns>
    public Pressure DivideByArea(Area value);
    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue();
}
