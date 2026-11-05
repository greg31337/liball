using System;

namespace http;

/// <summary>
/// Parses raw HTTP/2 frames.
/// </summary>
public interface IHttp2FrameCodec
{
    /// <summary>
    /// Parses one frame with incremental progress and error metadata.
    /// </summary>
    /// <param name="input">The available encoded frame bytes.</param>
    /// <returns>The frame parse result.</returns>
    Http2FrameResult ParseHttp2Frame(byte[] input);

    /// <summary>
    /// Serializes one decoded or extension frame.
    /// </summary>
    /// <param name="frame">The frame to serialize.</param>
    /// <returns>The frame serialization result.</returns>
    Http2FrameResult SerializeHttp2Frame(Http2Frame frame);

    /// <summary>Writes one HTTP/2 frame into bounded caller-provided storage.</summary>
    /// <param name="frame">The frame to serialize.</param>
    /// <param name="output">The destination storage.</param>
    /// <returns>The status and exact written or required size.</returns>
    Http2FrameResult WriteHttp2Frame(Http2Frame frame, Span<byte> output);
}
