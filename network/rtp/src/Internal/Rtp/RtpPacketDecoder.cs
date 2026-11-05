using System;

namespace rtp;

internal static class RtpPacketDecoder
{
    internal static RtpPacket DecodePacket(
        ReadOnlySpan<byte> source,
        bool strictValidation,
        bool preservePadding,
        int maximumPacketSize)
    {
        RtpCodecResult result = TryDecodePacket(
            source,
            strictValidation,
            preservePadding,
            maximumPacketSize);
        if (!result.Success)
        {
            string errorMessage = "The RTP packet could not be decoded.";
            if (result.ErrorMessage != null)
            {
                errorMessage = result.ErrorMessage;
            }

            throw new RtpPacketFormatException(errorMessage);
        }

        RtpPacket? packet = result.Packet;
        if (packet == null)
        {
            throw new RtpPacketFormatException(
                "The RTP decoder did not return a packet after successful validation.");
        }

        return packet;
    }

    internal static RtpCodecResult TryDecodePacket(
        ReadOnlySpan<byte> source,
        bool strictValidation,
        bool preservePadding,
        int maximumPacketSize)
    {
        if (source.Length > maximumPacketSize)
        {
            return CreateDecodeFailure(
                "The RTP packet exceeds the configured maximum packet size.");
        }

        if (source.Length < RtpProtocolConstants.FixedHeaderSize)
        {
            return CreateDecodeFailure(
                "The RTP packet is shorter than the fixed header.");
        }

        byte firstByte = NetworkBinaryReader.ReadByte(source, 0);
        byte version = (byte)((firstByte & RtpProtocolConstants.VersionMask) >>
            RtpProtocolConstants.VersionShift);
        if (version != RtpProtocolConstants.Version)
        {
            return CreateDecodeFailure("The RTP version must be 2.");
        }

        byte secondByte = NetworkBinaryReader.ReadByte(source, 1);
        if (strictValidation &&
            (secondByte == RtcpProtocolConstants.SenderReportPacketType ||
            secondByte == RtcpProtocolConstants.ReceiverReportPacketType))
        {
            return CreateDecodeFailure(
                "The RTP marker and payload type conflict with an RTCP report packet type.");
        }

        int contributingSourceCount = firstByte & RtpProtocolConstants.CsrcCountMask;
        int headerSize = RtpProtocolConstants.FixedHeaderSize +
            contributingSourceCount * RtpProtocolConstants.SourceIdentifierSize;
        if (source.Length < headerSize)
        {
            return CreateDecodeFailure(
                "The RTP packet is truncated within its contributing-source list.");
        }

        RtpPacket packet = DecodeFixedHeader(source);
        int payloadOffset = headerSize;
        if (packet.HasExtension)
        {
            if (source.Length - payloadOffset < RtpProtocolConstants.HeaderExtensionEnvelopeSize)
            {
                return CreateDecodeFailure(
                    "The RTP packet is truncated within its header-extension envelope.");
            }

            ushort profile = NetworkBinaryReader.ReadUInt16(source, payloadOffset);
            int extensionWordCount = NetworkBinaryReader.ReadUInt16(source, payloadOffset + 2);
            int extensionDataLength = extensionWordCount *
                RtpProtocolConstants.HeaderExtensionWordSize;
            int extensionDataOffset = payloadOffset +
                RtpProtocolConstants.HeaderExtensionEnvelopeSize;
            if (extensionDataLength > source.Length - extensionDataOffset)
            {
                return CreateDecodeFailure(
                    "The RTP packet is truncated within its header-extension data.");
            }

            RtpHeaderExtension extension = new RtpHeaderExtension();
            extension.Profile = profile;
            extension.Data = NetworkBinaryReader.ReadSlice(
                source,
                extensionDataOffset,
                extensionDataLength).ToArray();
            if (profile == RtpHeaderExtensionProfiles.OneByte)
            {
                RtpCodecResult extensionResult =
                    RtpHeaderExtensionWireCodec.ParseOneByteData(
                        profile,
                        extension.Data);
                if (!extensionResult.Success)
                {
                    string errorMessage = "The one-byte RTP header extension is malformed.";
                    if (extensionResult.ErrorMessage != null)
                    {
                        errorMessage = extensionResult.ErrorMessage;
                    }

                    return CreateDecodeFailure(errorMessage);
                }

                RtpHeaderExtension? parsedExtension = extensionResult.HeaderExtension;
                if (parsedExtension == null)
                {
                    return CreateDecodeFailure(
                        "The one-byte RTP header-extension decoder returned no extension.");
                }

                extension = parsedExtension;
            }
            else if ((profile & RtpHeaderExtensionProfiles.TwoByteProfileMask) ==
                RtpHeaderExtensionProfiles.TwoByte)
            {
                RtpCodecResult extensionResult =
                    RtpHeaderExtensionWireCodec.ParseTwoByteData(
                        profile,
                        extension.Data);
                if (!extensionResult.Success)
                {
                    string errorMessage = "The two-byte RTP header extension is malformed.";
                    if (extensionResult.ErrorMessage != null)
                    {
                        errorMessage = extensionResult.ErrorMessage;
                    }

                    return CreateDecodeFailure(errorMessage);
                }

                RtpHeaderExtension? parsedExtension = extensionResult.HeaderExtension;
                if (parsedExtension == null)
                {
                    return CreateDecodeFailure(
                        "The two-byte RTP header-extension decoder returned no extension.");
                }

                extension = parsedExtension;
            }
            else
            {
                extension.Format = RtpHeaderExtensionFormat.Raw;
            }

            packet.HeaderExtension = extension;
            payloadOffset = extensionDataOffset + extensionDataLength;
        }

        int payloadEnd = source.Length;
        if (packet.HasPadding)
        {
            int remainingLength = source.Length - payloadOffset;
            if (remainingLength == 0)
            {
                return CreateDecodeFailure(
                    "The RTP padding flag requires a final padding-count byte.");
            }

            int paddingCount = NetworkBinaryReader.ReadByte(source, source.Length - 1);
            if (paddingCount == 0)
            {
                return CreateDecodeFailure("The RTP padding count must be positive.");
            }

            if (paddingCount > remainingLength)
            {
                return CreateDecodeFailure(
                    "The RTP padding count exceeds the bytes following the header.");
            }

            payloadEnd = source.Length - paddingCount;
            if (preservePadding)
            {
                packet.Padding = NetworkBinaryReader.ReadSlice(
                    source,
                    payloadEnd,
                    paddingCount).ToArray();
            }
            else
            {
                packet.Padding = new byte[paddingCount];
                packet.Padding[paddingCount - 1] = (byte)paddingCount;
            }
        }

        packet.Payload = NetworkBinaryReader.ReadSlice(
            source,
            payloadOffset,
            payloadEnd - payloadOffset).ToArray();

        RtpCodecResult result = new RtpCodecResult();
        result.Success = true;
        result.ConsumedCount = source.Length;
        result.Packet = packet;
        return result;
    }

