namespace ttf;

/// <summary>Maps one fixed-width PDF character code to Unicode scalars.</summary>
public class PdfToUnicodeMapping : IPdfToUnicodeMapping
{
    int characterCode;
    int[] unicodeSequence;

    /// <summary>Creates one explicit ToUnicode mapping.</summary>
    /// <param name="characterCode">A character code from zero through 65535.</param>
    /// <param name="unicodeSequence">One or more valid Unicode scalars.</param>
    /// <exception cref="System.ArgumentNullException">
    /// <c>unicodeSequence</c> is null.
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>characterCode</c> is outside the supported two-byte codespace.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// The Unicode sequence is empty or contains an invalid scalar.
    /// </exception>
    public PdfToUnicodeMapping(
        int characterCode,
        int[] unicodeSequence)
    {
        if (unicodeSequence == null)
        {
            throw new System.ArgumentNullException("unicodeSequence");
        }

        if (characterCode < 0 || characterCode > ushort.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("characterCode");
        }

        ValidateUnicodeSequence(unicodeSequence);
        this.characterCode = characterCode;
        this.unicodeSequence = CopyIntegers(unicodeSequence);
    }

    /// <summary>Gets the fixed-width PDF character code.</summary>
    /// <returns>A value from zero through 65535.</returns>
    public int GetCharacterCode() { return characterCode; }

    /// <summary>Gets the number of mapped Unicode scalars.</summary>
    /// <returns>At least one.</returns>
    public int GetUnicodeScalarCount() { return unicodeSequence.Length; }

    /// <summary>Gets one mapped Unicode scalar.</summary>
    /// <param name="index">The zero-based scalar index.</param>
    /// <returns>A valid Unicode scalar value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the sequence.
    /// </exception>
    public int GetUnicodeScalar(int index)
    {
        if (index < 0 || index >= unicodeSequence.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return unicodeSequence[index];
    }

    /// <summary>Copies the mapped Unicode sequence.</summary>
    /// <returns>A new array containing valid Unicode scalars.</returns>
    public int[] GetUnicodeSequence()
    {
        return CopyIntegers(unicodeSequence);
    }

    static void ValidateUnicodeSequence(int[] values)
    {
        if (values.Length == 0)
        {
            throw new System.ArgumentException(
                "A ToUnicode mapping requires at least one Unicode scalar.",
                "unicodeSequence");
        }

        int index = 0;

        while (index < values.Length)
        {
            int scalar = values[index];

            if (scalar < 0 || scalar > 0x10FFFF ||
                scalar >= 0xD800 && scalar <= 0xDFFF)
            {
                throw new System.ArgumentException(
                    "A ToUnicode mapping contains an invalid Unicode scalar.",
                    "unicodeSequence");
            }

            index += 1;
        }
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
