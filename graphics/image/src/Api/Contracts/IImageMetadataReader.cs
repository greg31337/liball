using System.IO;

namespace image;

/// <summary>Defines image metadata reading operations for dependency-injected consumers.</summary>
/// <remarks>Use this contract when application code needs to substitute metadata input with a test double.</remarks>
public interface IImageMetadataReader
{
    /// <summary>Reads metadata from a file using default safety limits and parser options.</summary>
    /// <param name="path">The path of the image file to open, or null.</param>
    /// <returns>An extracted document. A null or empty path produces a document with a diagnostic instead of opening a file.</returns>
    /// <exception cref="System.IO.IOException">Thrown when the file cannot be opened or read.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when the caller cannot read the file.</exception>
    ImageMetadataDocument ReadFile(string? path);

    /// <summary>Reads metadata from a file using caller-supplied safety limits and parser options.</summary>
    /// <param name="path">The path of the image file to open, or null.</param>
    /// <param name="options">The options to apply, or null to use defaults.</param>
    /// <returns>An extracted document. A null or empty path produces a document with a diagnostic instead of opening a file.</returns>
    /// <exception cref="System.IO.IOException">Thrown when the file cannot be opened or read.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when the caller cannot read the file.</exception>
    ImageMetadataDocument ReadFileWithOptions(string? path, ImageMetadataReaderOptions? options);

    /// <summary>Reads metadata from the current position of a stream using default options.</summary>
    /// <param name="stream">The readable image stream.</param>
    /// <returns>An extracted document. A null stream produces a document with a diagnostic.</returns>
    /// <exception cref="System.IO.IOException">Thrown when the underlying stream reports a read or seek failure.</exception>
    ImageMetadataDocument ReadStream(Stream? stream);

    /// <summary>Reads metadata from the current stream position using caller-supplied options.</summary>
    /// <param name="stream">The readable image stream.</param>
    /// <param name="options">The options to apply, or null to use defaults.</param>
    /// <returns>An extracted document. A null stream produces a document with a diagnostic.</returns>
    /// <exception cref="System.IO.IOException">Thrown when the underlying stream reports a read or seek failure.</exception>
    ImageMetadataDocument ReadStreamWithOptions(Stream? stream, ImageMetadataReaderOptions? options);
}
