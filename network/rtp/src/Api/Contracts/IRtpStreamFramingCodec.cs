using System;

namespace rtp;

/// <summary>
/// Defines RFC 4571 framing for connection-oriented RTP and RTCP transport.
/// </summary>
public interface IRtpStreamFramingCodec
{
    /// <summary>Calculates the encoded frame size for one datagram.</summary>
    /// <param name="datagram">The datagram payload to frame.</param>
    /// <returns>The required destination size in bytes.</returns>
    int GetRtpStreamFrameEncodedSize(ReadOnlySpan<byte> datagram);

    /// <summary>Encodes one length-prefixed frame or throws for invalid capacity.</summary>
    /// <param name="datagram">The datagram payload to frame.</param>
    /// <param name="destination">The destination receiving the frame.</param>
    /// <returns>The number of bytes written.</returns>
    int EncodeRtpStreamFrame(ReadOnlySpan<byte> datagram, Span<byte> destination);

    /// <summary>Attempts to encode one frame without throwing for expected failure.</summary>
    /// <param name="datagram">The datagram payload to frame.</param>
    /// <param name="destination">The destination receiving the frame.</param>
    /// <returns>The encode outcome and written count.</returns>
    RtpCodecResult TryEncodeRtpStreamFrame(
        ReadOnlySpan<byte> datagram,
        Span<byte> destination);

    /// <summary>Decodes one complete frame or throws for malformed input.</summary>
    /// <param name="source">The length-prefixed frame bytes.</param>
    /// <returns>The newly owned unframed datagram bytes.</returns>
    byte[] DecodeRtpStreamFrame(ReadOnlySpan<byte> source);

    /// <summary>Attempts to decode one incremental frame without expected exceptions.</summary>
    /// <param name="source">The available length-prefixed stream bytes.</param>
    /// <returns>The outcome, consumed count, and owned datagram when complete.</returns>
    RtpCodecResult TryDecodeRtpStreamFrame(ReadOnlySpan<byte> source);
}
