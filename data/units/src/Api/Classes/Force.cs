namespace units;

/// <summary>
/// Represents a force as a numeric value paired with a compatible runtime unit.
/// </summary>
public class Force : IForce
{
    internal QuantityValue _quantity;

    /// <summary>
    /// Initializes a new force from a numeric value and a compatible unit.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public Force(double value, UnitDefinition unit)
    {
        ValidateForceUnit(unit);
        _quantity = new QuantityValue(value, unit);
    }

    internal static Force FromNewtons(double value)
    {
        return new Force(value, new StandardUnits().Newton());
    }

    internal static Force FromKilonewtons(double value)
    {
        return new Force(value, KilonewtonUnit());
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
    /// Returns the stored force converted to newtons.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Newtons()
    {
        return _quantity.As(new StandardUnits().Newton());
    }

    /// <summary>
    /// Returns the stored force converted to kilonewtons.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kilonewtons()
    {
        return _quantity.As(KilonewtonUnit());
    }

    /// <summary>
    /// Converts this force to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting force.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Force ToUnit(UnitDefinition unit)
    {
        ValidateForceUnit(unit);
        QuantityValue converted = _quantity.ToUnit(unit);
        return new Force(converted.GetValue(), converted.GetUnit());
    }

    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible force value to use.</param>
    /// <returns>The resulting force.</returns>
    public Force Add(Force value)
    {
        ValidateForce(value, "value");
        QuantityValue result = new QuantityMath().Add(_quantity, value.ToQuantityValue());
        return new Force(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible force value to use.</param>
    /// <returns>The resulting force.</returns>
    public Force Subtract(Force value)
    {
        ValidateForce(value, "value");
        QuantityValue result = new QuantityMath().Subtract(_quantity, value.ToQuantityValue());
        return new Force(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting force.</returns>
    public Force Scale(double factor)
    {
        QuantityValue result = new QuantityMath().Scale(_quantity, factor);
        return new Force(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting force.</returns>
    public Force Negate()
    {
        QuantityValue result = new QuantityMath().Negate(_quantity);
        return new Force(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting force.</returns>
    public Force AbsoluteValue()
    {
        QuantityValue result = new QuantityMath().AbsoluteValue(_quantity);
        return new Force(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting force.</returns>
    public Force Abs()
    {
        return AbsoluteValue();
    }

    /// <summary>
    /// Multiplies this force by a length.
    /// </summary>
    /// <param name="value">The length to use.</param>
    /// <returns>The resulting energy.</returns>
    public Energy MultiplyByLength(Length value)
    {
        Length.ValidateLength(value, "value");
        QuantityValue result = new QuantityMath().Multiply(_quantity, value.ToQuantityValue());
        return new Energy(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Divides this force by an area.
    /// </summary>
    /// <param name="value">The area to use.</param>
    /// <returns>The resulting pressure.</returns>
    public Pressure DivideByArea(Area value)
    {
        Area.ValidateArea(value, "value");
        QuantityValue result = new QuantityMath().Divide(_quantity, value.ToQuantityValue());
        return new Pressure(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue()
    {
        return new QuantityValue(_quantity.GetValue(), _quantity.GetUnit());
    }

    internal static UnitDefinition KilonewtonUnit()
    {
        return new StandardUnits().Kilonewton();
    }

    internal static void ValidateForce(Force value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidateForceUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (!unit.GetDimension().EqualsDimension(new StandardUnits().Newton().GetDimension()))
        {
            throw new System.ArgumentException("Force units must use the force dimension.");
        }
    }
}
