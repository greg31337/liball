namespace units;

/// <summary>
/// Represents a data size as a numeric value paired with a compatible runtime unit.
/// </summary>
public class DataSize : IDataSize
{
    internal QuantityValue _quantity;

    /// <summary>
    /// Initializes a new data size from a numeric value and a compatible unit.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public DataSize(double value, UnitDefinition unit)
    {
        ValidateDataSizeUnit(unit);
        _quantity = new QuantityValue(value, unit);
    }

    internal static DataSize FromBits(double value)
    {
        return new DataSize(value, new StandardUnits().Bit());
    }

    internal static DataSize FromBytes(double value)
    {
        return new DataSize(value, new StandardUnits().Byte());
    }

    internal static DataSize FromKilobytes(double value)
    {
        return new DataSize(value, new StandardUnits().Kilobyte());
    }

    internal static DataSize FromKibibytes(double value)
    {
        return new DataSize(value, new StandardUnits().Kibibyte());
    }

    internal static DataSize FromMegabytes(double value)
    {
        return new DataSize(value, new StandardUnits().Megabyte());
    }

    internal static DataSize FromMebibytes(double value)
    {
        return new DataSize(value, new StandardUnits().Mebibyte());
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
    /// Returns the stored data size converted to bits.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Bits()
    {
        return _quantity.As(new StandardUnits().Bit());
    }

    /// <summary>
    /// Returns the stored data size converted to bytes.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Bytes()
    {
        return _quantity.As(new StandardUnits().Byte());
    }

    /// <summary>
    /// Returns the stored data size converted to kilobytes.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kilobytes()
    {
        return _quantity.As(new StandardUnits().Kilobyte());
    }

    /// <summary>
    /// Returns the stored data size converted to kibibytes.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kibibytes()
    {
        return _quantity.As(new StandardUnits().Kibibyte());
    }

    /// <summary>
    /// Returns the stored data size converted to megabytes.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Megabytes()
    {
        return _quantity.As(new StandardUnits().Megabyte());
    }

    /// <summary>
    /// Returns the stored data size converted to mebibytes.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Mebibytes()
    {
        return _quantity.As(new StandardUnits().Mebibyte());
    }

    /// <summary>
    /// Converts this data size to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting data size.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public DataSize ToUnit(UnitDefinition unit)
    {
        ValidateDataSizeUnit(unit);
        QuantityValue converted = _quantity.ToUnit(unit);
        return new DataSize(converted.GetValue(), converted.GetUnit());
    }

    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible data size value to use.</param>
    /// <returns>The resulting data size.</returns>
    public DataSize Add(DataSize value)
    {
        ValidateDataSize(value, "value");
        QuantityValue result = new QuantityMath().Add(_quantity, value.ToQuantityValue());
        return new DataSize(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible data size value to use.</param>
    /// <returns>The resulting data size.</returns>
    public DataSize Subtract(DataSize value)
    {
        ValidateDataSize(value, "value");
        QuantityValue result = new QuantityMath().Subtract(_quantity, value.ToQuantityValue());
        return new DataSize(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting data size.</returns>
    public DataSize Scale(double factor)
    {
        QuantityValue result = new QuantityMath().Scale(_quantity, factor);
        return new DataSize(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting data size.</returns>
    public DataSize Negate()
    {
        QuantityValue result = new QuantityMath().Negate(_quantity);
        return new DataSize(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting data size.</returns>
    public DataSize AbsoluteValue()
    {
        QuantityValue result = new QuantityMath().AbsoluteValue(_quantity);
        return new DataSize(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting data size.</returns>
    public DataSize Abs()
    {
        return AbsoluteValue();
    }

    /// <summary>
    /// Divides this data size by a duration.
    /// </summary>
    /// <param name="value">The duration to use.</param>
    /// <returns>The resulting data rate.</returns>
    public DataRate DivideByDuration(Duration value)
    {
        Duration.ValidateDuration(value, "value");
        QuantityValue result = new QuantityMath().Divide(_quantity, value.ToQuantityValue());
        return new DataRate(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue()
    {
        return new QuantityValue(_quantity.GetValue(), _quantity.GetUnit());
    }

    internal static void ValidateDataSize(DataSize value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidateDataSizeUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (!unit.GetDimension().EqualsDimension(new StandardDimensions().Information()))
        {
            throw new System.ArgumentException("Data size units must use the information dimension.");
        }
    }
}
