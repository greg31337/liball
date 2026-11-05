namespace unicode;

/// <summary>
/// Represents an optional exact integer or rational Unicode numeric value.
/// </summary>
public class UnicodeRationalValue
{
    /// <summary>
    /// Whether a numeric value is assigned.
    /// </summary>
    public bool HasValue;
    /// <summary>
    /// The signed numerator of the value.
    /// </summary>
    public long Numerator;
    /// <summary>
    /// The positive denominator of the value.
    /// </summary>
    public long Denominator;

    /// <summary>
    /// Initializes an optional exact numeric value.
    /// </summary>
    /// <param name="hasValue">Whether a numeric value is assigned.</param>
    /// <param name="numerator">The signed numerator.</param>
    /// <param name="denominator">The positive denominator.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>denominator</c> is not positive.</exception>
    public UnicodeRationalValue(bool hasValue, long numerator, long denominator)
    {
        if (denominator <= 0)
        {
            throw new System.ArgumentOutOfRangeException("denominator");
        }

        HasValue = hasValue;
        Numerator = numerator;
        Denominator = denominator;
    }
}
