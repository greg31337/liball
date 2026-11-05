using System;
using System.Collections.Generic;

namespace rtp;

internal static class RtcpPacketWireCodec
{
    internal static int GetPacketEncodedSize(
        RtcpPacket packet,
        bool strictValidation,
        int maximumPacketSize)
    {
        string? validationError = GetPacketValidationError(
            packet,
            strictValidation,
            maximumPacketSize);
        if (validationError != null)
        {
            throw new RtcpPacketFormatException(validationError);
        }

        return CalculatePacketEncodedSize(packet);
    }

    internal static int EncodePacket(
        RtcpPacket packet,
        Span<byte> destination,
        bool strictValidation,
        bool preservePadding,
        int maximumPacketSize)
    {
        int encodedSize = GetPacketEncodedSize(
            packet,
            strictValidation,
            maximumPacketSize);
        if (destination.Length < encodedSize)
        {
            throw new RtpBufferCapacityException(
                "The destination is too small for the encoded RTCP packet.");
        }

        EncodeValidatedPacket(packet, destination, preservePadding);
        return encodedSize;
    }

    internal static RtcpCodecResult TryEncodePacket(
        RtcpPacket packet,
        Span<byte> destination,
        bool strictValidation,
        bool preservePadding,
        int maximumPacketSize)
    {
        string? validationError = GetPacketValidationError(
            packet,
            strictValidation,
            maximumPacketSize);
        if (validationError != null)
        {
            return CreateFailure(validationError);
        }

        int encodedSize = CalculatePacketEncodedSize(packet);
        if (destination.Length < encodedSize)
        {
            return CreateFailure(
                "The destination is too small for the encoded RTCP packet.");
        }

        EncodeValidatedPacket(packet, destination, preservePadding);
        RtcpCodecResult result = new RtcpCodecResult();
        result.Success = true;
        result.WrittenCount = encodedSize;
        return result;
    }

    internal static byte[] EncodePacketToArray(
        RtcpPacket packet,
        bool strictValidation,
        bool preservePadding,
        int maximumPacketSize)
    {
        int encodedSize = GetPacketEncodedSize(
            packet,
            strictValidation,
            maximumPacketSize);
        byte[] encoded = new byte[encodedSize];
        EncodeValidatedPacket(packet, encoded, preservePadding);
        return encoded;
    }

    internal static RtcpPacket DecodePacket(
        ReadOnlySpan<byte> source,
        bool strictValidation,
        bool preservePadding,
        int maximumPacketSize)
    {
        RtcpCodecResult result = TryDecodePacket(
            source,
            strictValidation,
            preservePadding,
            maximumPacketSize);
        if (!result.Success)
        {
            string errorMessage = "The RTCP packet could not be decoded.";
            if (result.ErrorMessage != null)
            {
                errorMessage = result.ErrorMessage;
            }

            throw new RtcpPacketFormatException(errorMessage);
        }

        RtcpPacket? packet = result.Packet;
        if (packet == null)
        {
            throw new RtcpPacketFormatException(
                "The RTCP decoder returned no packet after successful validation.");
        }

        return packet;
    }

