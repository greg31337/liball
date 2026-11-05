namespace unicode;

/// <summary>
/// Supplies a named snapshot of PDF character-code-to-Unicode mappings.
/// </summary>
/// <remarks>
/// Font and PDF tooling can implement this interface to generate and validate ToUnicode CMaps
/// without first converting its mapping source to <see cref="UnicodePdfGlyphMap"/>.
/// </remarks>
public interface IUnicodePdfGlyphMap
{
    /// <summary>
    /// Returns the PDF font name written into the generated ToUnicode CMap.
    /// </summary>
    /// <returns>The PDF font name.</returns>
    string GetFontName();

    /// <summary>
    /// Returns a snapshot of the character-code-to-Unicode mappings for the font, or <c>null</c> when no mapping data is available.
    /// </summary>
    /// <returns>A new array containing the mappings, or <c>null</c>.</returns>
    UnicodePdfGlyphMapping[]? GetMappings();
}
