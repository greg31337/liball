namespace units;

/// <summary>
/// Represents a power as a numeric value paired with a compatible runtime unit.
/// </summary>
public class Power : IPower
{
    internal QuantityValue _quantity;

    /// <summary>
    /// Initializes a new power from a numeric value and a compatible unit.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public Power(double value, UnitDefinition unit)
    {
        ValidatePowerUnit(unit);
        _quantity = new QuantityValue(value, unit);
    }

    internal static Power FromWatts(double value)
    {
        return new Power(value, new StandardUnits().Watt());
    }

    internal static Power FromKilowatts(double value)
    {
        return new Power(value, KilowattUnit());
    }

    internal static Power FromHorsepower(double value)
    {
        return new Power(value, new StandardUnits().Horsepower());
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
    /// Returns the stored power converted to watts.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Watts()
    {
        return _quantity.As(new StandardUnits().Watt());
    }

    /// <summary>
    /// Returns the stored power converted to kilowatts.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kilowatts()
    {
        return _quantity.As(KilowattUnit());
    }

    /// <summary>
    /// Returns the stored power converted to horsepower.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Horsepower()
    {
        return _quantity.As(new StandardUnits().Horsepower());
    }

    /// <summary>
    /// Converts this power to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting power.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Power ToUnit(UnitDefinition unit)
    {
        ValidatePowerUnit(unit);
        QuantityValue converted = _quantity.ToUnit(unit);
        return new Power(converted.GetValue(), converted.GetUnit());
    }

    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible power value to use.</param>
    /// <returns>The resulting power.</returns>
    public Power Add(Power value)
    {
        ValidatePower(value, "value");
        QuantityValue result = new QuantityMath().Add(_quantity, value.ToQuantityValue());
        return new Power(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible power value to use.</param>
    /// <returns>The resulting power.</returns>
    public Power Subtract(Power value)
    {
        ValidatePower(value, "value");
        QuantityValue result = new QuantityMath().Subtract(_quantity, value.ToQuantityValue());
        return new Power(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting power.</returns>
    public Power Scale(double factor)
    {
        QuantityValue result = new QuantityMath().Scale(_quantity, factor);
        return new Power(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting power.</returns>
    public Power Negate()
    {
        QuantityValue result = new QuantityMath().Negate(_quantity);
        return new Power(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting power.</returns>
    public Power AbsoluteValue()
    {
        QuantityValue result = new QuantityMath().AbsoluteValue(_quantity);
        return new Power(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting power.</returns>
    public Power Abs()
    {
        return AbsoluteValue();
    }

    /// <summary>
    /// Multiplies this power by a duration.
    /// </summary>
    /// <param name="value">The duration to use.</param>
    /// <returns>The resulting energy.</returns>
    public Energy MultiplyByDuration(Duration value)
    {
        Duration.ValidateDuration(value, "value");
        QuantityValue result = new QuantityMath().Multiply(_quantity, value.ToQuantityValue());
        return new Energy(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue()
    {
        return new QuantityValue(_quantity.GetValue(), _quantity.GetUnit());
    }

    internal static UnitDefinition KilowattUnit()
    {
        return new StandardUnits().Kilowatt();
    }

    internal static void ValidatePower(Power value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidatePowerUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (!unit.GetDimension().EqualsDimension(new StandardUnits().Watt().GetDimension()))
        {
            throw new System.ArgumentException("Power units must use the power dimension.");
        }
    }
}