    internal static RtcpCodecResult TryDecodePacket(
        ReadOnlySpan<byte> source,
        bool strictValidation,
        bool preservePadding,
        int maximumPacketSize)
    {
        if (source.Length < RtcpProtocolConstants.HeaderSize)
        {
            return CreateFailure("The RTCP packet is shorter than the common header.");
        }

        byte firstByte = NetworkBinaryReader.ReadByte(source, 0);
        byte version = (byte)((firstByte & RtcpProtocolConstants.VersionMask) >>
            RtcpProtocolConstants.VersionShift);
        if (version != RtcpProtocolConstants.Version)
        {
            return CreateFailure("The RTCP version must be 2.");
        }

        byte packetType = NetworkBinaryReader.ReadByte(source, 1);
        if (strictValidation && !IsStrictPacketType(packetType))
        {
            return CreateFailure("The RTCP packet type is outside the registered range.");
        }

        int lengthInWordsMinusOne = NetworkBinaryReader.ReadUInt16(source, 2);
        int packetSize = (lengthInWordsMinusOne + 1) *
            RtcpProtocolConstants.LengthWordSize;
        if (packetSize > maximumPacketSize)
        {
            return CreateFailure("The RTCP packet exceeds the configured size limit.");
        }

        if (packetSize > source.Length)
        {
            return CreateFailure("The RTCP packet is truncated according to its length field.");
        }

        bool hasPadding = (firstByte & RtcpProtocolConstants.PaddingMask) != 0;
        int paddingCount = 0;
        if (hasPadding)
        {
            paddingCount = source[packetSize - 1];
            if (paddingCount == 0)
            {
                return CreateFailure("The RTCP padding count must be positive.");
            }

            if (paddingCount > packetSize - RtcpProtocolConstants.HeaderSize)
            {
                return CreateFailure("The RTCP padding exceeds the packet body.");
            }
        }

        int bodyLength = packetSize - RtcpProtocolConstants.HeaderSize - paddingCount;
        byte count = (byte)(firstByte & RtcpProtocolConstants.CountMask);
        ReadOnlySpan<byte> body = source.Slice(
            RtcpProtocolConstants.HeaderSize,
            bodyLength);
        int fixedBodyLength = 0;
        if (packetType == RtcpProtocolConstants.SenderReportPacketType)
        {
            fixedBodyLength = RtcpProtocolConstants.SenderReportMinimumSize -
                RtcpProtocolConstants.HeaderSize +
                count * RtcpProtocolConstants.ReceptionReportSize;
            if (bodyLength < fixedBodyLength)
            {
                return CreateFailure(
                    "The sender report length is too short for its report count.");
            }

            if ((bodyLength - fixedBodyLength) %
                RtcpProtocolConstants.LengthWordSize != 0)
            {
                return CreateFailure(
                    "The sender report profile extension must be word-aligned.");
            }
        }
        else if (packetType == RtcpProtocolConstants.ReceiverReportPacketType)
        {
            fixedBodyLength = RtcpProtocolConstants.ReceiverReportMinimumSize -
                RtcpProtocolConstants.HeaderSize +
                count * RtcpProtocolConstants.ReceptionReportSize;
            if (bodyLength < fixedBodyLength)
            {
                return CreateFailure(
                    "The receiver report length is too short for its report count.");
            }

            if ((bodyLength - fixedBodyLength) %
                RtcpProtocolConstants.LengthWordSize != 0)
            {
                return CreateFailure(
                    "The receiver report profile extension must be word-aligned.");
            }
        }
        else if (packetType == RtcpProtocolConstants.SourceDescriptionPacketType)
        {
            string? sourceDescriptionError =
                RtcpSourceDescriptionWireCodec.GetSourceDescriptionDecodeError(
                    body,
                    count);
            if (sourceDescriptionError != null)
            {
                return CreateFailure(sourceDescriptionError);
            }
        }
        else if (packetType == RtcpProtocolConstants.GoodbyePacketType)
        {
            string? goodbyeError = RtcpGoodbyeWireCodec.GetGoodbyeDecodeError(
                body,
                count);
            if (goodbyeError != null)
            {
                return CreateFailure(goodbyeError);
            }
        }
        else if (packetType == RtcpProtocolConstants.ApplicationDefinedPacketType)
        {
            string? applicationError =
                RtcpApplicationDefinedWireCodec.GetApplicationDefinedDecodeError(body);
            if (applicationError != null)
            {
                return CreateFailure(applicationError);
            }
        }
        else if (packetType == RtcpProtocolConstants.ExtendedReportPacketType)
        {
            string? extendedReportError =
                RtcpExtendedReportWireCodec.GetExtendedReportDecodeError(body);
            if (extendedReportError != null)
            {
                return CreateFailure(extendedReportError);
            }
        }
        else if (packetType == RtcpProtocolConstants.TransportFeedbackPacketType ||
            packetType == RtcpProtocolConstants.PayloadFeedbackPacketType)
        {
            string? feedbackError =
                RtcpFeedbackWireCodec.GetFeedbackDecodeError(
                    body,
                    packetType,
                    count,
                    lengthInWordsMinusOne);
            if (feedbackError != null)
            {
                return CreateFailure(feedbackError);
            }
        }

        RtcpHeader header = new RtcpHeader();
        header.Version = version;
        header.HasPadding = hasPadding;
        header.Count = count;
        header.PacketType = packetType;
        header.LengthInWordsMinusOne = (ushort)lengthInWordsMinusOne;

        RtcpPacket packet = new RtcpPacket();
        packet.Header = header;
        packet.Type = GetPacketType(packetType);
        if (packetType == RtcpProtocolConstants.SenderReportPacketType)
        {
            packet.SenderReport = DecodeSenderReportBody(body, count);
        }
        else if (packetType == RtcpProtocolConstants.ReceiverReportPacketType)
        {
            packet.ReceiverReport = DecodeReceiverReportBody(body, count);
        }
        else if (packetType == RtcpProtocolConstants.SourceDescriptionPacketType)
        {
            packet.SourceDescription =
                RtcpSourceDescriptionWireCodec.DecodeSourceDescription(body, count);
        }
        else if (packetType == RtcpProtocolConstants.GoodbyePacketType)
        {
            packet.Goodbye = RtcpGoodbyeWireCodec.DecodeGoodbye(body, count);
        }
        else if (packetType == RtcpProtocolConstants.ApplicationDefinedPacketType)
        {
            packet.ApplicationDefined =
                RtcpApplicationDefinedWireCodec.DecodeApplicationDefined(body, count);
        }
        else if (packetType == RtcpProtocolConstants.ExtendedReportPacketType)
        {
            packet.ExtendedReport =
                RtcpExtendedReportWireCodec.DecodeExtendedReport(body);
        }
        else if (packetType == RtcpProtocolConstants.TransportFeedbackPacketType ||
            packetType == RtcpProtocolConstants.PayloadFeedbackPacketType)
        {
            packet.Feedback = RtcpFeedbackWireCodec.DecodeFeedback(
                body,
                packetType,
                count);
        }
        else
        {
            RtcpUnknownPacket opaqueBody = new RtcpUnknownPacket();
            opaqueBody.PacketType = packetType;
            opaqueBody.Count = count;
            opaqueBody.Data = body.ToArray();
            packet.UnknownPacket = opaqueBody;
        }
        if (hasPadding)
        {
            packet.Padding = source.Slice(packetSize - paddingCount, paddingCount).ToArray();
            if (!preservePadding)
            {
                int paddingIndex = 0;
                while (paddingIndex < packet.Padding.Length - 1)
                {
                    packet.Padding[paddingIndex] = 0;
                    paddingIndex = paddingIndex + 1;
                }
            }
        }

        RtcpCodecResult result = new RtcpCodecResult();
        result.Success = true;
        result.ConsumedCount = packetSize;
        result.Packet = packet;
        return result;
    }

