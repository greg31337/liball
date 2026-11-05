namespace units;

/// <summary>
/// Represents a data rate as a numeric value paired with a compatible runtime unit.
/// </summary>
public class DataRate : IDataRate
{
    internal QuantityValue _quantity;

    /// <summary>
    /// Initializes a new data rate from a numeric value and a compatible unit.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public DataRate(double value, UnitDefinition unit)
    {
        ValidateDataRateUnit(unit);
        _quantity = new QuantityValue(value, unit);
    }

    internal static DataRate FromBitsPerSecond(double value)
    {
        return new DataRate(value, BitPerSecondUnit());
    }

    internal static DataRate FromBytesPerSecond(double value)
    {
        return new DataRate(value, BytePerSecondUnit());
    }

    internal static DataRate FromKilobytesPerSecond(double value)
    {
        return new DataRate(value, KilobytePerSecondUnit());
    }

    internal static DataRate FromKibibytesPerSecond(double value)
    {
        return new DataRate(value, KibibytePerSecondUnit());
    }

    internal static DataRate FromMegabytesPerSecond(double value)
    {
        return new DataRate(value, MegabytePerSecondUnit());
    }

    internal static DataRate FromMebibytesPerSecond(double value)
    {
        return new DataRate(value, MebibytePerSecondUnit());
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
    /// Returns the stored data rate converted to bits per second.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double BitsPerSecond()
    {
        return _quantity.As(BitPerSecondUnit());
    }

    /// <summary>
    /// Returns the stored data rate converted to bytes per second.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double BytesPerSecond()
    {
        return _quantity.As(BytePerSecondUnit());
    }

    /// <summary>
    /// Returns the stored data rate converted to kilobytes per second.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double KilobytesPerSecond()
    {
        return _quantity.As(KilobytePerSecondUnit());
    }

    /// <summary>
    /// Returns the stored data rate converted to kibibytes per second.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double KibibytesPerSecond()
    {
        return _quantity.As(KibibytePerSecondUnit());
    }

    /// <summary>
    /// Returns the stored data rate converted to megabytes per second.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double MegabytesPerSecond()
    {
        return _quantity.As(MegabytePerSecondUnit());
    }

    /// <summary>
    /// Returns the stored data rate converted to mebibytes per second.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double MebibytesPerSecond()
    {
        return _quantity.As(MebibytePerSecondUnit());
    }

    /// <summary>
    /// Converts this data rate to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting data rate.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public DataRate ToUnit(UnitDefinition unit)
    {
        ValidateDataRateUnit(unit);
        QuantityValue converted = _quantity.ToUnit(unit);
        return new DataRate(converted.GetValue(), converted.GetUnit());
    }

    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible data rate value to use.</param>
    /// <returns>The resulting data rate.</returns>
    public DataRate Add(DataRate value)
    {
        ValidateDataRate(value, "value");
        QuantityValue result = new QuantityMath().Add(_quantity, value.ToQuantityValue());
        return new DataRate(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible data rate value to use.</param>
    /// <returns>The resulting data rate.</returns>
    public DataRate Subtract(DataRate value)
    {
        ValidateDataRate(value, "value");
        QuantityValue result = new QuantityMath().Subtract(_quantity, value.ToQuantityValue());
        return new DataRate(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting data rate.</returns>
    public DataRate Scale(double factor)
    {
        QuantityValue result = new QuantityMath().Scale(_quantity, factor);
        return new DataRate(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting data rate.</returns>
    public DataRate Negate()
    {
        QuantityValue result = new QuantityMath().Negate(_quantity);
        return new DataRate(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting data rate.</returns>
    public DataRate AbsoluteValue()
    {
        QuantityValue result = new QuantityMath().AbsoluteValue(_quantity);
        return new DataRate(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting data rate.</returns>
    public DataRate Abs()
    {
        return AbsoluteValue();
    }

    /// <summary>
    /// Multiplies this data rate by a duration.
    /// </summary>
    /// <param name="value">The duration to use.</param>
    /// <returns>The resulting data size.</returns>
    public DataSize MultiplyByDuration(Duration value)
    {
        Duration.ValidateDuration(value, "value");
        QuantityValue result = new QuantityMath().Multiply(_quantity, value.ToQuantityValue());
        return new DataSize(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue()
    {
        return new QuantityValue(_quantity.GetValue(), _quantity.GetUnit());
    }

    internal static UnitDefinition BitPerSecondUnit()
    {
        return new StandardUnits().BitPerSecond();
    }

    internal static UnitDefinition BytePerSecondUnit()
    {
        return new StandardUnits().BytePerSecond();
    }

    internal static UnitDefinition KilobytePerSecondUnit()
    {
        return new StandardUnits().KilobytePerSecond();
    }

    internal static UnitDefinition KibibytePerSecondUnit()
    {
        return new StandardUnits().KibibytePerSecond();
    }

    internal static UnitDefinition MegabytePerSecondUnit()
    {
        return new StandardUnits().MegabytePerSecond();
    }

    internal static UnitDefinition MebibytePerSecondUnit()
    {
        return new StandardUnits().MebibytePerSecond();
    }

    internal static void ValidateDataRate(DataRate value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidateDataRateUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (!unit.GetDimension().EqualsDimension(new StandardDimensions().Information().Divide(new StandardDimensions().Time())))
        {
            throw new System.ArgumentException("Data rate units must use the information per time dimension.");
        }
    }
}
