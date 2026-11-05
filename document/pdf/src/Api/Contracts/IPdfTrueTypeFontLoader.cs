using System;
using System.IO;

namespace pdf;

/// <summary>
/// Defines operations for loading and validating TrueType and OpenType fonts.
/// </summary>
public interface IPdfTrueTypeFontLoader
{
    /// <summary>
    /// Loads a font from an in-memory byte array.
    /// </summary>
    /// <param name="fontData">The complete TrueType or OpenType font data.</param>
    /// <returns>The validated font metadata and Unicode mapping.</returns>
    /// <exception cref="ArgumentNullException">Thrown when font data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    PdfTrueTypeFont Load(byte[] fontData);

    /// <summary>
    /// Loads a font from the current position of a readable stream without taking ownership of it.
    /// </summary>
    /// <param name="stream">The readable stream containing complete TrueType or OpenType font data.</param>
    /// <returns>The validated font metadata and Unicode mapping.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    PdfTrueTypeFont LoadFromStream(Stream stream);

    /// <summary>
    /// Loads a font from a file.
    /// </summary>
    /// <param name="path">The path to a TrueType or OpenType font file.</param>
    /// <returns>The validated font metadata and Unicode mapping.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    PdfTrueTypeFont LoadFromFile(string path);
}
