namespace units;

/// <summary>
/// Represents a frequency as a numeric value paired with a compatible runtime unit.
/// </summary>
public class Frequency : IFrequency
{
    internal QuantityValue _quantity;

    /// <summary>
    /// Initializes a new frequency from a numeric value and a compatible unit.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public Frequency(double value, UnitDefinition unit)
    {
        ValidateFrequencyUnit(unit);
        _quantity = new QuantityValue(value, unit);
    }

    internal static Frequency FromHertz(double value)
    {
        return new Frequency(value, new StandardUnits().Hertz());
    }

    internal static Frequency FromKilohertz(double value)
    {
        return new Frequency(value, KilohertzUnit());
    }

    internal static Frequency FromMegahertz(double value)
    {
        return new Frequency(value, MegahertzUnit());
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
    /// Returns the stored frequency converted to hertz.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Hertz()
    {
        return _quantity.As(new StandardUnits().Hertz());
    }

    /// <summary>
    /// Returns the stored frequency converted to kilohertz.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kilohertz()
    {
        return _quantity.As(KilohertzUnit());
    }

    /// <summary>
    /// Returns the stored frequency converted to megahertz.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Megahertz()
    {
        return _quantity.As(MegahertzUnit());
    }

    /// <summary>
    /// Converts this frequency to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting frequency.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Frequency ToUnit(UnitDefinition unit)
    {
        ValidateFrequencyUnit(unit);
        QuantityValue converted = _quantity.ToUnit(unit);
        return new Frequency(converted.GetValue(), converted.GetUnit());
    }

    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible frequency value to use.</param>
    /// <returns>The resulting frequency.</returns>
    public Frequency Add(Frequency value)
    {
        ValidateFrequency(value, "value");
        QuantityValue result = new QuantityMath().Add(_quantity, value.ToQuantityValue());
        return new Frequency(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible frequency value to use.</param>
    /// <returns>The resulting frequency.</returns>
    public Frequency Subtract(Frequency value)
    {
        ValidateFrequency(value, "value");
        QuantityValue result = new QuantityMath().Subtract(_quantity, value.ToQuantityValue());
        return new Frequency(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting frequency.</returns>
    public Frequency Scale(double factor)
    {
        QuantityValue result = new QuantityMath().Scale(_quantity, factor);
        return new Frequency(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting frequency.</returns>
    public Frequency Negate()
    {
        QuantityValue result = new QuantityMath().Negate(_quantity);
        return new Frequency(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting frequency.</returns>
    public Frequency AbsoluteValue()
    {
        QuantityValue result = new QuantityMath().AbsoluteValue(_quantity);
        return new Frequency(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting frequency.</returns>
    public Frequency Abs()
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

    internal static UnitDefinition KilohertzUnit()
    {
        return new StandardUnits().Kilohertz();
    }

    internal static UnitDefinition MegahertzUnit()
    {
        return new StandardUnits().Megahertz();
    }

    internal static void ValidateFrequency(Frequency value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidateFrequencyUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (!unit.GetDimension().EqualsDimension(new StandardUnits().Hertz().GetDimension()))
        {
            throw new System.ArgumentException("Frequency units must use the frequency dimension.");
        }
    }
}
