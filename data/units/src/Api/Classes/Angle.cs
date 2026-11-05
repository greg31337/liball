namespace units;

/// <summary>
/// Represents an angle as a numeric value paired with a compatible runtime unit.
/// </summary>
public class Angle : IAngle
{
    internal QuantityValue _quantity;

    /// <summary>
    /// Initializes a new angle from a numeric value and a compatible unit.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public Angle(double value, UnitDefinition unit)
    {
        ValidateAngleUnit(unit);
        _quantity = new QuantityValue(value, unit);
    }

    internal static Angle FromRadians(double value)
    {
        return new Angle(value, new StandardUnits().Radian());
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
    /// Returns the stored angle converted to radians.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Radians()
    {
        return _quantity.As(new StandardUnits().Radian());
    }

    /// <summary>
    /// Converts this angle to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting angle.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Angle ToUnit(UnitDefinition unit)
    {
        ValidateAngleUnit(unit);
        QuantityValue converted = _quantity.ToUnit(unit);
        return new Angle(converted.GetValue(), converted.GetUnit());
    }

    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible angle value to use.</param>
    /// <returns>The resulting angle.</returns>
    public Angle Add(Angle value)
    {
        ValidateAngle(value, "value");
        QuantityValue result = new QuantityMath().Add(_quantity, value.ToQuantityValue());
        return new Angle(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible angle value to use.</param>
    /// <returns>The resulting angle.</returns>
    public Angle Subtract(Angle value)
    {
        ValidateAngle(value, "value");
        QuantityValue result = new QuantityMath().Subtract(_quantity, value.ToQuantityValue());
        return new Angle(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting angle.</returns>
    public Angle Scale(double factor)
    {
        QuantityValue result = new QuantityMath().Scale(_quantity, factor);
        return new Angle(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting angle.</returns>
    public Angle Negate()
    {
        QuantityValue result = new QuantityMath().Negate(_quantity);
        return new Angle(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting angle.</returns>
    public Angle AbsoluteValue()
    {
        QuantityValue result = new QuantityMath().AbsoluteValue(_quantity);
        return new Angle(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting angle.</returns>
    public Angle Abs()
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

    internal static void ValidateAngle(Angle value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidateAngleUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (!unit.GetDimension().EqualsDimension(new StandardDimensions().PlaneAngle()))
        {
            throw new System.ArgumentException("Angle units must use the plane angle dimension.");
        }
    }
}
