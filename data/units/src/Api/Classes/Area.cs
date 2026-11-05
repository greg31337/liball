namespace units;

/// <summary>
/// Represents an area as a numeric value paired with a compatible runtime unit.
/// </summary>
public class Area : IArea
{
    internal QuantityValue _quantity;

    /// <summary>
    /// Initializes a new area from a numeric value and a compatible unit.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public Area(double value, UnitDefinition unit)
    {
        ValidateAreaUnit(unit);
        _quantity = new QuantityValue(value, unit);
    }

    internal static Area FromSquareMeters(double value)
    {
        return new Area(value, SquareMeterUnit());
    }

    internal static Area FromHectares(double value)
    {
        return new Area(value, new StandardUnits().Hectare());
    }

    internal static Area FromAcres(double value)
    {
        return new Area(value, new StandardUnits().Acre());
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
    /// Returns the stored area converted to square meters.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double SquareMeters()
    {
        return _quantity.As(SquareMeterUnit());
    }

    /// <summary>
    /// Returns the stored area converted to hectares.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Hectares()
    {
        return _quantity.As(new StandardUnits().Hectare());
    }

    /// <summary>
    /// Returns the stored area converted to acres.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Acres()
    {
        return _quantity.As(new StandardUnits().Acre());
    }

    /// <summary>
    /// Converts this area to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting area.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Area ToUnit(UnitDefinition unit)
    {
        ValidateAreaUnit(unit);
        QuantityValue converted = _quantity.ToUnit(unit);
        return new Area(converted.GetValue(), converted.GetUnit());
    }

    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible area value to use.</param>
    /// <returns>The resulting area.</returns>
    public Area Add(Area value)
    {
        ValidateArea(value, "value");
        QuantityValue result = new QuantityMath().Add(_quantity, value.ToQuantityValue());
        return new Area(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible area value to use.</param>
    /// <returns>The resulting area.</returns>
    public Area Subtract(Area value)
    {
        ValidateArea(value, "value");
        QuantityValue result = new QuantityMath().Subtract(_quantity, value.ToQuantityValue());
        return new Area(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting area.</returns>
    public Area Scale(double factor)
    {
        QuantityValue result = new QuantityMath().Scale(_quantity, factor);
        return new Area(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting area.</returns>
    public Area Negate()
    {
        QuantityValue result = new QuantityMath().Negate(_quantity);
        return new Area(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting area.</returns>
    public Area AbsoluteValue()
    {
        QuantityValue result = new QuantityMath().AbsoluteValue(_quantity);
        return new Area(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting area.</returns>
    public Area Abs()
    {
        return AbsoluteValue();
    }

    /// <summary>
    /// Multiplies this area by a length.
    /// </summary>
    /// <param name="value">The length to use.</param>
    /// <returns>The resulting volume.</returns>
    public Volume MultiplyByLength(Length value)
    {
        Length.ValidateLength(value, "value");
        QuantityValue result = new QuantityMath().Multiply(_quantity, value.ToQuantityValue());
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

    internal static UnitDefinition SquareMeterUnit()
    {
        return new StandardUnits().SquareMeter();
    }

    internal static void ValidateArea(Area value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidateAreaUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (!unit.GetDimension().EqualsDimension(new StandardDimensions().Length().Pow(2)))
        {
            throw new System.ArgumentException("Area units must use the area dimension.");
        }
    }
}
