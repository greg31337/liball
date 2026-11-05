namespace ttf;

/// <summary>Maps one simple-font character code to a source glyph name.</summary>
public class PdfSimpleFontEncodingDifference : IPdfSimpleFontEncodingDifference
{
    int characterCode;
    int sourceGlyphId;
    string glyphName;

    internal PdfSimpleFontEncodingDifference(
        int characterCode,
        int sourceGlyphId,
        string glyphName)
    {
        this.characterCode = characterCode;
        this.sourceGlyphId = sourceGlyphId;
        this.glyphName = glyphName;
    }

    /// <summary>Gets the one-byte PDF character code.</summary>
    /// <returns>A value from zero through 255.</returns>
    public int GetCharacterCode()
    {
        return characterCode;
    }

    /// <summary>Gets the corresponding glyph in the source font instance.</summary>
    /// <returns>The source glyph identifier.</returns>
    public int GetSourceGlyphId()
    {
        return sourceGlyphId;
    }

    /// <summary>Gets the glyph name to place in the Differences array.</summary>
    /// <returns>The parsed or stable synthetic glyph name.</returns>
    public string GetGlyphName()
    {
        return glyphName;
    }
}
