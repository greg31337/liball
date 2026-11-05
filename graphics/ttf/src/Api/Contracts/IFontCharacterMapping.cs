namespace ttf;

/// <summary>Defines one resolved or missing Unicode character mapping.</summary>
public interface IFontCharacterMapping
{
    /// <summary>Gets the requested Unicode scalar value.</summary>
    /// <returns>The Unicode scalar value.</returns>
    int GetUnicodeValue();

    /// <summary>Reports whether the query included a variation selector.</summary>
    /// <returns>True for a variation-sequence query.</returns>
    bool HasVariationSelector();

    /// <summary>Gets the requested variation selector.</summary>
    /// <returns>The variation-selector scalar.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The query did not include a variation selector.
    /// </exception>
    int GetVariationSelector();

    /// <summary>Gets the number of Unicode scalars in this query sequence.</summary>
    /// <returns>One for nominal mappings or two for variation mappings.</returns>
    int GetUnicodeSequenceLength();

    /// <summary>Gets one scalar from this query sequence.</summary>
    /// <param name="index">The zero-based scalar index.</param>
    /// <returns>The Unicode scalar.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    int GetUnicodeSequenceScalar(int index);

    /// <summary>Copies the Unicode scalar sequence represented by this query.</summary>
    /// <returns>A new array containing one or two Unicode scalars.</returns>
    int[] CopyUnicodeSequence();

    /// <summary>Reports whether the query resolved to a nonzero glyph identifier.</summary>
    /// <returns>True when a mapping was found.</returns>
    bool IsMapped();

    /// <summary>Gets the resolved glyph identifier.</summary>
    /// <returns>The glyph identifier, or zero for a missing mapping.</returns>
    int GetGlyphId();

    /// <summary>Gets the mapping resolution kind.</summary>
    /// <returns>The resolution kind.</returns>
    FontCharacterMappingKind GetKind();

    /// <summary>Gets the subtable that supplied the resolved glyph.</summary>
    /// <returns>The source, or null for a missing mapping.</returns>
    FontCharacterMappingSource? GetSource();

    /// <summary>Gets the format 14 source consulted by a variation query.</summary>
    /// <returns>The variation source, or null when none was available.</returns>
    FontCharacterMappingSource? GetVariationSource();
}
