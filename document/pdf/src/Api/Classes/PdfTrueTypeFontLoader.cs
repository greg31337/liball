using System;
using System.IO;

namespace pdf;

/// <summary>
/// Loads validated TrueType and OpenType metadata using built-in .NET APIs.
/// </summary>
public class PdfTrueTypeFontLoader : IPdfTrueTypeFontLoader
{
    /// <summary>
    /// Initializes a font loader.
    /// </summary>
    public PdfTrueTypeFontLoader()
    {
    }

    /// <summary>
    /// Loads a font from an in-memory byte array.
    /// </summary>
    /// <param name="fontData">The complete TrueType or OpenType font data.</param>
    /// <returns>The validated font metadata and Unicode mapping.</returns>
    /// <exception cref="ArgumentNullException">Thrown when font data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    public PdfTrueTypeFont Load(byte[] fontData)
    {
        if (fontData == null)
        {
            throw new ArgumentNullException("fontData");
        }

        byte[] ownedData = new byte[fontData.Length];
        fontData.CopyTo(ownedData, 0);
        return PdfTrueTypeFontParser.Parse(ownedData);
    }

    /// <summary>
    /// Loads a font from the current position of a readable stream without taking ownership of it.
    /// </summary>
    /// <param name="stream">The readable stream containing complete TrueType or OpenType font data.</param>
    /// <returns>The validated font metadata and Unicode mapping.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    public PdfTrueTypeFont LoadFromStream(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (!stream.CanRead)
        {
            throw new ArgumentException("Font stream must be readable.", "stream");
        }

        using (MemoryStream output = new MemoryStream())
        {
            stream.CopyTo(output);
            return PdfTrueTypeFontParser.Parse(output.ToArray());
        }
    }

    /// <summary>
    /// Loads a font from a file.
    /// </summary>
    /// <param name="path">The path to a TrueType or OpenType font file.</param>
    /// <returns>The validated font metadata and Unicode mapping.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    public PdfTrueTypeFont LoadFromFile(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        return PdfTrueTypeFontParser.Parse(File.ReadAllBytes(path));
    }
}
