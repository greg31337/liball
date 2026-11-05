namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="RationalNumber"/>.
/// </summary>
public interface IRationalNumber
{
    /// <summary>
    /// Gets the numerator.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public long GetNumerator();
    /// <summary>
    /// Gets the denominator.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public long GetDenominator();
    /// <summary>
    /// Gets the numerator as big integer.
    /// </summary>
    /// <returns>The resulting system.numerics.big integer.</returns>
    public System.Numerics.BigInteger GetNumeratorAsBigInteger();
    /// <summary>
    /// Gets the denominator as big integer.
    /// </summary>
    /// <returns>The resulting system.numerics.big integer.</returns>
    public System.Numerics.BigInteger GetDenominatorAsBigInteger();
    /// <summary>
    /// Gets the numerator text.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetNumeratorText();
    /// <summary>
    /// Gets the denominator text.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetDenominatorText();
    /// <summary>
    /// Reports whether the rational value is zero.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsZero();
    /// <summary>
    /// Reports whether the rational value is greater than zero.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsPositive();
    /// <summary>
    /// Reports whether the rational value is less than zero.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsNegative();
    /// <summary>
    /// Converts the exact rational value to double precision.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double ToDouble();
    /// <summary>
    /// Reports whether the supplied rational number has the same normalized value.
    /// </summary>
    /// <param name="value">The rational number to compare.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool EqualsValue(RationalNumber? value);
    /// <summary>
    /// Adds the supplied rational number using a common denominator and normalizes the result.
    /// </summary>
    /// <param name="value">The rational number to add.</param>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber Add(RationalNumber value);
    /// <summary>
    /// Subtracts the supplied rational number using a common denominator and normalizes the result.
    /// </summary>
    /// <param name="value">The rational number to subtract.</param>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber Subtract(RationalNumber value);
    /// <summary>
    /// Multiplies the numerators and denominators and normalizes the result.
    /// </summary>
    /// <param name="value">The rational number to multiply by.</param>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber Multiply(RationalNumber value);
    /// <summary>
    /// Multiplies by the reciprocal of the supplied nonzero rational number.
    /// </summary>
    /// <param name="value">The nonzero rational number to divide by.</param>
    /// <exception cref="System.ArgumentException">The supplied rational number is zero.</exception>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber Divide(RationalNumber value);
    /// <summary>
    /// Compares this rational number with the supplied value.
    /// </summary>
    /// <param name="value">The rational number to compare.</param>
    /// <returns>Negative one, zero, or positive one when this value is less than, equal to, or greater than the supplied value.</returns>
    public int CompareTo(RationalNumber value);
}
