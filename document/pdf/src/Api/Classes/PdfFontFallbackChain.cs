using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Resolves Unicode values against an ordered collection of loaded fonts.
/// </summary>
public class PdfFontFallbackChain : IPdfFontFallbackChain
{
    internal List<PdfTrueTypeFont> _fonts;
    internal IReadOnlyList<PdfTrueTypeFont> Fonts;

    /// <summary>
    /// Initializes an empty fallback-font chain.
    /// </summary>
    public PdfFontFallbackChain()
    {
        _fonts = new List<PdfTrueTypeFont>();
        Fonts = _fonts;
    }

    /// <summary>
    /// Appends a font to the fallback chain unless the same font identity is already present.
    /// </summary>
    /// <param name="font">The loaded font to append.</param>
    /// <exception cref="ArgumentNullException">Thrown when font is null.</exception>
    public void AddFont(PdfTrueTypeFont font)
    {
        if (font == null)
        {
            throw new ArgumentNullException("font");
        }

        foreach (PdfTrueTypeFont existingFont in _fonts)
        {
            if (existingFont.Identity == font.Identity)
            {
                return;
            }
        }

        _fonts.Add(font);
    }

    /// <summary>
    /// Returns the number of fonts in the fallback chain.
    /// </summary>
    /// <returns>The current font count.</returns>
    public int GetFontCount()
    {
        return _fonts.Count;
    }

    /// <summary>
    /// Returns the font at the requested zero-based position.
    /// </summary>
    /// <param name="index">The zero-based font position.</param>
    /// <returns>The loaded font at the requested position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is outside the chain.</exception>
    public PdfTrueTypeFont GetFont(int index)
    {
        if (index < 0 || index >= _fonts.Count)
        {
            throw new ArgumentOutOfRangeException("index", "Font index must be within the fallback chain.");
        }

        return _fonts[index];
    }

    /// <summary>
    /// Returns the first font containing the supplied Unicode scalar value.
    /// </summary>
    /// <param name="unicodeValue">The Unicode scalar value to resolve.</param>
    /// <returns>The first matching loaded font.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is not a Unicode scalar value.</exception>
    /// <exception cref="ArgumentException">Thrown when no font contains the value.</exception>
    public PdfTrueTypeFont FindFontForUnicode(int unicodeValue)
    {
        if (unicodeValue < 0 || unicodeValue > 0x10FFFF || (unicodeValue >= 0xD800 && unicodeValue <= 0xDFFF))
        {
            throw new ArgumentOutOfRangeException("unicodeValue", "The value must be a Unicode scalar value.");
        }

        foreach (PdfTrueTypeFont font in _fonts)
        {
            if (font.GetGlyphIndex(unicodeValue) >= 0)
            {
                return font;
            }
        }

        throw new ArgumentException("No font in the fallback chain contains the Unicode value.", "unicodeValue");
    }
}
