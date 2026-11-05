namespace units;

/// <summary>
/// Represents a duration as a numeric value paired with a compatible runtime unit.
/// </summary>
public class Duration : IDuration
{
    internal QuantityValue _quantity;

    /// <summary>
    /// Initializes a new duration from a numeric value and a compatible unit.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public Duration(double value, UnitDefinition unit)
    {
        ValidateDurationUnit(unit);
        _quantity = new QuantityValue(value, unit);
    }

    internal static Duration FromSeconds(double value)
    {
        return new Duration(value, new StandardUnits().Second());
    }

    internal static Duration FromMinutes(double value)
    {
        return new Duration(value, new StandardUnits().Minute());
    }

    internal static Duration FromHours(double value)
    {
        return new Duration(value, new StandardUnits().Hour());
    }

    internal static Duration FromDays(double value)
    {
        return new Duration(value, new StandardUnits().Day());
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
    /// Returns the stored duration converted to seconds.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Seconds()
    {
        return _quantity.As(new StandardUnits().Second());
    }

    /// <summary>
    /// Returns the stored duration converted to minutes.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Minutes()
    {
        return _quantity.As(new StandardUnits().Minute());
    }

    /// <summary>
    /// Returns the stored duration converted to hours.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Hours()
    {
        return _quantity.As(new StandardUnits().Hour());
    }

    /// <summary>
    /// Returns the stored duration converted to days.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Days()
    {
        return _quantity.As(new StandardUnits().Day());
    }

    /// <summary>
    /// Converts this duration to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting duration.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Duration ToUnit(UnitDefinition unit)
    {
        ValidateDurationUnit(unit);
        QuantityValue converted = _quantity.ToUnit(unit);
        return new Duration(converted.GetValue(), converted.GetUnit());
    }

    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible duration value to use.</param>
    /// <returns>The resulting duration.</returns>
    public Duration Add(Duration value)
    {
        ValidateDuration(value, "value");
        QuantityValue result = new QuantityMath().Add(_quantity, value.ToQuantityValue());
        return new Duration(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible duration value to use.</param>
    /// <returns>The resulting duration.</returns>
    public Duration Subtract(Duration value)
    {
        ValidateDuration(value, "value");
        QuantityValue result = new QuantityMath().Subtract(_quantity, value.ToQuantityValue());
        return new Duration(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting duration.</returns>
    public Duration Scale(double factor)
    {
        QuantityValue result = new QuantityMath().Scale(_quantity, factor);
        return new Duration(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting duration.</returns>
    public Duration Negate()
    {
        QuantityValue result = new QuantityMath().Negate(_quantity);
        return new Duration(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting duration.</returns>
    public Duration AbsoluteValue()
    {
        QuantityValue result = new QuantityMath().AbsoluteValue(_quantity);
        return new Duration(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting duration.</returns>
    public Duration Abs()
    {
        return AbsoluteValue();
    }

    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue()
    {
        return new QuantityValue(_quantity.GetValue(), _quantity.GetUnit());
    }

    internal static void ValidateDuration(Duration value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidateDurationUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (!unit.GetDimension().EqualsDimension(new StandardDimensions().Time()))
        {
            throw new System.ArgumentException("Duration units must use the time dimension.");
        }
    }
}
