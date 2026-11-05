using System;

namespace rtp;

/// <summary>
/// Provides built-in RTCP packet and compound-packet wire operations.
/// </summary>
public class RtcpPacketCodec : IRtcpPacketCodec
{
    internal bool strictValidation;
    internal bool preservePadding;
    internal RtcpCompoundMode compoundMode;
    internal int maximumPacketSize;
    internal int maximumCompoundPacketCount;

    /// <summary>
    /// Initializes an RTCP packet codec using an independent configuration snapshot.
    /// </summary>
    /// <param name="configuration">The validation, padding, and size-limit settings.</param>
    public RtcpPacketCodec(RtpSessionConfiguration configuration)
    {
        RtpConfigurationValidator.ValidateRtcpPacketCodecConfiguration(configuration);
        strictValidation = configuration.StrictValidation;
        preservePadding = configuration.PreservePadding;
        compoundMode = configuration.RtcpCompoundMode;
        maximumPacketSize = configuration.MaximumRtcpCompoundPacketSize;
        maximumCompoundPacketCount = configuration.MaximumRtcpCompoundPacketCount;
    }

    /// <summary>Calculates the exact encoded size of one RTCP packet.</summary>
    /// <param name="packet">The packet to size.</param>
    /// <returns>The required destination size in bytes.</returns>
    public int GetRtcpPacketEncodedSize(RtcpPacket packet)
    {
        return RtcpPacketWireCodec.GetPacketEncodedSize(
            packet,
            strictValidation,
            maximumPacketSize);
    }

    /// <summary>Encodes one RTCP packet or throws when input or capacity is invalid.</summary>
    /// <param name="packet">The packet to encode.</param>
    /// <param name="destination">The destination receiving the encoded bytes.</param>
    /// <returns>The number of bytes written.</returns>
    public int EncodeRtcpPacket(RtcpPacket packet, Span<byte> destination)
    {
        return RtcpPacketWireCodec.EncodePacket(
            packet,
            destination,
            strictValidation,
            preservePadding,
            maximumPacketSize);
    }

    /// <summary>Attempts to encode one RTCP packet without throwing for expected failure.</summary>
    /// <param name="packet">The packet to encode.</param>
    /// <param name="destination">The destination receiving the encoded bytes.</param>
    /// <returns>The encode outcome and written count.</returns>
    public RtcpCodecResult TryEncodeRtcpPacket(RtcpPacket packet, Span<byte> destination)
    {
        return RtcpPacketWireCodec.TryEncodePacket(
            packet,
            destination,
            strictValidation,
            preservePadding,
            maximumPacketSize);
    }

    /// <summary>Encodes one RTCP packet into a newly owned byte array.</summary>
    /// <param name="packet">The packet to encode.</param>
    /// <returns>The newly allocated encoded packet bytes.</returns>
    public byte[] EncodeRtcpPacketToArray(RtcpPacket packet)
    {
        return RtcpPacketWireCodec.EncodePacketToArray(
            packet,
            strictValidation,
            preservePadding,
            maximumPacketSize);
    }

    /// <summary>Decodes one RTCP packet or throws for malformed input.</summary>
    /// <param name="source">The packet bytes to decode.</param>
    /// <returns>An owned RTCP packet model.</returns>
    public RtcpPacket DecodeRtcpPacket(ReadOnlySpan<byte> source)
    {
        return RtcpPacketWireCodec.DecodePacket(
            source,
            strictValidation,
            preservePadding,
            maximumPacketSize);
    }

    /// <summary>Attempts to decode one RTCP packet without throwing for malformed input.</summary>
    /// <param name="source">The packet bytes to decode.</param>
    /// <returns>The decode outcome and owned packet data.</returns>
    public RtcpCodecResult TryDecodeRtcpPacket(ReadOnlySpan<byte> source)
    {
        return RtcpPacketWireCodec.TryDecodePacket(
            source,
            strictValidation,
            preservePadding,
            maximumPacketSize);
    }

    /// <summary>Packs lost RTP sequence numbers into canonical Generic NACK entries.</summary>
    /// <param name="lostSequenceNumbers">The lost 16-bit RTP sequence numbers.</param>
    /// <returns>The canonical Generic NACK representation.</returns>
    public RtcpGenericNack PackRtcpGenericNack(ushort[] lostSequenceNumbers)
    {
        return RtcpGenericNackWireCodec.PackLostSequenceNumbers(lostSequenceNumbers);
    }

    /// <summary>Expands Generic NACK entries into sorted unique RTP sequence numbers.</summary>
    /// <param name="genericNack">The Generic NACK entries to expand.</param>
    /// <returns>The wrap-aware ordered unique lost sequence numbers.</returns>
    public ushort[] ExpandRtcpGenericNack(RtcpGenericNack genericNack)
    {
        return RtcpGenericNackWireCodec.ExpandLostSequenceNumbers(genericNack);
    }

