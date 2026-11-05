using System;

namespace rtp;

/// <summary>
/// Provides RFC 4571 framing for connection-oriented RTP and RTCP transport.
/// </summary>
public class RtpStreamFramingCodec : IRtpStreamFramingCodec
{
    /// <summary>
    /// Initializes an RTP stream framing codec.
    /// </summary>
    public RtpStreamFramingCodec()
    {
    }

    /// <summary>Calculates the encoded frame size for one datagram.</summary>
    /// <param name="datagram">The datagram payload to frame.</param>
    /// <returns>The required destination size in bytes.</returns>
    public int GetRtpStreamFrameEncodedSize(ReadOnlySpan<byte> datagram)
    {
        return RtpStreamFramingWireCodec.GetEncodedSize(datagram);
    }

    /// <summary>Encodes one length-prefixed frame or throws for invalid capacity.</summary>
    /// <param name="datagram">The datagram payload to frame.</param>
    /// <param name="destination">The destination receiving the frame.</param>
    /// <returns>The number of bytes written.</returns>
    public int EncodeRtpStreamFrame(
        ReadOnlySpan<byte> datagram,
        Span<byte> destination)
    {
        return RtpStreamFramingWireCodec.EncodeFrame(datagram, destination);
    }

    /// <summary>Attempts to encode one frame without throwing for expected failure.</summary>
    /// <param name="datagram">The datagram payload to frame.</param>
    /// <param name="destination">The destination receiving the frame.</param>
    /// <returns>The encode outcome and written count.</returns>
    public RtpCodecResult TryEncodeRtpStreamFrame(
        ReadOnlySpan<byte> datagram,
        Span<byte> destination)
    {
        return RtpStreamFramingWireCodec.TryEncodeFrame(datagram, destination);
    }

    /// <summary>Decodes one complete frame or throws for malformed input.</summary>
    /// <param name="source">The length-prefixed frame bytes.</param>
    /// <returns>The newly owned unframed datagram bytes.</returns>
    public byte[] DecodeRtpStreamFrame(ReadOnlySpan<byte> source)
    {
        return RtpStreamFramingWireCodec.DecodeFrame(source);
    }

    /// <summary>Attempts to decode one incremental frame without expected exceptions.</summary>
    /// <param name="source">The available length-prefixed stream bytes.</param>
    /// <returns>The outcome, consumed count, and owned datagram when complete.</returns>
    public RtpCodecResult TryDecodeRtpStreamFrame(ReadOnlySpan<byte> source)
    {
        return RtpStreamFramingWireCodec.TryDecodeFrame(source);
    }
}
