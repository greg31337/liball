namespace unicode;

/// <summary>
/// Maps one PDF character code to one or more Unicode scalar values.
/// </summary>
public class UnicodePdfGlyphMapping
{
    /// <summary>
    /// The PDF character code associated with this mapping or diagnostic.
    /// </summary>
    public int CharacterCode;
    /// <summary>
    /// The Unicode scalar sequence produced by this PDF character code.
    /// </summary>
    public int[] UnicodeCodePoints;

    /// <summary>
    /// Initializes a new instance with the supplied values.
    /// </summary>
    /// <param name="characterCode">The PDF character code.</param>
    /// <param name="unicodeCodePoints">The Unicode scalar sequence mapped from the character code.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>characterCode</c> is outside the supported range.</exception>
    public UnicodePdfGlyphMapping(int characterCode, int[] unicodeCodePoints)
    {
        if (characterCode < 0)
        {
            throw new System.ArgumentOutOfRangeException("characterCode");
        }

        ValidateUnicodeCodePoints(unicodeCodePoints);

        CharacterCode = characterCode;
        UnicodeCodePoints = CopyCodePoints(unicodeCodePoints);
    }

    /// <summary>
    /// Returns the number of code points, mappings, scalars, or UTF-16 positions represented by this value.
    /// </summary>
    /// <returns>The resulting integer value.</returns>
    internal int Count()
    {
        return UnicodeCodePoints.Length;
    }

    /// <summary>
    /// Reports whether this PDF character code maps to exactly one Unicode scalar.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool MapsToSingleScalar()
    {
        return UnicodeCodePoints.Length == 1;
    }

    /// <summary>
    /// Returns a defensive copy of the Unicode sequence mapped from this character code.
    /// </summary>
    /// <returns>A new array containing the resulting values.</returns>
    internal int[] GetUnicodeCodePoints()
    {
        return CopyCodePoints(UnicodeCodePoints);
    }

    internal static void ValidateUnicodeCodePoints(int[] unicodeCodePoints)
    {
        System.ArgumentNullException.ThrowIfNull(unicodeCodePoints);

        if (unicodeCodePoints.Length == 0)
        {
            throw new System.ArgumentException("Glyph mapping must contain at least one Unicode scalar.", "unicodeCodePoints");
        }

        int index = 0;
        while (index < unicodeCodePoints.Length)
        {
            if (!UnicodeScalar.Instance.IsValid(unicodeCodePoints[index]))
            {
                throw new System.ArgumentOutOfRangeException("unicodeCodePoints");
            }

            index = index + 1;
        }
    }

    internal static int[] CopyCodePoints(int[] codePoints)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);

        int[] copy = new int[codePoints.Length];
        int index = 0;
        while (index < codePoints.Length)
        {
            copy[index] = codePoints[index];
            index = index + 1;
        }

        return copy;
    }
}
