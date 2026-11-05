namespace unicode;

/// <summary>
/// Associates a PDF font name with character-code-to-Unicode mappings.
/// </summary>
public class UnicodePdfGlyphMap : IUnicodePdfGlyphMap
{
    /// <summary>
    /// The PDF font name written into the generated ToUnicode CMap.
    /// </summary>
    internal string FontName;
    /// <summary>
    /// The character-code-to-Unicode mappings for the font.
    /// </summary>
    internal UnicodePdfGlyphMapping[] Mappings;

    /// <summary>
    /// Initializes a new instance with the supplied values.
    /// </summary>
    /// <param name="fontName">The PDF font name.</param>
    /// <param name="mappings">The glyph mappings to copy and validate.</param>
    /// <exception cref="System.ArgumentException">Thrown when <c>fontName</c> is empty or consists only of white space.</exception>
    public UnicodePdfGlyphMap(string fontName, UnicodePdfGlyphMapping[] mappings)
    {
        System.ArgumentException.ThrowIfNullOrWhiteSpace(fontName);
        ValidateMappings(mappings);

        FontName = fontName;
        Mappings = CopyMappings(mappings);
    }

    /// <summary>
    /// Returns the number of code points, mappings, scalars, or UTF-16 positions represented by this value.
    /// </summary>
    /// <returns>The resulting integer value.</returns>
    internal int Count()
    {
        return Mappings.Length;
    }

    /// <summary>
    /// Reports whether this mapping or range contains the specified PDF character code.
    /// </summary>
    /// <param name="characterCode">The PDF character code.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool ContainsCharacterCode(int characterCode)
    {
        return FindMapping(characterCode) != null;
    }

    /// <summary>
    /// Returns the glyph mapping for a PDF character code, or <c>null</c> when none exists.
    /// </summary>
    /// <param name="characterCode">The PDF character code.</param>
    /// <returns>An object containing the result described in the summary.</returns>
    internal UnicodePdfGlyphMapping? FindMapping(int characterCode)
    {
        int index = 0;
        while (index < Mappings.Length)
        {
            if (Mappings[index].CharacterCode == characterCode)
            {
                return Mappings[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>
    /// Returns the PDF font name written into the generated ToUnicode CMap.
    /// </summary>
    /// <returns>The PDF font name.</returns>
    public string GetFontName()
    {
        return FontName;
    }

    /// <summary>
    /// Returns a defensive copy of the glyph mappings.
    /// </summary>
    /// <returns>A new array containing the resulting values.</returns>
    public UnicodePdfGlyphMapping[] GetMappings()
    {
        return CopyMappings(Mappings);
    }

    internal static void ValidateMappings(UnicodePdfGlyphMapping[] mappings)
    {
        System.ArgumentNullException.ThrowIfNull(mappings);

        int index = 0;
        while (index < mappings.Length)
        {
            if (mappings[index] == null)
            {
                throw new System.ArgumentNullException("mappings");
            }

            index = index + 1;
        }
    }

    internal static UnicodePdfGlyphMapping[] CopyMappings(UnicodePdfGlyphMapping[] mappings)
    {
        System.ArgumentNullException.ThrowIfNull(mappings);

        UnicodePdfGlyphMapping[] copy = new UnicodePdfGlyphMapping[mappings.Length];
        int index = 0;
        while (index < mappings.Length)
        {
            if (mappings[index] == null)
            {
                throw new System.ArgumentNullException("mappings");
            }

            copy[index] = new UnicodePdfGlyphMapping(
                mappings[index].CharacterCode,
                mappings[index].UnicodeCodePoints);
            index = index + 1;
        }

        return copy;
    }
}