    internal static int GetCompoundEncodedSize(
        RtcpCompoundPacket compoundPacket,
        bool strictValidation,
        RtcpCompoundMode compoundMode,
        int maximumPacketSize,
        int maximumCompoundPacketCount)
    {
        string? validationError = GetCompoundValidationError(
            compoundPacket,
            strictValidation,
            compoundMode,
            maximumPacketSize,
            maximumCompoundPacketCount);
        if (validationError != null)
        {
            throw new RtcpPacketFormatException(validationError);
        }

        return CalculateCompoundEncodedSize(compoundPacket);
    }

    internal static int EncodeCompound(
        RtcpCompoundPacket compoundPacket,
        Span<byte> destination,
        bool strictValidation,
        bool preservePadding,
        RtcpCompoundMode compoundMode,
        int maximumPacketSize,
        int maximumCompoundPacketCount)
    {
        int encodedSize = GetCompoundEncodedSize(
            compoundPacket,
            strictValidation,
            compoundMode,
            maximumPacketSize,
            maximumCompoundPacketCount);
        if (destination.Length < encodedSize)
        {
            throw new RtpBufferCapacityException(
                "The destination is too small for the encoded RTCP compound packet.");
        }

        EncodeValidatedCompound(
            compoundPacket,
            destination.Slice(0, encodedSize),
            preservePadding);
        return encodedSize;
    }

    internal static RtcpCodecResult TryEncodeCompound(
        RtcpCompoundPacket compoundPacket,
        Span<byte> destination,
        bool strictValidation,
        bool preservePadding,
        RtcpCompoundMode compoundMode,
        int maximumPacketSize,
        int maximumCompoundPacketCount)
    {
        string? validationError = GetCompoundValidationError(
            compoundPacket,
            strictValidation,
            compoundMode,
            maximumPacketSize,
            maximumCompoundPacketCount);
        if (validationError != null)
        {
            return CreateFailure(validationError);
        }

        int encodedSize = CalculateCompoundEncodedSize(compoundPacket);
        if (destination.Length < encodedSize)
        {
            return CreateFailure(
                "The destination is too small for the encoded RTCP compound packet.");
        }

        EncodeValidatedCompound(
            compoundPacket,
            destination.Slice(0, encodedSize),
            preservePadding);
        RtcpCodecResult result = new RtcpCodecResult();
        result.Success = true;
        result.WrittenCount = encodedSize;
        return result;
    }

    internal static byte[] EncodeCompoundToArray(
        RtcpCompoundPacket compoundPacket,
        bool strictValidation,
        bool preservePadding,
        RtcpCompoundMode compoundMode,
        int maximumPacketSize,
        int maximumCompoundPacketCount)
    {
        int encodedSize = GetCompoundEncodedSize(
            compoundPacket,
            strictValidation,
            compoundMode,
            maximumPacketSize,
            maximumCompoundPacketCount);
        byte[] encoded = new byte[encodedSize];
        EncodeValidatedCompound(compoundPacket, encoded, preservePadding);
        return encoded;
    }

    internal static RtcpCompoundPacket DecodeCompound(
        ReadOnlySpan<byte> source,
        bool strictValidation,
        bool preservePadding,
        RtcpCompoundMode compoundMode,
        int maximumPacketSize,
        int maximumCompoundPacketCount)
    {
        RtcpCodecResult result = TryDecodeCompound(
            source,
            strictValidation,
            preservePadding,
            compoundMode,
            maximumPacketSize,
            maximumCompoundPacketCount);
        if (!result.Success)
        {
            string errorMessage = "The RTCP compound packet could not be decoded.";
            if (result.ErrorMessage != null)
            {
                errorMessage = result.ErrorMessage;
            }

            throw new RtcpPacketFormatException(errorMessage);
        }

        RtcpCompoundPacket? compoundPacket = result.CompoundPacket;
        if (compoundPacket == null)
        {
            throw new RtcpPacketFormatException(
                "The RTCP decoder returned no compound packet after successful validation.");
        }

        return compoundPacket;
    }

    internal static RtcpCodecResult TryDecodeCompound(
        ReadOnlySpan<byte> source,
        bool strictValidation,
        bool preservePadding,
        RtcpCompoundMode compoundMode,
        int maximumPacketSize,
        int maximumCompoundPacketCount)
    {
        if (source.Length < RtcpProtocolConstants.HeaderSize)
        {
            return CreateFailure("The RTCP compound packet is empty or truncated.");
        }

        if (source.Length > maximumPacketSize)
        {
            return CreateFailure("The RTCP compound packet exceeds the configured size limit.");
        }

        List<RtcpPacket> packets = new List<RtcpPacket>();
        int offset = 0;
        while (offset < source.Length)
        {
            if (packets.Count >= maximumCompoundPacketCount)
            {
                return CreateFailure(
                    "The RTCP compound packet exceeds the configured packet-count limit.");
            }

            RtcpCodecResult packetResult = TryDecodePacket(
                source.Slice(offset),
                strictValidation,
                preservePadding,
                maximumPacketSize);
            if (!packetResult.Success)
            {
                return CreateFailure(GetFailureMessage(
                    packetResult,
                    "An RTCP packet in the compound could not be decoded."));
            }

            RtcpPacket? packet = packetResult.Packet;
            if (packet == null || packetResult.ConsumedCount <= 0)
            {
                return CreateFailure(
                    "The RTCP packet decoder made no progress within the compound packet.");
            }

            packets.Add(packet);
            offset = offset + packetResult.ConsumedCount;
        }

        RtcpCompoundPacket compoundPacket = new RtcpCompoundPacket();
        compoundPacket.Packets = packets.ToArray();
        if (strictValidation)
        {
            string? compoundError = RtcpCompoundValidator.GetValidationError(
                compoundPacket,
                compoundMode);
            if (compoundError != null)
            {
                return CreateFailure(compoundError);
            }
        }

        RtcpCodecResult result = new RtcpCodecResult();
        result.Success = true;
        result.ConsumedCount = offset;
        result.CompoundPacket = compoundPacket;
        return result;
    }

