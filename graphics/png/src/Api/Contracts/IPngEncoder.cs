using System.IO;

namespace png;

/// <summary>
/// Defines PNG image encoding operations for applications that use dependency injection or test doubles.
/// </summary>
public interface IPngEncoder
{
    /// <summary>Encodes an image to a PNG byte array using the supplied options.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="options">The encoding options and safety limits.</param>
    /// <returns>The encoded PNG datastream.</returns>
    byte[] EncodeToBytes(PngImage image, PngEncoderOptions options);

    /// <summary>Encodes an image to a stream using the supplied options.</summary>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="image">The image to encode.</param>
    /// <param name="options">The encoding options and safety limits.</param>
    void EncodeToStream(Stream stream, PngImage image, PngEncoderOptions options);
}
