using System;
using System.IO;

namespace pdf;

/// <summary>
/// Loads JPEG images and validates their marker structure using built-in .NET APIs.
/// </summary>
public class PdfJpegLoader : IPdfJpegLoader
{
    /// <summary>
    /// Initializes a JPEG loader.
    /// </summary>
    public PdfJpegLoader()
    {
    }

    /// <summary>
    /// Loads a JPEG image from an in-memory byte array.
    /// </summary>
    /// <param name="jpegData">The complete encoded JPEG data.</param>
    /// <returns>The validated JPEG metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when JPEG data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when JPEG data is malformed or unsupported.</exception>
    public PdfJpegData Load(byte[] jpegData)
    {
        if (jpegData == null)
        {
            throw new ArgumentNullException("jpegData");
        }

        byte[] ownedData = new byte[jpegData.Length];
        jpegData.CopyTo(ownedData, 0);
        return PdfJpegParser.Parse(ownedData);
    }

    /// <summary>
    /// Loads a JPEG image from a read-only memory span.
    /// </summary>
    /// <param name="jpegData">The complete encoded JPEG data.</param>
    /// <returns>The validated JPEG metadata.</returns>
    /// <exception cref="InvalidDataException">Thrown when JPEG data is malformed or unsupported.</exception>
    public PdfJpegData LoadFromSpan(ReadOnlySpan<byte> jpegData)
    {
        return PdfJpegParser.Parse(jpegData.ToArray());
    }

    /// <summary>
    /// Loads a JPEG image from the current position of a readable stream without taking ownership of it.
    /// </summary>
    /// <param name="stream">The readable stream containing complete encoded JPEG data.</param>
    /// <returns>The validated JPEG metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when JPEG data is malformed or unsupported.</exception>
    public PdfJpegData LoadFromStream(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (!stream.CanRead)
        {
            throw new ArgumentException("JPEG stream must be readable.", "stream");
        }

        using (MemoryStream output = new MemoryStream())
        {
            stream.CopyTo(output);
            return PdfJpegParser.Parse(output.ToArray());
        }
    }

    /// <summary>
    /// Loads a JPEG image from a file.
    /// </summary>
    /// <param name="path">The path to the JPEG file.</param>
    /// <returns>The validated JPEG metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when JPEG data is malformed or unsupported.</exception>
    public PdfJpegData LoadFromFile(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        return PdfJpegParser.Parse(File.ReadAllBytes(path));
    }
}
