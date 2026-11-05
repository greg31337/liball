using System;
using System.Collections.Generic;
using System.Globalization;

namespace sdp;

internal static class SdpCodecServiceCore
{
    internal static List<SdpCodec> ResolveMediaCodecs(SdpMediaDescription media)
    {
        List<SdpCodec> codecs = new List<SdpCodec>();
        if (!IsRtpProtocol(media.Protocol))
        {
            return codecs;
        }

        int formatIndex = 0;
        while (formatIndex < media.Formats.Count)
        {
            SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(media.Formats[formatIndex].AsSpan());
            if (payload.Success)
            {
                SdpCodec codec = new SdpCodec();
                codec.PayloadType = payload.Int32Value;
                codec.MediaKind = media.MediaKind;
                codec.MediaType = media.MediaType;
                PopulateRtpMap(codec, media);
                if (codec.RtpMap == null)
                {
                    PopulateStaticMapping(codec);
                }
                PopulateRelatedAttributes(codec, media);
                codecs.Add(codec);
            }
            formatIndex = formatIndex + 1;
        }
        return codecs;
    }

    internal static bool IsRtpProtocol(string protocol)
    {
        return string.Equals(protocol, SdpTransportProtocols.RtpAvp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocol, SdpTransportProtocols.RtpAvpf, StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocol, SdpTransportProtocols.RtpSavp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocol, SdpTransportProtocols.RtpSavpf, StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocol, SdpTransportProtocols.TcpRtpAvp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocol, SdpTransportProtocols.TcpRtpAvpf, StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocol, SdpTransportProtocols.UdpTlsRtpSavp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocol, SdpTransportProtocols.UdpTlsRtpSavpf, StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocol, SdpTransportProtocols.TcpDtlsRtpSavp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocol, SdpTransportProtocols.TcpDtlsRtpSavpf, StringComparison.OrdinalIgnoreCase);
    }

    internal static void PopulateRtpMap(SdpCodec codec, SdpMediaDescription media)
    {
        int attributeIndex = 0;
        while (attributeIndex < media.Attributes.Count)
        {
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(media.Attributes[attributeIndex]);
            SdpRtpMapAttribute? mapping = decoded.RtpMapAttribute;
            if (decoded.Success && mapping != null && mapping.PayloadType == codec.PayloadType)
            {
                codec.RtpMap = mapping;
                codec.EncodingName = mapping.EncodingName;
                codec.ClockRate = mapping.ClockRate;
                codec.HasChannelCount = mapping.HasChannelCount;
                codec.ChannelCount = mapping.ChannelCount;
                return;
            }
            attributeIndex = attributeIndex + 1;
        }
    }

    internal static void PopulateRelatedAttributes(SdpCodec codec, SdpMediaDescription media)
    {
        int attributeIndex = 0;
        while (attributeIndex < media.Attributes.Count)
        {
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(media.Attributes[attributeIndex]);
            SdpFormatParametersAttribute? formatParameters = decoded.FormatParametersAttribute;
            if (decoded.Success && formatParameters != null && formatParameters.PayloadType == codec.PayloadType)
            {
                codec.FormatParameters.Add(formatParameters);
            }
            SdpRtcpFeedbackAttribute? feedback = decoded.RtcpFeedbackAttribute;
            if (decoded.Success && feedback != null && FeedbackApplies(feedback, codec.PayloadType))
            {
                codec.RtcpFeedback.Add(feedback);
            }
            SdpCommonAttribute? common = decoded.CommonAttribute;
            if (codec.PacketTime == null && decoded.Success && common != null && common.Name == SdpAttributeNames.PacketTime)
            {
                codec.PacketTime = common;
            }
            if (codec.MaximumPacketTime == null && decoded.Success && common != null && common.Name == SdpAttributeNames.MaximumPacketTime)
            {
                codec.MaximumPacketTime = common;
            }
            attributeIndex = attributeIndex + 1;
        }
    }

    internal static bool FeedbackApplies(SdpRtcpFeedbackAttribute feedback, int payloadType)
    {
        if (feedback.PayloadType == "*")
        {
            return true;
        }
        SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(feedback.PayloadType.AsSpan());
        return payload.Success && payload.Int32Value == payloadType;
    }

