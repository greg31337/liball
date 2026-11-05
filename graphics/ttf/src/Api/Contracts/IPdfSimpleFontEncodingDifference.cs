namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfSimpleFontEncodingDifference"/>.</summary>
public interface IPdfSimpleFontEncodingDifference
{
    /// <summary>Gets the one-byte PDF character code.</summary>
    /// <returns>A value from zero through 255.</returns>
    int GetCharacterCode();

    /// <summary>Gets the corresponding glyph in the source font instance.</summary>
    /// <returns>The source glyph identifier.</returns>
    int GetSourceGlyphId();

    /// <summary>Gets the glyph name to place in the Differences array.</summary>
    /// <returns>The parsed or stable synthetic glyph name.</returns>
    string GetGlyphName();
}
