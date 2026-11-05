using System;
using System.IO;

namespace pdf;

/// <summary>
/// Defines operations for loading and validating JPEG images.
/// </summary>
public interface IPdfJpegLoader
{
    /// <summary>
    /// Loads a JPEG image from an in-memory byte array.
    /// </summary>
    /// <param name="jpegData">The complete encoded JPEG data.</param>
    /// <returns>The validated JPEG metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when JPEG data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when JPEG data is malformed or unsupported.</exception>
    PdfJpegData Load(byte[] jpegData);

    /// <summary>
    /// Loads a JPEG image from a read-only memory span.
    /// </summary>
    /// <param name="jpegData">The complete encoded JPEG data.</param>
    /// <returns>The validated JPEG metadata.</returns>
    /// <exception cref="InvalidDataException">Thrown when JPEG data is malformed or unsupported.</exception>
    PdfJpegData LoadFromSpan(ReadOnlySpan<byte> jpegData);

    /// <summary>
    /// Loads a JPEG image from the current position of a readable stream without taking ownership of it.
    /// </summary>
    /// <param name="stream">The readable stream containing complete encoded JPEG data.</param>
    /// <returns>The validated JPEG metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when JPEG data is malformed or unsupported.</exception>
    PdfJpegData LoadFromStream(Stream stream);

    /// <summary>
    /// Loads a JPEG image from a file.
    /// </summary>
    /// <param name="path">The path to the JPEG file.</param>
    /// <returns>The validated JPEG metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when JPEG data is malformed or unsupported.</exception>
    PdfJpegData LoadFromFile(string path);
}
