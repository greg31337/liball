namespace units;

/// <summary>
/// Represents a mass as a numeric value paired with a compatible runtime unit.
/// </summary>
public class Mass : IMass
{
    internal QuantityValue _quantity;

    /// <summary>
    /// Initializes a new mass from a numeric value and a compatible unit.
    /// </summary>
    /// <param name="value">The numeric value to use.</param>
    /// <param name="unit">The unit definition to use.</param>
    public Mass(double value, UnitDefinition unit)
    {
        ValidateMassUnit(unit);
        _quantity = new QuantityValue(value, unit);
    }

    internal static Mass FromKilograms(double value)
    {
        return new Mass(value, new StandardUnits().Kilogram());
    }

    internal static Mass FromGrams(double value)
    {
        return new Mass(value, new StandardUnits().Gram());
    }

    internal static Mass FromTonnes(double value)
    {
        return new Mass(value, new StandardUnits().Tonne());
    }

    internal static Mass FromPounds(double value)
    {
        return new Mass(value, new StandardUnits().Pound());
    }

    internal static Mass FromOunces(double value)
    {
        return new Mass(value, new StandardUnits().Ounce());
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
    /// Returns the stored mass converted to kilograms.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Kilograms()
    {
        return _quantity.As(new StandardUnits().Kilogram());
    }

    /// <summary>
    /// Returns the stored mass converted to grams.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Grams()
    {
        return _quantity.As(new StandardUnits().Gram());
    }

    /// <summary>
    /// Returns the stored mass converted to tonnes.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Tonnes()
    {
        return _quantity.As(new StandardUnits().Tonne());
    }

    /// <summary>
    /// Returns the stored mass converted to pounds.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Pounds()
    {
        return _quantity.As(new StandardUnits().Pound());
    }

    /// <summary>
    /// Returns the stored mass converted to ounces.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double Ounces()
    {
        return _quantity.As(new StandardUnits().Ounce());
    }

    /// <summary>
    /// Converts this mass to the supplied compatible unit.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting mass.</returns>
    /// <exception cref="System.ArgumentException">The target unit has an incompatible dimension or conversion kind.</exception>
    public Mass ToUnit(UnitDefinition unit)
    {
        ValidateMassUnit(unit);
        QuantityValue converted = _quantity.ToUnit(unit);
        return new Mass(converted.GetValue(), converted.GetUnit());
    }

    /// <summary>
    /// Adds the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible mass value to use.</param>
    /// <returns>The resulting mass.</returns>
    public Mass Add(Mass value)
    {
        ValidateMass(value, "value");
        QuantityValue result = new QuantityMath().Add(_quantity, value.ToQuantityValue());
        return new Mass(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Subtracts the supplied compatible value and preserves this value's unit.
    /// </summary>
    /// <param name="value">The compatible mass value to use.</param>
    /// <returns>The resulting mass.</returns>
    public Mass Subtract(Mass value)
    {
        ValidateMass(value, "value");
        QuantityValue result = new QuantityMath().Subtract(_quantity, value.ToQuantityValue());
        return new Mass(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Multiplies the quantity's numeric value by the supplied factor.
    /// </summary>
    /// <param name="factor">The scalar multiplier.</param>
    /// <returns>The resulting mass.</returns>
    public Mass Scale(double factor)
    {
        QuantityValue result = new QuantityMath().Scale(_quantity, factor);
        return new Mass(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with its numeric sign reversed.
    /// </summary>
    /// <returns>The resulting mass.</returns>
    public Mass Negate()
    {
        QuantityValue result = new QuantityMath().Negate(_quantity);
        return new Mass(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting mass.</returns>
    public Mass AbsoluteValue()
    {
        QuantityValue result = new QuantityMath().AbsoluteValue(_quantity);
        return new Mass(result.GetValue(), result.GetUnit());
    }

    /// <summary>
    /// Returns the quantity with a nonnegative numeric value.
    /// </summary>
    /// <returns>The resulting mass.</returns>
    public Mass Abs()
    {
        return AbsoluteValue();
    }

    /// <summary>
    /// Multiplies this mass by an acceleration.
    /// </summary>
    /// <param name="value">The acceleration to use.</param>
    /// <returns>The resulting force.</returns>
    public Force MultiplyByAcceleration(Acceleration value)
    {
        Acceleration.ValidateAcceleration(value, "value");
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

    internal static void ValidateMass(Mass value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static void ValidateMassUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }

        if (!unit.GetDimension().EqualsDimension(new StandardDimensions().Mass()))
        {
            throw new System.ArgumentException("Mass units must use the mass dimension.");
        }
    }
}
