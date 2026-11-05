using System;

namespace pdf;

/// <summary>
/// Defines an ordered collection of loaded fonts used to resolve Unicode values.
/// </summary>
public interface IPdfFontFallbackChain
{
    /// <summary>
    /// Appends a font to the fallback chain unless the same font identity is already present.
    /// </summary>
    /// <param name="font">The loaded font to append.</param>
    /// <exception cref="ArgumentNullException">Thrown when font is null.</exception>
    void AddFont(PdfTrueTypeFont font);

    /// <summary>
    /// Returns the number of fonts in the fallback chain.
    /// </summary>
    /// <returns>The current font count.</returns>
    int GetFontCount();

    /// <summary>
    /// Returns the font at the requested zero-based position.
    /// </summary>
    /// <param name="index">The zero-based font position.</param>
    /// <returns>The loaded font at the requested position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is outside the chain.</exception>
    PdfTrueTypeFont GetFont(int index);

    /// <summary>
    /// Returns the first font containing the supplied Unicode scalar value.
    /// </summary>
    /// <param name="unicodeValue">The Unicode scalar value to resolve.</param>
    /// <returns>The first matching loaded font.</returns>
    /// <exception cref="ArgumentException">Thrown when no font contains the value.</exception>
    PdfTrueTypeFont FindFontForUnicode(int unicodeValue);
}
