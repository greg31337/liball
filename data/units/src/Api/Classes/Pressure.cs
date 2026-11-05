namespace units;

/// <summary>
/// Represents a pressure as a numeric value paired with a compatible runtime unit.
/// </summary>
public class Pressure : IPressure
{
    internal QuantityValue _quantity;

    /// <summary>
    /// Initializes a new pressure from a numeric value and a compatible unit.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public Pressure(double value, UnitDefinition unit)
    {
        ValidatePressureUnit(unit);
        _quantity = new QuantityValue(value, unit);
    }

    internal static Pressure FromPascals(double value)
    {
        return new Pressure(value, new StandardUnits().Pascal());
    }

    internal static Pressure FromKilopascals(double value)
    {
        return new Pressure(value, KilopascalUnit());
    }

    internal static Pressure FromPsi(double value)
    {
        return new Pressure(value, new StandardUnits().Psi());
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
    /// Returns the stored pressure converted to pascals.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Pascals()
    {
        return _quantity.As(new StandardUnits().Pascal());
    }

    /// <summary>
    /// Returns the stored pressure converted to kilopascals.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kilopascals()
    {
        return _quantity.As(KilopascalUnit());
    }

    /// <summary>
    /// Returns the stored pressure converted to psi.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Psi()
    {
        return _quantity.As(new StandardUnits().Psi());
    }

    /// <summary>
    /// Converts this pressure to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting pressure.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Pressure ToUnit(UnitDefinition unit)
    {
        ValidatePressureUnit(unit);
        QuantityValue converted = _quantity.ToUnit(unit);
        return new Pressure(converted.GetValue(), converted.GetUnit());
    }

    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible pressure value to use.</param>
    /// <returns>The resulting pressure.</returns>
    public Pressure Add(Pressure value)
    {
        ValidatePressure(value, "value");
        QuantityValue result = new QuantityMath().Add(_quantity, value.ToQuantityValue());
        return new Pressure(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible pressure value to use.</param>
    /// <returns>The resulting pressure.</returns>
    public Pressure Subtract(Pressure value)
    {
        ValidatePressure(value, "value");
        QuantityValue result = new QuantityMath().Subtract(_quantity, value.ToQuantityValue());
        return new Pressure(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting pressure.</returns>
    public Pressure Scale(double factor)
    {
        QuantityValue result = new QuantityMath().Scale(_quantity, factor);
        return new Pressure(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting pressure.</returns>
    public Pressure Negate()
    {
        QuantityValue result = new QuantityMath().Negate(_quantity);
        return new Pressure(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting pressure.</returns>
    public Pressure AbsoluteValue()
    {
        QuantityValue result = new QuantityMath().AbsoluteValue(_quantity);
        return new Pressure(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting pressure.</returns>
    public Pressure Abs()
    {
        return AbsoluteValue();
    }

    /// <summary>
    /// Multiplies this pressure by an area.
    /// </summary>
    /// <param name="value">The area to use.</param>
    /// <returns>The resulting force.</returns>
    public Force MultiplyByArea(Area value)
    {
        Area.ValidateArea(value, "value");
        QuantityValue result = new QuantityMath().Multiply(_quantity, value.ToQuantityValue());
        return new Force(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue()
    {
        return new QuantityValue(_quantity.GetValue(), _quantity.GetUnit());
    }

    internal static UnitDefinition KilopascalUnit()
    {
        return new StandardUnits().Kilopascal();
    }

    internal static void ValidatePressure(Pressure value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidatePressureUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (!unit.GetDimension().EqualsDimension(new StandardUnits().Pascal().GetDimension()))
        {
            throw new System.ArgumentException("Pressure units must use the pressure dimension.");
        }
    }
}