    internal static string? GetPacketValidationError(
        RtcpPacket packet,
        bool strictValidation,
        int maximumPacketSize)
    {
        if (packet == null)
        {
            return "The RTCP packet is required.";
        }

        if (packet.Header == null)
        {
            return "The RTCP common header is required.";
        }

        if (packet.Header.Version != RtcpProtocolConstants.Version)
        {
            return "The RTCP version must be 2.";
        }

        string? bodyError = GetPacketBodyValidationError(packet, strictValidation);
        if (bodyError != null)
        {
            return bodyError;
        }

        if (packet.Header.HasPadding &&
            ((packet.Type == RtcpPacketType.PayloadFeedback &&
            RtcpFeedbackWireCodec.GetFeedbackWireFormat(
                GetRequiredFeedback(packet)) ==
            RtcpFeedbackFormats.PayloadPictureLossIndication) ||
            (packet.Type == RtcpPacketType.TransportFeedback &&
            RtcpFeedbackWireCodec.GetFeedbackWireFormat(
                GetRequiredFeedback(packet)) ==
            RtcpFeedbackFormats.TransportRapidResynchronizationRequest)))
        {
            return "An empty-FCI feedback packet with a fixed length of 2 cannot use padding.";
        }

        if (packet.Header.HasPadding &&
            packet.Type == RtcpPacketType.PayloadFeedback &&
            RtcpFeedbackWireCodec.GetFeedbackWireFormat(
                GetRequiredFeedback(packet)) ==
            RtcpFeedbackFormats.PayloadFullIntraRequest)
        {
            return "A full intra request cannot use RTCP packet padding.";
        }

        if (packet.Header.HasPadding &&
            packet.Type == RtcpPacketType.TransportFeedback &&
            (RtcpFeedbackWireCodec.GetFeedbackWireFormat(
                GetRequiredFeedback(packet)) ==
            RtcpFeedbackFormats.TransportTemporaryMaximumBitrateRequest ||
            RtcpFeedbackWireCodec.GetFeedbackWireFormat(
                GetRequiredFeedback(packet)) ==
            RtcpFeedbackFormats.TransportTemporaryMaximumBitrateNotification))
        {
            return "TMMBR and TMMBN cannot use RTCP packet padding.";
        }

        if (packet.Header.HasPadding &&
            packet.Type == RtcpPacketType.PayloadFeedback &&
            (RtcpFeedbackWireCodec.GetFeedbackWireFormat(
                GetRequiredFeedback(packet)) ==
            RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffRequest ||
            RtcpFeedbackWireCodec.GetFeedbackWireFormat(
                GetRequiredFeedback(packet)) ==
            RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffNotification))
        {
            return "TSTR and TSTN cannot use RTCP packet padding.";
        }

        if (packet.Padding == null)
        {
            return "The RTCP padding array is required.";
        }

        if (packet.Header.HasPadding)
        {
            if (packet.Padding.Length == 0)
            {
                return "The RTCP padding count must be positive.";
            }

            if (packet.Padding.Length > RtcpProtocolConstants.MaximumPaddingCount)
            {
                return "The RTCP padding count exceeds 255.";
            }

            if (packet.Padding[packet.Padding.Length - 1] != packet.Padding.Length)
            {
                return "The final RTCP padding byte must equal the padding length.";
            }
        }
        else if (packet.Padding.Length != 0)
        {
            return "RTCP padding bytes require the padding flag.";
        }

        long encodedSize = (long)RtcpProtocolConstants.HeaderSize +
            CalculatePacketBodySizeLong(packet) + packet.Padding.Length;
        if (encodedSize % RtcpProtocolConstants.LengthWordSize != 0)
        {
            return "The encoded RTCP packet size must be a multiple of four bytes.";
        }

        if (encodedSize > RtcpProtocolConstants.MaximumPacketSize)
        {
            return "The encoded RTCP packet exceeds the wire length limit.";
        }

        if (encodedSize > maximumPacketSize)
        {
            return "The encoded RTCP packet exceeds the configured size limit.";
        }

        return null;
    }

