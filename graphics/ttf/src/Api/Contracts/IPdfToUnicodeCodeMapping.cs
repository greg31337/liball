namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfToUnicodeCodeMapping"/>.</summary>
public interface IPdfToUnicodeCodeMapping
{
    /// <summary>Gets the allocated PDF character code.</summary>
    /// <returns>A one-byte simple-font code or a two-byte Type 0 code.</returns>
    int GetCharacterCode();

    /// <summary>Gets the source glyph selected by the character code.</summary>
    /// <returns>The source-font glyph identifier.</returns>
    int GetSourceGlyphId();

    /// <summary>Gets the number of original Unicode sequence candidates.</summary>
    /// <returns>One or more distinct sequences.</returns>
    int GetUnicodeSequenceCount();

    /// <summary>Reports whether the code has exactly one Unicode sequence.</summary>
    /// <returns>True when a font-level ToUnicode mapping is unambiguous.</returns>
    bool IsUnambiguous();

    /// <summary>Reports whether the resolved CMap contains this code.</summary>
    /// <returns>True when one candidate owns the font-level mapping.</returns>
    bool HasCMapMapping();

    /// <summary>Gets the sequence selected by the stable CMap policy.</summary>
    /// <returns>The candidate index, or -1 when another code owns the sequence.</returns>
    int GetCMapUnicodeSequenceIndex();

    /// <summary>Copies the sequence selected by the stable CMap policy.</summary>
    /// <returns>A new array containing the selected Unicode sequence.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Another character code owns the font-level mapping for this sequence.
    /// </exception>
    int[] GetCMapUnicodeSequence();

    /// <summary>Gets the scalar count in one original Unicode sequence.</summary>
    /// <param name="sequenceIndex">The zero-based sequence index.</param>
    /// <returns>The number of Unicode scalars in the sequence.</returns>
    int GetUnicodeScalarCount(int sequenceIndex);

    /// <summary>Gets one scalar from an original Unicode sequence.</summary>
    /// <param name="sequenceIndex">The zero-based sequence index.</param>
    /// <param name="scalarIndex">The zero-based scalar index.</param>
    /// <returns>A valid Unicode scalar.</returns>
    int GetUnicodeScalar(int sequenceIndex, int scalarIndex);

    /// <summary>Copies one original Unicode sequence.</summary>
    /// <param name="sequenceIndex">The zero-based sequence index.</param>
    /// <returns>A new array containing the original Unicode scalars.</returns>
    int[] GetUnicodeSequence(int sequenceIndex);
}
