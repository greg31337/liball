namespace units;

/// <summary>
/// Represents the integer exponents of the library's canonical base dimensions.
/// </summary>
/// <remarks>
/// Slots are ordered as length, mass, time, electric current, temperature, amount, luminous intensity, plane angle, solid angle, information, currency, and count. Multiplication and division add or subtract these exponents.
/// </remarks>
public class DimensionVector : IDimensionVector
{
    // Canonical dimension slot order for construction, algebra, and ToKey:
    // length, mass, time, electric current, thermodynamic temperature,
    // amount of substance, luminous intensity, plane angle, solid angle,
    // information, currency, count.
    internal int _length;
    internal int _mass;
    internal int _time;
    internal int _current;
    internal int _temperature;
    internal int _amount;
    internal int _luminousIntensity;
    internal int _angle;
    internal int _solidAngle;
    internal int _information;
    internal int _currency;
    internal int _count;

    /// <summary>
    /// Initializes a new dimension vector from the supplied metadata.
    /// </summary>
    /// <param name="length">The exponent for the length base dimension.</param>
    /// <param name="mass">The exponent for the mass base dimension.</param>
    /// <param name="time">The exponent for the time base dimension.</param>
    /// <param name="current">The exponent for the current base dimension.</param>
    /// <param name="temperature">The exponent for the temperature base dimension.</param>
    /// <param name="amount">The exponent for the amount base dimension.</param>
    /// <param name="luminousIntensity">The exponent for the luminous intensity base dimension.</param>
    /// <param name="angle">The exponent for the angle base dimension.</param>
    /// <param name="solidAngle">The exponent for the solid angle base dimension.</param>
    /// <param name="information">The exponent for the information base dimension.</param>
    /// <param name="currency">The exponent for the currency base dimension.</param>
    /// <param name="count">The exponent for the count base dimension.</param>
    public DimensionVector(int length, int mass, int time, int current, int temperature, int amount, int luminousIntensity, int angle, int solidAngle, int information, int currency, int count)
    {
        _length = length;
        _mass = mass;
        _time = time;
        _current = current;
        _temperature = temperature;
        _amount = amount;
        _luminousIntensity = luminousIntensity;
        _angle = angle;
        _solidAngle = solidAngle;
        _information = information;
        _currency = currency;
        _count = count;
    }

    internal static int GetSlotCount()
    {
        return 12;
    }

