using System;
using System.Collections.Generic;
using System.IO;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfFontRegistry"/>.
/// </summary>
public interface IPdfFontRegistry
{
    /// <summary>
    /// Registers a standard font for reuse while writing the document.
    /// </summary>
    /// <param name="font">The font to use for this operation.</param>
    /// <param name="resourceName">The resource name to use for this operation.</param>
    /// <param name="baseFontName">The base font name to use for this operation.</param>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    void RegisterStandardFont(PdfFont font, string resourceName, string baseFontName);

    /// <summary>
    /// Loads and registers a TrueType or OpenType font from an in-memory byte array.
    /// </summary>
    /// <param name="fontData">The complete TrueType or OpenType font data.</param>
    /// <returns>The registered font, or the existing registration when the same font data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when font data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    PdfTrueTypeFont RegisterTrueTypeFont(byte[] fontData);

    /// <summary>
    /// Loads and registers a TrueType or OpenType font from the current position of a readable stream.
    /// </summary>
    /// <param name="stream">The readable stream containing complete TrueType or OpenType font data.</param>
    /// <returns>The registered font, or the existing registration when the same font data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    PdfTrueTypeFont RegisterTrueTypeFontFromStream(Stream stream);

    /// <summary>
    /// Loads and registers a TrueType or OpenType font from a file.
    /// </summary>
    /// <param name="path">The path to a TrueType or OpenType font file.</param>
    /// <returns>The registered font, or the existing registration when the same font data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    PdfTrueTypeFont RegisterTrueTypeFontFromFile(string path);

    /// <summary>
    /// Determines whether a loaded font with the supplied deterministic identity is registered.
    /// </summary>
    /// <param name="identity">The SHA-256 font identity returned by the loader.</param>
    /// <returns>True when the font is registered; otherwise, false.</returns>
    bool HasTrueTypeFont(string identity);

    /// <summary>
    /// Returns the registered TrueType or OpenType font with the supplied deterministic identity.
    /// </summary>
    /// <param name="identity">The SHA-256 font identity returned by the loader.</param>
    /// <returns>The registered font.</returns>
    /// <exception cref="ArgumentException">Thrown when the identity is not registered.</exception>
    PdfTrueTypeFont GetTrueTypeFont(string identity);

    /// <summary>
    /// Returns the number of distinct registered TrueType and OpenType fonts.
    /// </summary>
    /// <returns>The current registered font count.</returns>
    int GetTrueTypeFontCount();

    /// <summary>
    /// Determines whether the current object has font.
    /// </summary>
    /// <param name="font">The font to use for this operation.</param>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    bool HasFont(PdfFont font);

    /// <summary>
    /// Returns the font calculated from the object's current state.
    /// </summary>
    /// <param name="font">The font to use for this operation.</param>
    /// <returns>The resulting standard font.</returns>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    PdfStandardFont GetFont(PdfFont font);

    /// <summary>
    /// Returns the resource name calculated from the object's current state.
    /// </summary>
    /// <param name="font">The font to use for this operation.</param>
    /// <returns>The resulting text.</returns>
    string GetResourceName(PdfFont font);

    /// <summary>
    /// Returns the base font name calculated from the object's current state.
    /// </summary>
    /// <param name="font">The font to use for this operation.</param>
    /// <returns>The resulting text.</returns>
    string GetBaseFontName(PdfFont font);
}
