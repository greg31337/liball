namespace units;

/// <summary>
/// Represents a length as a numeric value paired with a compatible runtime unit.
/// </summary>
public class Length : ILength
{
    internal QuantityValue _quantity;

    /// <summary>
    /// Initializes a new length from a numeric value and a compatible unit.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public Length(double value, UnitDefinition unit)
    {
        ValidateLengthUnit(unit);
        _quantity = new QuantityValue(value, unit);
    }

    internal static Length FromMeters(double value)
    {
        return new Length(value, new StandardUnits().Meter());
    }

    internal static Length FromKilometers(double value)
    {
        return new Length(value, KilometerUnit());
    }

    internal static Length FromFeet(double value)
    {
        return new Length(value, new StandardUnits().Foot());
    }

    internal static Length FromInches(double value)
    {
        return new Length(value, new StandardUnits().Inch());
    }

    internal static Length FromMiles(double value)
    {
        return new Length(value, new StandardUnits().Mile());
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
    /// Returns the stored length converted to meters.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Meters()
    {
        return _quantity.As(new StandardUnits().Meter());
    }

    /// <summary>
    /// Returns the stored length converted to kilometers.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kilometers()
    {
        return _quantity.As(KilometerUnit());
    }

    /// <summary>
    /// Returns the stored length converted to feet.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Feet()
    {
        return _quantity.As(new StandardUnits().Foot());
    }

    /// <summary>
    /// Returns the stored length converted to inches.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Inches()
    {
        return _quantity.As(new StandardUnits().Inch());
    }

    /// <summary>
    /// Returns the stored length converted to miles.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Miles()
    {
        return _quantity.As(new StandardUnits().Mile());
    }

    /// <summary>
    /// Converts this length to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting length.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Length ToUnit(UnitDefinition unit)
    {
        ValidateLengthUnit(unit);
        QuantityValue converted = _quantity.ToUnit(unit);
        return new Length(converted.GetValue(), converted.GetUnit());
    }

    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible length value to use.</param>
    /// <returns>The resulting length.</returns>
    public Length Add(Length value)
    {
        ValidateLength(value, "value");
        QuantityValue result = new QuantityMath().Add(_quantity, value.ToQuantityValue());
        return new Length(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible length value to use.</param>
    /// <returns>The resulting length.</returns>
    public Length Subtract(Length value)
    {
        ValidateLength(value, "value");
        QuantityValue result = new QuantityMath().Subtract(_quantity, value.ToQuantityValue());
        return new Length(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting length.</returns>
    public Length Scale(double factor)
    {
        QuantityValue result = new QuantityMath().Scale(_quantity, factor);
        return new Length(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting length.</returns>
    public Length Negate()
    {
        QuantityValue result = new QuantityMath().Negate(_quantity);
        return new Length(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting length.</returns>
    public Length AbsoluteValue()
    {
        QuantityValue result = new QuantityMath().AbsoluteValue(_quantity);
        return new Length(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting length.</returns>
    public Length Abs()
    {
        return AbsoluteValue();
    }

    /// <summary>
    /// Divides this length by a duration.
    /// </summary>
    /// <param name="value">The duration to use.</param>
    /// <returns>The resulting speed.</returns>
    public Speed DivideByDuration(Duration value)
    {
        Duration.ValidateDuration(value, "value");
        QuantityValue result = new QuantityMath().Divide(_quantity, value.ToQuantityValue());
        return new Speed(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Multiplies this length by another length.
    /// </summary>
    /// <param name="value">The length to use.</param>
    /// <returns>The resulting area.</returns>
    public Area MultiplyByLength(Length value)
    {
        ValidateLength(value, "value");
        QuantityValue result = new QuantityMath().Multiply(_quantity, value.ToQuantityValue());
        return new Area(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Raises this length to the second power.
    /// </summary>
    /// <returns>The resulting area.</returns>
    public Area Square()
    {
        QuantityValue result = new QuantityMath().Pow(_quantity, 2);
        return new Area(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Raises this length to the third power.
    /// </summary>
    /// <returns>The resulting volume.</returns>
    public Volume Cube()
    {
        QuantityValue result = new QuantityMath().Pow(_quantity, 3);
        return new Volume(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue()
    {
        return new QuantityValue(_quantity.GetValue(), _quantity.GetUnit());
    }

    internal static UnitDefinition KilometerUnit()
    {
        return new StandardUnits().Kilometer();
    }

    internal static void ValidateLength(Length value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidateLengthUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (!unit.GetDimension().EqualsDimension(new StandardDimensions().Length()))
        {
            throw new System.ArgumentException("Length units must use the length dimension.");
        }
    }
}
