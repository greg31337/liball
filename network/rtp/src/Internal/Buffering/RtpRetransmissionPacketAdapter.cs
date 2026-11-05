using System;

namespace rtp;

internal static class RtpRetransmissionPacketAdapter
{
    internal const int OriginalSequenceNumberSize = 2;
    internal const int MaximumPacketSize = 65535;

    internal static RtpPacket CreateRetransmissionPacket(
        RtpPacket originalPacket,
        uint retransmissionSourceIdentifier,
        byte retransmissionPayloadType,
        ushort retransmissionSequenceNumber)
    {
        ValidatePacket(originalPacket);
        ValidatePayloadType(retransmissionPayloadType);

        RtpPacket retransmissionPacket = originalPacket.Copy();
        retransmissionPacket.SynchronizationSource =
            retransmissionSourceIdentifier;
        retransmissionPacket.PayloadType = retransmissionPayloadType;
        retransmissionPacket.SequenceNumber = retransmissionSequenceNumber;
        retransmissionPacket.HasPadding = false;
        retransmissionPacket.Padding = [];
        int retransmissionPayloadSize = SizeArithmetic.Add(
            originalPacket.Payload.Length,
            OriginalSequenceNumberSize);
        retransmissionPacket.Payload = new byte[retransmissionPayloadSize];
        retransmissionPacket.Payload[0] = (byte)(
            originalPacket.SequenceNumber >> 8);
        retransmissionPacket.Payload[1] = (byte)(
            originalPacket.SequenceNumber & 255);
        Array.Copy(
            originalPacket.Payload,
            0,
            retransmissionPacket.Payload,
            OriginalSequenceNumberSize,
            originalPacket.Payload.Length);

        ValidatePacket(retransmissionPacket);
        return retransmissionPacket;
    }

    internal static RtpPacket RestoreOriginalPacket(
        RtpPacket retransmissionPacket,
        uint originalSourceIdentifier,
        byte originalPayloadType)
    {
        ValidatePacket(retransmissionPacket);
        ValidatePayloadType(originalPayloadType);
        if (retransmissionPacket.Payload.Length < OriginalSequenceNumberSize)
        {
            throw new RtpPacketFormatException(
                "The RTX payload must contain a two-byte original sequence number.");
        }

        RtpPacket originalPacket = retransmissionPacket.Copy();
        originalPacket.SynchronizationSource = originalSourceIdentifier;
        originalPacket.PayloadType = originalPayloadType;
        originalPacket.SequenceNumber = (ushort)(
            retransmissionPacket.Payload[0] << 8 |
            retransmissionPacket.Payload[1]);
        originalPacket.HasPadding = false;
        originalPacket.Padding = [];
        originalPacket.Payload = new byte[
            retransmissionPacket.Payload.Length - OriginalSequenceNumberSize];
        Array.Copy(
            retransmissionPacket.Payload,
            OriginalSequenceNumberSize,
            originalPacket.Payload,
            0,
            originalPacket.Payload.Length);
        return originalPacket;
    }

    internal static void ValidatePacket(RtpPacket packet)
    {
        RtpPacketEncoder.GetEncodedSize(packet, false, MaximumPacketSize);
    }

    internal static void ValidatePayloadType(byte payloadType)
    {
        if (payloadType > RtpProtocolConstants.MaximumPayloadType)
        {
            throw new RtpPacketFormatException(
                "The RTP payload type exceeds seven bits.");
        }
    }
}
