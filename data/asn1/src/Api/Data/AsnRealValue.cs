namespace asn1;

/// <summary>
/// Describes the normalized form of an ASN.1 REAL value.
/// </summary>
public class AsnRealValue
{
    /// <summary>
    /// Indicates whether the finite payload has been reduced to its exact normalized form.
    /// </summary>
    public bool Normalized;

    /// <summary>
    /// Contains the normalized REAL category.
    /// </summary>
    public AsnRealKind Kind;

    /// <summary>
    /// Indicates whether DecimalText contains a finite decimal representation.
    /// </summary>
    public bool HasDecimalValue;

    /// <summary>
    /// Contains a finite decimal representation when HasDecimalValue is true.
    /// </summary>
    public string DecimalText = "";

    /// <summary>
    /// Contains the signed coefficient of a normalized finite decimal value.
    /// </summary>
    /// <remarks>
    /// The exact value is DecimalCoefficient multiplied by ten raised to DecimalExponent.
    /// </remarks>
    public System.Numerics.BigInteger DecimalCoefficient;

    /// <summary>
    /// Contains the base-ten exponent of a normalized finite decimal value.
    /// </summary>
    public System.Numerics.BigInteger DecimalExponent;

    /// <summary>
    /// Indicates whether Mantissa, Radix, and Exponent contain a finite binary tuple.
    /// </summary>
    public bool HasBinaryValue;

    /// <summary>
    /// Contains the signed arbitrary-size mantissa when HasBinaryValue is true.
    /// </summary>
    public System.Numerics.BigInteger Mantissa;

    /// <summary>
    /// Contains the radix, which is 2, 8, or 16, when HasBinaryValue is true.
    /// </summary>
    public int Radix;

    /// <summary>
    /// Contains the signed arbitrary-size exponent when HasBinaryValue is true.
    /// </summary>
    public System.Numerics.BigInteger Exponent;

    internal bool IsValid()
    {
        if (this.DecimalText == null)
        {
            return false;
        }

        int kindValue = (int)this.Kind;
        if (kindValue < (int)AsnRealKind.Zero || kindValue > (int)AsnRealKind.NotANumber)
        {
            return false;
        }

        if (this.Kind == AsnRealKind.FiniteDecimal)
        {
            return this.HasDecimalValue
                && !this.HasBinaryValue
                && this.DecimalText.Length > 0
                && (!this.Normalized
                    || (this.DecimalCoefficient != System.Numerics.BigInteger.Zero
                        && this.Mantissa == System.Numerics.BigInteger.Zero
                        && this.Radix == 0
                        && this.Exponent == System.Numerics.BigInteger.Zero));
        }

        if (this.Kind == AsnRealKind.FiniteBinary)
        {
            return !this.HasDecimalValue
                && this.HasBinaryValue
                && this.Mantissa != System.Numerics.BigInteger.Zero
                && (this.Radix == 2 || this.Radix == 8 || this.Radix == 16)
                && (!this.Normalized
                    || (this.Radix == 2
                        && this.Mantissa % new System.Numerics.BigInteger(2)
                            != System.Numerics.BigInteger.Zero
                        && this.DecimalText.Length == 0
                        && this.DecimalCoefficient == System.Numerics.BigInteger.Zero
                        && this.DecimalExponent == System.Numerics.BigInteger.Zero));
        }

        return !this.HasDecimalValue
            && !this.HasBinaryValue
            && (!this.Normalized
                || (this.DecimalText.Length == 0
                    && this.DecimalCoefficient == System.Numerics.BigInteger.Zero
                    && this.DecimalExponent == System.Numerics.BigInteger.Zero
                    && this.Mantissa == System.Numerics.BigInteger.Zero
                    && this.Radix == 0
                    && this.Exponent == System.Numerics.BigInteger.Zero));
    }
}
