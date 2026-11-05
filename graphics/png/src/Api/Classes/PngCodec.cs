using System.IO;

namespace png;

/// <summary>
/// Provides the public instance-based PNG decode and encode workflows.
/// </summary>
/// <remarks>
/// Use this class directly or depend on one of its focused interfaces.
/// </remarks>
public class PngCodec : IPngDecoder, IPngEncoder
{
    /// <summary>
    /// Initializes a stateless PNG codec.
    /// </summary>
    public PngCodec()
    {
    }

    /// <summary>
    /// Decodes a PNG byte array using default options.
    /// </summary>
    /// <param name="data">The PNG datastream.</param>
    /// <returns>The decoded image.</returns>
    internal PngImage DecodeBytesWithDefaultOptions(byte[] data)
    {
        return PngDecoder.Decode(data);
    }

    /// <summary>
    /// Decodes a PNG byte array using the supplied options.
    /// </summary>
    /// <param name="data">The PNG datastream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The decoded image.</returns>
    public PngImage DecodeBytes(byte[] data, PngDecoderOptions options)
    {
        return PngDecoder.Decode(data, 0, options);
    }

    /// <summary>
    /// Decodes PNG data beginning at an offset in a byte array.
    /// </summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based offset of the PNG signature.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The decoded image.</returns>
    public PngImage DecodeBytesAtOffset(byte[] data, int offset, PngDecoderOptions options)
    {
        return PngDecoder.Decode(data, offset, options);
    }

    /// <summary>
    /// Decodes a PNG datastream bounded by a read-only byte span.
    /// </summary>
    /// <param name="data">The bounded PNG datastream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The decoded image.</returns>
    public PngImage DecodeSpan(ReadOnlySpan<byte> data, PngDecoderOptions options)
    {
        return PngDecoder.DecodeSpan(data, options);
    }

    /// <summary>
    /// Decodes PNG data from the current stream position using default options.
    /// </summary>
    /// <param name="stream">The readable stream.</param>
    /// <returns>The decoded image.</returns>
    internal PngImage DecodeStreamWithDefaultOptions(Stream stream)
    {
        return PngDecoder.Decode(stream);
    }

    /// <summary>
    /// Decodes PNG data from the current stream position using the supplied options.
    /// </summary>
    /// <param name="stream">The readable stream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The decoded image.</returns>
    public PngImage DecodeStream(Stream stream, PngDecoderOptions options)
    {
        return PngDecoder.Decode(stream, options);
    }

    /// <summary>
    /// Encodes an image to a PNG byte array using default options.
    /// </summary>
    /// <param name="image">The image to encode.</param>
    /// <returns>The encoded PNG datastream.</returns>
    internal byte[] EncodeToBytesWithDefaultOptions(PngImage image)
    {
        return PngEncoder.EncodeToBytes(image);
    }

    /// <summary>
    /// Encodes an image to a PNG byte array using the supplied options.
    /// </summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="options">The encoding options and safety limits.</param>
    /// <returns>The encoded PNG datastream.</returns>
    public byte[] EncodeToBytes(PngImage image, PngEncoderOptions options)
    {
        return PngEncoder.EncodeToBytes(image, options);
    }

    /// <summary>
    /// Encodes an image to a stream using default options.
    /// </summary>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="image">The image to encode.</param>
    internal void EncodeToStreamWithDefaultOptions(Stream stream, PngImage image)
    {
        PngEncoder.EncodeToStream(stream, image);
    }

    /// <summary>
    /// Encodes an image to a stream using the supplied options.
    /// </summary>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="image">The image to encode.</param>
    /// <param name="options">The encoding options and safety limits.</param>
    public void EncodeToStream(Stream stream, PngImage image, PngEncoderOptions options)
    {
        PngEncoder.EncodeToStream(stream, image, options);
    }
}
