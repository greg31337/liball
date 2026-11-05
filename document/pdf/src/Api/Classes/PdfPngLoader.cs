using System;
using System.IO;

namespace pdf;

/// <summary>
/// Loads, validates, and decodes baseline PNG images using built-in .NET APIs.
/// </summary>
public class PdfPngLoader : IPdfPngLoader
{
    /// <summary>
    /// Initializes a PNG loader.
    /// </summary>
    public PdfPngLoader()
    {
    }

    /// <summary>
    /// Loads a PNG image from an in-memory byte array.
    /// </summary>
    /// <param name="pngData">The complete encoded PNG data.</param>
    /// <returns>The validated and decoded PNG metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when PNG data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when PNG data is malformed, animated, or unsupported.</exception>
    public PdfPngData Load(byte[] pngData)
    {
        if (pngData == null)
        {
            throw new ArgumentNullException("pngData");
        }

        byte[] ownedData = new byte[pngData.Length];
        pngData.CopyTo(ownedData, 0);
        return PdfPngParser.Parse(ownedData);
    }

    /// <summary>
    /// Loads a PNG image from a read-only memory span.
    /// </summary>
    /// <param name="pngData">The complete encoded PNG data.</param>
    /// <returns>The validated and decoded PNG metadata.</returns>
    /// <exception cref="InvalidDataException">Thrown when PNG data is malformed, animated, or unsupported.</exception>
    public PdfPngData LoadFromSpan(ReadOnlySpan<byte> pngData)
    {
        return PdfPngParser.Parse(pngData.ToArray());
    }

    /// <summary>
    /// Loads a PNG image from the current position of a readable stream without taking ownership of it.
    /// </summary>
    /// <param name="stream">The readable stream containing complete encoded PNG data.</param>
    /// <returns>The validated and decoded PNG metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when PNG data is malformed, animated, or unsupported.</exception>
    public PdfPngData LoadFromStream(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (!stream.CanRead)
        {
            throw new ArgumentException("PNG stream must be readable.", "stream");
        }

        using (MemoryStream output = new MemoryStream())
        {
            stream.CopyTo(output);
            return PdfPngParser.Parse(output.ToArray());
        }
    }

    /// <summary>
    /// Loads a PNG image from a file.
    /// </summary>
    /// <param name="path">The path to the PNG file.</param>
    /// <returns>The validated and decoded PNG metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when PNG data is malformed, animated, or unsupported.</exception>
    public PdfPngData LoadFromFile(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        return PdfPngParser.Parse(File.ReadAllBytes(path));
    }
}
