namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgPngCodec"/>.</summary>
public interface ISvgPngCodec
{
    /// <summary>Probes a PNG header using secure raster defaults.</summary>
    /// <param name="bytes">The PNG byte source.</param>
    /// <returns>The intrinsic dimensions and header facts, or a stable failure.</returns>
    SvgPngProbeResult ProbeWithBytes(byte[] bytes);
    /// <summary>Probes a PNG header under explicit dimension and pixel limits.</summary>
    /// <param name="bytes">The PNG byte source.</param>
    /// <param name="limits">The raster limits applied before accepting dimensions.</param>
    /// <returns>The intrinsic dimensions and header facts, or a stable failure.</returns>
    SvgPngProbeResult ProbeWithBytesAndLimits(byte[]? bytes, SvgRasterLimits limits);
    /// <summary>Decodes one complete PNG using secure raster defaults.</summary>
    /// <param name="bytes">The complete PNG byte source.</param>
    /// <returns>The premultiplied sRGB pixels or a stable failure.</returns>
    SvgPngDecodeResult DecodeWithBytes(byte[] bytes);
    /// <summary>Decodes one complete PNG under explicit chunk, dimension, byte, and inflate limits.</summary>
    /// <param name="bytes">The complete PNG byte source.</param>
    /// <param name="limits">The raster and PNG security limits.</param>
    /// <returns>The premultiplied sRGB pixels or a stable failure.</returns>
    SvgPngDecodeResult DecodeWithBytesAndLimits(byte[]? bytes, SvgRasterLimits limits);
    /// <summary>Encodes premultiplied pixels as a deterministic RGBA8 PNG using secure defaults.</summary>
    /// <param name="pixels">The positive-size premultiplied pixel buffer.</param>
    /// <returns>The complete PNG bytes or a stable failure.</returns>
    SvgPngEncodeResult EncodeWithPixels(SvgPixelBuffer pixels);
    /// <summary>Encodes premultiplied pixels as a deterministic RGBA8 PNG.</summary>
    /// <param name="pixels">The positive-size premultiplied pixel buffer.</param>
    /// <param name="options">The filtering, compression, metadata, and security configuration.</param>
    /// <returns>The complete PNG bytes or a stable failure.</returns>
    SvgPngEncodeResult EncodeWithPixelsAndOptions(SvgPixelBuffer pixels, SvgPngEncodeOptions options);
    /// <summary>Encodes premultiplied pixels and writes the complete PNG to a writable stream using secure defaults.</summary>
    /// <param name="pixels">The positive-size premultiplied pixel buffer.</param>
    /// <param name="destination">The writable destination stream.</param>
    /// <returns>The PNG bytes and stream-write outcome.</returns>
    SvgPngEncodeResult EncodeWithPixelsAndDestination(SvgPixelBuffer pixels, System.IO.Stream destination);
    /// <summary>Encodes premultiplied pixels and writes the complete PNG to a writable stream.</summary>
    /// <param name="pixels">The positive-size premultiplied pixel buffer.</param>
    /// <param name="destination">The writable destination stream.</param>
    /// <param name="options">The filtering, compression, metadata, and security configuration.</param>
    /// <returns>The PNG bytes and stream-write outcome.</returns>
    SvgPngEncodeResult EncodeWithPixelsAndDestinationAndOptions(SvgPixelBuffer pixels, System.IO.Stream? destination, SvgPngEncodeOptions options);
    /// <summary>Encodes premultiplied pixels as a base64 PNG data URL using secure defaults.</summary>
    /// <param name="pixels">The positive-size premultiplied pixel buffer.</param>
    /// <returns>The PNG bytes and data URL or a stable failure.</returns>
    SvgPngEncodeResult EncodeDataUrlWithPixels(SvgPixelBuffer pixels);
    /// <summary>Encodes premultiplied pixels as a base64 PNG data URL.</summary>
    /// <param name="pixels">The positive-size premultiplied pixel buffer.</param>
    /// <param name="options">The filtering, compression, metadata, and security configuration.</param>
    /// <returns>The PNG bytes and data URL or a stable failure.</returns>
    SvgPngEncodeResult EncodeDataUrlWithPixelsAndOptions(SvgPixelBuffer pixels, SvgPngEncodeOptions options);
}

/// <summary>Defines the public operations exposed by <see cref="SvgImageResolver"/>.</summary>
public interface ISvgImageResolver
{
    /// <summary>Resolves an already-normalized image reference through explicit caller mediation.</summary>
    /// <param name="reference">The selected and normalized image reference.</param>
    /// <param name="options">The inherited callback, policy, cache, and limits.</param>
    /// <returns>Encoded image bytes plus callback or PNG intrinsic metadata.</returns>
    SvgImageResolveResult Resolve(SvgReferenceResult? reference, SvgImageResolveOptions options);
}
