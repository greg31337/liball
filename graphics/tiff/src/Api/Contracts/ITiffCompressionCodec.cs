using System;

namespace tiff;

/// <summary>Defines a replaceable compression implementation used by the TIFF codec pipeline.</summary>
/// <remarks>The surrounding pipeline validates the context and applies fill order, predictors, and sample byte order. Implementations only encode or decode the compression format and should report failures through the returned diagnostics.</remarks>
public interface ITiffCompressionCodec
{
    /// <summary>Determines whether this implementation can decode the supplied TIFF compression value.</summary>
    /// <param name="compression">The TIFF compression value.</param>
    /// <returns><c>true</c> when this implementation can decode the compression value; otherwise, <c>false</c>.</returns>
    bool CanDecode(TiffCompression compression);

    /// <summary>Determines whether this implementation can encode the supplied TIFF compression value.</summary>
    /// <param name="compression">The TIFF compression value.</param>
    /// <returns><c>true</c> when this implementation can encode the compression value; otherwise, <c>false</c>.</returns>
    bool CanEncode(TiffCompression compression);

    /// <summary>Decodes compression-format bytes after common TIFF input normalization has been applied.</summary>
    /// <param name="context">The codec context describing compression, layout, segment size, and safety limits.</param>
    /// <param name="source">The normalized compression-format bytes.</param>
    /// <returns>A result containing decoded bytes and any diagnostics.</returns>
    TiffCodecResult Decode(TiffCodecContext context, ReadOnlySpan<byte> source);

    /// <summary>Encodes decoded bytes after common TIFF predictor and sample-order preparation has been applied.</summary>
    /// <param name="context">The codec context describing compression, layout, segment size, and safety limits.</param>
    /// <param name="source">The prepared decoded bytes.</param>
    /// <returns>A result containing compression-format bytes and any diagnostics.</returns>
    TiffCodecResult Encode(TiffCodecContext context, ReadOnlySpan<byte> source);
}
