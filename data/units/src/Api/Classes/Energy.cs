namespace units;

/// <summary>
/// Represents an energy value paired with a compatible runtime unit.
/// </summary>
public class Energy : IEnergy
{
    internal QuantityValue _quantity;

    /// <summary>
    /// Initializes a new energy from a numeric value and a compatible unit.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public Energy(double value, UnitDefinition unit)
    {
        ValidateEnergyUnit(unit);
        _quantity = new QuantityValue(value, unit);
    }

    internal static Energy FromJoules(double value)
    {
        return new Energy(value, new StandardUnits().Joule());
    }

    internal static Energy FromKilojoules(double value)
    {
        return new Energy(value, KilojouleUnit());
    }

    internal static Energy FromKilowattHours(double value)
    {
        return new Energy(value, KilowattHourUnit());
    }

    internal static Energy FromBritishThermalUnits(double value)
    {
        return new Energy(value, new StandardUnits().Btu());
    }

    internal static Energy FromElectronvolts(double value)
    {
        return new Energy(value, new StandardUnits().Electronvolt());
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
    /// Returns the stored energy converted to joules.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Joules()
    {
        return _quantity.As(new StandardUnits().Joule());
    }

    /// <summary>
    /// Returns the stored energy converted to kilojoules.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kilojoules()
    {
        return _quantity.As(KilojouleUnit());
    }

    /// <summary>
    /// Returns the stored energy converted to kilowatt hours.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double KilowattHours()
    {
        return _quantity.As(KilowattHourUnit());
    }

    /// <summary>
    /// Returns the stored energy converted to british thermal units.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double BritishThermalUnits()
    {
        return _quantity.As(new StandardUnits().Btu());
    }

    /// <summary>
    /// Returns the stored energy converted to electronvolts.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Electronvolts()
    {
        return _quantity.As(new StandardUnits().Electronvolt());
    }

    /// <summary>
    /// Converts this energy to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting energy.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Energy ToUnit(UnitDefinition unit)
    {
        ValidateEnergyUnit(unit);
        QuantityValue converted = _quantity.ToUnit(unit);
        return new Energy(converted.GetValue(), converted.GetUnit());
    }

    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible energy value to use.</param>
    /// <returns>The resulting energy.</returns>
    public Energy Add(Energy value)
    {
        ValidateEnergy(value, "value");
        QuantityValue result = new QuantityMath().Add(_quantity, value.ToQuantityValue());
        return new Energy(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible energy value to use.</param>
    /// <returns>The resulting energy.</returns>
    public Energy Subtract(Energy value)
    {
        ValidateEnergy(value, "value");
        QuantityValue result = new QuantityMath().Subtract(_quantity, value.ToQuantityValue());
        return new Energy(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting energy.</returns>
    public Energy Scale(double factor)
    {
        QuantityValue result = new QuantityMath().Scale(_quantity, factor);
        return new Energy(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting energy.</returns>
    public Energy Negate()
    {
        QuantityValue result = new QuantityMath().Negate(_quantity);
        return new Energy(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting energy.</returns>
    public Energy AbsoluteValue()
    {
        QuantityValue result = new QuantityMath().AbsoluteValue(_quantity);
        return new Energy(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting energy.</returns>
    public Energy Abs()
    {
        return AbsoluteValue();
    }

    /// <summary>
    /// Divides this energy by a duration.
    /// </summary>
    /// <param name="value">The duration to use.</param>
    /// <returns>The resulting power.</returns>
    public Power DivideByDuration(Duration value)
    {
        Duration.ValidateDuration(value, "value");
        QuantityValue result = new QuantityMath().Divide(_quantity, value.ToQuantityValue());
        return new Power(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns a dynamic quantity containing the same value and unit.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue ToQuantityValue()
    {
        return new QuantityValue(_quantity.GetValue(), _quantity.GetUnit());
    }

    internal static UnitDefinition KilojouleUnit()
    {
        return new StandardUnits().Kilojoule();
    }

    internal static UnitDefinition KilowattHourUnit()
    {
        return new StandardUnits().KilowattHour();
    }

    internal static UnitDefinition KilowattUnit()
    {
        return new StandardUnits().Kilowatt();
    }

    internal static void ValidateEnergy(Energy value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidateEnergyUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (!unit.GetDimension().EqualsDimension(new StandardUnits().Joule().GetDimension()))
        {
            throw new System.ArgumentException("Energy units must use the energy dimension.");
        }
    }
}
