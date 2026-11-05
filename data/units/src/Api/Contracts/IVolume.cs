namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="Volume"/>.
/// </summary>
public interface IVolume
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
    /// Returns the stored volume converted to cubic meters.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double CubicMeters();
    /// <summary>
    /// Returns the stored volume converted to liters.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Liters();
    /// <summary>
    /// Returns the stored volume converted to gallons.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Gallons();
    /// <summary>
    /// Returns the stored volume converted to quarts.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Quarts();
    /// <summary>
    /// Returns the stored volume converted to pints.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Pints();
    /// <summary>
    /// Returns the stored volume converted to cups.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Cups();
    /// <summary>
    /// Converts this volume to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting volume.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Volume ToUnit(UnitDefinition unit);
    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible volume value to use.</param>
    /// <returns>The resulting volume.</returns>
    public Volume Add(Volume value);
    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible volume value to use.</param>
    /// <returns>The resulting volume.</returns>
    public Volume Subtract(Volume value);
    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting volume.</returns>
    public Volume Scale(double factor);
    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting volume.</returns>
    public Volume Negate();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting volume.</returns>
    public Volume AbsoluteValue();
    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting volume.</returns>
    public Volume Abs();
    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue();
}