    internal static RtpPacket DecodeFixedHeader(ReadOnlySpan<byte> source)
    {
        if (source.Length < RtpProtocolConstants.FixedHeaderSize)
        {
            throw new RtpPacketFormatException(
                "The RTP packet is shorter than the fixed header.");
        }

        byte firstByte = NetworkBinaryReader.ReadByte(source, 0);
        byte version = (byte)((firstByte & RtpProtocolConstants.VersionMask) >>
            RtpProtocolConstants.VersionShift);
        if (version != RtpProtocolConstants.Version)
        {
            throw new RtpPacketFormatException("The RTP version must be 2.");
        }

        int contributingSourceCount = firstByte & RtpProtocolConstants.CsrcCountMask;
        int headerSize = RtpProtocolConstants.FixedHeaderSize +
            contributingSourceCount * RtpProtocolConstants.SourceIdentifierSize;
        if (source.Length < headerSize)
        {
            throw new RtpPacketFormatException(
                "The RTP packet is truncated within its contributing-source list.");
        }

        byte secondByte = NetworkBinaryReader.ReadByte(source, 1);
        RtpPacket packet = new RtpPacket();
        packet.Version = version;
        packet.HasPadding = (firstByte & RtpProtocolConstants.PaddingMask) != 0;
        packet.HasExtension = (firstByte & RtpProtocolConstants.ExtensionMask) != 0;
        packet.Marker = (secondByte & RtpProtocolConstants.MarkerMask) != 0;
        packet.PayloadType = (byte)(secondByte & RtpProtocolConstants.PayloadTypeMask);
        packet.SequenceNumber = NetworkBinaryReader.ReadUInt16(source, 2);
        packet.Timestamp = NetworkBinaryReader.ReadUInt32(source, 4);
        packet.SynchronizationSource = NetworkBinaryReader.ReadUInt32(source, 8);
        packet.ContributingSources = new uint[contributingSourceCount];
        int sourceIndex = 0;
        int sourceOffset = RtpProtocolConstants.FixedHeaderSize;
        while (sourceIndex < contributingSourceCount)
        {
            packet.ContributingSources[sourceIndex] =
                NetworkBinaryReader.ReadUInt32(source, sourceOffset);
            sourceIndex = sourceIndex + 1;
            sourceOffset = sourceOffset + RtpProtocolConstants.SourceIdentifierSize;
        }

        return packet;
    }

    static RtpCodecResult CreateDecodeFailure(string errorMessage)
    {
        RtpCodecResult result = new RtpCodecResult();
        result.ErrorMessage = errorMessage;
        return result;
    }
}
