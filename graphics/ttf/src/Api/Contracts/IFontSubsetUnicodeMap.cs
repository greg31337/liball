namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontSubsetUnicodeMap"/>.</summary>
public interface IFontSubsetUnicodeMap
{
    /// <summary>Gets the glyph identifier in the source font instance.</summary>
    /// <returns>The source glyph identifier.</returns>
    int GetOriginalGlyphId();

    /// <summary>Gets the glyph identifier in the emitted font.</summary>
    /// <returns>The subset glyph identifier.</returns>
    int GetSubsetGlyphId();

    /// <summary>Copies the exact Unicode scalar sequence for this mapping.</summary>
    /// <returns>A new array containing the Unicode scalar sequence.</returns>
    int[] GetUnicodeSequence();
}
