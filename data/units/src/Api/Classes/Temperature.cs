namespace units;

/// <summary>
/// Represents a temperature as a numeric value paired with a compatible runtime unit.
/// </summary>
/// <remarks>
/// This type represents a temperature point. Adding or subtracting a TemperatureInterval preserves the point semantics, while Difference produces an interval.
/// </remarks>
public class Temperature : ITemperature
{
    internal QuantityValue _quantity;

    /// <summary>
    /// Initializes a new temperature from a numeric value and a compatible unit.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public Temperature(double value, UnitDefinition unit)
    {
        ValidateTemperatureUnit(unit);
        _quantity = new QuantityValue(value, unit);
    }

    internal static Temperature FromKelvins(double value)
    {
        return new Temperature(value, new StandardUnits().Kelvin());
    }

    internal static Temperature FromCelsius(double value)
    {
        return new Temperature(value, CelsiusUnit());
    }

    internal static Temperature FromFahrenheit(double value)
    {
        return new Temperature(value, FahrenheitUnit());
    }

    internal static Temperature FromRankine(double value)
    {
        return new Temperature(value, RankineUnit());
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
    /// Returns the stored temperature converted to kelvins.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kelvins()
    {
        return _quantity.As(new StandardUnits().Kelvin());
    }

    /// <summary>
    /// Returns the stored temperature converted to celsius.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Celsius()
    {
        return _quantity.As(CelsiusUnit());
    }

    /// <summary>
    /// Returns the stored temperature converted to fahrenheit.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Fahrenheit()
    {
        return _quantity.As(FahrenheitUnit());
    }

    /// <summary>
    /// Returns the stored temperature converted to rankine.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Rankine()
    {
        return _quantity.As(RankineUnit());
    }

    /// <summary>
    /// Converts this temperature to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting temperature.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Temperature ToUnit(UnitDefinition unit)
    {
        ValidateTemperatureUnit(unit);
        QuantityValue converted = _quantity.ToUnit(unit);
        return new Temperature(converted.GetValue(), converted.GetUnit());
    }

    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible temperature interval value to use.</param>
    /// <returns>The resulting temperature.</returns>
    public Temperature Add(TemperatureInterval value)
    {
        ValidateTemperatureInterval(value, "value");
        Temperature result = new Temperature(Kelvins() + value.Kelvins(), new StandardUnits().Kelvin());
        return result.ToUnit(_quantity.GetUnit());
    }

    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible temperature interval value to use.</param>
    /// <returns>The resulting temperature.</returns>
    public Temperature Subtract(TemperatureInterval value)
    {
        ValidateTemperatureInterval(value, "value");
        Temperature result = new Temperature(Kelvins() - value.Kelvins(), new StandardUnits().Kelvin());
        return result.ToUnit(_quantity.GetUnit());
    }

    /// <summary>
    /// Returns the temperature interval between this point and the supplied point.
    /// </summary>
    /// <param name="value">The compatible temperature value to use.</param>
    /// <returns>The resulting temperature interval.</returns>
    public TemperatureInterval Difference(Temperature value)
    {
        ValidateTemperature(value, "value");
        return new TemperatureInterval(Kelvins() - value.Kelvins(), TemperatureInterval.KelvinUnit());
    }

    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue()
    {
        return new QuantityValue(_quantity.GetValue(), _quantity.GetUnit());
    }

    internal static UnitDefinition CelsiusUnit()
    {
        return new StandardUnits().DegreeCelsius();
    }

    internal static UnitDefinition FahrenheitUnit()
    {
        return new StandardUnits().DegreeFahrenheit();
    }

    internal static UnitDefinition RankineUnit()
    {
        return new StandardUnits().Rankine();
    }

    internal static void ValidateTemperature(Temperature value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidateTemperatureInterval(TemperatureInterval value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidateTemperatureUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (!unit.GetDimension().EqualsDimension(new StandardDimensions().ThermodynamicTemperature()))
        {
            throw new System.ArgumentException("Temperature units must use the temperature dimension.");
        }

        if (unit.GetConversionKind() != UnitConversionKind.Affine)
        {
            throw new System.ArgumentException("Temperature units must be affine temperature point units.");
        }
    }
}
