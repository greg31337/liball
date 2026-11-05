namespace units;

/// <summary>
/// Represents a volume as a numeric value paired with a compatible runtime unit.
/// </summary>
public class Volume : IVolume
{
    internal QuantityValue _quantity;

    /// <summary>
    /// Initializes a new volume from a numeric value and a compatible unit.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public Volume(double value, UnitDefinition unit)
    {
        ValidateVolumeUnit(unit);
        _quantity = new QuantityValue(value, unit);
    }

    internal static Volume FromCubicMeters(double value)
    {
        return new Volume(value, CubicMeterUnit());
    }

    internal static Volume FromLiters(double value)
    {
        return new Volume(value, new StandardUnits().Liter());
    }

    internal static Volume FromGallons(double value)
    {
        return new Volume(value, new StandardUnits().Gallon());
    }

    internal static Volume FromQuarts(double value)
    {
        return new Volume(value, new StandardUnits().Quart());
    }

    internal static Volume FromPints(double value)
    {
        return new Volume(value, new StandardUnits().Pint());
    }

    internal static Volume FromCups(double value)
    {
        return new Volume(value, new StandardUnits().Cup());
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
    /// Returns the stored volume converted to cubic meters.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double CubicMeters()
    {
        return _quantity.As(CubicMeterUnit());
    }

    /// <summary>
    /// Returns the stored volume converted to liters.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Liters()
    {
        return _quantity.As(new StandardUnits().Liter());
    }

    /// <summary>
    /// Returns the stored volume converted to gallons.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Gallons()
    {
        return _quantity.As(new StandardUnits().Gallon());
    }

    /// <summary>
    /// Returns the stored volume converted to quarts.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Quarts()
    {
        return _quantity.As(new StandardUnits().Quart());
    }

    /// <summary>
    /// Returns the stored volume converted to pints.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Pints()
    {
        return _quantity.As(new StandardUnits().Pint());
    }

    /// <summary>
    /// Returns the stored volume converted to cups.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Cups()
    {
        return _quantity.As(new StandardUnits().Cup());
    }

    /// <summary>
    /// Converts this volume to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting volume.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Volume ToUnit(UnitDefinition unit)
    {
        ValidateVolumeUnit(unit);
        QuantityValue converted = _quantity.ToUnit(unit);
        return new Volume(converted.GetValue(), converted.GetUnit());
    }

    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible volume value to use.</param>
    /// <returns>The resulting volume.</returns>
    public Volume Add(Volume value)
    {
        ValidateVolume(value, "value");
        QuantityValue result = new QuantityMath().Add(_quantity, value.ToQuantityValue());
        return new Volume(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible volume value to use.</param>
    /// <returns>The resulting volume.</returns>
    public Volume Subtract(Volume value)
    {
        ValidateVolume(value, "value");
        QuantityValue result = new QuantityMath().Subtract(_quantity, value.ToQuantityValue());
        return new Volume(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting volume.</returns>
    public Volume Scale(double factor)
    {
        QuantityValue result = new QuantityMath().Scale(_quantity, factor);
        return new Volume(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting volume.</returns>
    public Volume Negate()
    {
        QuantityValue result = new QuantityMath().Negate(_quantity);
        return new Volume(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting volume.</returns>
    public Volume AbsoluteValue()
    {
        QuantityValue result = new QuantityMath().AbsoluteValue(_quantity);
        return new Volume(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting volume.</returns>
    public Volume Abs()
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

    internal static UnitDefinition CubicMeterUnit()
    {
        return new StandardUnits().CubicMeter();
    }

    internal static void ValidateVolume(Volume value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidateVolumeUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (!unit.GetDimension().EqualsDimension(new StandardDimensions().Length().Pow(3)))
        {
            throw new System.ArgumentException("Volume units must use the volume dimension.");
        }
    }
}
