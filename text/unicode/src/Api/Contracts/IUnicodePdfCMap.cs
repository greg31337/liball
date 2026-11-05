namespace unicode;

/// <summary>
/// Provides PDF ToUnicode CMap generation and validation operations.
/// </summary>
public interface IUnicodePdfCMap
{
    /// <summary>
    /// Validates, sorts, and serializes a supplied glyph-map snapshot as a complete PDF ToUnicode CMap.
    /// </summary>
    /// <param name="glyphMap">The PDF glyph-map source to create or validate.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>glyphMap</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the supplied values do not form a valid input.</exception>
    string CreateToUnicodeMap(IUnicodePdfGlyphMap glyphMap);

    /// <summary>
    /// Checks a supplied glyph-map snapshot for empty, duplicate, invalid, missing, or replacement Unicode values.
    /// </summary>
    /// <param name="glyphMap">The PDF glyph-map source to validate.</param>
    /// <returns>An object containing the result described in the summary.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>glyphMap</c> is <c>null</c>.</exception>
    UnicodePdfCMapValidationResult ValidateToUnicodeMap(IUnicodePdfGlyphMap glyphMap);
}

