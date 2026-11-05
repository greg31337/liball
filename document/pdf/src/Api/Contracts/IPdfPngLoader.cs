using System;
using System.IO;

namespace pdf;

/// <summary>
/// Defines operations for loading and validating baseline PNG images.
/// </summary>
public interface IPdfPngLoader
{
    /// <summary>
    /// Loads a PNG image from an in-memory byte array.
    /// </summary>
    /// <param name="pngData">The complete encoded PNG data.</param>
    /// <returns>The validated and decoded PNG metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when PNG data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when PNG data is malformed, animated, or unsupported.</exception>
    PdfPngData Load(byte[] pngData);

    /// <summary>
    /// Loads a PNG image from a read-only memory span.
    /// </summary>
    /// <param name="pngData">The complete encoded PNG data.</param>
    /// <returns>The validated and decoded PNG metadata.</returns>
    /// <exception cref="InvalidDataException">Thrown when PNG data is malformed, animated, or unsupported.</exception>
    PdfPngData LoadFromSpan(ReadOnlySpan<byte> pngData);

    /// <summary>
    /// Loads a PNG image from the current position of a readable stream without taking ownership of it.
    /// </summary>
    /// <param name="stream">The readable stream containing complete encoded PNG data.</param>
    /// <returns>The validated and decoded PNG metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when PNG data is malformed, animated, or unsupported.</exception>
    PdfPngData LoadFromStream(Stream stream);

    /// <summary>
    /// Loads a PNG image from a file.
    /// </summary>
    /// <param name="path">The path to the PNG file.</param>
    /// <returns>The validated and decoded PNG metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when PNG data is malformed, animated, or unsupported.</exception>
    PdfPngData LoadFromFile(string path);
}
