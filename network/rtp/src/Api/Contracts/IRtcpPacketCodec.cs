using System;

namespace rtp;

/// <summary>
/// Defines RTCP packet and compound-packet wire sizing, encoding, and decoding operations.
/// </summary>
public interface IRtcpPacketCodec
{
    /// <summary>Calculates the exact encoded size of one RTCP packet.</summary>
    /// <param name="packet">The packet to size.</param>
    /// <returns>The required destination size in bytes.</returns>
    int GetRtcpPacketEncodedSize(RtcpPacket packet);

    /// <summary>Encodes one RTCP packet or throws when input or capacity is invalid.</summary>
    /// <param name="packet">The packet to encode.</param>
    /// <param name="destination">The destination receiving the encoded bytes.</param>
    /// <returns>The number of bytes written.</returns>
    int EncodeRtcpPacket(RtcpPacket packet, Span<byte> destination);

    /// <summary>Attempts to encode one RTCP packet without throwing for expected failure.</summary>
    /// <param name="packet">The packet to encode.</param>
    /// <param name="destination">The destination receiving the encoded bytes.</param>
    /// <returns>The encode outcome and written count.</returns>
    RtcpCodecResult TryEncodeRtcpPacket(RtcpPacket packet, Span<byte> destination);

    /// <summary>Encodes one RTCP packet into a newly owned byte array.</summary>
    /// <param name="packet">The packet to encode.</param>
    /// <returns>The newly allocated encoded packet bytes.</returns>
    byte[] EncodeRtcpPacketToArray(RtcpPacket packet);

    /// <summary>Decodes one RTCP packet or throws for malformed input.</summary>
    /// <param name="source">The packet bytes to decode.</param>
    /// <returns>An owned RTCP packet model.</returns>
    RtcpPacket DecodeRtcpPacket(ReadOnlySpan<byte> source);

    /// <summary>Attempts to decode one RTCP packet without throwing for malformed input.</summary>
    /// <param name="source">The packet bytes to decode.</param>
    /// <returns>The decode outcome and owned packet data.</returns>
    RtcpCodecResult TryDecodeRtcpPacket(ReadOnlySpan<byte> source);

    /// <summary>Packs lost RTP sequence numbers into canonical Generic NACK entries.</summary>
    /// <param name="lostSequenceNumbers">The lost 16-bit RTP sequence numbers.</param>
    /// <returns>The canonical Generic NACK representation.</returns>
    RtcpGenericNack PackRtcpGenericNack(ushort[] lostSequenceNumbers);

    /// <summary>Expands Generic NACK entries into sorted unique RTP sequence numbers.</summary>
    /// <param name="genericNack">The Generic NACK entries to expand.</param>
    /// <returns>The wrap-aware ordered unique lost sequence numbers.</returns>
    ushort[] ExpandRtcpGenericNack(RtcpGenericNack genericNack);

    /// <summary>Advances a FIR command sequence number modulo 256.</summary>
    /// <remarks>
    /// Retain one sequence independently for each command-source and target pair,
    /// and advance it only for a new command, not for a retransmission.
    /// </remarks>
    /// <param name="sequenceNumber">The sequence number of the previous command.</param>
    /// <returns>The sequence number for a new command.</returns>
    byte AdvanceRtcpFullIntraRequestSequenceNumber(byte sequenceNumber);

    /// <summary>Gets the wrapped RTP sequence number for one congestion metric.</summary>
    /// <param name="report">The congestion-control report containing the range.</param>
    /// <param name="metricIndex">The zero-based packet metric index.</param>
    /// <returns>The represented RTP sequence number modulo 65536.</returns>
    /// <exception cref="RtcpPacketFormatException">
    /// Thrown when the report is absent or the metric index is outside its range.
    /// </exception>
    ushort GetRtcpCongestionControlSequenceNumber(
        RtcpCongestionControlReport report,
        int metricIndex);

    /// <summary>Calculates the exact encoded size of an RTCP compound packet.</summary>
    /// <param name="compoundPacket">The compound packet to size.</param>
    /// <returns>The required destination size in bytes.</returns>
    int GetRtcpCompoundPacketEncodedSize(RtcpCompoundPacket compoundPacket);

    /// <summary>Encodes an RTCP compound packet or throws for invalid input or capacity.</summary>
    /// <param name="compoundPacket">The compound packet to encode.</param>
    /// <param name="destination">The destination receiving the encoded bytes.</param>
    /// <returns>The number of bytes written.</returns>
    int EncodeRtcpCompoundPacket(RtcpCompoundPacket compoundPacket, Span<byte> destination);

    /// <summary>Attempts to encode an RTCP compound packet without expected exceptions.</summary>
    /// <param name="compoundPacket">The compound packet to encode.</param>
    /// <param name="destination">The destination receiving the encoded bytes.</param>
    /// <returns>The encode outcome and written count.</returns>
    RtcpCodecResult TryEncodeRtcpCompoundPacket(
        RtcpCompoundPacket compoundPacket,
        Span<byte> destination);

    /// <summary>Encodes an RTCP compound packet into a newly owned byte array.</summary>
    /// <param name="compoundPacket">The compound packet to encode.</param>
    /// <returns>The newly allocated encoded compound packet bytes.</returns>
    byte[] EncodeRtcpCompoundPacketToArray(RtcpCompoundPacket compoundPacket);

    /// <summary>Decodes an RTCP compound packet or throws for malformed input.</summary>
    /// <param name="source">The compound packet bytes to decode.</param>
    /// <returns>An owned compound packet model.</returns>
    RtcpCompoundPacket DecodeRtcpCompoundPacket(ReadOnlySpan<byte> source);

    /// <summary>Attempts to decode an RTCP compound packet without expected exceptions.</summary>
    /// <param name="source">The compound packet bytes to decode.</param>
    /// <returns>The decode outcome and owned compound data.</returns>
    RtcpCodecResult TryDecodeRtcpCompoundPacket(ReadOnlySpan<byte> source);
}
