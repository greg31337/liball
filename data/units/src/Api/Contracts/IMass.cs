namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="Mass"/>.
/// </summary>
public interface IMass
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
    /// Returns the stored mass converted to kilograms.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kilograms();
    /// <summary>
    /// Returns the stored mass converted to grams.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Grams();
    /// <summary>
    /// Returns the stored mass converted to tonnes.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Tonnes();
    /// <summary>
    /// Returns the stored mass converted to pounds.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Pounds();
    /// <summary>
    /// Returns the stored mass converted to ounces.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Ounces();
    /// <summary>
    /// Converts this mass to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting mass.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Mass ToUnit(UnitDefinition unit);
    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible mass value to use.</param>
    /// <returns>The resulting mass.</returns>
    public Mass Add(Mass value);
    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible mass value to use.</param>
    /// <returns>The resulting mass.</returns>
    public Mass Subtract(Mass value);
    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting mass.</returns>
    public Mass Scale(double factor);
    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting mass.</returns>
    public Mass Negate();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting mass.</returns>
    public Mass AbsoluteValue();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting mass.</returns>
    public Mass Abs();
    /// <summary>
    /// Multiplies this mass by an acceleration.
    /// </summary>
    /// <param name="value">The acceleration to use.</param>
    /// <returns>The resulting force.</returns>
    public Force MultiplyByAcceleration(Acceleration value);
    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue();
}