    internal static string? GetPacketBodyValidationError(
        RtcpPacket packet,
        bool strictValidation)
    {
        if (packet.Type == RtcpPacketType.SenderReport)
        {
            return GetSenderReportValidationError(packet.SenderReport);
        }

        if (packet.Type == RtcpPacketType.ReceiverReport)
        {
            return GetReceiverReportValidationError(packet.ReceiverReport);
        }

        if (packet.Type == RtcpPacketType.SourceDescription)
        {
            return RtcpSourceDescriptionWireCodec.GetSourceDescriptionValidationError(
                packet.SourceDescription);
        }

        if (packet.Type == RtcpPacketType.Goodbye)
        {
            return RtcpGoodbyeWireCodec.GetGoodbyeValidationError(packet.Goodbye);
        }

        if (packet.Type == RtcpPacketType.ApplicationDefined)
        {
            return RtcpApplicationDefinedWireCodec.GetApplicationDefinedValidationError(
                packet.ApplicationDefined);
        }

        if (packet.Type == RtcpPacketType.ExtendedReport)
        {
            return RtcpExtendedReportWireCodec.GetExtendedReportValidationError(
                packet.ExtendedReport);
        }

        if (packet.Type == RtcpPacketType.TransportFeedback ||
            packet.Type == RtcpPacketType.PayloadFeedback)
        {
            return RtcpFeedbackWireCodec.GetFeedbackValidationError(
                packet.Feedback,
                packet.Type);
        }

        RtcpUnknownPacket? opaqueBody = packet.UnknownPacket;
        if (opaqueBody == null)
        {
            return "An opaque RTCP packet body is required until its typed codec is available.";
        }

        if (strictValidation && !IsStrictPacketType(opaqueBody.PacketType))
        {
            return "The RTCP packet type is outside the registered range.";
        }

        if (opaqueBody.Count > RtcpProtocolConstants.MaximumCount)
        {
            return "The RTCP count or subtype exceeds five bits.";
        }

        if (packet.Type != GetPacketType(opaqueBody.PacketType))
        {
            return "The RTCP packet discriminator does not match its wire packet type.";
        }

        if (opaqueBody.Data == null)
        {
            return "The opaque RTCP packet body data is required.";
        }

        return null;
    }

    internal static string? GetSenderReportValidationError(RtcpSenderReport? report)
    {
        if (report == null)
        {
            return "The RTCP sender report body is required.";
        }

        if (report.SenderInformation == null)
        {
            return "RTCP sender information is required.";
        }

        string? reportError = RtcpReportBlockWireCodec.GetReceptionReportsValidationError(
            report.ReceptionReports);
        if (reportError != null)
        {
            return reportError;
        }

        if (report.ProfileExtension == null)
        {
            return "The sender report profile-extension array is required.";
        }

        if (report.ProfileExtension.Length % RtcpProtocolConstants.LengthWordSize != 0)
        {
            return "The sender report profile extension must be word-aligned.";
        }

        return null;
    }

    internal static string? GetReceiverReportValidationError(RtcpReceiverReport? report)
    {
        if (report == null)
        {
            return "The RTCP receiver report body is required.";
        }

        string? reportError = RtcpReportBlockWireCodec.GetReceptionReportsValidationError(
            report.ReceptionReports);
        if (reportError != null)
        {
            return reportError;
        }

        if (report.ProfileExtension == null)
        {
            return "The receiver report profile-extension array is required.";
        }

        if (report.ProfileExtension.Length % RtcpProtocolConstants.LengthWordSize != 0)
        {
            return "The receiver report profile extension must be word-aligned.";
        }

        return null;
    }

    internal static string? GetCompoundValidationError(
        RtcpCompoundPacket compoundPacket,
        bool strictValidation,
        RtcpCompoundMode compoundMode,
        int maximumPacketSize,
        int maximumCompoundPacketCount)
    {
        if (compoundPacket == null)
        {
            return "The RTCP compound packet is required.";
        }

        if (compoundPacket.Packets == null || compoundPacket.Packets.Length == 0)
        {
            return "The RTCP compound packet must contain at least one packet.";
        }

        if (compoundPacket.Packets.Length > maximumCompoundPacketCount)
        {
            return "The RTCP compound packet exceeds the configured packet-count limit.";
        }

        long compoundSize = 0;
        int packetIndex = 0;
        while (packetIndex < compoundPacket.Packets.Length)
        {
            string? packetError = GetPacketValidationError(
                compoundPacket.Packets[packetIndex],
                strictValidation,
                maximumPacketSize);
            if (packetError != null)
            {
                return packetError;
            }

            compoundSize = compoundSize +
                CalculatePacketEncodedSize(compoundPacket.Packets[packetIndex]);
            if (compoundSize > maximumPacketSize)
            {
                return "The encoded RTCP compound packet exceeds the configured size limit.";
            }

            packetIndex = packetIndex + 1;
        }

        if (strictValidation)
        {
            return RtcpCompoundValidator.GetValidationError(compoundPacket, compoundMode);
        }

        return null;
    }

