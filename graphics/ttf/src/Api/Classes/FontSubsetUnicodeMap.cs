namespace ttf;

/// <summary>Maps one retained Unicode sequence to its source and subset glyphs.</summary>
public class FontSubsetUnicodeMap : IFontSubsetUnicodeMap
{
    int originalGlyphId;
    int subsetGlyphId;
    int[] unicodeSequence;

    internal FontSubsetUnicodeMap(
        int originalGlyphId,
        int subsetGlyphId,
        int[] unicodeSequence)
    {
        this.originalGlyphId = originalGlyphId;
        this.subsetGlyphId = subsetGlyphId;
        this.unicodeSequence = CopyIntegers(unicodeSequence);
    }

    /// <summary>Gets the glyph identifier in the source font instance.</summary>
    /// <returns>The source glyph identifier.</returns>
    public int GetOriginalGlyphId() { return originalGlyphId; }

    /// <summary>Gets the glyph identifier in the emitted font.</summary>
    /// <returns>The subset glyph identifier.</returns>
    public int GetSubsetGlyphId() { return subsetGlyphId; }

    /// <summary>Copies the exact Unicode scalar sequence for this mapping.</summary>
    /// <returns>A new array containing the Unicode scalar sequence.</returns>
    public int[] GetUnicodeSequence()
    {
        return CopyIntegers(unicodeSequence);
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
