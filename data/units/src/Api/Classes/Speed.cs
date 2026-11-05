namespace units;

/// <summary>
/// Represents a speed as a numeric value paired with a compatible runtime unit.
/// </summary>
public class Speed : ISpeed
{
    internal QuantityValue _quantity;

    /// <summary>
    /// Initializes a new speed from a numeric value and a compatible unit.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public Speed(double value, UnitDefinition unit)
    {
        ValidateSpeedUnit(unit);
        _quantity = new QuantityValue(value, unit);
    }

    internal static Speed FromMetersPerSecond(double value)
    {
        return new Speed(value, MeterPerSecondUnit());
    }

    internal static Speed FromKilometersPerHour(double value)
    {
        return new Speed(value, KilometerPerHourUnit());
    }

    internal static Speed FromMilesPerHour(double value)
    {
        return new Speed(value, MilePerHourUnit());
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
    /// Returns the stored speed converted to meters per second.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double MetersPerSecond()
    {
        return _quantity.As(MeterPerSecondUnit());
    }

    /// <summary>
    /// Returns the stored speed converted to kilometers per hour.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double KilometersPerHour()
    {
        return _quantity.As(KilometerPerHourUnit());
    }

    /// <summary>
    /// Returns the stored speed converted to miles per hour.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double MilesPerHour()
    {
        return _quantity.As(MilePerHourUnit());
    }

    /// <summary>
    /// Converts this speed to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting speed.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Speed ToUnit(UnitDefinition unit)
    {
        ValidateSpeedUnit(unit);
        QuantityValue converted = _quantity.ToUnit(unit);
        return new Speed(converted.GetValue(), converted.GetUnit());
    }

    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible speed value to use.</param>
    /// <returns>The resulting speed.</returns>
    public Speed Add(Speed value)
    {
        ValidateSpeed(value, "value");
        QuantityValue result = new QuantityMath().Add(_quantity, value.ToQuantityValue());
        return new Speed(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible speed value to use.</param>
    /// <returns>The resulting speed.</returns>
    public Speed Subtract(Speed value)
    {
        ValidateSpeed(value, "value");
        QuantityValue result = new QuantityMath().Subtract(_quantity, value.ToQuantityValue());
        return new Speed(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting speed.</returns>
    public Speed Scale(double factor)
    {
        QuantityValue result = new QuantityMath().Scale(_quantity, factor);
        return new Speed(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting speed.</returns>
    public Speed Negate()
    {
        QuantityValue result = new QuantityMath().Negate(_quantity);
        return new Speed(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting speed.</returns>
    public Speed AbsoluteValue()
    {
        QuantityValue result = new QuantityMath().AbsoluteValue(_quantity);
        return new Speed(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting speed.</returns>
    public Speed Abs()
    {
        return AbsoluteValue();
    }

    /// <summary>
    /// Multiplies this speed by a duration.
    /// </summary>
    /// <param name="value">The duration to use.</param>
    /// <returns>The resulting length.</returns>
    public Length MultiplyByDuration(Duration value)
    {
        Duration.ValidateDuration(value, "value");
        QuantityValue result = new QuantityMath().Multiply(_quantity, value.ToQuantityValue());
        return new Length(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Divides this speed by a duration.
    /// </summary>
    /// <param name="value">The duration to use.</param>
    /// <returns>The resulting acceleration.</returns>
    public Acceleration DivideByDuration(Duration value)
    {
        Duration.ValidateDuration(value, "value");
        QuantityValue result = new QuantityMath().Divide(_quantity, value.ToQuantityValue());
        return new Acceleration(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue()
    {
        return new QuantityValue(_quantity.GetValue(), _quantity.GetUnit());
    }

    internal static UnitDefinition MeterPerSecondUnit()
    {
        return new StandardUnits().MeterPerSecond();
    }

    internal static UnitDefinition KilometerPerHourUnit()
    {
        return new StandardUnits().KilometerPerHour();
    }

    internal static UnitDefinition MilePerHourUnit()
    {
        return new StandardUnits().MilePerHour();
    }

    internal static UnitDefinition KilometerUnit()
    {
        return new StandardUnits().Kilometer();
    }

    internal static void ValidateSpeed(Speed value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidateSpeedUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (!unit.GetDimension().EqualsDimension(new StandardDimensions().Length().Divide(new StandardDimensions().Time())))
        {
            throw new System.ArgumentException("Speed units must use the speed dimension.");
        }
    }
}
