namespace units;

/// <summary>
/// Represents an acceleration as a numeric value paired with a compatible runtime unit.
/// </summary>
public class Acceleration : IAcceleration
{
    internal QuantityValue _quantity;

    /// <summary>
    /// Initializes a new acceleration from a numeric value and a compatible unit.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public Acceleration(double value, UnitDefinition unit)
    {
        ValidateAccelerationUnit(unit);
        _quantity = new QuantityValue(value, unit);
    }

    internal static Acceleration FromMetersPerSecondSquared(double value)
    {
        return new Acceleration(value, MeterPerSecondSquaredUnit());
    }

    internal static Acceleration FromFeetPerSecondSquared(double value)
    {
        return new Acceleration(value, FootPerSecondSquaredUnit());
    }

    /// <summary>
    /// Gets the numeric value expressed in the stored unit.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double GetValue()
    {
        return _quantity.GetValue();
    }

    /// <summary>
    /// Gets the unit paired with the stored numeric value.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition GetUnit()
    {
        return _quantity.GetUnit();
    }

    /// <summary>
    /// Returns the stored acceleration converted to meters per second squared.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double MetersPerSecondSquared()
    {
        return _quantity.As(MeterPerSecondSquaredUnit());
    }

    /// <summary>
    /// Returns the stored acceleration converted to feet per second squared.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double FeetPerSecondSquared()
    {
        return _quantity.As(FootPerSecondSquaredUnit());
    }

    /// <summary>
    /// Converts this acceleration to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting acceleration.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Acceleration ToUnit(UnitDefinition unit)
    {
        ValidateAccelerationUnit(unit);
        QuantityValue converted = _quantity.ToUnit(unit);
        return new Acceleration(converted.GetValue(), converted.GetUnit());
    }

    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible acceleration value to use.</param>
    /// <returns>The resulting acceleration.</returns>
    public Acceleration Add(Acceleration value)
    {
        ValidateAcceleration(value, "value");
        QuantityValue result = new QuantityMath().Add(_quantity, value.ToQuantityValue());
        return new Acceleration(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible acceleration value to use.</param>
    /// <returns>The resulting acceleration.</returns>
    public Acceleration Subtract(Acceleration value)
    {
        ValidateAcceleration(value, "value");
        QuantityValue result = new QuantityMath().Subtract(_quantity, value.ToQuantityValue());
        return new Acceleration(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting acceleration.</returns>
    public Acceleration Scale(double factor)
    {
        QuantityValue result = new QuantityMath().Scale(_quantity, factor);
        return new Acceleration(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting acceleration.</returns>
    public Acceleration Negate()
    {
        QuantityValue result = new QuantityMath().Negate(_quantity);
        return new Acceleration(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting acceleration.</returns>
    public Acceleration AbsoluteValue()
    {
        QuantityValue result = new QuantityMath().AbsoluteValue(_quantity);
        return new Acceleration(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting acceleration.</returns>
    public Acceleration Abs()
    {
        return AbsoluteValue();
    }

    /// <summary>
    /// Multiplies this acceleration by a duration.
    /// </summary>
    /// <param name="value">The duration to use.</param>
    /// <returns>The resulting speed.</returns>
    public Speed MultiplyByDuration(Duration value)
    {
        Duration.ValidateDuration(value, "value");
        QuantityValue result = new QuantityMath().Multiply(_quantity, value.ToQuantityValue());
        return new Speed(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue()
    {
        return new QuantityValue(_quantity.GetValue(), _quantity.GetUnit());
    }

    internal static UnitDefinition MeterPerSecondSquaredUnit()
    {
        return new StandardUnits().MeterPerSecondSquared();
    }

    internal static UnitDefinition FootPerSecondSquaredUnit()
    {
        return new StandardUnits().FootPerSecondSquared();
    }

    internal static void ValidateAcceleration(Acceleration value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidateAccelerationUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (!unit.GetDimension().EqualsDimension(new StandardDimensions().Length().Divide(new StandardDimensions().Time().Pow(2))))
        {
            throw new System.ArgumentException("Acceleration units must use the acceleration dimension.");
        }
    }
}
