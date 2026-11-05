using System;

namespace rtp;

/// <summary>Classifies multiplexed RTP and RTCP datagrams without guessing.</summary>
public class RtpRtcpDemultiplexer : IRtpRtcpDemultiplexer
{
    internal const int MaximumRtpDatagramSize = 65535;

    /// <summary>Initializes an RTP and RTCP datagram classifier.</summary>
    public RtpRtcpDemultiplexer()
    {
    }

    /// <summary>Classifies a datagram without guessing when input is ambiguous.</summary>
    /// <param name="datagram">The datagram bytes to classify.</param>
    /// <returns>The classification result.</returns>
    public RtpDatagramClassification ClassifyDatagram(
        ReadOnlySpan<byte> datagram)
    {
        if (datagram.Length < 2)
        {
            return CreateUnknownClassification(
                "The datagram is too short to contain an RTP or RTCP header.");
        }

        byte firstByte = NetworkBinaryReader.ReadByte(datagram, 0);
        byte version = (byte)((firstByte & RtpProtocolConstants.VersionMask) >>
            RtpProtocolConstants.VersionShift);
        if (version != RtpProtocolConstants.Version)
        {
            return CreateUnknownClassification(
                "The datagram does not contain RTP version 2.");
        }

        byte discriminator = NetworkBinaryReader.ReadByte(datagram, 1);
        if (discriminator >=
            RtcpProtocolConstants.MultiplexingPacketTypeMinimum &&
            discriminator <=
            RtcpProtocolConstants.MultiplexingPacketTypeMaximum)
        {
            return ClassifyControlDatagram(datagram, discriminator);
        }

        byte payloadType = (byte)(
            discriminator & RtpProtocolConstants.PayloadTypeMask);
        if (payloadType >=
            RtpProtocolConstants.MultiplexingForbiddenPayloadTypeMinimum &&
            payloadType <=
            RtpProtocolConstants.MultiplexingForbiddenPayloadTypeMaximum)
        {
            return CreateUnknownClassification(
                "The RTP payload type is forbidden when RTP and RTCP are multiplexed.");
        }

        return ClassifyMediaDatagram(datagram, payloadType);
    }

    internal RtpDatagramClassification ClassifyControlDatagram(
        ReadOnlySpan<byte> datagram,
        byte packetType)
    {
        RtcpPacketType controlPacketType =
            RtcpPacketWireCodec.GetPacketType(packetType);
        if (controlPacketType == RtcpPacketType.Unknown)
        {
            return CreateUnknownClassification(
                "The multiplexing discriminator is not an assigned RTCP packet type.");
        }

        RtcpCodecResult decodeResult = RtcpPacketWireCodec.TryDecodePacket(
            datagram,
            true,
            true,
            RtcpProtocolConstants.MaximumPacketSize);
        if (!decodeResult.Success)
        {
            string errorMessage = "The RTCP datagram is malformed.";
            if (decodeResult.ErrorMessage != null)
            {
                errorMessage = decodeResult.ErrorMessage;
            }

            return CreateUnknownClassification(errorMessage);
        }

        RtpDatagramClassification classification =
            new RtpDatagramClassification();
        classification.Kind = RtpPacketKind.Rtcp;
        classification.ControlPacketType = controlPacketType;
        return classification;
    }

    internal RtpDatagramClassification ClassifyMediaDatagram(
        ReadOnlySpan<byte> datagram,
        byte payloadType)
    {
        RtpCodecResult decodeResult = RtpPacketDecoder.TryDecodePacket(
            datagram,
            false,
            true,
            MaximumRtpDatagramSize);
        if (!decodeResult.Success)
        {
            string errorMessage = "The RTP datagram is malformed.";
            if (decodeResult.ErrorMessage != null)
            {
                errorMessage = decodeResult.ErrorMessage;
            }

            return CreateUnknownClassification(errorMessage);
        }

        RtpDatagramClassification classification =
            new RtpDatagramClassification();
        classification.Kind = RtpPacketKind.Rtp;
        classification.PayloadType = payloadType;
        return classification;
    }

    internal RtpDatagramClassification CreateUnknownClassification(
        string errorMessage)
    {
        RtpDatagramClassification classification =
            new RtpDatagramClassification();
        classification.Kind = RtpPacketKind.Unknown;
        classification.ErrorMessage = errorMessage;
        return classification;
    }
}
