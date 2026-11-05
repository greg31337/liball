using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpRtpValidationCore
{
    internal static void ValidateRtpDescription(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        ValidateSessionAttributeScopes(description.Attributes, result, limiter);
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            bool rtpProtocol = IsRtpProtocol(media.Protocol);
            bool feedbackProtocol = IsFeedbackProtocol(media.Protocol);
            ValidateMediaAttributeApplicability(media, mediaIndex, rtpProtocol, feedbackProtocol, result, limiter);
            if (rtpProtocol)
            {
                bool validatePayloadSignaling = media.Port != 0 || SdpBundleValidationCore.ContainsValidPropertyAttribute(media.Attributes, SdpAttributeNames.BundleOnly);
                if (validatePayloadSignaling)
                {
                    ValidateMediaFormats(media, mediaIndex, result, limiter);
                    ValidateRtpAttributeValues(media, mediaIndex, feedbackProtocol, result, limiter);
                    ValidateOpusPacketTimes(media, mediaIndex, result, limiter);
                    ValidateRequiredMappings(media, mediaIndex, result, limiter);
                    ValidateCodecAssociations(media, mediaIndex, result, limiter);
                }
                SdpRtcpValidationCore.ValidateRtcpDescription(description, media, mediaIndex, feedbackProtocol, result, limiter);
                SdpSourceValidationCore.ValidateSourceDescription(media, mediaIndex, result, limiter);
            }
            mediaIndex = mediaIndex + 1;
        }
        SdpMsidValidationCore.ValidateMsidDescription(description, result, limiter);
        SdpRidValidationCore.ValidateRidDescription(description, result, limiter);
        SdpSimulcastValidationCore.ValidateSimulcastDescription(description, result, limiter);
        SdpExtMapValidationCore.ValidateExtMapDescription(description, result, limiter);
        SdpEffectiveValueCore.ValidateEffectiveValues(description, result, limiter);
    }

    internal static void ValidateSessionAttributeScopes(List<SdpAttribute> attributes, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (IsMediaOnlyRtpAttribute(attribute.Name))
            {
                int occurrenceIndex = ReadOccurrenceIndex(attributes, index);
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpValidationSeverity.Error, SdpScope.Session, attribute.Name, -1, occurrenceIndex, "The RTP-related attribute is permitted only at media scope.", attribute.Location);
            }
            index = index + 1;
        }
    }

    internal static void ValidateMediaAttributeApplicability(SdpMediaDescription media, int mediaIndex, bool rtpProtocol, bool feedbackProtocol, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            bool rtpMap = NameEquals(attribute.Name, SdpAttributeNames.RtpMap);
            bool feedback = NameEquals(attribute.Name, SdpAttributeNames.RtcpFeedback);
            bool rtcpTransport = SdpRtcpValidationCore.IsRtcpTransportAttribute(attribute.Name);
            bool source = SdpSourceValidationCore.IsSourceAttribute(attribute.Name);
            if (rtpMap && !rtpProtocol)
            {
                int occurrenceIndex = ReadOccurrenceIndex(media.Attributes, index);
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "The rtpmap attribute requires an RTP transport protocol.", attribute.Location);
            }
            else if (feedback && !feedbackProtocol)
            {
                int occurrenceIndex = ReadOccurrenceIndex(media.Attributes, index);
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "The rtcp-fb attribute requires an RTP feedback profile transport.", attribute.Location);
            }
            else if (rtcpTransport && !rtpProtocol)
            {
                int occurrenceIndex = ReadOccurrenceIndex(media.Attributes, index);
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "The RTCP transport attribute requires an RTP transport protocol.", attribute.Location);
            }
            else if (source && !rtpProtocol)
            {
                int occurrenceIndex = ReadOccurrenceIndex(media.Attributes, index);
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "The synchronization-source attribute requires an RTP transport protocol.", attribute.Location);
            }
            index = index + 1;
        }
    }

    internal static void ValidateMediaFormats(SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<int> payloads = new List<int>();
        int formatIndex = 0;
        while (formatIndex < media.Formats.Count)
        {
            string format = media.Formats[formatIndex];
            SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(format.AsSpan());
            if (!SdpCommonAttributeCodecCore.IsZeroBasedIntegerValue(format) || !payload.Success)
            {
                AddMediaFormatIssue(result, limiter, mediaIndex, formatIndex, "Every RTP media format must be a canonical payload type from zero through 127.", media.Location);
            }
            else if (payloads.Contains(payload.Int32Value))
            {
                AddMediaFormatIssue(result, limiter, mediaIndex, formatIndex, "An RTP payload type must not occur more than once in one media format list.", media.Location);
            }
            else
            {
                payloads.Add(payload.Int32Value);
                if (!HasValidRtpMap(media, payload.Int32Value) && StaticPayloadConflictsWithMedia(payload.Int32Value, media))
                {
                    AddMediaFormatIssue(result, limiter, mediaIndex, formatIndex, "The static RTP payload type is not defined for this media kind.", media.Location);
                }
            }
            formatIndex = formatIndex + 1;
        }
    }

    internal static void ValidateRtpAttributeValues(SdpMediaDescription media, int mediaIndex, bool feedbackProtocol, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<SdpRtpMapAttribute> mappings = new List<SdpRtpMapAttribute>();
        List<int> formatParameterPayloads = new List<int>();
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            int occurrenceIndex = ReadOccurrenceIndex(media.Attributes, index);
            if (NameEquals(attribute.Name, SdpAttributeNames.RtpMap))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success || decoded.RtpMapAttribute == null)
                {
                    AddDecodeIssue(decoded, attribute, mediaIndex, occurrenceIndex, result, limiter);
                }
                else
                {
                    ValidateRtpMap(media, decoded.RtpMapAttribute, mappings, mediaIndex, occurrenceIndex, attribute, result, limiter);
                }
            }
            else if (NameEquals(attribute.Name, SdpAttributeNames.FormatParameters))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success || decoded.FormatParametersAttribute == null)
                {
                    AddDecodeIssue(decoded, attribute, mediaIndex, occurrenceIndex, result, limiter);
                }
                else
                {
                    ValidateFormatParameters(media, decoded.FormatParametersAttribute, formatParameterPayloads, mediaIndex, occurrenceIndex, attribute, result, limiter);
                }
            }
            else if (feedbackProtocol && NameEquals(attribute.Name, SdpAttributeNames.RtcpFeedback))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success || decoded.RtcpFeedbackAttribute == null)
                {
                    AddDecodeIssue(decoded, attribute, mediaIndex, occurrenceIndex, result, limiter);
                }
                else
                {
                    ValidateFeedback(media, decoded.RtcpFeedbackAttribute, mediaIndex, occurrenceIndex, attribute, result, limiter);
                }
            }
            else if (NameEquals(attribute.Name, SdpAttributeNames.PacketTime) || NameEquals(attribute.Name, SdpAttributeNames.MaximumPacketTime))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success)
                {
                    AddDecodeIssue(decoded, attribute, mediaIndex, occurrenceIndex, result, limiter);
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateRtpMap(SdpMediaDescription media, SdpRtpMapAttribute mapping, List<SdpRtpMapAttribute> mappings, int mediaIndex, int occurrenceIndex, SdpAttribute attribute, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!ContainsMediaPayload(media, mapping.PayloadType))
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationDanglingPayloadReference, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "The rtpmap attribute refers to a payload absent from the media format list.", attribute.Location);
        }

        SdpRtpMapAttribute? previous = FindMapping(mappings, mapping.PayloadType);
        if (previous != null)
        {
            if (MappingsAgree(previous, mapping))
            {
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "At most one rtpmap attribute may be defined for each media format.", attribute.Location);
            }
            else
            {
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationPayloadType, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "The rtpmap attribute conflicts with an earlier mapping for the same payload type.", attribute.Location);
            }
        }
        else
        {
            mappings.Add(mapping);
        }

        bool audio = media.MediaKind == SdpMediaKind.Audio || string.Equals(media.MediaType, SdpMediaTypes.Audio, StringComparison.OrdinalIgnoreCase);
        bool video = media.MediaKind == SdpMediaKind.Video || string.Equals(media.MediaType, SdpMediaTypes.Video, StringComparison.OrdinalIgnoreCase);
        if (audio && mapping.HasEncodingParameters && !mapping.HasChannelCount)
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "An audio rtpmap encoding parameter must be a positive channel count.", attribute.Location);
        }
        else if (video && mapping.HasEncodingParameters)
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "A video rtpmap attribute must not contain audio channel parameters.", attribute.Location);
        }
        bool genericEncodingParameterIssue = audio && mapping.HasEncodingParameters && !mapping.HasChannelCount || video && mapping.HasEncodingParameters;
        if (previous == null && !genericEncodingParameterIssue && SdpOpusCodecCore.IsOpusEncoding(mapping.EncodingName) && (!audio || mapping.ClockRate != 48000 || !mapping.HasChannelCount || mapping.ChannelCount != 2))
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "An Opus RTP mapping must use audio media, a 48000 Hz clock rate, and two channels.", attribute.Location);
        }
        if (previous == null && !genericEncodingParameterIssue && SdpH264CodecCore.IsH264Encoding(mapping.EncodingName) && (!video || mapping.ClockRate != 90000 || mapping.HasEncodingParameters))
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "An H.264 RTP mapping must use video media, a 90000 Hz clock rate, and no encoding parameters.", attribute.Location);
        }
        string additionalError = SdpAdditionalCodecCore.ReadRtpMapValidationError(mapping, audio, video);
        if (previous == null && !genericEncodingParameterIssue && additionalError.Length > 0)
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, additionalError, attribute.Location);
        }
    }

    internal static void ValidateFormatParameters(SdpMediaDescription media, SdpFormatParametersAttribute formatParameters, List<int> payloads, int mediaIndex, int occurrenceIndex, SdpAttribute attribute, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!ContainsMediaPayload(media, formatParameters.PayloadType))
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationDanglingPayloadReference, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "The fmtp attribute refers to a payload absent from the media format list.", attribute.Location);
        }
        if (payloads.Contains(formatParameters.PayloadType))
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "At most one fmtp attribute may be defined for each media format.", attribute.Location);
        }
        else
        {
            payloads.Add(formatParameters.PayloadType);
        }
        SdpRtpMapAttribute? mapping = FindMediaRtpMapForPayload(media, formatParameters.PayloadType);
        if (mapping != null && SdpOpusCodecCore.IsOpusEncoding(mapping.EncodingName))
        {
            ValidateOpusFormatParameters(formatParameters, mediaIndex, occurrenceIndex, attribute, result, limiter);
        }
        if (mapping != null && SdpH264CodecCore.IsH264Encoding(mapping.EncodingName))
        {
            ValidateH264FormatParameters(formatParameters, mediaIndex, occurrenceIndex, attribute, result, limiter);
        }
        if (mapping != null && SdpAdditionalCodecCore.IsAdditionalTypedEncoding(mapping.EncodingName))
        {
            ValidateAdditionalCodecFormatParameters(mapping.EncodingName, formatParameters, mediaIndex, occurrenceIndex, attribute, result, limiter);
        }
    }

    internal static void ValidateAdditionalCodecFormatParameters(string encodingName, SdpFormatParametersAttribute formatParameters, int mediaIndex, int occurrenceIndex, SdpAttribute attribute, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (SdpAdditionalCodecCore.UsesRawPayloadList(encodingName))
        {
            if (!SdpAdditionalCodecCore.IsRawFormatValueValid(encodingName, formatParameters.RawParameters))
            {
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "A RED payload list or telephone-event range list contains an invalid value.", attribute.Location);
            }
            return;
        }
        if (formatParameters.RawParameters.Length > 0 && formatParameters.Parameters.Count == 0)
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "Recognized codec format parameters must use semicolon-separated parameter=value syntax.", attribute.Location);
            return;
        }
        List<string> recognizedNames = new List<string>();
        int parameterIndex = 0;
        while (parameterIndex < formatParameters.Parameters.Count)
        {
            SdpFormatParameter parameter = formatParameters.Parameters[parameterIndex];
            if (SdpAdditionalCodecCore.IsRecognizedParameter(encodingName, parameter.Name))
            {
                if (SdpOpusCodecCore.ContainsParameterName(recognizedNames, parameter.Name))
                {
                    SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "A recognized codec format parameter must not occur more than once.", attribute.Location);
                }
                else
                {
                    recognizedNames.Add(parameter.Name);
                }
                string detail = SdpAdditionalCodecCore.ReadParameterValidationError(encodingName, parameter);
                if (detail.Length > 0)
                {
                    SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, detail, attribute.Location);
                }
            }
            parameterIndex = parameterIndex + 1;
        }
        string collectionError = SdpAdditionalCodecCore.ReadParameterCollectionValidationError(encodingName, formatParameters.Parameters);
        if (collectionError.Length > 0)
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, collectionError, attribute.Location);
        }
    }

    internal static void ValidateH264FormatParameters(SdpFormatParametersAttribute formatParameters, int mediaIndex, int occurrenceIndex, SdpAttribute attribute, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (formatParameters.RawParameters.Length > 0 && formatParameters.Parameters.Count == 0)
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "H.264 format parameters must use semicolon-separated parameter=value syntax.", attribute.Location);
            return;
        }
        List<string> recognizedNames = new List<string>();
        int parameterIndex = 0;
        while (parameterIndex < formatParameters.Parameters.Count)
        {
            SdpFormatParameter parameter = formatParameters.Parameters[parameterIndex];
            if (SdpH264CodecCore.IsRecognizedParameter(parameter.Name))
            {
                if (SdpOpusCodecCore.ContainsParameterName(recognizedNames, parameter.Name))
                {
                    SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "An H.264 format parameter must not occur more than once.", attribute.Location);
                }
                else
                {
                    recognizedNames.Add(parameter.Name);
                }
                string detail = SdpH264CodecCore.ReadParameterValidationError(parameter);
                if (detail.Length > 0)
                {
                    SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, detail, attribute.Location);
                }
            }
            parameterIndex = parameterIndex + 1;
        }
    }

    internal static void ValidateOpusFormatParameters(SdpFormatParametersAttribute formatParameters, int mediaIndex, int occurrenceIndex, SdpAttribute attribute, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (formatParameters.RawParameters.Length > 0 && formatParameters.Parameters.Count == 0)
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "Opus format parameters must use semicolon-separated parameter=value syntax.", attribute.Location);
            return;
        }
        List<string> recognizedNames = new List<string>();
        int parameterIndex = 0;
        while (parameterIndex < formatParameters.Parameters.Count)
        {
            SdpFormatParameter parameter = formatParameters.Parameters[parameterIndex];
            if (SdpOpusCodecCore.IsRecognizedParameter(parameter.Name))
            {
                if (SdpOpusCodecCore.ContainsParameterName(recognizedNames, parameter.Name))
                {
                    SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "An Opus format parameter must not occur more than once.", attribute.Location);
                }
                else
                {
                    recognizedNames.Add(parameter.Name);
                }
                string detail = SdpOpusCodecCore.ReadParameterValidationError(parameter);
                if (detail.Length > 0)
                {
                    SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, detail, attribute.Location);
                }
            }
            parameterIndex = parameterIndex + 1;
        }
    }

    internal static void ValidateOpusPacketTimes(SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!ContainsOpusRtpMap(media))
        {
            return;
        }
        SdpCommonAttribute? packetTime = null;
        SdpCommonAttribute? maximumPacketTime = null;
        SdpAttribute? maximumSource = null;
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (NameEquals(attribute.Name, SdpAttributeNames.PacketTime) || NameEquals(attribute.Name, SdpAttributeNames.MaximumPacketTime))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.CommonAttribute != null)
                {
                    SdpCommonAttribute common = decoded.CommonAttribute;
                    int occurrenceIndex = ReadOccurrenceIndex(media.Attributes, index);
                    if (!SdpOpusCodecCore.IsPacketTimeAttributeValid(common))
                    {
                        SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "An Opus packet time must be a supported integer duration from 3 through 120 milliseconds.", attribute.Location);
                    }
                    else if (NameEquals(attribute.Name, SdpAttributeNames.PacketTime) && packetTime == null)
                    {
                        packetTime = common;
                    }
                    else if (NameEquals(attribute.Name, SdpAttributeNames.MaximumPacketTime) && maximumPacketTime == null)
                    {
                        maximumPacketTime = common;
                        maximumSource = attribute;
                    }
                }
            }
            index = index + 1;
        }
        if (packetTime != null && maximumPacketTime != null && maximumSource != null && packetTime.NumericValue > maximumPacketTime.NumericValue)
        {
            int occurrenceIndex = ReadOccurrenceIndex(media.Attributes, media.Attributes.IndexOf(maximumSource));
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, SdpScope.Media, maximumSource.Name, mediaIndex, occurrenceIndex, "The Opus maximum packet time must not be less than the preferred packet time.", maximumSource.Location);
        }
    }

    internal static bool ContainsOpusRtpMap(SdpMediaDescription media)
    {
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (NameEquals(attribute.Name, SdpAttributeNames.RtpMap))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.RtpMapAttribute != null && SdpOpusCodecCore.IsOpusEncoding(decoded.RtpMapAttribute.EncodingName))
                {
                    return true;
                }
            }
            index = index + 1;
        }
        return false;
    }

    internal static SdpRtpMapAttribute? FindMediaRtpMapForPayload(SdpMediaDescription media, int payloadType)
    {
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (NameEquals(attribute.Name, SdpAttributeNames.RtpMap))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.RtpMapAttribute != null && decoded.RtpMapAttribute.PayloadType == payloadType)
                {
                    return decoded.RtpMapAttribute;
                }
            }
            index = index + 1;
        }
        return null;
    }

    internal static void ValidateFeedback(SdpMediaDescription media, SdpRtcpFeedbackAttribute feedback, int mediaIndex, int occurrenceIndex, SdpAttribute attribute, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (feedback.PayloadType == "*")
        {
            return;
        }
        SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(feedback.PayloadType.AsSpan());
        if (!payload.Success || !ContainsMediaPayload(media, payload.Int32Value))
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationDanglingPayloadReference, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, "The rtcp-fb attribute refers to a payload absent from the media format list.", attribute.Location);
        }
    }

    internal static void ValidateRequiredMappings(SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<int> checkedPayloads = new List<int>();
        int formatIndex = 0;
        while (formatIndex < media.Formats.Count)
        {
            string format = media.Formats[formatIndex];
            SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(format.AsSpan());
            if (SdpCommonAttributeCodecCore.IsZeroBasedIntegerValue(format) && payload.Success && !checkedPayloads.Contains(payload.Int32Value))
            {
                checkedPayloads.Add(payload.Int32Value);
                if (!HasStaticAvpMapping(payload.Int32Value) && !HasValidRtpMap(media, payload.Int32Value))
                {
                    SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationDynamicPayloadMissingRtpMap, SdpValidationSeverity.Error, SdpScope.Media, SdpAttributeNames.RtpMap, mediaIndex, -1, "A dynamically assigned RTP payload type requires a valid rtpmap attribute.", media.Location);
                }
            }
            formatIndex = formatIndex + 1;
        }
    }

    internal static void ValidateCodecAssociations(SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<SdpCodec> codecs = SdpCodecServiceCore.ResolveMediaCodecs(media);
        int index = 0;
        while (index < codecs.Count)
        {
            SdpCodec codec = codecs[index];
            if (SdpAdditionalCodecCore.IsAdditionalTypedEncoding(codec.EncodingName))
            {
                string missingParameters = SdpAdditionalCodecCore.ReadMissingFormatParametersError(codec);
                if (missingParameters.Length > 0)
                {
                    AddCodecAssociationIssue(media, codec, mediaIndex, SdpDiagnosticCodes.ValidationAttribute, missingParameters, result, limiter);
                }
                string associationError = SdpAdditionalCodecCore.ReadAssociationValidationError(codec, codecs);
                if (associationError.Length > 0)
                {
                    AddCodecAssociationIssue(media, codec, mediaIndex, SdpDiagnosticCodes.ValidationDanglingPayloadReference, associationError, result, limiter);
                }
            }
            index = index + 1;
        }
    }

    internal static void AddCodecAssociationIssue(SdpMediaDescription media, SdpCodec codec, int mediaIndex, string code, string detail, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpAttribute? source = null;
        if (codec.FormatParameters.Count > 0)
        {
            source = codec.FormatParameters[0].SourceAttribute;
        }
        if (source == null && codec.RtpMap != null)
        {
            source = codec.RtpMap.SourceAttribute;
        }
        string attributeName = SdpAttributeNames.RtpMap;
        int occurrenceIndex = -1;
        SdpSourceLocation location = media.Location;
        if (source != null)
        {
            attributeName = source.Name;
            int sourceIndex = media.Attributes.IndexOf(source);
            if (sourceIndex >= 0)
            {
                occurrenceIndex = ReadOccurrenceIndex(media.Attributes, sourceIndex);
            }
            location = source.Location;
        }
        SdpValidationCore.AddValidationIssue(result, limiter, code, SdpValidationSeverity.Error, SdpScope.Media, attributeName, mediaIndex, occurrenceIndex, detail, location);
    }

    internal static bool IsRtpProtocol(string protocol)
    {
        return protocol.StartsWith("RTP/", StringComparison.OrdinalIgnoreCase) || protocol.IndexOf("/RTP/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    internal static bool IsFeedbackProtocol(string protocol)
    {
        return IsRtpProtocol(protocol) && protocol.EndsWith("AVPF", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsMediaOnlyRtpAttribute(string name)
    {
        return NameEquals(name, SdpAttributeNames.RtpMap)
            || NameEquals(name, SdpAttributeNames.FormatParameters)
            || NameEquals(name, SdpAttributeNames.RtcpFeedback)
            || SdpRtcpValidationCore.IsRtcpTransportAttribute(name)
            || SdpSourceValidationCore.IsSourceAttribute(name)
            || NameEquals(name, SdpAttributeNames.PacketTime)
            || NameEquals(name, SdpAttributeNames.MaximumPacketTime);
    }

    internal static bool NameEquals(string first, string second)
    {
        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool ContainsMediaPayload(SdpMediaDescription media, int payloadType)
    {
        int index = 0;
        while (index < media.Formats.Count)
        {
            string format = media.Formats[index];
            if (SdpCommonAttributeCodecCore.IsZeroBasedIntegerValue(format))
            {
                SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(format.AsSpan());
                if (payload.Success && payload.Int32Value == payloadType)
                {
                    return true;
                }
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool HasValidRtpMap(SdpMediaDescription media, int payloadType)
    {
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (NameEquals(attribute.Name, SdpAttributeNames.RtpMap))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.RtpMapAttribute != null && decoded.RtpMapAttribute.PayloadType == payloadType)
                {
                    return true;
                }
            }
            index = index + 1;
        }
        return false;
    }

    internal static SdpRtpMapAttribute? FindMapping(List<SdpRtpMapAttribute> mappings, int payloadType)
    {
        int index = 0;
        while (index < mappings.Count)
        {
            if (mappings[index].PayloadType == payloadType)
            {
                return mappings[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static bool MappingsAgree(SdpRtpMapAttribute first, SdpRtpMapAttribute second)
    {
        if (!string.Equals(first.EncodingName, second.EncodingName, StringComparison.OrdinalIgnoreCase) || first.ClockRate != second.ClockRate)
        {
            return false;
        }
        if (first.HasChannelCount || second.HasChannelCount)
        {
            int firstChannels = first.HasChannelCount ? first.ChannelCount : 1;
            int secondChannels = second.HasChannelCount ? second.ChannelCount : 1;
            return firstChannels == secondChannels;
        }
        if (first.HasEncodingParameters != second.HasEncodingParameters)
        {
            return false;
        }
        return !first.HasEncodingParameters || first.EncodingParameters == second.EncodingParameters;
    }

    internal static bool HasStaticAvpMapping(int payloadType)
    {
        switch (payloadType)
        {
            case SdpStaticPayloadTypes.Pcmu:
            case SdpStaticPayloadTypes.Gsm:
            case SdpStaticPayloadTypes.G723:
            case SdpStaticPayloadTypes.Dvi4_8000:
            case SdpStaticPayloadTypes.Dvi4_16000:
            case SdpStaticPayloadTypes.Lpc:
            case SdpStaticPayloadTypes.Pcma:
            case SdpStaticPayloadTypes.G722:
            case SdpStaticPayloadTypes.Linear16Stereo:
            case SdpStaticPayloadTypes.Linear16Mono:
            case SdpStaticPayloadTypes.Qcelp:
            case SdpStaticPayloadTypes.ComfortNoise:
            case SdpStaticPayloadTypes.MpegAudio:
            case SdpStaticPayloadTypes.G728:
            case SdpStaticPayloadTypes.Dvi4_11025:
            case SdpStaticPayloadTypes.Dvi4_22050:
            case SdpStaticPayloadTypes.G729:
            case SdpStaticPayloadTypes.CellB:
            case SdpStaticPayloadTypes.Jpeg:
            case SdpStaticPayloadTypes.Nv:
            case SdpStaticPayloadTypes.H261:
            case SdpStaticPayloadTypes.MpegVideo:
            case SdpStaticPayloadTypes.Mpeg2TransportStream:
            case SdpStaticPayloadTypes.H263:
                return true;
            default:
                return false;
        }
    }

    internal static bool StaticPayloadConflictsWithMedia(int payloadType, SdpMediaDescription media)
    {
        bool audioPayload = payloadType >= SdpStaticPayloadTypes.Pcmu && payloadType <= SdpStaticPayloadTypes.G729 && HasStaticAvpMapping(payloadType);
        bool videoPayload = payloadType == SdpStaticPayloadTypes.CellB || payloadType == SdpStaticPayloadTypes.Jpeg || payloadType == SdpStaticPayloadTypes.Nv || payloadType == SdpStaticPayloadTypes.H261 || payloadType == SdpStaticPayloadTypes.MpegVideo || payloadType == SdpStaticPayloadTypes.H263;
        bool audioMedia = media.MediaKind == SdpMediaKind.Audio || string.Equals(media.MediaType, SdpMediaTypes.Audio, StringComparison.OrdinalIgnoreCase);
        bool videoMedia = media.MediaKind == SdpMediaKind.Video || string.Equals(media.MediaType, SdpMediaTypes.Video, StringComparison.OrdinalIgnoreCase);
        return audioPayload && !audioMedia || videoPayload && !videoMedia;
    }

    internal static void AddDecodeIssue(SdpAttributeDecodeResult decoded, SdpAttribute attribute, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        string code = SdpDiagnosticCodes.ValidationAttribute;
        string detail = "The RTP-related attribute value does not satisfy its registered syntax.";
        if (decoded.Diagnostics.Count > 0)
        {
            code = decoded.Diagnostics[0].Code;
            detail = decoded.Diagnostics[0].Detail;
        }
        SdpValidationCore.AddValidationIssue(result, limiter, code, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, detail, attribute.Location);
    }

    internal static int ReadOccurrenceIndex(List<SdpAttribute> attributes, int targetIndex)
    {
        string name = attributes[targetIndex].Name;
        int occurrenceIndex = 0;
        int index = 0;
        while (index < targetIndex)
        {
            if (NameEquals(attributes[index].Name, name))
            {
                occurrenceIndex = occurrenceIndex + 1;
            }
            index = index + 1;
        }
        return occurrenceIndex;
    }

    internal static void AddMediaFormatIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, int mediaIndex, int formatIndex, string detail, SdpSourceLocation location)
    {
        string code = SdpDiagnosticCodes.ValidationPayloadType;
        if (SdpValidationCore.ContainsValidationCode(result.AppliedOptions.SuppressedDiagnosticCodes, code))
        {
            return;
        }
        SdpValidationSeverity severity = SdpValidationSeverity.Error;
        if ((int)severity < (int)result.AppliedOptions.MinimumSeverity)
        {
            return;
        }
        SdpSourcePath path = new SdpSourcePath();
        path.Scope = SdpScope.Media;
        path.MediaIndex = mediaIndex;
        path.FieldName = 'm';
        path.ValueIndex = formatIndex;
        SdpValidationIssue issue = SdpDiagnosticBuilder.CreateValidationIssue(code, severity, SdpScope.Media, 'm', string.Empty, mediaIndex, detail, path, SdpDiagnosticBuilder.CopySourceLocation(location));
        limiter.AddIssue(issue);
    }
}
