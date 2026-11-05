using System;

namespace rtp;

internal static class RtcpFeedbackWireCodec
{
    internal static string? GetFeedbackValidationError(
        RtcpFeedbackPacket? feedback,
        RtcpPacketType packetType)
    {
        if (feedback == null)
        {
            return "The RTCP feedback body is required.";
        }

        if (feedback.PacketType != RtcpFeedbackPacketType.TransportLayer &&
            feedback.PacketType != RtcpFeedbackPacketType.PayloadSpecific)
        {
            return "The RTCP feedback packet type is not recognized.";
        }

        if (packetType == RtcpPacketType.TransportFeedback &&
            feedback.PacketType != RtcpFeedbackPacketType.TransportLayer)
        {
            return "The RTPFB packet discriminator does not match its feedback body.";
        }

        if (packetType == RtcpPacketType.PayloadFeedback &&
            feedback.PacketType != RtcpFeedbackPacketType.PayloadSpecific)
        {
            return "The PSFB packet discriminator does not match its feedback body.";
        }

        if (feedback.Format > RtcpProtocolConstants.MaximumCount)
        {
            return "The RTCP feedback format exceeds five bits.";
        }

        if (feedback.RawFeedbackControlInformation == null)
        {
            return "The raw RTCP feedback control information array is required.";
        }

        if (feedback.PacketType == RtcpFeedbackPacketType.TransportLayer)
        {
            if (!IsRecognizedTransportFormat(feedback.TransportFormat))
            {
                return "The RTPFB format discriminator is not recognized.";
            }

            if (feedback.PayloadFormat != RtcpPayloadFeedbackFormat.Unknown)
            {
                return "An RTPFB packet cannot contain a PSFB format discriminator.";
            }

            byte typedFormat = (byte)feedback.TransportFormat;
            if (feedback.TransportFormat != RtcpTransportFeedbackFormat.Unknown &&
                feedback.Format != 0 &&
                feedback.Format != typedFormat)
            {
                return "The RTPFB wire format does not match its typed discriminator.";
            }
        }
        else
        {
            if (!IsRecognizedPayloadFormat(feedback.PayloadFormat))
            {
                return "The PSFB format discriminator is not recognized.";
            }

            if (feedback.TransportFormat != RtcpTransportFeedbackFormat.Unknown)
            {
                return "A PSFB packet cannot contain an RTPFB format discriminator.";
            }

            byte typedFormat = (byte)feedback.PayloadFormat;
            if (feedback.PayloadFormat != RtcpPayloadFeedbackFormat.Unknown &&
                feedback.Format != 0 &&
                feedback.Format != typedFormat)
            {
                return "The PSFB wire format does not match its typed discriminator.";
            }
        }

        if (feedback.PacketType == RtcpFeedbackPacketType.TransportLayer &&
            GetFeedbackWireFormat(feedback) == RtcpFeedbackFormats.TransportGenericNack)
        {
            return RtcpGenericNackWireCodec.GetGenericNackValidationError(
                feedback.GenericNack);
        }

        if (feedback.PacketType == RtcpFeedbackPacketType.TransportLayer &&
            (GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.TransportTemporaryMaximumBitrateRequest ||
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.TransportTemporaryMaximumBitrateNotification))
        {
            bool isNotification = GetFeedbackWireFormat(feedback) ==
                RtcpFeedbackFormats.TransportTemporaryMaximumBitrateNotification;
            return RtcpBitrateControlWireCodec.GetBitrateControlValidationError(
                feedback.BitrateControl,
                isNotification,
                feedback.MediaSourceIdentifier);
        }

        if (feedback.PacketType == RtcpFeedbackPacketType.TransportLayer &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.TransportCongestionControlFeedback)
        {
            return RtcpCongestionControlFeedbackWireCodec.
                GetCongestionControlFeedbackValidationError(
                    feedback.CongestionControlFeedback,
                    feedback.MediaSourceIdentifier);
        }

        if (feedback.PacketType == RtcpFeedbackPacketType.TransportLayer &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.TransportRapidResynchronizationRequest)
        {
            if (feedback.RawFeedbackControlInformation.Length != 0)
            {
                return "A rapid resynchronization request must not contain feedback control information.";
            }

            return RtcpRapidResynchronizationRequestWireCodec.
                GetRapidResynchronizationRequestValidationError(
                    feedback.RapidResynchronizationRequest);
        }

        if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadPictureLossIndication)
        {
            if (feedback.RawFeedbackControlInformation.Length != 0)
            {
                return "A picture loss indication must not contain feedback control information.";
            }

            return RtcpPictureLossIndicationWireCodec.
                GetPictureLossIndicationValidationError(
                    feedback.PictureLossIndication);
        }