    internal static bool IsCodecMatch(SdpCodec codec, SdpCodecCapability capability, SdpCodecMatchMode matchMode)
    {
        if (!MediaIdentityMatches(codec, capability))
        {
            return false;
        }
        if (codec.EncodingName.Length == 0 || capability.EncodingName.Length == 0 || !EncodingNamesMatch(codec.EncodingName, capability.EncodingName))
        {
            return false;
        }
        if (codec.ClockRate <= 0 || capability.ClockRate <= 0 || codec.ClockRate != capability.ClockRate)
        {
            return false;
        }
        if (capability.HasChannelCount && (capability.ChannelCount <= 0 || codec.ChannelCount <= 0 || codec.ChannelCount != capability.ChannelCount))
        {
            return false;
        }
        if (SdpOpusCodecCore.IsOpusEncoding(codec.EncodingName))
        {
            if (codec.ClockRate != 48000 || capability.ClockRate != 48000 || codec.RtpMap != null && (!codec.RtpMap.HasChannelCount || codec.RtpMap.ChannelCount != 2) || !SdpOpusCodecCore.AreCodecParametersValid(codec) || !SdpOpusCodecCore.AreCapabilityParametersValid(capability))
            {
                return false;
            }
            if (matchMode == SdpCodecMatchMode.Compatible)
            {
                return FeedbackCapabilitiesMatch(codec, capability, matchMode);
            }
        }
        if (SdpH264CodecCore.IsH264Encoding(codec.EncodingName))
        {
            if (codec.ClockRate != 90000 || capability.ClockRate != 90000 || codec.RtpMap != null && codec.RtpMap.HasEncodingParameters || !SdpH264CodecCore.AreCodecParametersValid(codec) || !SdpH264CodecCore.AreCapabilityParametersValid(capability))
            {
                return false;
            }
            if (matchMode == SdpCodecMatchMode.Compatible)
            {
                return SdpH264CodecCore.AreCompatible(CollectCodecParameters(codec), capability.FormatParameters) && FeedbackCapabilitiesMatch(codec, capability, matchMode);
            }
        }
        if (SdpAdditionalCodecCore.IsAdditionalTypedEncoding(codec.EncodingName))
        {
            if (!SdpAdditionalCodecCore.AreMappingValuesValid(codec, capability) || !SdpAdditionalCodecCore.AreCodecParametersValid(codec) || !SdpAdditionalCodecCore.AreCapabilityParametersValid(capability))
            {
                return false;
            }
            if (matchMode == SdpCodecMatchMode.Compatible)
            {
                return SdpAdditionalCodecCore.AreCompatible(codec, capability) && FeedbackCapabilitiesMatch(codec, capability, matchMode);
            }
            if (SdpAdditionalCodecCore.UsesRawPayloadList(codec.EncodingName))
            {
                return SdpAdditionalCodecCore.RawFormatParametersMatch(codec, capability) && FeedbackCapabilitiesMatch(codec, capability, matchMode);
            }
        }
        return FormatParametersMatch(codec, capability, matchMode) && FeedbackCapabilitiesMatch(codec, capability, matchMode);
    }

    internal static bool FeedbackCapabilitiesMatch(SdpCodec codec, SdpCodecCapability capability, SdpCodecMatchMode matchMode)
    {
        if (matchMode == SdpCodecMatchMode.Strict && codec.RtcpFeedback.Count != capability.RtcpFeedbackTypes.Count)
        {
            return false;
        }

        List<bool> matched = new List<bool>();
        int feedbackIndex = 0;
        while (feedbackIndex < codec.RtcpFeedback.Count)
        {
            matched.Add(false);
            feedbackIndex = feedbackIndex + 1;
        }

        int capabilityIndex = 0;
        while (capabilityIndex < capability.RtcpFeedbackTypes.Count)
        {
            string required = capability.RtcpFeedbackTypes[capabilityIndex];
            bool found = false;
            feedbackIndex = 0;
            while (feedbackIndex < codec.RtcpFeedback.Count && !found)
            {
                if (!matched[feedbackIndex] && FeedbackMatchesCapability(codec.RtcpFeedback[feedbackIndex], required))
                {
                    matched[feedbackIndex] = true;
                    found = true;
                }
                feedbackIndex = feedbackIndex + 1;
            }
            if (!found)
            {
                return false;
            }
            capabilityIndex = capabilityIndex + 1;
        }
        return true;
    }

    internal static bool FeedbackMatchesCapability(SdpRtcpFeedbackAttribute feedback, string capability)
    {
        if (feedback == null || capability == null || capability.Length == 0)
        {
            return false;
        }
        if (capability == feedback.Type)
        {
            return true;
        }
        if (!feedback.HasParameter)
        {
            return false;
        }
        return capability == feedback.Type + " " + feedback.Parameter;
    }