    /// <summary>Advances a FIR command sequence number modulo 256.</summary>
    /// <remarks>
    /// Retain one sequence independently for each command-source and target pair,
    /// and advance it only for a new command, not for a retransmission.
    /// </remarks>
    /// <param name="sequenceNumber">The sequence number of the previous command.</param>
    /// <returns>The sequence number for a new command.</returns>
    public byte AdvanceRtcpFullIntraRequestSequenceNumber(byte sequenceNumber)
    {
        return RtcpFullIntraRequestWireCodec.
            AdvanceFullIntraRequestSequenceNumber(sequenceNumber);
    }

    /// <summary>Gets the wrapped RTP sequence number for one congestion metric.</summary>
    /// <param name="report">The congestion-control report containing the range.</param>
    /// <param name="metricIndex">The zero-based packet metric index.</param>
    /// <returns>The represented RTP sequence number modulo 65536.</returns>
    /// <exception cref="RtcpPacketFormatException">
    /// Thrown when the report is absent or the metric index is outside its range.
    /// </exception>
    public ushort GetRtcpCongestionControlSequenceNumber(
        RtcpCongestionControlReport report,
        int metricIndex)
    {
        return RtcpCongestionControlFeedbackWireCodec.
            GetCongestionControlSequenceNumber(report, metricIndex);
    }

    /// <summary>Calculates the exact encoded size of an RTCP compound packet.</summary>
    /// <param name="compoundPacket">The compound packet to size.</param>
    /// <returns>The required destination size in bytes.</returns>
    public int GetRtcpCompoundPacketEncodedSize(RtcpCompoundPacket compoundPacket)
    {
        return RtcpPacketWireCodec.GetCompoundEncodedSize(
            compoundPacket,
            strictValidation,
            compoundMode,
            maximumPacketSize,
            maximumCompoundPacketCount);
    }

    /// <summary>Encodes an RTCP compound packet or throws for invalid input or capacity.</summary>
    /// <param name="compoundPacket">The compound packet to encode.</param>
    /// <param name="destination">The destination receiving the encoded bytes.</param>
    /// <returns>The number of bytes written.</returns>
    public int EncodeRtcpCompoundPacket(
        RtcpCompoundPacket compoundPacket,
        Span<byte> destination)
    {
        return RtcpPacketWireCodec.EncodeCompound(
            compoundPacket,
            destination,
            strictValidation,
            preservePadding,
            compoundMode,
            maximumPacketSize,
            maximumCompoundPacketCount);
    }

    /// <summary>Attempts to encode an RTCP compound packet without expected exceptions.</summary>
    /// <param name="compoundPacket">The compound packet to encode.</param>
    /// <param name="destination">The destination receiving the encoded bytes.</param>
    /// <returns>The encode outcome and written count.</returns>
    public RtcpCodecResult TryEncodeRtcpCompoundPacket(
        RtcpCompoundPacket compoundPacket,
        Span<byte> destination)
    {
        return RtcpPacketWireCodec.TryEncodeCompound(
            compoundPacket,
            destination,
            strictValidation,
            preservePadding,
            compoundMode,
            maximumPacketSize,
            maximumCompoundPacketCount);
    }

    /// <summary>Encodes an RTCP compound packet into a newly owned byte array.</summary>
    /// <param name="compoundPacket">The compound packet to encode.</param>
    /// <returns>The newly allocated encoded compound packet bytes.</returns>
    public byte[] EncodeRtcpCompoundPacketToArray(RtcpCompoundPacket compoundPacket)
    {
        return RtcpPacketWireCodec.EncodeCompoundToArray(
            compoundPacket,
            strictValidation,
            preservePadding,
            compoundMode,
            maximumPacketSize,
            maximumCompoundPacketCount);
    }

    /// <summary>Decodes an RTCP compound packet or throws for malformed input.</summary>
    /// <param name="source">The compound packet bytes to decode.</param>
    /// <returns>An owned compound packet model.</returns>
    public RtcpCompoundPacket DecodeRtcpCompoundPacket(ReadOnlySpan<byte> source)
    {
        return RtcpPacketWireCodec.DecodeCompound(
            source,
            strictValidation,
            preservePadding,
            compoundMode,
            maximumPacketSize,
            maximumCompoundPacketCount);
    }

    /// <summary>Attempts to decode an RTCP compound packet without expected exceptions.</summary>
    /// <param name="source">The compound packet bytes to decode.</param>
    /// <returns>The decode outcome and owned compound data.</returns>
    public RtcpCodecResult TryDecodeRtcpCompoundPacket(ReadOnlySpan<byte> source)
    {
        return RtcpPacketWireCodec.TryDecodeCompound(
            source,
            strictValidation,
            preservePadding,
            compoundMode,
            maximumPacketSize,
            maximumCompoundPacketCount);
    }
}
