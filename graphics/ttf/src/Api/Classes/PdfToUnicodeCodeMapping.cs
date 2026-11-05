namespace ttf;

/// <summary>Associates one allocated PDF code with its original text.</summary>
/// <remarks>
/// A code normally has one Unicode sequence. More than one sequence is retained
/// when the same glyph and code was produced from different source text; the
/// caller can inspect that ambiguity without losing either original sequence.
/// </remarks>
public class PdfToUnicodeCodeMapping : IPdfToUnicodeCodeMapping
{
    int characterCode;
    int sourceGlyphId;
    int[][] unicodeSequences;
    int cmapSequenceIndex;

    internal PdfToUnicodeCodeMapping(
        int characterCode,
        int sourceGlyphId,
        int[][] unicodeSequences,
        int cmapSequenceIndex)
    {
        this.characterCode = characterCode;
        this.sourceGlyphId = sourceGlyphId;
        this.unicodeSequences = CopySequences(unicodeSequences);
        this.cmapSequenceIndex = cmapSequenceIndex;
    }

    /// <summary>Gets the allocated PDF character code.</summary>
    /// <returns>A one-byte simple-font code or a two-byte Type 0 code.</returns>
    public int GetCharacterCode() { return characterCode; }

    /// <summary>Gets the source glyph selected by the character code.</summary>
    /// <returns>The source-font glyph identifier.</returns>
    public int GetSourceGlyphId() { return sourceGlyphId; }

    /// <summary>Gets the number of original Unicode sequence candidates.</summary>
    /// <returns>One or more distinct sequences.</returns>
    public int GetUnicodeSequenceCount() { return unicodeSequences.Length; }

    /// <summary>Reports whether the code has exactly one Unicode sequence.</summary>
    /// <returns>True when a font-level ToUnicode mapping is unambiguous.</returns>
    public bool IsUnambiguous() { return unicodeSequences.Length == 1; }

    /// <summary>Reports whether the resolved CMap contains this code.</summary>
    /// <returns>True when one candidate owns the font-level mapping.</returns>
    public bool HasCMapMapping() { return cmapSequenceIndex >= 0; }

    /// <summary>Gets the sequence selected by the stable CMap policy.</summary>
    /// <returns>The candidate index, or -1 when another code owns the sequence.</returns>
    public int GetCMapUnicodeSequenceIndex() { return cmapSequenceIndex; }

    /// <summary>Copies the sequence selected by the stable CMap policy.</summary>
    /// <returns>A new array containing the selected Unicode sequence.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Another character code owns the font-level mapping for this sequence.
    /// </exception>
    public int[] GetCMapUnicodeSequence()
    {
        if (cmapSequenceIndex < 0)
        {
            throw new System.InvalidOperationException(
                "The character code has no resolved CMap mapping.");
        }

        return CopyIntegers(unicodeSequences[cmapSequenceIndex]);
    }

    /// <summary>Gets the scalar count in one original Unicode sequence.</summary>
    /// <param name="sequenceIndex">The zero-based sequence index.</param>
    /// <returns>The number of Unicode scalars in the sequence.</returns>
    public int GetUnicodeScalarCount(int sequenceIndex)
    {
        ValidateSequenceIndex(sequenceIndex);
        return unicodeSequences[sequenceIndex].Length;
    }

    /// <summary>Gets one scalar from an original Unicode sequence.</summary>
    /// <param name="sequenceIndex">The zero-based sequence index.</param>
    /// <param name="scalarIndex">The zero-based scalar index.</param>
    /// <returns>A valid Unicode scalar.</returns>
    public int GetUnicodeScalar(int sequenceIndex, int scalarIndex)
    {
        ValidateSequenceIndex(sequenceIndex);

        if (scalarIndex < 0 ||
            scalarIndex >= unicodeSequences[sequenceIndex].Length)
        {
            throw new System.ArgumentOutOfRangeException("scalarIndex");
        }

        return unicodeSequences[sequenceIndex][scalarIndex];
    }

    /// <summary>Copies one original Unicode sequence.</summary>
    /// <param name="sequenceIndex">The zero-based sequence index.</param>
    /// <returns>A new array containing the original Unicode scalars.</returns>
    public int[] GetUnicodeSequence(int sequenceIndex)
    {
        ValidateSequenceIndex(sequenceIndex);
        return CopyIntegers(unicodeSequences[sequenceIndex]);
    }

    void ValidateSequenceIndex(int index)
    {
        if (index < 0 || index >= unicodeSequences.Length)
        {
            throw new System.ArgumentOutOfRangeException("sequenceIndex");
        }
    }

    static int[][] CopySequences(int[][] values)
    {
        int[][] copy = new int[values.Length][];
        int index = 0;

        while (index < values.Length)
        {
            copy[index] = CopyIntegers(values[index]);
            index += 1;
        }

        return copy;
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
