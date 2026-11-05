namespace units;

/// <summary>
/// Pairs a scalar value with a runtime unit definition.
/// </summary>
/// <remarks>
/// Conversions are checked at runtime using dimension vectors. The stored numeric value uses double precision even when a unit's scale is represented exactly.
/// </remarks>
public class QuantityValue : IQuantityValue
{
    internal double _value;
    internal UnitDefinition _unit;

    /// <summary>
    /// Initializes a new quantity value from the supplied metadata.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public QuantityValue(double value, UnitDefinition unit)
    {
        ValidateRequiredValue(unit, "unit");

        _value = value;
        _unit = unit;
    }

    /// <summary>
    /// Gets the numeric value expressed in the stored unit.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double GetValue()
    {
        return _value;
    }

    /// <summary>
    /// Gets the unit paired with the stored numeric value.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition GetUnit()
    {
        return _unit;
    }

    /// <summary>
    /// Converts this quantity to the target unit and returns a nonthrowing result.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>A result that describes success or failure.</returns>
    public ConversionResult ConvertTo(UnitDefinition unit)
    {
        ValidateRequiredValue(unit, "unit");

        UnitConverter converter = new UnitConverter();
        return converter.Convert(_value, _unit, unit);
    }

    /// <summary>
    /// Converts this quantity to the target unit, throwing if conversion is invalid.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting quantity value.</returns>
    /// <exception cref="System.InvalidOperationException">The target unit has an incompatible dimension or conversion kind.</exception>
    public QuantityValue ToUnit(UnitDefinition unit)
    {
        ConversionResult result = ConvertTo(unit);
        if (result.IsFailure())
        {
            throw new System.InvalidOperationException(result.GetMessage());
        }

        return new QuantityValue(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns this quantity's numeric value expressed in the target unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The requested numeric value.</returns>
    /// <exception cref="System.InvalidOperationException">The target unit has an incompatible dimension or conversion kind.</exception>
    public double As(UnitDefinition unit)
    {
        return ToUnit(unit).GetValue();
    }

    /// <summary>
    /// Reports whether this quantity has the same dimension as the supplied value or unit.
    /// </summary>
    /// <param name="value">The quantity to compare.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsCompatibleWith(QuantityValue? value)
    {
        if (value == null)
        {
            return false;
        }

        return _unit.IsCompatibleWith(value.GetUnit());
    }

    /// <summary>
    /// Reports whether this quantity has the same dimension as the supplied value or unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsCompatibleWithUnit(UnitDefinition? unit)
    {
        return _unit.IsCompatibleWith(unit);
    }

    /// <summary>
    /// Reports whether every dimension exponent is zero.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsDimensionless()
    {
        return _unit.GetDimension().IsDimensionless();
    }

    /// <summary>
    /// Creates a typed length facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting length.</returns>
    public Length ToLength()
    {
        return new Length(_value, _unit);
    }

    /// <summary>
    /// Creates a typed mass facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting mass.</returns>
    public Mass ToMass()
    {
        return new Mass(_value, _unit);
    }

    /// <summary>
    /// Creates a typed duration facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting duration.</returns>
    public Duration ToDuration()
    {
        return new Duration(_value, _unit);
    }

    /// <summary>
    /// Creates a typed area facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting area.</returns>
    public Area ToArea()
    {
        return new Area(_value, _unit);
    }

    /// <summary>
    /// Creates a typed volume facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting volume.</returns>
    public Volume ToVolume()
    {
        return new Volume(_value, _unit);
    }

    /// <summary>
    /// Creates a typed speed facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting speed.</returns>
    public Speed ToSpeed()
    {
        return new Speed(_value, _unit);
    }

    /// <summary>
    /// Creates a typed acceleration facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting acceleration.</returns>
    public Acceleration ToAcceleration()
    {
        return new Acceleration(_value, _unit);
    }

    /// <summary>
    /// Creates a typed force facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting force.</returns>
    public Force ToForce()
    {
        return new Force(_value, _unit);
    }

    /// <summary>
    /// Creates a typed pressure facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting pressure.</returns>
    public Pressure ToPressure()
    {
        return new Pressure(_value, _unit);
    }

    /// <summary>
    /// Creates a typed energy facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting energy.</returns>
    public Energy ToEnergy()
    {
        return new Energy(_value, _unit);
    }

    /// <summary>
    /// Creates a typed power facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting power.</returns>
    public Power ToPower()
    {
        return new Power(_value, _unit);
    }

    /// <summary>
    /// Creates a typed frequency facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting frequency.</returns>
    public Frequency ToFrequency()
    {
        return new Frequency(_value, _unit);
    }

    /// <summary>
    /// Creates a typed angle facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting angle.</returns>
    public Angle ToAngle()
    {
        return new Angle(_value, _unit);
    }

    /// <summary>
    /// Creates a typed temperature-point facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting temperature.</returns>
    public Temperature ToTemperature()
    {
        return new Temperature(_value, _unit);
    }

    /// <summary>
    /// Creates a typed temperature-interval facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting temperature interval.</returns>
    public TemperatureInterval ToTemperatureInterval()
    {
        return new TemperatureInterval(_value, _unit);
    }

    /// <summary>
    /// Creates a typed data-size facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting data size.</returns>
    public DataSize ToDataSize()
    {
        return new DataSize(_value, _unit);
    }

    /// <summary>
    /// Creates a typed data-rate facade from this dynamic quantity.
    /// </summary>
    /// <returns>The resulting data rate.</returns>
    public DataRate ToDataRate()
    {
        return new DataRate(_value, _unit);
    }

    internal static void ValidateRequiredValue(object value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }
}
