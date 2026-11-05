namespace ttf;

/// <summary>Describes one resolved or missing Unicode character mapping.</summary>
public class FontCharacterMapping : IFontCharacterMapping
{
    int unicodeValue;
    int variationSelector;
    int glyphId;
    FontCharacterMappingKind kind;
    FontCharacterMappingSource? source;
    FontCharacterMappingSource? variationSource;

    internal FontCharacterMapping(
        int unicodeValue,
        int variationSelector,
        int glyphId,
        FontCharacterMappingKind kind,
        FontCharacterMappingSource? source,
        FontCharacterMappingSource? variationSource)
    {
        this.unicodeValue = unicodeValue;
        this.variationSelector = variationSelector;
        this.glyphId = glyphId;
        this.kind = kind;
        this.source = source;
        this.variationSource = variationSource;
    }

    /// <summary>Gets the requested Unicode scalar value.</summary>
    /// <returns>The Unicode scalar value.</returns>
    public int GetUnicodeValue() { return unicodeValue; }

    /// <summary>Reports whether the query included a variation selector.</summary>
    /// <returns>True for a variation-sequence query.</returns>
    public bool HasVariationSelector() { return variationSelector >= 0; }

    /// <summary>Gets the requested variation selector.</summary>
    /// <returns>The variation-selector scalar.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The query did not include a variation selector.
    /// </exception>
    public int GetVariationSelector()
    {
        if (variationSelector < 0)
        {
            throw new System.InvalidOperationException(
                "The mapping query did not include a variation selector.");
        }

        return variationSelector;
    }

    /// <summary>Gets the number of Unicode scalars in this query sequence.</summary>
    /// <returns>One for nominal mappings or two for variation mappings.</returns>
    public int GetUnicodeSequenceLength()
    {
        return variationSelector < 0 ? 1 : 2;
    }

    /// <summary>Gets one scalar from this query sequence.</summary>
    /// <param name="index">The zero-based scalar index.</param>
    /// <returns>The Unicode scalar.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public int GetUnicodeSequenceScalar(int index)
    {
        if (index == 0)
        {
            return unicodeValue;
        }

        if (index == 1 && variationSelector >= 0)
        {
            return variationSelector;
        }

        throw new System.ArgumentOutOfRangeException("index");
    }

    /// <summary>Copies the Unicode scalar sequence represented by this query.</summary>
    /// <returns>A new array containing one or two Unicode scalars.</returns>
    public int[] CopyUnicodeSequence()
    {
        if (variationSelector < 0)
        {
            return new int[] { unicodeValue };
        }

        return new int[] { unicodeValue, variationSelector };
    }

    /// <summary>Reports whether the query resolved to a nonzero glyph identifier.</summary>
    /// <returns>True when a mapping was found.</returns>
    public bool IsMapped() { return glyphId != 0; }

    /// <summary>Gets the resolved glyph identifier.</summary>
    /// <returns>The glyph identifier, or zero for a missing mapping.</returns>
    public int GetGlyphId() { return glyphId; }

    /// <summary>Gets the mapping resolution kind.</summary>
    /// <returns>The resolution kind.</returns>
    public FontCharacterMappingKind GetKind() { return kind; }

    /// <summary>Gets the subtable that supplied the resolved glyph.</summary>
    /// <returns>The source, or null for a missing mapping.</returns>
    public FontCharacterMappingSource? GetSource() { return source; }

    /// <summary>Gets the format 14 source consulted by a variation query.</summary>
    /// <returns>The variation source, or null when none was available.</returns>
    public FontCharacterMappingSource? GetVariationSource()
    {
        return variationSource;
    }
}
