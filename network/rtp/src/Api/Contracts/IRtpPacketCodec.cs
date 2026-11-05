using System;

namespace rtp;

/// <summary>
/// Defines RTP packet wire sizing, encoding, and decoding operations.
/// </summary>
public interface IRtpPacketCodec
{
    /// <summary>Calculates the exact encoded size of one RTP packet.</summary>
    /// <param name="packet">The packet to size.</param>
    /// <returns>The required destination size in bytes.</returns>
    int GetRtpPacketEncodedSize(RtpPacket packet);

    /// <summary>Encodes one RTP packet or throws when input or capacity is invalid.</summary>
    /// <param name="packet">The packet to encode.</param>
    /// <param name="destination">The destination receiving the encoded bytes.</param>
    /// <returns>The number of bytes written.</returns>
    int EncodeRtpPacket(RtpPacket packet, Span<byte> destination);

    /// <summary>Attempts to encode one RTP packet without throwing for expected failure.</summary>
    /// <param name="packet">The packet to encode.</param>
    /// <param name="destination">The destination receiving the encoded bytes.</param>
    /// <returns>The encode outcome and written count.</returns>
    RtpCodecResult TryEncodeRtpPacket(RtpPacket packet, Span<byte> destination);

    /// <summary>Encodes one RTP packet into a newly owned byte array.</summary>
    /// <param name="packet">The packet to encode.</param>
    /// <returns>The newly allocated encoded packet bytes.</returns>
    byte[] EncodeRtpPacketToArray(RtpPacket packet);

    /// <summary>Decodes one RTP packet or throws for malformed input.</summary>
    /// <param name="source">The packet bytes to decode.</param>
    /// <returns>An owned RTP packet model.</returns>
    RtpPacket DecodeRtpPacket(ReadOnlySpan<byte> source);

    /// <summary>Attempts to decode one RTP packet without throwing for malformed input.</summary>
    /// <param name="source">The packet bytes to decode.</param>
    /// <returns>The decode outcome and owned packet data.</returns>
    RtpCodecResult TryDecodeRtpPacket(ReadOnlySpan<byte> source);
}
