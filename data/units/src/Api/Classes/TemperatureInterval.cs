namespace units;

/// <summary>
/// Represents a temperature interval as a numeric value paired with a compatible runtime unit.
/// </summary>
/// <remarks>
/// This type represents a temperature difference, so conversions use scale only and do not apply the offsets used for temperature points.
/// </remarks>
public class TemperatureInterval : ITemperatureInterval
{
    internal QuantityValue _quantity;

    /// <summary>
    /// Initializes a new temperature interval from a numeric value and a compatible unit.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public TemperatureInterval(double value, UnitDefinition unit)
    {
        ValidateTemperatureIntervalUnit(unit);
        _quantity = new QuantityValue(value, unit);
    }

    internal static TemperatureInterval FromKelvins(double value)
    {
        return new TemperatureInterval(value, KelvinUnit());
    }

    internal static TemperatureInterval FromCelsius(double value)
    {
        return new TemperatureInterval(value, CelsiusUnit());
    }

    internal static TemperatureInterval FromFahrenheit(double value)
    {
        return new TemperatureInterval(value, FahrenheitUnit());
    }

    internal static TemperatureInterval FromRankine(double value)
    {
        return new TemperatureInterval(value, RankineUnit());
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
    /// Returns the stored temperature interval converted to kelvins.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kelvins()
    {
        return _quantity.As(KelvinUnit());
    }

    /// <summary>
    /// Returns the stored temperature interval converted to celsius.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Celsius()
    {
        return _quantity.As(CelsiusUnit());
    }

    /// <summary>
    /// Returns the stored temperature interval converted to fahrenheit.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Fahrenheit()
    {
        return _quantity.As(FahrenheitUnit());
    }

    /// <summary>
    /// Returns the stored temperature interval converted to rankine.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Rankine()
    {
        return _quantity.As(RankineUnit());
    }

    /// <summary>
    /// Converts this temperature interval to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting temperature interval.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public TemperatureInterval ToUnit(UnitDefinition unit)
    {
        ValidateTemperatureIntervalUnit(unit);
        QuantityValue converted = _quantity.ToUnit(unit);
        return new TemperatureInterval(converted.GetValue(), converted.GetUnit());
    }

    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible temperature interval value to use.</param>
    /// <returns>The resulting temperature interval.</returns>
    public TemperatureInterval Add(TemperatureInterval value)
    {
        ValidateTemperatureInterval(value, "value");
        QuantityValue result = new QuantityMath().Add(_quantity, value.ToQuantityValue());
        return new TemperatureInterval(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible temperature interval value to use.</param>
    /// <returns>The resulting temperature interval.</returns>
    public TemperatureInterval Subtract(TemperatureInterval value)
    {
        ValidateTemperatureInterval(value, "value");
        QuantityValue result = new QuantityMath().Subtract(_quantity, value.ToQuantityValue());
        return new TemperatureInterval(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting temperature interval.</returns>
    public TemperatureInterval Scale(double factor)
    {
        QuantityValue result = new QuantityMath().Scale(_quantity, factor);
        return new TemperatureInterval(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting temperature interval.</returns>
    public TemperatureInterval Negate()
    {
        QuantityValue result = new QuantityMath().Negate(_quantity);
        return new TemperatureInterval(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting temperature interval.</returns>
    public TemperatureInterval AbsoluteValue()
    {
        QuantityValue result = new QuantityMath().AbsoluteValue(_quantity);
        return new TemperatureInterval(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting temperature interval.</returns>
    public TemperatureInterval Abs()
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

    internal static UnitDefinition KelvinUnit()
    {
        return new StandardUnits().KelvinInterval();
    }

    internal static UnitDefinition CelsiusUnit()
    {
        return new StandardUnits().CelsiusInterval();
    }

    internal static UnitDefinition FahrenheitUnit()
    {
        return new StandardUnits().FahrenheitInterval();
    }

    internal static UnitDefinition RankineUnit()
    {
        return new StandardUnits().RankineInterval();
    }

    internal static void ValidateTemperatureInterval(TemperatureInterval value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidateTemperatureIntervalUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (!unit.GetDimension().EqualsDimension(new StandardDimensions().ThermodynamicTemperature()))
        {
            throw new System.ArgumentException("Temperature interval units must use the temperature dimension.");
        }

        if (unit.GetConversionKind() != UnitConversionKind.Scale || !unit.GetOffsetToSystemUnit().IsZero())
        {
            throw new System.ArgumentException("Temperature interval units must be scale-only interval units.");
        }
    }
}
