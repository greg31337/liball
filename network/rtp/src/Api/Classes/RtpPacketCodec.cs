using System;

namespace rtp;

/// <summary>
/// Provides built-in RTP packet wire encoding and decoding operations.
/// </summary>
public class RtpPacketCodec : IRtpPacketCodec
{
    internal bool strictValidation;
    internal bool preservePadding;
    internal int maximumPacketSize;

    /// <summary>
    /// Initializes an RTP packet codec using an independent configuration snapshot.
    /// </summary>
    /// <param name="configuration">The validation, padding, and size-limit settings.</param>
    public RtpPacketCodec(RtpSessionConfiguration configuration)
    {
        RtpConfigurationValidator.ValidateRtpPacketCodecConfiguration(configuration);
        strictValidation = configuration.StrictValidation;
        preservePadding = configuration.PreservePadding;
        maximumPacketSize = configuration.MaximumRtpPacketSize;
    }

    /// <summary>Calculates the exact encoded size of one RTP packet.</summary>
    /// <param name="packet">The packet to size.</param>
    /// <returns>The required destination size in bytes.</returns>
    public int GetRtpPacketEncodedSize(RtpPacket packet)
    {
        return RtpPacketEncoder.GetEncodedSize(packet, strictValidation, maximumPacketSize);
    }

    /// <summary>Encodes one RTP packet or throws when input or capacity is invalid.</summary>
    /// <param name="packet">The packet to encode.</param>
    /// <param name="destination">The destination receiving the encoded bytes.</param>
    /// <returns>The number of bytes written.</returns>
    public int EncodeRtpPacket(RtpPacket packet, Span<byte> destination)
    {
        return RtpPacketEncoder.EncodePacket(
            packet,
            destination,
            strictValidation,
            preservePadding,
            maximumPacketSize);
    }

    /// <summary>Attempts to encode one RTP packet without throwing for expected failure.</summary>
    /// <param name="packet">The packet to encode.</param>
    /// <param name="destination">The destination receiving the encoded bytes.</param>
    /// <returns>The encode outcome and written count.</returns>
    public RtpCodecResult TryEncodeRtpPacket(RtpPacket packet, Span<byte> destination)
    {
        return RtpPacketEncoder.TryEncodePacket(
            packet,
            destination,
            strictValidation,
            preservePadding,
            maximumPacketSize);
    }

    /// <summary>Encodes one RTP packet into a newly owned byte array.</summary>
    /// <param name="packet">The packet to encode.</param>
    /// <returns>The newly allocated encoded packet bytes.</returns>
    public byte[] EncodeRtpPacketToArray(RtpPacket packet)
    {
        return RtpPacketEncoder.EncodePacketToArray(
            packet,
            strictValidation,
            preservePadding,
            maximumPacketSize);
    }

    /// <summary>Decodes one RTP packet with configured validation and padding behavior.</summary>
    /// <param name="source">The packet bytes to decode.</param>
    /// <returns>An owned RTP packet containing decoded header and payload data.</returns>
    public RtpPacket DecodeRtpPacket(ReadOnlySpan<byte> source)
    {
        return RtpPacketDecoder.DecodePacket(
            source,
            strictValidation,
            preservePadding,
            maximumPacketSize);
    }

    /// <summary>Attempts to decode one RTP packet without throwing for malformed input.</summary>
    /// <param name="source">The packet bytes to decode.</param>
    /// <returns>The decode outcome and owned packet data.</returns>
    public RtpCodecResult TryDecodeRtpPacket(ReadOnlySpan<byte> source)
    {
        return RtpPacketDecoder.TryDecodePacket(
            source,
            strictValidation,
            preservePadding,
            maximumPacketSize);
    }
}