    internal static void EncodeValidatedPacket(
        RtcpPacket packet,
        Span<byte> destination,
        bool preservePadding)
    {
        int encodedSize = CalculatePacketEncodedSize(packet);
        int lengthInWordsMinusOne =
            encodedSize / RtcpProtocolConstants.LengthWordSize - 1;
        byte firstByte = (byte)(RtcpProtocolConstants.Version <<
            RtcpProtocolConstants.VersionShift);
        if (packet.Header.HasPadding)
        {
            firstByte = (byte)(firstByte | RtcpProtocolConstants.PaddingMask);
        }

        firstByte = (byte)(firstByte | GetPacketWireCount(packet));
        NetworkBinaryWriter.WriteByte(destination, 0, firstByte);
        NetworkBinaryWriter.WriteByte(destination, 1, GetPacketWireType(packet));
        NetworkBinaryWriter.WriteUInt16(
            destination,
            2,
            (ushort)lengthInWordsMinusOne);
        if (packet.Type == RtcpPacketType.SenderReport)
        {
            EncodeSenderReportBody(
                GetRequiredSenderReport(packet),
                destination.Slice(RtcpProtocolConstants.HeaderSize));
        }
        else if (packet.Type == RtcpPacketType.ReceiverReport)
        {
            EncodeReceiverReportBody(
                GetRequiredReceiverReport(packet),
                destination.Slice(RtcpProtocolConstants.HeaderSize));
        }
        else if (packet.Type == RtcpPacketType.SourceDescription)
        {
            RtcpSourceDescriptionWireCodec.EncodeSourceDescription(
                GetRequiredSourceDescription(packet),
                destination.Slice(RtcpProtocolConstants.HeaderSize));
        }
        else if (packet.Type == RtcpPacketType.Goodbye)
        {
            RtcpGoodbyeWireCodec.EncodeGoodbye(
                GetRequiredGoodbye(packet),
                destination.Slice(RtcpProtocolConstants.HeaderSize));
        }
        else if (packet.Type == RtcpPacketType.ApplicationDefined)
        {
            RtcpApplicationDefinedWireCodec.EncodeApplicationDefined(
                GetRequiredApplicationDefined(packet),
                destination.Slice(RtcpProtocolConstants.HeaderSize));
        }
        else if (packet.Type == RtcpPacketType.ExtendedReport)
        {
            RtcpExtendedReportWireCodec.EncodeExtendedReport(
                GetRequiredExtendedReport(packet),
                destination.Slice(RtcpProtocolConstants.HeaderSize));
        }
        else if (packet.Type == RtcpPacketType.TransportFeedback ||
            packet.Type == RtcpPacketType.PayloadFeedback)
        {
            RtcpFeedbackWireCodec.EncodeFeedback(
                GetRequiredFeedback(packet),
                destination.Slice(RtcpProtocolConstants.HeaderSize));
        }
        else
        {
            RtcpUnknownPacket opaqueBody = GetRequiredOpaqueBody(packet);
            NetworkBinaryWriter.WriteBytes(
                destination,
                RtcpProtocolConstants.HeaderSize,
                opaqueBody.Data);
        }

        int paddingOffset = encodedSize - packet.Padding.Length;
        if (preservePadding)
        {
            NetworkBinaryWriter.WriteBytes(destination, paddingOffset, packet.Padding);
        }
        else if (packet.Header.HasPadding)
        {
            int paddingIndex = 0;
            while (paddingIndex < packet.Padding.Length - 1)
            {
                destination[paddingOffset + paddingIndex] = 0;
                paddingIndex = paddingIndex + 1;
            }

            destination[encodedSize - 1] = (byte)packet.Padding.Length;
        }
    }

    internal static void EncodeValidatedCompound(
        RtcpCompoundPacket compoundPacket,
        Span<byte> destination,
        bool preservePadding)
    {
        int offset = 0;
        int packetIndex = 0;
        while (packetIndex < compoundPacket.Packets.Length)
        {
            RtcpPacket packet = compoundPacket.Packets[packetIndex];
            int packetSize = CalculatePacketEncodedSize(packet);
            EncodeValidatedPacket(
                packet,
                destination.Slice(offset, packetSize),
                preservePadding);
            offset = offset + packetSize;
            packetIndex = packetIndex + 1;
        }
    }

    internal static int CalculatePacketEncodedSize(RtcpPacket packet)
    {
        return (int)((long)RtcpProtocolConstants.HeaderSize +
            CalculatePacketBodySizeLong(packet) + packet.Padding.Length);
    }

    internal static long CalculatePacketBodySizeLong(RtcpPacket packet)
    {
        if (packet.Type == RtcpPacketType.SenderReport)
        {
            RtcpSenderReport senderReport = GetRequiredSenderReport(packet);
            return RtcpProtocolConstants.SenderReportMinimumSize -
                RtcpProtocolConstants.HeaderSize +
                (long)senderReport.ReceptionReports.Length *
                RtcpProtocolConstants.ReceptionReportSize +
                senderReport.ProfileExtension.Length;
        }

        if (packet.Type == RtcpPacketType.ReceiverReport)
        {
            RtcpReceiverReport receiverReport = GetRequiredReceiverReport(packet);
            return RtcpProtocolConstants.ReceiverReportMinimumSize -
                RtcpProtocolConstants.HeaderSize +
                (long)receiverReport.ReceptionReports.Length *
                RtcpProtocolConstants.ReceptionReportSize +
                receiverReport.ProfileExtension.Length;
        }

        if (packet.Type == RtcpPacketType.SourceDescription)
        {
            return RtcpSourceDescriptionWireCodec.CalculateSourceDescriptionBodySize(
                GetRequiredSourceDescription(packet));
        }

        if (packet.Type == RtcpPacketType.Goodbye)
        {
            return RtcpGoodbyeWireCodec.CalculateGoodbyeBodySize(
                GetRequiredGoodbye(packet));
        }

        if (packet.Type == RtcpPacketType.ApplicationDefined)
        {
            return RtcpApplicationDefinedWireCodec.CalculateApplicationDefinedBodySize(
                GetRequiredApplicationDefined(packet));
        }

        if (packet.Type == RtcpPacketType.ExtendedReport)
        {
            return RtcpExtendedReportWireCodec.CalculateExtendedReportBodySize(
                GetRequiredExtendedReport(packet));
        }

        if (packet.Type == RtcpPacketType.TransportFeedback ||
            packet.Type == RtcpPacketType.PayloadFeedback)
        {
            return RtcpFeedbackWireCodec.CalculateFeedbackBodySize(
                GetRequiredFeedback(packet));
        }

        return GetRequiredOpaqueBody(packet).Data.Length;
    }

