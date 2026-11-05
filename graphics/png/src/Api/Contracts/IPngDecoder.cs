using System.IO;

namespace png;

/// <summary>
/// Defines PNG image decoding operations for applications that use dependency injection or test doubles.
/// </summary>
public interface IPngDecoder
{
    /// <summary>Decodes a PNG byte array using the supplied options.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The decoded image.</returns>
    PngImage DecodeBytes(byte[] data, PngDecoderOptions options);

    /// <summary>Decodes PNG data beginning at an offset in a byte array.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based offset of the PNG signature.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The decoded image.</returns>
    PngImage DecodeBytesAtOffset(byte[] data, int offset, PngDecoderOptions options);

    /// <summary>Decodes a PNG datastream bounded by a read-only byte span.</summary>
    /// <param name="data">The bounded PNG datastream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The decoded image.</returns>
    PngImage DecodeSpan(ReadOnlySpan<byte> data, PngDecoderOptions options);

    /// <summary>Decodes PNG data from the current stream position using the supplied options.</summary>
    /// <param name="stream">The readable stream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The decoded image.</returns>
    PngImage DecodeStream(Stream stream, PngDecoderOptions options);
}
