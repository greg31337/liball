using System.IO;

namespace png;

/// <summary>
/// Receives progressive PNG rows, Adam7 passes, optional previews, animation frames, and warnings.
/// </summary>
public interface IPngProgressiveSink
{
    /// <summary>Determines whether cumulative pixel previews are produced.</summary>
    /// <returns><c>true</c> to receive previews; otherwise <c>false</c>.</returns>
    bool WantsPreviews();

    /// <summary>Receives one reconstructed non-interlaced row.</summary>
    /// <param name="row">The row update.</param>
    void ReceiveRow(PngProgressiveRow row);

    /// <summary>Receives completion information for one Adam7 pass.</summary>
    /// <param name="pass">The pass update.</param>
    void ReceivePass(PngAdam7PassUpdate pass);

    /// <summary>Receives an optional cumulative image preview.</summary>
    /// <param name="preview">The preview update.</param>
    void ReceivePreview(PngProgressivePreview preview);

    /// <summary>Receives one decoded APNG frame in display order.</summary>
    /// <param name="frame">The decoded animation frame.</param>
    void ReceiveFrame(PngFrame frame);

    /// <summary>Receives one non-fatal decoder warning.</summary>
    /// <param name="warning">The decoder warning.</param>
    void ReceiveWarning(PngWarning warning);
}

/// <summary>
/// Defines progressive PNG decoding from arrays, bounded spans, and streams.
/// </summary>
public interface IPngProgressiveDecoder
{
    /// <summary>Progressively decodes a PNG byte array.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <param name="sink">The progressive update receiver.</param>
    /// <returns>The fully decoded image.</returns>
    PngImage DecodeProgressiveBytes(byte[] data, PngDecoderOptions options, IPngProgressiveSink sink);

    /// <summary>Progressively decodes PNG data beginning at an array offset.</summary>
    /// <param name="data">The array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based offset of the PNG signature.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <param name="sink">The progressive update receiver.</param>
    /// <returns>The fully decoded image.</returns>
    PngImage DecodeProgressiveBytesAtOffset(byte[] data, int offset, PngDecoderOptions options, IPngProgressiveSink sink);

    /// <summary>Progressively decodes a bounded read-only byte span.</summary>
    /// <param name="data">The bounded PNG datastream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <param name="sink">The progressive update receiver.</param>
    /// <returns>The fully decoded image.</returns>
    PngImage DecodeProgressiveSpan(ReadOnlySpan<byte> data, PngDecoderOptions options, IPngProgressiveSink sink);

    /// <summary>Progressively decodes PNG data from the current stream position.</summary>
    /// <param name="stream">The readable stream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <param name="sink">The progressive update receiver.</param>
    /// <returns>The fully decoded image.</returns>
    PngImage DecodeProgressiveStream(Stream stream, PngDecoderOptions options, IPngProgressiveSink sink);
}