    internal static int CalculateCompoundEncodedSize(RtcpCompoundPacket compoundPacket)
    {
        int encodedSize = 0;
        int packetIndex = 0;
        while (packetIndex < compoundPacket.Packets.Length)
        {
            encodedSize = encodedSize +
                CalculatePacketEncodedSize(compoundPacket.Packets[packetIndex]);
            packetIndex = packetIndex + 1;
        }

        return encodedSize;
    }

    internal static RtcpUnknownPacket GetRequiredOpaqueBody(RtcpPacket packet)
    {
        RtcpUnknownPacket? opaqueBody = packet.UnknownPacket;
        if (opaqueBody == null)
        {
            throw new RtcpPacketFormatException("The opaque RTCP packet body is required.");
        }

        return opaqueBody;
    }

    internal static RtcpSenderReport GetRequiredSenderReport(RtcpPacket packet)
    {
        RtcpSenderReport? report = packet.SenderReport;
        if (report == null)
        {
            throw new RtcpPacketFormatException("The RTCP sender report body is required.");
        }

        return report;
    }

    internal static RtcpReceiverReport GetRequiredReceiverReport(RtcpPacket packet)
    {
        RtcpReceiverReport? report = packet.ReceiverReport;
        if (report == null)
        {
            throw new RtcpPacketFormatException("The RTCP receiver report body is required.");
        }

        return report;
    }

    internal static RtcpSourceDescription GetRequiredSourceDescription(RtcpPacket packet)
    {
        RtcpSourceDescription? description = packet.SourceDescription;
        if (description == null)
        {
            throw new RtcpPacketFormatException(
                "The RTCP source-description body is required.");
        }

        return description;
    }

    internal static RtcpGoodbye GetRequiredGoodbye(RtcpPacket packet)
    {
        RtcpGoodbye? goodbye = packet.Goodbye;
        if (goodbye == null)
        {
            throw new RtcpPacketFormatException("The RTCP goodbye body is required.");
        }

        return goodbye;
    }

    internal static RtcpApplicationDefined GetRequiredApplicationDefined(
        RtcpPacket packet)
    {
        RtcpApplicationDefined? application = packet.ApplicationDefined;
        if (application == null)
        {
            throw new RtcpPacketFormatException(
                "The RTCP application-defined body is required.");
        }

        return application;
    }

    internal static RtcpFeedbackPacket GetRequiredFeedback(RtcpPacket packet)
    {
        RtcpFeedbackPacket? feedback = packet.Feedback;
        if (feedback == null)
        {
            throw new RtcpPacketFormatException("The RTCP feedback body is required.");
        }

        return feedback;
    }

    internal static RtcpExtendedReport GetRequiredExtendedReport(RtcpPacket packet)
    {
        RtcpExtendedReport? report = packet.ExtendedReport;
        if (report == null)
        {
            throw new RtcpPacketFormatException(
                "The RTCP extended-report body is required.");
        }

        return report;
    }

    internal static byte GetPacketWireCount(RtcpPacket packet)
    {
        if (packet.Type == RtcpPacketType.SenderReport)
        {
            return (byte)GetRequiredSenderReport(packet).ReceptionReports.Length;
        }

        if (packet.Type == RtcpPacketType.ReceiverReport)
        {
            return (byte)GetRequiredReceiverReport(packet).ReceptionReports.Length;
        }

        if (packet.Type == RtcpPacketType.SourceDescription)
        {
            return (byte)GetRequiredSourceDescription(packet).Chunks.Length;
        }

        if (packet.Type == RtcpPacketType.Goodbye)
        {
            return (byte)GetRequiredGoodbye(packet).Sources.Length;
        }

        if (packet.Type == RtcpPacketType.ApplicationDefined)
        {
            return GetRequiredApplicationDefined(packet).Subtype;
        }

        if (packet.Type == RtcpPacketType.ExtendedReport)
        {
            return 0;
        }

        if (packet.Type == RtcpPacketType.TransportFeedback ||
            packet.Type == RtcpPacketType.PayloadFeedback)
        {
            return RtcpFeedbackWireCodec.GetFeedbackWireFormat(
                GetRequiredFeedback(packet));
        }

        return GetRequiredOpaqueBody(packet).Count;
    }

    internal static byte GetPacketWireType(RtcpPacket packet)
    {
        if (packet.Type == RtcpPacketType.SenderReport)
        {
            return RtcpProtocolConstants.SenderReportPacketType;
        }

        if (packet.Type == RtcpPacketType.ReceiverReport)
        {
            return RtcpProtocolConstants.ReceiverReportPacketType;
        }

        if (packet.Type == RtcpPacketType.SourceDescription)
        {
            return RtcpProtocolConstants.SourceDescriptionPacketType;
        }

        if (packet.Type == RtcpPacketType.Goodbye)
        {
            return RtcpProtocolConstants.GoodbyePacketType;
        }

        if (packet.Type == RtcpPacketType.ApplicationDefined)
        {
            return RtcpProtocolConstants.ApplicationDefinedPacketType;
        }

        if (packet.Type == RtcpPacketType.ExtendedReport)
        {
            return RtcpProtocolConstants.ExtendedReportPacketType;
        }

        if (packet.Type == RtcpPacketType.TransportFeedback)
        {
            return RtcpProtocolConstants.TransportFeedbackPacketType;
        }

        if (packet.Type == RtcpPacketType.PayloadFeedback)
        {
            return RtcpProtocolConstants.PayloadFeedbackPacketType;
        }

        return GetRequiredOpaqueBody(packet).PacketType;
    }