        if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadSliceLossIndication)
        {
            return RtcpSliceLossIndicationWireCodec.
                GetSliceLossIndicationValidationError(
                    feedback.SliceLossIndication);
        }

        if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadReferencePictureSelectionIndication)
        {
            return RtcpReferencePictureSelectionWireCodec.
                GetReferencePictureSelectionValidationError(
                    feedback.ReferencePictureSelectionIndication);
        }

        if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadApplicationFeedback)
        {
            return RtcpApplicationFeedbackWireCodec.
                GetApplicationFeedbackValidationError(
                    feedback.ApplicationFeedback);
        }

        if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadFullIntraRequest)
        {
            return RtcpFullIntraRequestWireCodec.
                GetFullIntraRequestValidationError(
                    feedback.FullIntraRequest,
                    feedback.MediaSourceIdentifier);
        }

        if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            (GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffRequest ||
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffNotification))
        {
            bool isNotification = GetFeedbackWireFormat(feedback) ==
                RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffNotification;
            return RtcpTemporalSpatialTradeoffWireCodec.
                GetTemporalSpatialTradeoffValidationError(
                    feedback.TemporalSpatialTradeoffs,
                    isNotification,
                    feedback.MediaSourceIdentifier);
        }

        return null;
    }

    internal static string? GetFeedbackDecodeError(
        ReadOnlySpan<byte> body,
        byte packetType,
        byte format,
        int lengthInWordsMinusOne)
    {
        int minimumBodySize = RtcpProtocolConstants.FeedbackMinimumSize -
            RtcpProtocolConstants.HeaderSize;
        if (body.Length < minimumBodySize)
        {
            return "The RTCP feedback packet is shorter than its common feedback fields.";
        }

        if (packetType == RtcpProtocolConstants.TransportFeedbackPacketType &&
            format == RtcpFeedbackFormats.TransportGenericNack)
        {
            return RtcpGenericNackWireCodec.GetGenericNackDecodeError(
                body.Slice(minimumBodySize));
        }

        if (packetType == RtcpProtocolConstants.TransportFeedbackPacketType &&
            format == RtcpFeedbackFormats.TransportRapidResynchronizationRequest)
        {
            if (lengthInWordsMinusOne != 2)
            {
                return "A rapid resynchronization request length field must be 2.";
            }

            return RtcpRapidResynchronizationRequestWireCodec.
                GetRapidResynchronizationRequestDecodeError(
                    body.Slice(minimumBodySize));
        }

        if (packetType == RtcpProtocolConstants.TransportFeedbackPacketType &&
            (format ==
            RtcpFeedbackFormats.TransportTemporaryMaximumBitrateRequest ||
            format ==
            RtcpFeedbackFormats.TransportTemporaryMaximumBitrateNotification))
        {
            bool isNotification = format ==
                RtcpFeedbackFormats.TransportTemporaryMaximumBitrateNotification;
            return RtcpBitrateControlWireCodec.GetBitrateControlDecodeError(
                body.Slice(minimumBodySize),
                isNotification,
                NetworkBinaryReader.ReadUInt32(
                    body,
                    RtcpProtocolConstants.SourceIdentifierSize),
                lengthInWordsMinusOne);
        }

        if (packetType == RtcpProtocolConstants.TransportFeedbackPacketType &&
            format == RtcpFeedbackFormats.TransportCongestionControlFeedback)
        {
            return RtcpCongestionControlFeedbackWireCodec.
                GetCongestionControlFeedbackDecodeError(
                    body.Slice(RtcpProtocolConstants.SourceIdentifierSize));
        }

        if (packetType == RtcpProtocolConstants.PayloadFeedbackPacketType &&
            format == RtcpFeedbackFormats.PayloadPictureLossIndication)
        {
            if (lengthInWordsMinusOne != 2)
            {
                return "A picture loss indication length field must be 2.";
            }

            return RtcpPictureLossIndicationWireCodec.
                GetPictureLossIndicationDecodeError(
                    body.Slice(minimumBodySize));
        }

        if (packetType == RtcpProtocolConstants.PayloadFeedbackPacketType &&
            format == RtcpFeedbackFormats.PayloadSliceLossIndication)
        {
            return RtcpSliceLossIndicationWireCodec.
                GetSliceLossIndicationDecodeError(
                    body.Slice(minimumBodySize));
        }

        if (packetType == RtcpProtocolConstants.PayloadFeedbackPacketType &&
            format ==
            RtcpFeedbackFormats.PayloadReferencePictureSelectionIndication)
        {
            return RtcpReferencePictureSelectionWireCodec.
                GetReferencePictureSelectionDecodeError(
                    body.Slice(minimumBodySize));
        }

        if (packetType == RtcpProtocolConstants.PayloadFeedbackPacketType &&
            format == RtcpFeedbackFormats.PayloadApplicationFeedback)
        {
            return RtcpApplicationFeedbackWireCodec.
                GetApplicationFeedbackDecodeError(
                    body.Slice(minimumBodySize));
        }

        if (packetType == RtcpProtocolConstants.PayloadFeedbackPacketType &&
            format == RtcpFeedbackFormats.PayloadFullIntraRequest)
        {
            return RtcpFullIntraRequestWireCodec.GetFullIntraRequestDecodeError(
                body.Slice(minimumBodySize),
                NetworkBinaryReader.ReadUInt32(
                    body,
                    RtcpProtocolConstants.SourceIdentifierSize),
                lengthInWordsMinusOne);
        }

        if (packetType == RtcpProtocolConstants.PayloadFeedbackPacketType &&
            (format == RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffRequest ||
            format ==
            RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffNotification))
        {
            bool isNotification = format ==
                RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffNotification;
            return RtcpTemporalSpatialTradeoffWireCodec.
                GetTemporalSpatialTradeoffDecodeError(
                    body.Slice(minimumBodySize),
                    isNotification,
                    NetworkBinaryReader.ReadUInt32(
                        body,
                        RtcpProtocolConstants.SourceIdentifierSize),
                    lengthInWordsMinusOne);
        }

        return null;
    }

    internal static long CalculateFeedbackBodySize(RtcpFeedbackPacket feedback)
    {
        if (feedback.PacketType == RtcpFeedbackPacketType.TransportLayer &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.TransportCongestionControlFeedback)
        {
            return RtcpProtocolConstants.SourceIdentifierSize +
                RtcpCongestionControlFeedbackWireCodec.
                    CalculateCongestionControlFeedbackSize(
                        GetRequiredCongestionControlFeedback(feedback));
        }

        long feedbackControlSize = feedback.RawFeedbackControlInformation.Length;
        if (feedback.PacketType == RtcpFeedbackPacketType.TransportLayer &&
            GetFeedbackWireFormat(feedback) == RtcpFeedbackFormats.TransportGenericNack)
        {
            feedbackControlSize = RtcpGenericNackWireCodec.CalculateGenericNackSize(
                GetRequiredGenericNack(feedback));
        }
        else if (feedback.PacketType == RtcpFeedbackPacketType.TransportLayer &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.TransportRapidResynchronizationRequest)
        {
            feedbackControlSize = 0;
        }
        else if (feedback.PacketType == RtcpFeedbackPacketType.TransportLayer &&
            (GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.TransportTemporaryMaximumBitrateRequest ||
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.TransportTemporaryMaximumBitrateNotification))
        {
            feedbackControlSize = RtcpBitrateControlWireCodec.
                CalculateBitrateControlSize(
                    GetRequiredBitrateControl(feedback));
        }
        else if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadPictureLossIndication)
        {
            feedbackControlSize = 0;
        }
        else if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadSliceLossIndication)
        {
            feedbackControlSize = RtcpSliceLossIndicationWireCodec.
                CalculateSliceLossIndicationSize(
                    GetRequiredSliceLossIndication(feedback));
        }
        else if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadReferencePictureSelectionIndication)
        {
            feedbackControlSize = RtcpReferencePictureSelectionWireCodec.
                CalculateReferencePictureSelectionSize(
                    GetRequiredReferencePictureSelection(feedback));
        }
        else if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadApplicationFeedback)
        {
            feedbackControlSize = GetRequiredApplicationFeedback(feedback).
                Data.Length;
        }
        else if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadFullIntraRequest)
        {
            feedbackControlSize = RtcpFullIntraRequestWireCodec.
                CalculateFullIntraRequestSize(
                    GetRequiredFullIntraRequest(feedback));
        }
        else if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            (GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffRequest ||
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffNotification))
        {
            feedbackControlSize = RtcpTemporalSpatialTradeoffWireCodec.
                CalculateTemporalSpatialTradeoffSize(
                    feedback.TemporalSpatialTradeoffs);
        }

        return RtcpProtocolConstants.FeedbackMinimumSize -
            RtcpProtocolConstants.HeaderSize + feedbackControlSize;
    }

    internal static RtcpFeedbackPacket DecodeFeedback(
        ReadOnlySpan<byte> body,
        byte packetType,
        byte format)
    {
        int feedbackControlOffset = RtcpProtocolConstants.FeedbackMinimumSize -
            RtcpProtocolConstants.HeaderSize;
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.Format = format;
        feedback.SenderSourceIdentifier = NetworkBinaryReader.ReadUInt32(body, 0);
        if (packetType == RtcpProtocolConstants.TransportFeedbackPacketType &&
            format == RtcpFeedbackFormats.TransportCongestionControlFeedback)
        {
            feedback.MediaSourceIdentifier = 0;
            feedback.RawFeedbackControlInformation = body.Slice(
                RtcpProtocolConstants.SourceIdentifierSize).ToArray();
        }
        else
        {
            feedback.MediaSourceIdentifier = NetworkBinaryReader.ReadUInt32(
                body,
                RtcpProtocolConstants.SourceIdentifierSize);
            feedback.RawFeedbackControlInformation = body.Slice(
                feedbackControlOffset).ToArray();
        }

        if (packetType == RtcpProtocolConstants.TransportFeedbackPacketType)
        {
            feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
            feedback.TransportFormat = GetTransportFormat(format);
            if (format == RtcpFeedbackFormats.TransportGenericNack)
            {
                feedback.GenericNack = RtcpGenericNackWireCodec.DecodeGenericNack(
                    feedback.RawFeedbackControlInformation);
            }
            else if (format ==
                RtcpFeedbackFormats.TransportRapidResynchronizationRequest)
            {
                feedback.RapidResynchronizationRequest =
                    RtcpRapidResynchronizationRequestWireCodec.
                        DecodeRapidResynchronizationRequest();
            }
            else if (format ==
                RtcpFeedbackFormats.TransportTemporaryMaximumBitrateRequest ||
                format ==
                RtcpFeedbackFormats.TransportTemporaryMaximumBitrateNotification)
            {
                bool isNotification = format ==
                    RtcpFeedbackFormats.
                        TransportTemporaryMaximumBitrateNotification;
                feedback.BitrateControl =
                    RtcpBitrateControlWireCodec.DecodeBitrateControl(
                        feedback.RawFeedbackControlInformation,
                        isNotification);
            }
            else if (format ==
                RtcpFeedbackFormats.TransportCongestionControlFeedback)
            {
                feedback.CongestionControlFeedback =
                    RtcpCongestionControlFeedbackWireCodec.
                        DecodeCongestionControlFeedback(
                            feedback.RawFeedbackControlInformation);
            }
        }
        else
        {
            feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
            feedback.PayloadFormat = GetPayloadFormat(format);
            if (format == RtcpFeedbackFormats.PayloadPictureLossIndication)
            {
                feedback.PictureLossIndication =
                    RtcpPictureLossIndicationWireCodec.
                        DecodePictureLossIndication();
            }
            else if (format == RtcpFeedbackFormats.PayloadSliceLossIndication)
            {
                feedback.SliceLossIndication =
                    RtcpSliceLossIndicationWireCodec.DecodeSliceLossIndication(
                        feedback.RawFeedbackControlInformation);
            }
            else if (format ==
                RtcpFeedbackFormats.PayloadReferencePictureSelectionIndication)
            {
                feedback.ReferencePictureSelectionIndication =
                    RtcpReferencePictureSelectionWireCodec.
                        DecodeReferencePictureSelection(
                            feedback.RawFeedbackControlInformation);
            }
            else if (format == RtcpFeedbackFormats.PayloadApplicationFeedback)
            {
                feedback.ApplicationFeedback =
                    RtcpApplicationFeedbackWireCodec.DecodeApplicationFeedback(
                        feedback.RawFeedbackControlInformation);
            }
            else if (format == RtcpFeedbackFormats.PayloadFullIntraRequest)
            {
                feedback.FullIntraRequest =
                    RtcpFullIntraRequestWireCodec.DecodeFullIntraRequest(
                        feedback.RawFeedbackControlInformation);
            }
            else if (format ==
                RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffRequest ||
                format ==
                RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffNotification)
            {
                bool isNotification = format ==
                    RtcpFeedbackFormats.
                        PayloadTemporalSpatialTradeoffNotification;
                feedback.TemporalSpatialTradeoffs =
                    RtcpTemporalSpatialTradeoffWireCodec.
                        DecodeTemporalSpatialTradeoffs(
                            feedback.RawFeedbackControlInformation,
                            isNotification);
            }
        }

        return feedback;
    }

    internal static void EncodeFeedback(
        RtcpFeedbackPacket feedback,
        Span<byte> destination)
    {
        int feedbackControlOffset = RtcpProtocolConstants.FeedbackMinimumSize -
            RtcpProtocolConstants.HeaderSize;
        NetworkBinaryWriter.WriteUInt32(
            destination,
            0,
            feedback.SenderSourceIdentifier);
        NetworkBinaryWriter.WriteUInt32(
            destination,
            RtcpProtocolConstants.SourceIdentifierSize,
            feedback.MediaSourceIdentifier);
        if (feedback.PacketType == RtcpFeedbackPacketType.TransportLayer &&
            GetFeedbackWireFormat(feedback) == RtcpFeedbackFormats.TransportGenericNack)
        {
            RtcpGenericNackWireCodec.EncodeGenericNack(
                GetRequiredGenericNack(feedback),
                destination.Slice(feedbackControlOffset));
        }
        else if (feedback.PacketType == RtcpFeedbackPacketType.TransportLayer &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.TransportRapidResynchronizationRequest)
        {
            ;
        }
        else if (feedback.PacketType == RtcpFeedbackPacketType.TransportLayer &&
            (GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.TransportTemporaryMaximumBitrateRequest ||
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.TransportTemporaryMaximumBitrateNotification))
        {
            RtcpBitrateControlWireCodec.EncodeBitrateControl(
                GetRequiredBitrateControl(feedback),
                destination.Slice(feedbackControlOffset));
        }
        else if (feedback.PacketType == RtcpFeedbackPacketType.TransportLayer &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.TransportCongestionControlFeedback)
        {
            RtcpCongestionControlFeedbackWireCodec.
                EncodeCongestionControlFeedback(
                    GetRequiredCongestionControlFeedback(feedback),
                    destination.Slice(RtcpProtocolConstants.SourceIdentifierSize));
        }
        else if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadPictureLossIndication)
        {
            ;
        }
        else if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadSliceLossIndication)
        {
            RtcpSliceLossIndicationWireCodec.EncodeSliceLossIndication(
                GetRequiredSliceLossIndication(feedback),
                destination.Slice(feedbackControlOffset));
        }
        else if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadReferencePictureSelectionIndication)
        {
            RtcpReferencePictureSelectionWireCodec.
                EncodeReferencePictureSelection(
                    GetRequiredReferencePictureSelection(feedback),
                    destination.Slice(feedbackControlOffset));
        }
        else if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadApplicationFeedback)
        {
            RtcpApplicationFeedbackWireCodec.EncodeApplicationFeedback(
                GetRequiredApplicationFeedback(feedback),
                destination.Slice(feedbackControlOffset));
        }
        else if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadFullIntraRequest)
        {
            RtcpFullIntraRequestWireCodec.EncodeFullIntraRequest(
                GetRequiredFullIntraRequest(feedback),
                destination.Slice(feedbackControlOffset));
        }
        else if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            (GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffRequest ||
            GetFeedbackWireFormat(feedback) ==
            RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffNotification))
        {
            RtcpTemporalSpatialTradeoffWireCodec.
                EncodeTemporalSpatialTradeoffs(
                    feedback.TemporalSpatialTradeoffs,
                    destination.Slice(feedbackControlOffset));
        }
        else
        {
            NetworkBinaryWriter.WriteBytes(
                destination,
                feedbackControlOffset,
                feedback.RawFeedbackControlInformation);
        }
    }

    internal static byte GetFeedbackWireFormat(RtcpFeedbackPacket feedback)
    {
        if (feedback.PacketType == RtcpFeedbackPacketType.TransportLayer &&
            feedback.TransportFormat != RtcpTransportFeedbackFormat.Unknown)
        {
            return (byte)feedback.TransportFormat;
        }

        if (feedback.PacketType == RtcpFeedbackPacketType.PayloadSpecific &&
            feedback.PayloadFormat != RtcpPayloadFeedbackFormat.Unknown)
        {
            return (byte)feedback.PayloadFormat;
        }

        return feedback.Format;
    }

    internal static RtcpTransportFeedbackFormat GetTransportFormat(byte format)
    {
        if (format == RtcpFeedbackFormats.TransportGenericNack ||
            format == RtcpFeedbackFormats.TransportReserved ||
            format == RtcpFeedbackFormats.TransportTemporaryMaximumBitrateRequest ||
            format == RtcpFeedbackFormats.TransportTemporaryMaximumBitrateNotification ||
            format == RtcpFeedbackFormats.TransportRapidResynchronizationRequest ||
            format == RtcpFeedbackFormats.TransportRapidMulticastAcquisition ||
            format == RtcpFeedbackFormats.TransportThirdPartyLossEarlyIndication ||
            format == RtcpFeedbackFormats.TransportEcnFeedback ||
            format == RtcpFeedbackFormats.TransportPauseResume ||
            format == RtcpFeedbackFormats.TransportDelayBudgetInformation ||
            format == RtcpFeedbackFormats.TransportCongestionControlFeedback ||
            format == RtcpFeedbackFormats.TransportExtension)
        {
            return (RtcpTransportFeedbackFormat)format;
        }

        return RtcpTransportFeedbackFormat.Unknown;
    }

    internal static RtcpPayloadFeedbackFormat GetPayloadFormat(byte format)
    {
        if (format == RtcpFeedbackFormats.PayloadPictureLossIndication ||
            format == RtcpFeedbackFormats.PayloadSliceLossIndication ||
            format == RtcpFeedbackFormats.PayloadReferencePictureSelectionIndication ||
            format == RtcpFeedbackFormats.PayloadFullIntraRequest ||
            format == RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffRequest ||
            format == RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffNotification ||
            format == RtcpFeedbackFormats.PayloadVideoBackChannelMessage ||
            format == RtcpFeedbackFormats.PayloadThirdPartyLossEarlyIndication ||
            format == RtcpFeedbackFormats.PayloadRegionOfInterest ||
            format == RtcpFeedbackFormats.PayloadLayerRefreshRequest ||
            format == RtcpFeedbackFormats.PayloadViewport ||
            format == RtcpFeedbackFormats.PayloadApplicationFeedback ||
            format == RtcpFeedbackFormats.PayloadExtension)
        {
            return (RtcpPayloadFeedbackFormat)format;
        }

        return RtcpPayloadFeedbackFormat.Unknown;
    }

    internal static bool IsRecognizedTransportFormat(
        RtcpTransportFeedbackFormat format)
    {
        return format == RtcpTransportFeedbackFormat.Unknown ||
            GetTransportFormat((byte)format) == format;
    }

    internal static bool IsRecognizedPayloadFormat(RtcpPayloadFeedbackFormat format)
    {
        return format == RtcpPayloadFeedbackFormat.Unknown ||
            GetPayloadFormat((byte)format) == format;
    }

    internal static RtcpGenericNack GetRequiredGenericNack(
        RtcpFeedbackPacket feedback)
    {
        RtcpGenericNack? genericNack = feedback.GenericNack;
        if (genericNack == null)
        {
            throw new RtcpPacketFormatException("The Generic NACK body is required.");
        }

        return genericNack;
    }

    internal static RtcpSliceLossIndication GetRequiredSliceLossIndication(
        RtcpFeedbackPacket feedback)
    {
        RtcpSliceLossIndication? sliceLossIndication =
            feedback.SliceLossIndication;
        if (sliceLossIndication == null)
        {
            throw new RtcpPacketFormatException(
                "The slice loss indication body is required.");
        }

        return sliceLossIndication;
    }

    internal static RtcpReferencePictureSelectionIndication
        GetRequiredReferencePictureSelection(RtcpFeedbackPacket feedback)
    {
        RtcpReferencePictureSelectionIndication? indication =
            feedback.ReferencePictureSelectionIndication;
        if (indication == null)
        {
            throw new RtcpPacketFormatException(
                "The reference picture selection indication body is required.");
        }

        return indication;
    }

    internal static RtcpApplicationFeedback GetRequiredApplicationFeedback(
        RtcpFeedbackPacket feedback)
    {
        RtcpApplicationFeedback? applicationFeedback =
            feedback.ApplicationFeedback;
        if (applicationFeedback == null)
        {
            throw new RtcpPacketFormatException(
                "The application feedback body is required.");
        }

        return applicationFeedback;
    }

    internal static RtcpFullIntraRequest GetRequiredFullIntraRequest(
        RtcpFeedbackPacket feedback)
    {
        RtcpFullIntraRequest? fullIntraRequest = feedback.FullIntraRequest;
        if (fullIntraRequest == null)
        {
            throw new RtcpPacketFormatException(
                "The full intra request body is required.");
        }

        return fullIntraRequest;
    }

    internal static RtcpBitrateControl GetRequiredBitrateControl(
        RtcpFeedbackPacket feedback)
    {
        RtcpBitrateControl? bitrateControl = feedback.BitrateControl;
        if (bitrateControl == null)
        {
            throw new RtcpPacketFormatException(
                "The bitrate-control feedback body is required.");
        }

        return bitrateControl;
    }

    internal static RtcpCongestionControlFeedback
        GetRequiredCongestionControlFeedback(RtcpFeedbackPacket feedback)
    {
        RtcpCongestionControlFeedback? congestionControlFeedback =
            feedback.CongestionControlFeedback;
        if (congestionControlFeedback == null)
        {
            throw new RtcpPacketFormatException(
                "The congestion-control feedback body is required.");
        }

        return congestionControlFeedback;
    }
}
