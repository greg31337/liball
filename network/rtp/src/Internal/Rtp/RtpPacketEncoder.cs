using System;

namespace rtp;

internal static class RtpPacketEncoder
{
    internal static int GetEncodedSize(
        RtpPacket packet,
        bool strictValidation,
        int maximumPacketSize)
    {
        string? validationError = GetPacketValidationError(packet, strictValidation);
        if (validationError != null)
        {
            throw new RtpPacketFormatException(validationError);
        }

        long encodedSize = CalculateWideEncodedSize(packet);
        if (encodedSize > int.MaxValue)
        {
            throw new OverflowException("The encoded RTP packet size exceeds Int32.MaxValue.");
        }

        if (encodedSize > maximumPacketSize)
        {
            throw new RtpPacketFormatException(
                "The encoded RTP packet exceeds the configured maximum packet size.");
        }

        return (int)encodedSize;
    }

    internal static int EncodePacket(
        RtpPacket packet,
        Span<byte> destination,
        bool strictValidation,
        bool preservePadding,
        int maximumPacketSize)
    {
        int encodedSize = GetEncodedSize(packet, strictValidation, maximumPacketSize);
        if (destination.Length < encodedSize)
        {
            throw new RtpBufferCapacityException(
                "The destination is too small for the encoded RTP packet.");
        }

        EncodeValidatedPacket(packet, destination, preservePadding);
        return encodedSize;
    }

    internal static RtpCodecResult TryEncodePacket(
        RtpPacket packet,
        Span<byte> destination,
        bool strictValidation,
        bool preservePadding,
        int maximumPacketSize)
    {
        RtpCodecResult result = new RtpCodecResult();
        string? validationError = GetPacketValidationError(packet, strictValidation);
        if (validationError != null)
        {
            result.ErrorMessage = validationError;
            return result;
        }

        long wideEncodedSize = CalculateWideEncodedSize(packet);
        if (wideEncodedSize > int.MaxValue)
        {
            result.ErrorMessage = "The encoded RTP packet size exceeds Int32.MaxValue.";
            return result;
        }

        if (wideEncodedSize > maximumPacketSize)
        {
            result.ErrorMessage =
                "The encoded RTP packet exceeds the configured maximum packet size.";
            return result;
        }

        int encodedSize = (int)wideEncodedSize;
        if (destination.Length < encodedSize)
        {
            result.ErrorMessage = "The destination is too small for the encoded RTP packet.";
            return result;
        }

        EncodeValidatedPacket(packet, destination, preservePadding);
        result.Success = true;
        result.WrittenCount = encodedSize;
        return result;
    }

    internal static byte[] EncodePacketToArray(
        RtpPacket packet,
        bool strictValidation,
        bool preservePadding,
        int maximumPacketSize)
    {
        int encodedSize = GetEncodedSize(packet, strictValidation, maximumPacketSize);
        byte[] encodedPacket = new byte[encodedSize];
        EncodeValidatedPacket(packet, encodedPacket, preservePadding);
        return encodedPacket;
    }

    static void EncodeValidatedPacket(
        RtpPacket packet,
        Span<byte> destination,
        bool preservePadding)
    {
        byte firstByte = (byte)(packet.Version << RtpProtocolConstants.VersionShift);
        if (packet.HasPadding)
        {
            firstByte = (byte)(firstByte | RtpProtocolConstants.PaddingMask);
        }

        if (packet.HasExtension)
        {
            firstByte = (byte)(firstByte | RtpProtocolConstants.ExtensionMask);
        }

        firstByte = (byte)(firstByte | packet.ContributingSources.Length);

        byte secondByte = packet.PayloadType;
        if (packet.Marker)
        {
            secondByte = (byte)(secondByte | RtpProtocolConstants.MarkerMask);
        }

        NetworkBinaryWriter.WriteByte(destination, 0, firstByte);
        NetworkBinaryWriter.WriteByte(destination, 1, secondByte);
        NetworkBinaryWriter.WriteUInt16(destination, 2, packet.SequenceNumber);
        NetworkBinaryWriter.WriteUInt32(destination, 4, packet.Timestamp);
        NetworkBinaryWriter.WriteUInt32(destination, 8, packet.SynchronizationSource);

        int offset = RtpProtocolConstants.FixedHeaderSize;
        int sourceIndex = 0;
        while (sourceIndex < packet.ContributingSources.Length)
        {
            NetworkBinaryWriter.WriteUInt32(
                destination,
                offset,
                packet.ContributingSources[sourceIndex]);
            offset = offset + RtpProtocolConstants.SourceIdentifierSize;
            sourceIndex = sourceIndex + 1;
        }

        if (packet.HasExtension)
        {
            RtpHeaderExtension extension = GetRequiredHeaderExtension(packet);
            if (RtpHeaderExtensionWireCodec.UsesElementEncoding(extension))
            {
                int extensionSize =
                    RtpHeaderExtensionWireCodec.EncodeEnvelope(
                        extension,
                        destination.Slice(offset));
                offset = offset + extensionSize;
            }
            else
            {
                NetworkBinaryWriter.WriteUInt16(destination, offset, extension.Profile);
                int wordCount = extension.Data.Length /
                    RtpProtocolConstants.HeaderExtensionWordSize;
                NetworkBinaryWriter.WriteUInt16(destination, offset + 2, (ushort)wordCount);
                offset = offset + RtpProtocolConstants.HeaderExtensionEnvelopeSize;
                NetworkBinaryWriter.WriteBytes(destination, offset, extension.Data);
                offset = offset + extension.Data.Length;
            }
        }

        NetworkBinaryWriter.WriteBytes(destination, offset, packet.Payload);
        offset = offset + packet.Payload.Length;
        if (preservePadding)
        {
            NetworkBinaryWriter.WriteBytes(destination, offset, packet.Padding);
        }
        else if (packet.HasPadding)
        {
            int paddingIndex = 0;
            while (paddingIndex < packet.Padding.Length - 1)
            {
                NetworkBinaryWriter.WriteByte(destination, offset + paddingIndex, 0);
                paddingIndex = paddingIndex + 1;
            }

            NetworkBinaryWriter.WriteByte(
                destination,
                offset + packet.Padding.Length - 1,
                (byte)packet.Padding.Length);
        }
    }

