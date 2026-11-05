namespace units;

/// <summary>
/// Represents an exact rational number used for unit scales and offsets.
/// </summary>
/// <remarks>
/// Values are normalized when constructed. Arithmetic remains exact until ToDouble is called at a numeric conversion boundary.
/// </remarks>
public class RationalNumber : IRationalNumber
{
    internal System.Numerics.BigInteger _numerator;
    internal System.Numerics.BigInteger _denominator;

    /// <summary>
    /// Initializes a new rational number from arbitrary-range integer metadata.
    /// </summary>
    /// <param name="numerator">The numerator.</param>
    /// <param name="denominator">The nonzero denominator.</param>
    public RationalNumber(System.Numerics.BigInteger numerator, System.Numerics.BigInteger denominator)
    {
        Initialize(numerator, denominator);
    }

    internal static RationalNumber FromBigIntegers(System.Numerics.BigInteger numerator, System.Numerics.BigInteger denominator)
    {
        return new RationalNumber(numerator, denominator);
    }

    /// <summary>
    /// Gets the numerator.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public long GetNumerator()
    {
        return ToLong(_numerator, "Numerator");
    }

    /// <summary>
    /// Gets the denominator.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public long GetDenominator()
    {
        return ToLong(_denominator, "Denominator");
    }

    /// <summary>
    /// Gets the numerator as big integer.
    /// </summary>
    /// <returns>The resulting system.numerics.big integer.</returns>
    public System.Numerics.BigInteger GetNumeratorAsBigInteger()
    {
        return _numerator;
    }

    /// <summary>
    /// Gets the denominator as big integer.
    /// </summary>
    /// <returns>The resulting system.numerics.big integer.</returns>
    public System.Numerics.BigInteger GetDenominatorAsBigInteger()
    {
        return _denominator;
    }

    /// <summary>
    /// Gets the numerator text.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetNumeratorText()
    {
        return _numerator.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the denominator text.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetDenominatorText()
    {
        return _denominator.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Reports whether the rational value is zero.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsZero()
    {
        return _numerator == new System.Numerics.BigInteger(0);
    }

    /// <summary>
    /// Reports whether the rational value is greater than zero.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsPositive()
    {
        return _numerator > new System.Numerics.BigInteger(0);
    }

    /// <summary>
    /// Reports whether the rational value is less than zero.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsNegative()
    {
        return _numerator < new System.Numerics.BigInteger(0);
    }

    /// <summary>
    /// Converts the exact rational value to double precision.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double ToDouble()
    {
        return (double)_numerator / (double)_denominator;
    }

    /// <summary>
    /// Reports whether the supplied rational number has the same normalized value.
    /// </summary>
    /// <param name="value">The rational number to compare.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool EqualsValue(RationalNumber? value)
    {
        if (value == null)
        {
            return false;
        }

        return _numerator == value.GetNumeratorAsBigInteger() && _denominator == value.GetDenominatorAsBigInteger();
    }

    /// <summary>
    /// Adds the supplied rational number using a common denominator and normalizes the result.
    /// </summary>
    /// <param name="value">The rational number to add.</param>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber Add(RationalNumber value)
    {
        RequireValue(value);

        System.Numerics.BigInteger numerator = _numerator * value.GetDenominatorAsBigInteger() + value.GetNumeratorAsBigInteger() * _denominator;
        System.Numerics.BigInteger denominator = _denominator * value.GetDenominatorAsBigInteger();

        return FromBigIntegers(numerator, denominator);
    }

    /// <summary>
    /// Subtracts the supplied rational number using a common denominator and normalizes the result.
    /// </summary>
    /// <param name="value">The rational number to subtract.</param>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber Subtract(RationalNumber value)
    {
        RequireValue(value);

        System.Numerics.BigInteger numerator = _numerator * value.GetDenominatorAsBigInteger() - value.GetNumeratorAsBigInteger() * _denominator;
        System.Numerics.BigInteger denominator = _denominator * value.GetDenominatorAsBigInteger();

        return FromBigIntegers(numerator, denominator);
    }

    /// <summary>
    /// Multiplies the numerators and denominators and normalizes the result.
    /// </summary>
    /// <param name="value">The rational number to multiply by.</param>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber Multiply(RationalNumber value)
    {
        RequireValue(value);

        System.Numerics.BigInteger numerator = _numerator * value.GetNumeratorAsBigInteger();
        System.Numerics.BigInteger denominator = _denominator * value.GetDenominatorAsBigInteger();

        return FromBigIntegers(numerator, denominator);
    }

    /// <summary>
    /// Multiplies by the reciprocal of the supplied nonzero rational number.
    /// </summary>
    /// <param name="value">The nonzero rational number to divide by.</param>
    /// <exception cref="System.ArgumentException">The supplied rational number is zero.</exception>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber Divide(RationalNumber value)
    {
        RequireValue(value);

        if (value.IsZero())
        {
            throw new System.ArgumentException("Cannot divide by a zero rational value.");
        }

        System.Numerics.BigInteger numerator = _numerator * value.GetDenominatorAsBigInteger();
        System.Numerics.BigInteger denominator = _denominator * value.GetNumeratorAsBigInteger();

        return FromBigIntegers(numerator, denominator);
    }

    /// <summary>
    /// Compares this rational number with the supplied value.
    /// </summary>
    /// <param name="value">The rational number to compare.</param>
    /// <returns>Negative one, zero, or positive one when this value is less than, equal to, or greater than the supplied value.</returns>
    public int CompareTo(RationalNumber value)
    {
        RequireValue(value);

        System.Numerics.BigInteger left = _numerator * value.GetDenominatorAsBigInteger();
        System.Numerics.BigInteger right = value.GetNumeratorAsBigInteger() * _denominator;
        int comparison = left.CompareTo(right);

        if (comparison < 0)
        {
            return -1;
        }

        if (comparison > 0)
        {
            return 1;
        }

        return 0;
    }

    internal static void RequireValue(RationalNumber value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }
    }

    internal void Initialize(System.Numerics.BigInteger numerator, System.Numerics.BigInteger denominator)
    {
        if (denominator == new System.Numerics.BigInteger(0))
        {
            throw new System.ArgumentException("Denominator must not be zero.");
        }

        if (numerator == new System.Numerics.BigInteger(0))
        {
            _numerator = new System.Numerics.BigInteger(0);
            _denominator = new System.Numerics.BigInteger(1);
            return;
        }

        if (denominator < new System.Numerics.BigInteger(0))
        {
            numerator = new System.Numerics.BigInteger(0) - numerator;
            denominator = new System.Numerics.BigInteger(0) - denominator;
        }

        System.Numerics.BigInteger divisor = System.Numerics.BigInteger.GreatestCommonDivisor(System.Numerics.BigInteger.Abs(numerator), denominator);

        _numerator = numerator / divisor;
        _denominator = denominator / divisor;
    }

    internal static long ToLong(System.Numerics.BigInteger value, string fieldName)
    {
        if (value < new System.Numerics.BigInteger(long.MinValue) || value > new System.Numerics.BigInteger(long.MaxValue))
        {
            throw new System.OverflowException(fieldName + " does not fit in Int64.");
        }

        return (long)value;
    }
}