    internal static DimensionVector Dimensionless()
    {
        return new DimensionVector(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    internal static DimensionVector Length()
    {
        return new DimensionVector(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    internal static DimensionVector Mass()
    {
        return new DimensionVector(0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    internal static DimensionVector Time()
    {
        return new DimensionVector(0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    internal static DimensionVector ElectricCurrent()
    {
        return new DimensionVector(0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    internal static DimensionVector ThermodynamicTemperature()
    {
        return new DimensionVector(0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0);
    }

    internal static DimensionVector AmountOfSubstance()
    {
        return new DimensionVector(0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0);
    }

    internal static DimensionVector LuminousIntensity()
    {
        return new DimensionVector(0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0);
    }

    internal static DimensionVector PlaneAngle()
    {
        return new DimensionVector(0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0);
    }

    internal static DimensionVector SolidAngle()
    {
        return new DimensionVector(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0);
    }

    internal static DimensionVector Information()
    {
        return new DimensionVector(0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0);
    }

    internal static DimensionVector Currency()
    {
        return new DimensionVector(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0);
    }

    internal static DimensionVector Count()
    {
        return new DimensionVector(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
    }

    /// <summary>
    /// Gets the length exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetLengthExponent()
    {
        return _length;
    }

    /// <summary>
    /// Gets the mass exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetMassExponent()
    {
        return _mass;
    }

    /// <summary>
    /// Gets the time exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetTimeExponent()
    {
        return _time;
    }

    /// <summary>
    /// Gets the current exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetCurrentExponent()
    {
        return _current;
    }

    /// <summary>
    /// Gets the temperature exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetTemperatureExponent()
    {
        return _temperature;
    }

    /// <summary>
    /// Gets the amount exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetAmountExponent()
    {
        return _amount;
    }

    /// <summary>
    /// Gets the luminous intensity exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetLuminousIntensityExponent()
    {
        return _luminousIntensity;
    }

    /// <summary>
    /// Gets the angle exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetAngleExponent()
    {
        return _angle;
    }

    /// <summary>
    /// Gets the solid angle exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetSolidAngleExponent()
    {
        return _solidAngle;
    }

    /// <summary>
    /// Gets the information exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetInformationExponent()
    {
        return _information;
    }

    /// <summary>
    /// Gets the currency exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetCurrencyExponent()
    {
        return _currency;
    }

    /// <summary>
    /// Gets the count exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetCountExponent()
    {
        return _count;
    }

    /// <summary>
    /// Gets the exponent.
    /// </summary>
    /// <param name="slot">The exponent for the slot base dimension.</param>
    /// <returns>The requested numeric value.</returns>
    public int GetExponent(int slot)
    {
        switch (slot)
        {
            case DimensionSlots.Length:
                return _length;
            case DimensionSlots.Mass:
                return _mass;
            case DimensionSlots.Time:
                return _time;
            case DimensionSlots.ElectricCurrent:
                return _current;
            case DimensionSlots.ThermodynamicTemperature:
                return _temperature;
            case DimensionSlots.AmountOfSubstance:
                return _amount;
            case DimensionSlots.LuminousIntensity:
                return _luminousIntensity;
            case DimensionSlots.PlaneAngle:
                return _angle;
            case DimensionSlots.SolidAngle:
                return _solidAngle;
            case DimensionSlots.Information:
                return _information;
            case DimensionSlots.Currency:
                return _currency;
            case DimensionSlots.Count:
                return _count;
        }

        throw new System.ArgumentException("Unknown dimension slot.");
    }

    /// <summary>
    /// Reports whether the supplied vector has the same exponent in every dimension slot.
    /// </summary>
    /// <param name="value">The dimension vector to compare.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool EqualsDimension(DimensionVector? value)
    {
        if (value == null)
        {
            return false;
        }

        return _length == value.GetLengthExponent()
            && _mass == value.GetMassExponent()
            && _time == value.GetTimeExponent()
            && _current == value.GetCurrentExponent()
            && _temperature == value.GetTemperatureExponent()
            && _amount == value.GetAmountExponent()
            && _luminousIntensity == value.GetLuminousIntensityExponent()
            && _angle == value.GetAngleExponent()
            && _solidAngle == value.GetSolidAngleExponent()
            && _information == value.GetInformationExponent()
            && _currency == value.GetCurrencyExponent()
            && _count == value.GetCountExponent();
    }

    /// <summary>
    /// Reports whether every dimension exponent is zero.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsDimensionless()
    {
        return _length == 0
            && _mass == 0
            && _time == 0
            && _current == 0
            && _temperature == 0
            && _amount == 0
            && _luminousIntensity == 0
            && _angle == 0
            && _solidAngle == 0
            && _information == 0
            && _currency == 0
            && _count == 0;
    }

    /// <summary>
    /// Adds the supplied vector's exponents to represent dimension multiplication.
    /// </summary>
    /// <param name="value">The dimension vector whose exponents are added.</param>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Multiply(DimensionVector value)
    {
        RequireValue(value);

        return new DimensionVector(
            _length + value.GetLengthExponent(),
            _mass + value.GetMassExponent(),
            _time + value.GetTimeExponent(),
            _current + value.GetCurrentExponent(),
            _temperature + value.GetTemperatureExponent(),
            _amount + value.GetAmountExponent(),
            _luminousIntensity + value.GetLuminousIntensityExponent(),
            _angle + value.GetAngleExponent(),
            _solidAngle + value.GetSolidAngleExponent(),
            _information + value.GetInformationExponent(),
            _currency + value.GetCurrencyExponent(),
            _count + value.GetCountExponent());
    }

    /// <summary>
    /// Subtracts the supplied vector's exponents to represent dimension division.
    /// </summary>
    /// <param name="value">The dimension vector whose exponents are subtracted.</param>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Divide(DimensionVector value)
    {
        RequireValue(value);

        return new DimensionVector(
            _length - value.GetLengthExponent(),
            _mass - value.GetMassExponent(),
            _time - value.GetTimeExponent(),
            _current - value.GetCurrentExponent(),
            _temperature - value.GetTemperatureExponent(),
            _amount - value.GetAmountExponent(),
            _luminousIntensity - value.GetLuminousIntensityExponent(),
            _angle - value.GetAngleExponent(),
            _solidAngle - value.GetSolidAngleExponent(),
            _information - value.GetInformationExponent(),
            _currency - value.GetCurrencyExponent(),
            _count - value.GetCountExponent());
    }

    /// <summary>
    /// Multiplies every dimension exponent by the supplied integer power.
    /// </summary>
    /// <param name="power">The integer multiplier for every dimension exponent.</param>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Pow(int power)
    {
        return new DimensionVector(
            _length * power,
            _mass * power,
            _time * power,
            _current * power,
            _temperature * power,
            _amount * power,
            _luminousIntensity * power,
            _angle * power,
            _solidAngle * power,
            _information * power,
            _currency * power,
            _count * power);
    }

    /// <summary>
    /// Divides every exponent by the root when all divisions are exact.
    /// </summary>
    /// <param name="root">The positive integer divisor for every dimension exponent.</param>
    /// <returns>A result that describes success or failure.</returns>
    public DimensionRootResult Root(int root)
    {
        if (root <= 0)
        {
            return DimensionRootResult.CreateFailure(UnitErrorCode.ArithmeticInvalidOperation, "Root must be greater than zero.");
        }

        if (!CanDivideAllExponents(root))
        {
            return DimensionRootResult.CreateFailure(UnitErrorCode.ArithmeticInvalidOperation, "Dimension exponents must divide evenly by the requested root.");
        }

        return DimensionRootResult.CreateSuccess(
            new DimensionVector(
                _length / root,
                _mass / root,
                _time / root,
                _current / root,
                _temperature / root,
                _amount / root,
                _luminousIntensity / root,
                _angle / root,
                _solidAngle / root,
                _information / root,
                _currency / root,
                _count / root));
    }

    /// <summary>
    /// Returns the canonical key formed from the dimension exponents.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string ToKey()
    {
        return FormatInt(_length)
            + "," + FormatInt(_mass)
            + "," + FormatInt(_time)
            + "," + FormatInt(_current)
            + "," + FormatInt(_temperature)
            + "," + FormatInt(_amount)
            + "," + FormatInt(_luminousIntensity)
            + "," + FormatInt(_angle)
            + "," + FormatInt(_solidAngle)
            + "," + FormatInt(_information)
            + "," + FormatInt(_currency)
            + "," + FormatInt(_count);
    }

    internal bool CanDivideAllExponents(int root)
    {
        return _length % root == 0
            && _mass % root == 0
            && _time % root == 0
            && _current % root == 0
            && _temperature % root == 0
            && _amount % root == 0
            && _luminousIntensity % root == 0
            && _angle % root == 0
            && _solidAngle % root == 0
            && _information % root == 0
            && _currency % root == 0
            && _count % root == 0;
    }

    internal static string FormatInt(int value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static void RequireValue(DimensionVector value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }
    }
}
