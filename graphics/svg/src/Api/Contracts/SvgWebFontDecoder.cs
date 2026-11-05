namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgWebFontDecoder"/>.</summary>
public interface ISvgWebFontDecoder
{
    /// <summary>Reconstructs one web font using bounded defaults.</summary>
    /// <param name="bytes">The complete WOFF or WOFF2 bytes.</param>
    /// <returns>The reconstructed SFNT bytes or a stable failure.</returns>
    SvgWebFontDecodeResult DecodeWithBytes(byte[] bytes);
    /// <summary>Reconstructs one web font using explicit font limits and cancellation.</summary>
    /// <param name="bytes">The complete WOFF or WOFF2 bytes.</param>
    /// <param name="options">The bounded OpenType read options.</param>
    /// <returns>The reconstructed SFNT bytes or a stable failure.</returns>
    SvgWebFontDecodeResult DecodeWithBytesAndOptions(byte[]? bytes, SvgOpenTypeReadOptions options);
}