    internal static bool EncodingNamesMatch(string first, string second)
    {
        if (string.Equals(first, second, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return SdpAdditionalCodecCore.IsHevcEncoding(first) && SdpAdditionalCodecCore.IsHevcEncoding(second);
    }

    internal static bool MediaIdentityMatches(SdpCodec codec, SdpCodecCapability capability)
    {
        bool codecKindKnown = codec.MediaKind != SdpMediaKind.Unknown;
        bool capabilityKindKnown = capability.MediaKind != SdpMediaKind.Unknown;
        if (codecKindKnown && capabilityKindKnown && codec.MediaKind != capability.MediaKind)
        {
            return false;
        }
        bool codecTypeKnown = codec.MediaType.Length > 0;
        bool capabilityTypeKnown = capability.MediaType.Length > 0;
        if (codecTypeKnown && capabilityTypeKnown && !string.Equals(codec.MediaType, capability.MediaType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return codecKindKnown && capabilityKindKnown || codecTypeKnown && capabilityTypeKnown;
    }

    internal static bool FormatParametersMatch(SdpCodec codec, SdpCodecCapability capability, SdpCodecMatchMode matchMode)
    {
        List<SdpFormatParameter> offeredParameters = CollectCodecParameters(codec);
        if (matchMode == SdpCodecMatchMode.Strict && HasUnparsedFormatParameters(codec))
        {
            return false;
        }
        if (matchMode == SdpCodecMatchMode.Strict && offeredParameters.Count != capability.FormatParameters.Count)
        {
            return false;
        }

        List<bool> matched = new List<bool>();
        int offeredIndex = 0;
        while (offeredIndex < offeredParameters.Count)
        {
            matched.Add(false);
            offeredIndex = offeredIndex + 1;
        }

        int requiredIndex = 0;
        while (requiredIndex < capability.FormatParameters.Count)
        {
            SdpFormatParameter required = capability.FormatParameters[requiredIndex];
            bool found = false;
            offeredIndex = 0;
            while (offeredIndex < offeredParameters.Count && !found)
            {
                if (!matched[offeredIndex] && ParametersMatch(offeredParameters[offeredIndex], required))
                {
                    matched[offeredIndex] = true;
                    found = true;
                }
                offeredIndex = offeredIndex + 1;
            }
            if (!found)
            {
                return false;
            }
            requiredIndex = requiredIndex + 1;
        }
        return true;
    }

    internal static List<SdpFormatParameter> CollectCodecParameters(SdpCodec codec)
    {
        List<SdpFormatParameter> parameters = new List<SdpFormatParameter>();
        int attributeIndex = 0;
        while (attributeIndex < codec.FormatParameters.Count)
        {
            SdpFormatParametersAttribute attribute = codec.FormatParameters[attributeIndex];
            int parameterIndex = 0;
            while (parameterIndex < attribute.Parameters.Count)
            {
                parameters.Add(attribute.Parameters[parameterIndex]);
                parameterIndex = parameterIndex + 1;
            }
            attributeIndex = attributeIndex + 1;
        }
        return parameters;
    }

    internal static bool HasUnparsedFormatParameters(SdpCodec codec)
    {
        int index = 0;
        while (index < codec.FormatParameters.Count)
        {
            SdpFormatParametersAttribute attribute = codec.FormatParameters[index];
            if (attribute.RawParameters.Length > 0 && attribute.Parameters.Count == 0)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool ParametersMatch(SdpFormatParameter offered, SdpFormatParameter required)
    {
        if (offered == null || required == null)
        {
            return false;
        }
        if (!string.Equals(offered.Name, required.Name, StringComparison.OrdinalIgnoreCase) || offered.HasValue != required.HasValue)
        {
            return false;
        }
        return !offered.HasValue || offered.Value == required.Value;
    }

    internal static List<SdpCodec> OrderCodecsByPreference(List<SdpCodec> codecs, List<SdpCodecCapability> capabilities, SdpCodecMatchMode matchMode)
    {
        List<SdpCodec> ordered = new List<SdpCodec>();
        List<int> preferences = new List<int>();
        List<SdpCodec> unmatched = new List<SdpCodec>();
        int codecIndex = 0;
        while (codecIndex < codecs.Count)
        {
            SdpCodec codec = codecs[codecIndex];
            bool matched = false;
            int preference = 0;
            int capabilityIndex = 0;
            while (capabilityIndex < capabilities.Count)
            {
                SdpCodecCapability capability = capabilities[capabilityIndex];
                if (IsCodecMatch(codec, capability, matchMode) && (!matched || capability.Preference < preference))
                {
                    matched = true;
                    preference = capability.Preference;
                }
                capabilityIndex = capabilityIndex + 1;
            }
            if (matched)
            {
                int insertionIndex = 0;
                while (insertionIndex < preferences.Count && preferences[insertionIndex] <= preference)
                {
                    insertionIndex = insertionIndex + 1;
                }
                ordered.Insert(insertionIndex, codec);
                preferences.Insert(insertionIndex, preference);
            }
            else
            {
                unmatched.Add(codec);
            }
            codecIndex = codecIndex + 1;
        }

        codecIndex = 0;
        while (codecIndex < unmatched.Count)
        {
            ordered.Add(unmatched[codecIndex]);
            codecIndex = codecIndex + 1;
        }
        return ordered;
    }

    internal static List<SdpCodec> IntersectCodecs(List<SdpCodec> remoteCodecs, List<SdpCodecCapability> localCapabilities, SdpCodecMatchMode matchMode)
    {
        List<SdpCodec> intersection = new List<SdpCodec>();
        int codecIndex = 0;
        while (codecIndex < remoteCodecs.Count)
        {
            SdpCodec codec = remoteCodecs[codecIndex];
            bool matched = false;
            int capabilityIndex = 0;
            while (capabilityIndex < localCapabilities.Count && !matched)
            {
                matched = IsCodecMatch(codec, localCapabilities[capabilityIndex], matchMode);
                capabilityIndex = capabilityIndex + 1;
            }
            if (matched)
            {
                intersection.Add(codec);
            }
            codecIndex = codecIndex + 1;
        }
        return intersection;
    }

    internal static List<SdpPayloadMapping> AllocatePayloadTypes(List<SdpCodec> codecs, int mediaIndex, bool preserveOfferedPayloadTypes)
    {
        List<int> offeredPayloadTypes = new List<int>();
        int codecIndex = 0;
        while (codecIndex < codecs.Count)
        {
            int payloadType = codecs[codecIndex].PayloadType;
            if (payloadType < 0 || payloadType > 127)
            {
                throw new ArgumentOutOfRangeException("codecs");
            }
            if (offeredPayloadTypes.Contains(payloadType))
            {
                throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationPayloadAllocationFailed, null);
            }
            offeredPayloadTypes.Add(payloadType);
            codecIndex = codecIndex + 1;
        }
        int dynamicCapacity = SdpStaticPayloadTypes.DynamicMaximum - SdpStaticPayloadTypes.DynamicMinimum + 1;
        if (!preserveOfferedPayloadTypes && codecs.Count > dynamicCapacity)
        {
            throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationPayloadAllocationFailed, null);
        }

        List<SdpPayloadMapping> mappings = new List<SdpPayloadMapping>();
        codecIndex = 0;
        while (codecIndex < codecs.Count)
        {
            SdpCodec codec = codecs[codecIndex];
            SdpPayloadMapping mapping = new SdpPayloadMapping();
            mapping.MediaIndex = mediaIndex;
            mapping.OfferedPayloadType = codec.PayloadType;
            mapping.NegotiatedPayloadType = preserveOfferedPayloadTypes ? codec.PayloadType : SdpStaticPayloadTypes.DynamicMinimum + codecIndex;
            mapping.EncodingName = codec.EncodingName;
            mappings.Add(mapping);
            codecIndex = codecIndex + 1;
        }
        return mappings;
    }

    internal static void ValidatePayloadRemap(SdpEffectiveMedia media, List<SdpPayloadMapping> mappings)
    {
        List<int> offeredPayloadTypes = new List<int>();
        List<int> negotiatedPayloadTypes = new List<int>();
        int mappingIndex = 0;
        while (mappingIndex < mappings.Count)
        {
            SdpPayloadMapping mapping = mappings[mappingIndex];
            if (mapping == null)
            {
                throw new ArgumentException("A payload mapping collection must not contain null entries.", "mappings");
            }
            if (mapping.OfferedPayloadType < 0 || mapping.OfferedPayloadType > 127 || mapping.NegotiatedPayloadType < 0 || mapping.NegotiatedPayloadType > 127)
            {
                throw new ArgumentException("Every payload mapping must use payload types from zero through 127.", "mappings");
            }
            if (offeredPayloadTypes.Contains(mapping.OfferedPayloadType) || negotiatedPayloadTypes.Contains(mapping.NegotiatedPayloadType))
            {
                throw new ArgumentException("Payload mappings must have unique offered and negotiated assignments.", "mappings");
            }
            if (media.MediaIndex >= 0 && mapping.MediaIndex >= 0 && mapping.MediaIndex != media.MediaIndex)
            {
                throw new ArgumentException("Every payload mapping must belong to the effective media graph.", "mappings");
            }
            offeredPayloadTypes.Add(mapping.OfferedPayloadType);
            negotiatedPayloadTypes.Add(mapping.NegotiatedPayloadType);
            mappingIndex = mappingIndex + 1;
        }

        offeredPayloadTypes.Clear();
        int codecIndex = 0;
        while (codecIndex < media.Codecs.Count)
        {
            SdpCodec codec = media.Codecs[codecIndex];
            if (codec == null)
            {
                throw new ArgumentException("An effective media codec collection must not contain null entries.", "media");
            }
            if (codec.PayloadType < 0 || codec.PayloadType > 127 || offeredPayloadTypes.Contains(codec.PayloadType))
            {
                throw new ArgumentException("Effective media codecs must have unique payload types from zero through 127.", "media");
            }
            offeredPayloadTypes.Add(codec.PayloadType);
            RequirePayloadMapping(codec.PayloadType, mappings);
            ValidateCodecReferences(codec, mappings);
            codecIndex = codecIndex + 1;
        }

        int ridIndex = 0;
        while (ridIndex < media.Rids.Count)
        {
            SdpRidAttribute rid = media.Rids[ridIndex];
            if (rid == null)
            {
                throw new ArgumentException("An effective media RID collection must not contain null entries.", "media");
            }
            int payloadIndex = 0;
            while (payloadIndex < rid.PayloadTypes.Count)
            {
                RequirePayloadMapping(rid.PayloadTypes[payloadIndex], mappings);
                payloadIndex = payloadIndex + 1;
            }
            ridIndex = ridIndex + 1;
        }
        ValidateSimulcastReferences(media);
    }

    internal static void ValidateCodecReferences(SdpCodec codec, List<SdpPayloadMapping> mappings)
    {
        int formatIndex = 0;
        while (formatIndex < codec.FormatParameters.Count)
        {
            SdpFormatParametersAttribute format = codec.FormatParameters[formatIndex];
            if (format == null)
            {
                throw new ArgumentException("A codec format-parameter collection must not contain null entries.", "media");
            }
            int parameterIndex = 0;
            while (parameterIndex < format.Parameters.Count)
            {
                if (format.Parameters[parameterIndex] == null)
                {
                    throw new ArgumentException("A format-parameter collection must not contain null entries.", "media");
                }
                parameterIndex = parameterIndex + 1;
            }
            RequirePayloadMapping(format.PayloadType, mappings);
            if (string.Equals(codec.EncodingName, SdpCodecNames.Retransmission, StringComparison.OrdinalIgnoreCase))
            {
                ValidateNamedPayloadParameter(format, "apt", mappings);
            }
            if (IsPayloadListCodec(codec.EncodingName))
            {
                ValidatePayloadListReferences(format.RawParameters, mappings);
            }
            formatIndex = formatIndex + 1;
        }
        int feedbackIndex = 0;
        while (feedbackIndex < codec.RtcpFeedback.Count)
        {
            SdpRtcpFeedbackAttribute feedback = codec.RtcpFeedback[feedbackIndex];
            if (feedback == null)
            {
                throw new ArgumentException("A codec feedback collection must not contain null entries.", "media");
            }
            if (feedback.PayloadType != "*")
            {
                SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(feedback.PayloadType.AsSpan());
                if (payload.Success)
                {
                    RequirePayloadMapping(payload.Int32Value, mappings);
                }
            }
            feedbackIndex = feedbackIndex + 1;
        }
    }

    internal static void ValidateNamedPayloadParameter(SdpFormatParametersAttribute format, string name, List<SdpPayloadMapping> mappings)
    {
        int parameterIndex = 0;
        while (parameterIndex < format.Parameters.Count)
        {
            SdpFormatParameter parameter = format.Parameters[parameterIndex];
            if (parameter == null)
            {
                throw new ArgumentException("A format-parameter collection must not contain null entries.", "media");
            }
            if (parameter.HasValue && string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(parameter.Value.AsSpan());
                if (payload.Success)
                {
                    RequirePayloadMapping(payload.Int32Value, mappings);
                }
            }
            parameterIndex = parameterIndex + 1;
        }
    }

    internal static void ValidatePayloadListReferences(string value, List<SdpPayloadMapping> mappings)
    {
        List<int> payloadTypes = new List<int>();
        int start = 0;
        int index = 0;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == '/')
            {
                SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(value.AsSpan(start, index - start));
                if (!payload.Success)
                {
                    return;
                }
                payloadTypes.Add(payload.Int32Value);
                start = index + 1;
            }
            index = index + 1;
        }
        index = 0;
        while (index < payloadTypes.Count)
        {
            RequirePayloadMapping(payloadTypes[index], mappings);
            index = index + 1;
        }
    }

    internal static void ValidateSimulcastReferences(SdpEffectiveMedia media)
    {
        if (media.Simulcast == null)
        {
            return;
        }
        ValidateSimulcastStreams(media.Simulcast.SendStreams, media.Rids);
        ValidateSimulcastStreams(media.Simulcast.ReceiveStreams, media.Rids);
    }

    internal static void ValidateSimulcastStreams(List<SdpSimulcastStream> streams, List<SdpRidAttribute> rids)
    {
        int streamIndex = 0;
        while (streamIndex < streams.Count)
        {
            SdpSimulcastStream stream = streams[streamIndex];
            if (stream == null)
            {
                throw new ArgumentException("A simulcast stream collection must not contain null entries.", "media");
            }
            int alternativeIndex = 0;
            while (alternativeIndex < stream.Alternatives.Count)
            {
                SdpSimulcastAlternative alternative = stream.Alternatives[alternativeIndex];
                if (alternative == null || !ContainsRidIdentifier(rids, alternative.RidId))
                {
                    throw new ArgumentException("Every simulcast alternative must reference an effective RID.", "media");
                }
                alternativeIndex = alternativeIndex + 1;
            }
            streamIndex = streamIndex + 1;
        }
    }

    internal static bool ContainsRidIdentifier(List<SdpRidAttribute> rids, string identifier)
    {
        int ridIndex = 0;
        while (ridIndex < rids.Count)
        {
            SdpRidAttribute rid = rids[ridIndex];
            if (rid != null && rid.Id == identifier)
            {
                return true;
            }
            ridIndex = ridIndex + 1;
        }
        return false;
    }

    internal static void RequirePayloadMapping(int payloadType, List<SdpPayloadMapping> mappings)
    {
        if (FindPayloadMapping(payloadType, mappings) == null)
        {
            throw new ArgumentException("Every codec and associated payload reference must have a mapping.", "mappings");
        }
    }

    internal static SdpPayloadMapping? FindPayloadMapping(int payloadType, List<SdpPayloadMapping> mappings)
    {
        int mappingIndex = 0;
        while (mappingIndex < mappings.Count)
        {
            if (mappings[mappingIndex].OfferedPayloadType == payloadType)
            {
                return mappings[mappingIndex];
            }
            mappingIndex = mappingIndex + 1;
        }
        return null;
    }

    internal static void RemapPayloadTypes(SdpEffectiveMedia media, List<SdpPayloadMapping> mappings)
    {
        int codecIndex = 0;
        while (codecIndex < media.Codecs.Count)
        {
            RemapCodecGraph(media.Codecs[codecIndex], mappings);
            codecIndex = codecIndex + 1;
        }
        int ridIndex = 0;
        while (ridIndex < media.Rids.Count)
        {
            SdpRidAttribute rid = media.Rids[ridIndex];
            bool ridChanged = false;
            int payloadIndex = 0;
            while (payloadIndex < rid.PayloadTypes.Count)
            {
                SdpPayloadMapping? mapping = FindPayloadMapping(rid.PayloadTypes[payloadIndex], mappings);
                if (mapping != null && rid.PayloadTypes[payloadIndex] != mapping.NegotiatedPayloadType)
                {
                    rid.PayloadTypes[payloadIndex] = mapping.NegotiatedPayloadType;
                    ridChanged = true;
                }
                payloadIndex = payloadIndex + 1;
            }
            if (ridChanged)
            {
                SynchronizeRidSource(rid);
            }
            ridIndex = ridIndex + 1;
        }
    }

    internal static void RemapCodecGraph(SdpCodec codec, List<SdpPayloadMapping> mappings)
    {
        SdpPayloadMapping? codecMapping = FindPayloadMapping(codec.PayloadType, mappings);
        int formatIndex = 0;
        while (formatIndex < codec.FormatParameters.Count)
        {
            SdpFormatParametersAttribute format = codec.FormatParameters[formatIndex];
            SdpPayloadMapping? formatMapping = FindPayloadMapping(format.PayloadType, mappings);
            bool formatChanged = false;
            if (formatMapping != null)
            {
                formatChanged = format.PayloadType != formatMapping.NegotiatedPayloadType;
                format.PayloadType = formatMapping.NegotiatedPayloadType;
            }
            bool associationChanged = false;
            if (string.Equals(codec.EncodingName, SdpCodecNames.Retransmission, StringComparison.OrdinalIgnoreCase))
            {
                associationChanged = RemapNamedPayloadParameter(format, "apt", mappings);
            }
            if (IsPayloadListCodec(codec.EncodingName))
            {
                associationChanged = RemapPayloadList(format, mappings) || associationChanged;
            }
            if (associationChanged)
            {
                RebuildRawParameters(format);
            }
            if (formatChanged || associationChanged)
            {
                SynchronizeFormatSource(format);
            }
            formatIndex = formatIndex + 1;
        }
        int feedbackIndex = 0;
        while (feedbackIndex < codec.RtcpFeedback.Count)
        {
            SdpRtcpFeedbackAttribute feedback = codec.RtcpFeedback[feedbackIndex];
            string remappedPayloadType = RemapPayloadText(feedback.PayloadType, mappings);
            if (feedback.PayloadType != remappedPayloadType)
            {
                feedback.PayloadType = remappedPayloadType;
                SynchronizeFeedbackSource(feedback);
            }
            feedbackIndex = feedbackIndex + 1;
        }
        if (codec.RtpMap != null && codecMapping != null && codec.RtpMap.PayloadType != codecMapping.NegotiatedPayloadType)
        {
            codec.RtpMap.PayloadType = codecMapping.NegotiatedPayloadType;
            SynchronizeRtpMapSource(codec.RtpMap);
        }
        if (codecMapping != null)
        {
            codec.PayloadType = codecMapping.NegotiatedPayloadType;
        }
    }

    internal static bool RemapNamedPayloadParameter(SdpFormatParametersAttribute format, string name, List<SdpPayloadMapping> mappings)
    {
        bool changed = false;
        int parameterIndex = 0;
        while (parameterIndex < format.Parameters.Count)
        {
            SdpFormatParameter parameter = format.Parameters[parameterIndex];
            if (parameter != null && parameter.HasValue && string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                string remappedValue = RemapPayloadText(parameter.Value, mappings);
                if (parameter.Value != remappedValue)
                {
                    parameter.Value = remappedValue;
                    parameter.RawText = parameter.Name + "=" + parameter.Value;
                    changed = true;
                }
            }
            parameterIndex = parameterIndex + 1;
        }
        return changed;
    }

    internal static bool IsPayloadListCodec(string encodingName)
    {
        return string.Equals(encodingName, SdpCodecNames.RedundantAudioData, StringComparison.OrdinalIgnoreCase)
            || string.Equals(encodingName, SdpCodecNames.UnequalLossProtectionFec, StringComparison.OrdinalIgnoreCase)
            || string.Equals(encodingName, SdpCodecNames.FlexibleFec, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool RemapPayloadList(SdpFormatParametersAttribute format, List<SdpPayloadMapping> mappings)
    {
        bool changed = false;
        string remappedRawParameters = RemapSlashPayloadList(format.RawParameters, mappings);
        if (format.RawParameters != remappedRawParameters)
        {
            format.RawParameters = remappedRawParameters;
            changed = true;
        }
        int parameterIndex = 0;
        while (parameterIndex < format.Parameters.Count)
        {
            SdpFormatParameter parameter = format.Parameters[parameterIndex];
            if (parameter != null && !parameter.HasValue)
            {
                string remappedName = RemapSlashPayloadList(parameter.Name, mappings);
                if (parameter.Name != remappedName)
                {
                    parameter.Name = remappedName;
                    parameter.RawText = parameter.Name;
                    changed = true;
                }
            }
            parameterIndex = parameterIndex + 1;
        }
        return changed;
    }

    internal static string RemapSlashPayloadList(string value, List<SdpPayloadMapping> mappings)
    {
        string remapped = string.Empty;
        int start = 0;
        int index = 0;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == '/')
            {
                SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(value.AsSpan(start, index - start));
                if (!payload.Success)
                {
                    return value;
                }
                if (remapped.Length > 0)
                {
                    remapped = remapped + "/";
                }
                remapped = remapped + RemapPayloadNumber(payload.Int32Value, mappings).ToString(CultureInfo.InvariantCulture);
                start = index + 1;
            }
            index = index + 1;
        }
        return remapped;
    }

    internal static string RemapPayloadText(string value, List<SdpPayloadMapping> mappings)
    {
        if (value == "*")
        {
            return value;
        }
        SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(value.AsSpan());
        if (!payload.Success)
        {
            return value;
        }
        return RemapPayloadNumber(payload.Int32Value, mappings).ToString(CultureInfo.InvariantCulture);
    }

    internal static int RemapPayloadNumber(int payloadType, List<SdpPayloadMapping> mappings)
    {
        SdpPayloadMapping? mapping = FindPayloadMapping(payloadType, mappings);
        return mapping == null ? payloadType : mapping.NegotiatedPayloadType;
    }

    internal static void RebuildRawParameters(SdpFormatParametersAttribute format)
    {
        if (format.Parameters.Count == 0)
        {
            return;
        }
        string raw = string.Empty;
        int parameterIndex = 0;
        while (parameterIndex < format.Parameters.Count)
        {
            SdpFormatParameter parameter = format.Parameters[parameterIndex];
            if (parameterIndex > 0)
            {
                raw = raw + ";";
            }
            raw = raw + parameter.Name;
            if (parameter.HasValue)
            {
                raw = raw + "=" + parameter.Value;
            }
            parameter.RawText = parameter.HasValue ? parameter.Name + "=" + parameter.Value : parameter.Name;
            parameterIndex = parameterIndex + 1;
        }
        format.RawParameters = raw;
    }

    internal static void SynchronizeFormatSource(SdpFormatParametersAttribute format)
    {
        if (format.SourceAttribute == null)
        {
            return;
        }
        SdpAttributeEncodeResult encoded = SdpFormatParametersCodecCore.EncodeFormatParameters(format);
        if (encoded.Success && encoded.Attribute != null)
        {
            CopyEncodedAttribute(format.SourceAttribute, encoded.Attribute);
        }
    }

    internal static void SynchronizeFeedbackSource(SdpRtcpFeedbackAttribute feedback)
    {
        if (feedback.SourceAttribute == null)
        {
            return;
        }
        SdpAttributeEncodeResult encoded = SdpRtcpAttributeCodecCore.EncodeRtcpFeedback(feedback);
        if (encoded.Success && encoded.Attribute != null)
        {
            CopyEncodedAttribute(feedback.SourceAttribute, encoded.Attribute);
        }
    }

    internal static void SynchronizeRtpMapSource(SdpRtpMapAttribute mapping)
    {
        if (mapping.SourceAttribute == null)
        {
            return;
        }
        SdpAttributeEncodeResult encoded = SdpRtpMapCodecCore.EncodeRtpMap(mapping);
        if (encoded.Success && encoded.Attribute != null)
        {
            CopyEncodedAttribute(mapping.SourceAttribute, encoded.Attribute);
        }
    }

    internal static void SynchronizeRidSource(SdpRidAttribute rid)
    {
        if (rid.SourceAttribute == null)
        {
            return;
        }
        string value = rid.SourceAttribute.Value;
        int payloadStart = value.IndexOf("pt=", StringComparison.OrdinalIgnoreCase);
        if (payloadStart < 0)
        {
            return;
        }
        payloadStart = payloadStart + 3;
        int payloadEnd = payloadStart;
        while (payloadEnd < value.Length && value[payloadEnd] != ';' && value[payloadEnd] != ' ' && value[payloadEnd] != '\t')
        {
            payloadEnd = payloadEnd + 1;
        }
        string payloadList = string.Empty;
        int payloadIndex = 0;
        while (payloadIndex < rid.PayloadTypes.Count)
        {
            if (payloadIndex > 0)
            {
                payloadList = payloadList + ",";
            }
            payloadList = payloadList + rid.PayloadTypes[payloadIndex].ToString(CultureInfo.InvariantCulture);
            payloadIndex = payloadIndex + 1;
        }
        rid.SourceAttribute.Value = value.Substring(0, payloadStart) + payloadList + value.Substring(payloadEnd);
        rid.SourceAttribute.RawText = rid.SourceAttribute.Name + ":" + rid.SourceAttribute.Value;
    }

    internal static void CopyEncodedAttribute(SdpAttribute target, SdpAttribute source)
    {
        target.Kind = source.Kind;
        target.Name = source.Name;
        target.Value = source.Value;
        target.RawText = source.RawText;
    }

    internal static void PopulateStaticMapping(SdpCodec codec)
    {
        switch (codec.PayloadType)
        {
            case SdpStaticPayloadTypes.Pcmu:
                SetStaticMapping(codec, SdpCodecNames.Pcmu, 8000, 1);
                break;
            case SdpStaticPayloadTypes.Gsm:
                SetStaticMapping(codec, SdpCodecNames.Gsm, 8000, 1);
                break;
            case SdpStaticPayloadTypes.G723:
                SetStaticMapping(codec, SdpCodecNames.G723, 8000, 1);
                break;
            case SdpStaticPayloadTypes.Dvi4_8000:
                SetStaticMapping(codec, SdpCodecNames.Dvi4, 8000, 1);
                break;
            case SdpStaticPayloadTypes.Dvi4_16000:
                SetStaticMapping(codec, SdpCodecNames.Dvi4, 16000, 1);
                break;
            case SdpStaticPayloadTypes.Lpc:
                SetStaticMapping(codec, SdpCodecNames.Lpc, 8000, 1);
                break;
            case SdpStaticPayloadTypes.Pcma:
                SetStaticMapping(codec, SdpCodecNames.Pcma, 8000, 1);
                break;
            case SdpStaticPayloadTypes.G722:
                SetStaticMapping(codec, SdpCodecNames.G722, 8000, 1);
                break;
            case SdpStaticPayloadTypes.Linear16Stereo:
                SetStaticMapping(codec, SdpCodecNames.Linear16, 44100, 2);
                break;
            case SdpStaticPayloadTypes.Linear16Mono:
                SetStaticMapping(codec, SdpCodecNames.Linear16, 44100, 1);
                break;
            case SdpStaticPayloadTypes.Qcelp:
                SetStaticMapping(codec, SdpCodecNames.Qcelp, 8000, 1);
                break;
            case SdpStaticPayloadTypes.ComfortNoise:
                SetStaticMapping(codec, SdpCodecNames.ComfortNoise, 8000, 1);
                break;
            case SdpStaticPayloadTypes.MpegAudio:
                SetStaticMapping(codec, SdpCodecNames.MpegAudio, 90000, 1);
                break;
            case SdpStaticPayloadTypes.G728:
                SetStaticMapping(codec, SdpCodecNames.G728, 8000, 1);
                break;
            case SdpStaticPayloadTypes.Dvi4_11025:
                SetStaticMapping(codec, SdpCodecNames.Dvi4, 11025, 1);
                break;
            case SdpStaticPayloadTypes.Dvi4_22050:
                SetStaticMapping(codec, SdpCodecNames.Dvi4, 22050, 1);
                break;
            case SdpStaticPayloadTypes.G729:
                SetStaticMapping(codec, SdpCodecNames.G729, 8000, 1);
                break;
            case SdpStaticPayloadTypes.CellB:
                SetStaticMapping(codec, SdpCodecNames.CellB, 90000, 1);
                break;
            case SdpStaticPayloadTypes.Jpeg:
                SetStaticMapping(codec, SdpCodecNames.Jpeg, 90000, 1);
                break;
            case SdpStaticPayloadTypes.Nv:
                SetStaticMapping(codec, SdpCodecNames.Nv, 90000, 1);
                break;
            case SdpStaticPayloadTypes.H261:
                SetStaticMapping(codec, SdpCodecNames.H261, 90000, 1);
                break;
            case SdpStaticPayloadTypes.MpegVideo:
                SetStaticMapping(codec, SdpCodecNames.MpegVideo, 90000, 1);
                break;
            case SdpStaticPayloadTypes.Mpeg2TransportStream:
                SetStaticMapping(codec, SdpCodecNames.Mpeg2TransportStream, 90000, 1);
                break;
            case SdpStaticPayloadTypes.H263:
                SetStaticMapping(codec, SdpCodecNames.H263, 90000, 1);
                break;
            default:
                break;
        }
    }

    internal static void SetStaticMapping(SdpCodec codec, string encodingName, int clockRate, int channelCount)
    {
        codec.EncodingName = encodingName;
        codec.ClockRate = clockRate;
        codec.ChannelCount = channelCount;
    }
}
