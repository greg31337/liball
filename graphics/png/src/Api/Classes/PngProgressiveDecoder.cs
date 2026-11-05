using System.IO;

namespace png;

/// <summary>
/// Provides interface-driven progressive PNG decoding.
/// </summary>
public class PngProgressiveDecoder : IPngProgressiveDecoder
{
    /// <summary>
    /// Initializes a stateless progressive PNG decoder.
    /// </summary>
    public PngProgressiveDecoder()
    {
    }

    /// <summary>Progressively decodes a PNG byte array.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <param name="sink">The progressive update receiver.</param>
    /// <returns>The fully decoded image.</returns>
    public PngImage DecodeProgressiveBytes(byte[] data, PngDecoderOptions options, IPngProgressiveSink sink)
    {
        return PngProgressiveDecoderCore.DecodeBytes(data, options, sink);
    }

    /// <summary>Progressively decodes PNG data beginning at an array offset.</summary>
    /// <param name="data">The array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based offset of the PNG signature.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <param name="sink">The progressive update receiver.</param>
    /// <returns>The fully decoded image.</returns>
    public PngImage DecodeProgressiveBytesAtOffset(byte[] data, int offset, PngDecoderOptions options, IPngProgressiveSink sink)
    {
        return PngProgressiveDecoderCore.DecodeBytesAtOffset(data, offset, options, sink);
    }

    /// <summary>Progressively decodes a bounded read-only byte span.</summary>
    /// <param name="data">The bounded PNG datastream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <param name="sink">The progressive update receiver.</param>
    /// <returns>The fully decoded image.</returns>
    public PngImage DecodeProgressiveSpan(ReadOnlySpan<byte> data, PngDecoderOptions options, IPngProgressiveSink sink)
    {
        return PngProgressiveDecoderCore.DecodeSpan(data, options, sink);
    }

    /// <summary>Progressively decodes PNG data from the current stream position.</summary>
    /// <param name="stream">The readable stream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <param name="sink">The progressive update receiver.</param>
    /// <returns>The fully decoded image.</returns>
    public PngImage DecodeProgressiveStream(Stream stream, PngDecoderOptions options, IPngProgressiveSink sink)
    {
        return PngProgressiveDecoderCore.DecodeStream(stream, options, sink);
    }
}