    static long CalculateWideEncodedSize(RtpPacket packet)
    {
        long size = RtpProtocolConstants.FixedHeaderSize;
        size = size + (long)packet.ContributingSources.Length *
            RtpProtocolConstants.SourceIdentifierSize;
        if (packet.HasExtension)
        {
            RtpHeaderExtension extension = GetRequiredHeaderExtension(packet);
            if (RtpHeaderExtensionWireCodec.UsesElementEncoding(extension))
            {
                size = size +
                    RtpHeaderExtensionWireCodec.GetEnvelopeEncodedSize(extension);
            }
            else
            {
                size = size + RtpProtocolConstants.HeaderExtensionEnvelopeSize;
                size = size + extension.Data.Length;
            }
        }

        size = size + packet.Payload.Length;
        size = size + packet.Padding.Length;
        return size;
    }

    internal static string? GetPacketValidationError(
        RtpPacket packet,
        bool strictValidation)
    {
        if (packet == null)
        {
            return "The RTP packet is required.";
        }

        if (packet.Version != RtpProtocolConstants.Version)
        {
            return "The RTP version must be 2.";
        }

        if (packet.PayloadType > RtpProtocolConstants.MaximumPayloadType)
        {
            return "The RTP payload type exceeds seven bits.";
        }

        int markerPayloadType = packet.PayloadType;
        if (packet.Marker)
        {
            markerPayloadType = markerPayloadType | RtpProtocolConstants.MarkerMask;
        }

        if (strictValidation &&
            (markerPayloadType == RtcpProtocolConstants.SenderReportPacketType ||
            markerPayloadType == RtcpProtocolConstants.ReceiverReportPacketType))
        {
            return "The RTP marker and payload type conflict with an RTCP report packet type.";
        }

        if (packet.ContributingSources == null)
        {
            return "The RTP contributing-source array is required.";
        }

        if (packet.ContributingSources.Length > RtpProtocolConstants.MaximumCsrcCount)
        {
            return "The RTP contributing-source count exceeds 15.";
        }

        if (packet.Payload == null)
        {
            return "The RTP payload array is required.";
        }

        if (packet.Padding == null)
        {
            return "The RTP padding array is required.";
        }

        if (packet.HasExtension)
        {
            RtpHeaderExtension? extension = packet.HeaderExtension;
            if (extension == null)
            {
                return "The RTP extension flag requires a header extension.";
            }

            if (RtpHeaderExtensionWireCodec.UsesElementEncoding(extension))
            {
                string? extensionError =
                    RtpHeaderExtensionWireCodec.GetEnvelopeValidationError(extension);
                if (extensionError != null)
                {
                    return extensionError;
                }
            }
            else
            {
                if (extension.Data == null)
                {
                    return "The RTP header-extension data array is required.";
                }

                if (extension.Data.Length >
                    RtpProtocolConstants.MaximumHeaderExtensionDataSize)
                {
                    return "The RTP header-extension data is too large.";
                }

                if (extension.Data.Length %
                    RtpProtocolConstants.HeaderExtensionWordSize != 0)
                {
                    return "The RTP header-extension data length must be a multiple of four bytes.";
                }
            }
        }
        else if (packet.HeaderExtension != null)
        {
            return "An RTP header extension requires the extension flag.";
        }

        if (packet.HasPadding)
        {
            if (packet.Padding.Length == 0)
            {
                return "The RTP padding count must be positive.";
            }

            if (packet.Padding.Length > RtpProtocolConstants.MaximumPaddingCount)
            {
                return "The RTP padding count exceeds 255.";
            }

            if (packet.Padding[packet.Padding.Length - 1] != packet.Padding.Length)
            {
                return "The final RTP padding byte must equal the padding length.";
            }
        }
        else if (packet.Padding.Length != 0)
        {
            return "RTP padding bytes require the padding flag.";
        }

        return null;
    }

    static RtpHeaderExtension GetRequiredHeaderExtension(RtpPacket packet)
    {
        RtpHeaderExtension? extension = packet.HeaderExtension;
        if (extension == null)
        {
            throw new RtpPacketFormatException(
                "The RTP extension flag requires a header extension.");
        }

        return extension;
    }
}
