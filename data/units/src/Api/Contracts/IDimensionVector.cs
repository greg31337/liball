namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="DimensionVector"/>.
/// </summary>
public interface IDimensionVector
{
    /// <summary>
    /// Gets the length exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetLengthExponent();
    /// <summary>
    /// Gets the mass exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetMassExponent();
    /// <summary>
    /// Gets the time exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetTimeExponent();
    /// <summary>
    /// Gets the current exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetCurrentExponent();
    /// <summary>
    /// Gets the temperature exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetTemperatureExponent();
    /// <summary>
    /// Gets the amount exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetAmountExponent();
    /// <summary>
    /// Gets the luminous intensity exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetLuminousIntensityExponent();
    /// <summary>
    /// Gets the angle exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetAngleExponent();
    /// <summary>
    /// Gets the solid angle exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetSolidAngleExponent();
    /// <summary>
    /// Gets the information exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetInformationExponent();
    /// <summary>
    /// Gets the currency exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetCurrencyExponent();
    /// <summary>
    /// Gets the count exponent.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetCountExponent();
    /// <summary>
    /// Gets the exponent.
    /// </summary>
    /// <param name="slot">The exponent for the slot base dimension.</param>
    /// <returns>The requested numeric value.</returns>
    public int GetExponent(int slot);
    /// <summary>
    /// Reports whether the supplied vector has the same exponent in every dimension slot.
    /// </summary>
    /// <param name="value">The dimension vector to compare.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool EqualsDimension(DimensionVector? value);
    /// <summary>
    /// Reports whether every dimension exponent is zero.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsDimensionless();
    /// <summary>
    /// Adds the supplied vector's exponents to represent dimension multiplication.
    /// </summary>
    /// <param name="value">The dimension vector whose exponents are added.</param>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Multiply(DimensionVector value);
    /// <summary>
    /// Subtracts the supplied vector's exponents to represent dimension division.
    /// </summary>
    /// <param name="value">The dimension vector whose exponents are subtracted.</param>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Divide(DimensionVector value);
    /// <summary>
    /// Multiplies every dimension exponent by the supplied integer power.
    /// </summary>
    /// <param name="power">The integer multiplier for every dimension exponent.</param>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector Pow(int power);
    /// <summary>
    /// Divides every exponent by the root when all divisions are exact.
    /// </summary>
    /// <param name="root">The positive integer divisor for every dimension exponent.</param>
    /// <returns>A result that describes success or failure.</returns>
    public DimensionRootResult Root(int root);
    /// <summary>
    /// Returns the canonical key formed from the dimension exponents.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string ToKey();
}
