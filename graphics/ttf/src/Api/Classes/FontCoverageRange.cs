namespace ttf;

/// <summary>Represents one inclusive Unicode scalar coverage range.</summary>
public class FontCoverageRange : IFontCoverageRange
{
    int startUnicodeValue;
    int endUnicodeValue;

    internal FontCoverageRange(int startUnicodeValue, int endUnicodeValue)
    {
        this.startUnicodeValue = startUnicodeValue;
        this.endUnicodeValue = endUnicodeValue;
    }

    /// <summary>Gets the first covered Unicode scalar value.</summary>
    /// <returns>The inclusive start value.</returns>
    public int GetStartUnicodeValue() { return startUnicodeValue; }

    /// <summary>Gets the last covered Unicode scalar value.</summary>
    /// <returns>The inclusive end value.</returns>
    public int GetEndUnicodeValue() { return endUnicodeValue; }

    /// <summary>Reports whether this range contains a Unicode scalar value.</summary>
    /// <param name="unicodeValue">The value to test.</param>
    /// <returns>True when the value is in the range.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>unicodeValue</c> is not a Unicode scalar.
    /// </exception>
    public bool Contains(int unicodeValue)
    {
        if (unicodeValue < 0 || unicodeValue > 0x10FFFF ||
            unicodeValue >= 0xD800 && unicodeValue <= 0xDFFF)
        {
            throw new System.ArgumentOutOfRangeException("unicodeValue");
        }

        return unicodeValue >= startUnicodeValue && unicodeValue <= endUnicodeValue;
    }
}