    internal static RtcpSenderReport DecodeSenderReportBody(
        ReadOnlySpan<byte> body,
        int reportCount)
    {
        int reportBytes = reportCount * RtcpProtocolConstants.ReceptionReportSize;
        int reportsOffset = RtcpProtocolConstants.SourceIdentifierSize +
            RtcpProtocolConstants.SenderInformationSize;
        int profileOffset = reportsOffset + reportBytes;
        RtcpSenderReport report = new RtcpSenderReport();
        report.SenderSynchronizationSource = NetworkBinaryReader.ReadUInt32(body, 0);
        report.SenderInformation = RtcpReportBlockWireCodec.DecodeSenderInformation(
            body.Slice(
                RtcpProtocolConstants.SourceIdentifierSize,
                RtcpProtocolConstants.SenderInformationSize));
        report.ReceptionReports = RtcpReportBlockWireCodec.DecodeReceptionReports(
            body.Slice(reportsOffset, reportBytes),
            reportCount);
        report.ProfileExtension = body.Slice(profileOffset).ToArray();
        return report;
    }

    internal static RtcpReceiverReport DecodeReceiverReportBody(
        ReadOnlySpan<byte> body,
        int reportCount)
    {
        int reportBytes = reportCount * RtcpProtocolConstants.ReceptionReportSize;
        int profileOffset = RtcpProtocolConstants.SourceIdentifierSize + reportBytes;
        RtcpReceiverReport report = new RtcpReceiverReport();
        report.SenderSynchronizationSource = NetworkBinaryReader.ReadUInt32(body, 0);
        report.ReceptionReports = RtcpReportBlockWireCodec.DecodeReceptionReports(
            body.Slice(RtcpProtocolConstants.SourceIdentifierSize, reportBytes),
            reportCount);
        report.ProfileExtension = body.Slice(profileOffset).ToArray();
        return report;
    }

    internal static void EncodeSenderReportBody(
        RtcpSenderReport report,
        Span<byte> destination)
    {
        int reportsOffset = RtcpProtocolConstants.SourceIdentifierSize +
            RtcpProtocolConstants.SenderInformationSize;
        int reportBytes = report.ReceptionReports.Length *
            RtcpProtocolConstants.ReceptionReportSize;
        int profileOffset = reportsOffset + reportBytes;
        NetworkBinaryWriter.WriteUInt32(
            destination,
            0,
            report.SenderSynchronizationSource);
        RtcpReportBlockWireCodec.EncodeSenderInformation(
            report.SenderInformation,
            destination.Slice(
                RtcpProtocolConstants.SourceIdentifierSize,
                RtcpProtocolConstants.SenderInformationSize));
        RtcpReportBlockWireCodec.EncodeReceptionReports(
            report.ReceptionReports,
            destination.Slice(reportsOffset, reportBytes));
        NetworkBinaryWriter.WriteBytes(
            destination,
            profileOffset,
            report.ProfileExtension);
    }

    internal static void EncodeReceiverReportBody(
        RtcpReceiverReport report,
        Span<byte> destination)
    {
        int reportBytes = report.ReceptionReports.Length *
            RtcpProtocolConstants.ReceptionReportSize;
        int profileOffset = RtcpProtocolConstants.SourceIdentifierSize + reportBytes;
        NetworkBinaryWriter.WriteUInt32(
            destination,
            0,
            report.SenderSynchronizationSource);
        RtcpReportBlockWireCodec.EncodeReceptionReports(
            report.ReceptionReports,
            destination.Slice(RtcpProtocolConstants.SourceIdentifierSize, reportBytes));
        NetworkBinaryWriter.WriteBytes(
            destination,
            profileOffset,
            report.ProfileExtension);
    }

    internal static bool IsStrictPacketType(byte packetType)
    {
        return packetType >= RtcpProtocolConstants.MultiplexingPacketTypeMinimum &&
            packetType <= RtcpProtocolConstants.MultiplexingPacketTypeMaximum;
    }

    internal static RtcpPacketType GetPacketType(byte packetType)
    {
        if (packetType == RtcpProtocolConstants.HistoricFullIntraRequestPacketType)
        {
            return RtcpPacketType.HistoricFullIntraRequest;
        }

        if (packetType == RtcpProtocolConstants.HistoricNegativeAcknowledgementPacketType)
        {
            return RtcpPacketType.HistoricNegativeAcknowledgement;
        }

        if (packetType == RtcpProtocolConstants.SmpteTimeCodePacketType)
        {
            return RtcpPacketType.SmpteTimeCode;
        }

        if (packetType == RtcpProtocolConstants.InterArrivalJitterPacketType)
        {
            return RtcpPacketType.ExtendedInterArrivalJitter;
        }

        if (packetType >= RtcpProtocolConstants.SenderReportPacketType &&
            packetType <= RtcpProtocolConstants.SplicingNotificationPacketType)
        {
            return (RtcpPacketType)packetType;
        }

        return RtcpPacketType.Unknown;
    }

    internal static RtcpCodecResult CreateFailure(string errorMessage)
    {
        RtcpCodecResult result = new RtcpCodecResult();
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal static string GetFailureMessage(RtcpCodecResult result, string fallback)
    {
        if (result.ErrorMessage != null)
        {
            return result.ErrorMessage;
        }

        return fallback;
    }
}
