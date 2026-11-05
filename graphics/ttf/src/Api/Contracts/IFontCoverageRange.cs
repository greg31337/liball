namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontCoverageRange"/>.</summary>
public interface IFontCoverageRange
{
    /// <summary>Gets the first covered Unicode scalar value.</summary>
    /// <returns>The inclusive start value.</returns>
    int GetStartUnicodeValue();

    /// <summary>Gets the last covered Unicode scalar value.</summary>
    /// <returns>The inclusive end value.</returns>
    int GetEndUnicodeValue();

    /// <summary>Reports whether this range contains a Unicode scalar value.</summary>
    /// <param name="unicodeValue">The value to test.</param>
    /// <returns>True when the value is in the range.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>unicodeValue</c> is not a Unicode scalar.
    /// </exception>
    bool Contains(int unicodeValue);
}
