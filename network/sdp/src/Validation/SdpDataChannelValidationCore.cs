using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpDataChannelValidationCore
{
    internal static void ValidateDataChannelDescription(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        ValidateSessionAttributes(description, result, limiter);
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            ValidateMedia(description, description.MediaDescriptions[mediaIndex], mediaIndex, result, limiter);
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void ValidateSessionAttributes(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < description.Attributes.Count)
        {
            SdpAttribute attribute = description.Attributes[index];
            if (IsDataChannelAttributeName(attribute.Name))
            {
                int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(description.Attributes, index);
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpValidationSeverity.Error, SdpScope.Session, attribute.Name, -1, occurrenceIndex, "SCTP and data-channel attributes are media-level attributes.", attribute.Location);
            }
            index = index + 1;
        }
    }

    internal static void ValidateMedia(SdpSessionDescription description, SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool modernProtocol = IsModernSctpProtocol(media.Protocol);
        bool legacyProtocol = IsLegacySctpProtocol(media.Protocol);
        bool dataChannelFormat = ContainsFormat(media.Formats, SdpDataChannelTokens.WebRtcDataChannel);
        int validPortCount = 0;
        List<SdpDataChannelAttribute> channels = new List<SdpDataChannelAttribute>();
        List<SdpAttribute> channelSources = new List<SdpAttribute>();
        List<int> channelOccurrences = new List<int>();
        List<SdpDataChannelSubprotocolAttribute> subprotocolAttributes = new List<SdpDataChannelSubprotocolAttribute>();
        List<SdpAttribute> subprotocolSources = new List<SdpAttribute>();
        List<int> subprotocolOccurrences = new List<int>();

        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (IsDataChannelAttributeName(attribute.Name))
            {
                int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, index);
                ValidateAttributeApplicability(attribute, modernProtocol, legacyProtocol, dataChannelFormat, mediaIndex, occurrenceIndex, result, limiter);
                if (IsSingletonAttributeName(attribute.Name) && occurrenceIndex > 0)
                {
                    AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, attribute, mediaIndex, occurrenceIndex, "This SCTP attribute may appear at most once in a media description.");
                }

                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success)
                {
                    SdpRtpValidationCore.AddDecodeIssue(decoded, attribute, mediaIndex, occurrenceIndex, result, limiter);
                }
                else if (decoded.SctpPortAttribute != null)
                {
                    validPortCount = validPortCount + 1;
                }
                else if (decoded.DataChannelAttribute != null)
                {
                    ValidateChannel(decoded.DataChannelAttribute, attribute, mediaIndex, occurrenceIndex, channels, result, limiter);
                    channels.Add(decoded.DataChannelAttribute);
                    channelSources.Add(attribute);
                    channelOccurrences.Add(occurrenceIndex);
                }
                else if (decoded.DataChannelSubprotocolAttribute != null)
                {
                    ValidateChannelId(decoded.DataChannelSubprotocolAttribute.ChannelId, attribute, mediaIndex, occurrenceIndex, result, limiter);
                    subprotocolAttributes.Add(decoded.DataChannelSubprotocolAttribute);
                    subprotocolSources.Add(attribute);
                    subprotocolOccurrences.Add(occurrenceIndex);
                }
            }
            index = index + 1;
        }

        if (modernProtocol)
        {
            if (!string.Equals(media.MediaType, SdpMediaTypes.Application, StringComparison.OrdinalIgnoreCase))
            {
                AddMediaIssue(result, limiter, media, mediaIndex, "An SCTP-over-DTLS transport requires an application media description.");
            }
            if (media.Port != 0 && validPortCount == 0)
            {
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationDataChannel, SdpValidationSeverity.Error, SdpScope.Media, SdpAttributeNames.SctpPort, mediaIndex, -1, "An SCTP-over-DTLS media description requires one valid sctp-port attribute.", media.Location);
            }
        }

        ValidateSubprotocolReferences(channels, subprotocolAttributes, subprotocolSources, subprotocolOccurrences, mediaIndex, result, limiter);
        if (modernProtocol && dataChannelFormat)
        {
            ValidateChannelParity(description, media, mediaIndex, channels, channelSources, channelOccurrences, result, limiter);
        }
        ValidateAnswer(description, media, mediaIndex, channels, channelSources, channelOccurrences, result, limiter);
    }

    internal static void ValidateAttributeApplicability(SdpAttribute attribute, bool modernProtocol, bool legacyProtocol, bool dataChannelFormat, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool applicable = false;
        if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.SctpPort) || SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.MaximumMessageSize))
        {
            applicable = modernProtocol;
        }
        else if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.SctpMap))
        {
            applicable = legacyProtocol;
        }
        else if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.DataChannelMap) || SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.DataChannelSubprotocol))
        {
            applicable = modernProtocol && dataChannelFormat;
        }
        if (!applicable)
        {
            AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationDataChannel, attribute, mediaIndex, occurrenceIndex, "The SCTP or data-channel attribute is not applicable to this media transport and format.");
        }
    }

    internal static void ValidateChannel(SdpDataChannelAttribute channel, SdpAttribute source, int mediaIndex, int occurrenceIndex, List<SdpDataChannelAttribute> earlierChannels, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        ValidateChannelId(channel.ChannelId, source, mediaIndex, occurrenceIndex, result, limiter);
        int index = 0;
        while (index < earlierChannels.Count)
        {
            if (earlierChannels[index].ChannelId == channel.ChannelId)
            {
                AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, source, mediaIndex, occurrenceIndex, "A dcmap stream identifier must be unique within its media description.");
                break;
            }
            index = index + 1;
        }
        ValidateChannelParameters(channel, source, mediaIndex, occurrenceIndex, result, limiter);
    }

    internal static void ValidateChannelId(int channelId, SdpAttribute source, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (channelId < 0 || channelId > 65534)
        {
            AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationDataChannel, source, mediaIndex, occurrenceIndex, "A data-channel stream identifier must be from zero through 65534.");
        }
    }

    internal static void ValidateChannelParameters(SdpDataChannelAttribute channel, SdpAttribute source, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int maximumRetransmissionsCount = 0;
        int maximumTimeCount = 0;
        int index = 0;
        while (index < channel.Parameters.Count)
        {
            SdpDataChannelParameter parameter = channel.Parameters[index];
            int duplicateCount = CountEarlierKnownParameters(channel.Parameters, index);
            if (duplicateCount > 0)
            {
                AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, source, mediaIndex, occurrenceIndex, "A known dcmap parameter must not be repeated within one mapping.");
            }
            if (SdpRtpValidationCore.NameEquals(parameter.Name, SdpDataChannelTokens.Ordered))
            {
                if (!string.Equals(parameter.Value, "true", StringComparison.OrdinalIgnoreCase) && !string.Equals(parameter.Value, "false", StringComparison.OrdinalIgnoreCase))
                {
                    AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationDataChannel, source, mediaIndex, occurrenceIndex, "The dcmap ordered parameter must be true or false.");
                }
            }
            else if (SdpRtpValidationCore.NameEquals(parameter.Name, SdpDataChannelTokens.MaximumRetransmissions))
            {
                maximumRetransmissionsCount = maximumRetransmissionsCount + 1;
                ValidateUnsignedParameter(parameter, 4294967295UL, "The dcmap max-retr parameter must be a canonical unsigned 32-bit integer.", source, mediaIndex, occurrenceIndex, result, limiter);
            }
            else if (SdpRtpValidationCore.NameEquals(parameter.Name, SdpDataChannelTokens.MaximumRetransmissionTime))
            {
                maximumTimeCount = maximumTimeCount + 1;
                ValidateUnsignedParameter(parameter, 4294967295UL, "The dcmap max-time parameter must be a canonical unsigned 32-bit integer.", source, mediaIndex, occurrenceIndex, result, limiter);
            }
            else if (SdpRtpValidationCore.NameEquals(parameter.Name, SdpDataChannelTokens.Priority))
            {
                ValidateUnsignedParameter(parameter, 65535UL, "The dcmap priority parameter must be a canonical unsigned 16-bit integer.", source, mediaIndex, occurrenceIndex, result, limiter);
            }
            index = index + 1;
        }
        if (maximumRetransmissionsCount > 0 && maximumTimeCount > 0)
        {
            AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationDataChannel, source, mediaIndex, occurrenceIndex, "The dcmap max-retr and max-time parameters are mutually exclusive.");
        }
    }

    internal static int CountEarlierKnownParameters(List<SdpDataChannelParameter> parameters, int targetIndex)
    {
        string name = parameters[targetIndex].Name;
        if (!IsKnownParameterName(name))
        {
            return 0;
        }
        int count = 0;
        int index = 0;
        while (index < targetIndex)
        {
            if (SdpRtpValidationCore.NameEquals(parameters[index].Name, name))
            {
                count = count + 1;
            }
            index = index + 1;
        }
        return count;
    }

    internal static void ValidateUnsignedParameter(SdpDataChannelParameter parameter, ulong maximum, string detail, SdpAttribute source, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!parameter.HasValue || !SdpCommonAttributeCodecCore.IsZeroBasedIntegerValue(parameter.Value))
        {
            AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationDataChannel, source, mediaIndex, occurrenceIndex, detail);
            return;
        }
        SdpNumberParseResult parsed = SdpNumberParser.ParseCharacterUnsignedDecimal(parameter.Value.AsSpan());
        if (!parsed.Success || parsed.UnsignedValue > maximum)
        {
            AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationDataChannel, source, mediaIndex, occurrenceIndex, detail);
        }
    }

    internal static void ValidateSubprotocolReferences(List<SdpDataChannelAttribute> channels, List<SdpDataChannelSubprotocolAttribute> subprotocolAttributes, List<SdpAttribute> sources, List<int> occurrences, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < subprotocolAttributes.Count)
        {
            SdpDataChannelSubprotocolAttribute projection = subprotocolAttributes[index];
            if (FindChannel(channels, projection.ChannelId) == null)
            {
                AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationDataChannel, sources[index], mediaIndex, occurrences[index], "A dcsa attribute must reference a dcmap stream identifier in the same media description.");
            }
            index = index + 1;
        }
    }

    internal static void ValidateChannelParity(SdpSessionDescription description, SdpMediaDescription media, int mediaIndex, List<SdpDataChannelAttribute> channels, List<SdpAttribute> sources, List<int> occurrences, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpDtlsSetupRole localRole = ReadLocalDtlsRole(description, media);
        bool expectEven = false;
        bool knownExpectation = false;
        if (result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Offerer)
        {
            expectEven = localRole == SdpDtlsSetupRole.Active;
            knownExpectation = localRole == SdpDtlsSetupRole.Active || localRole == SdpDtlsSetupRole.Passive;
        }
        else if (result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Answerer)
        {
            expectEven = localRole == SdpDtlsSetupRole.Passive;
            knownExpectation = localRole == SdpDtlsSetupRole.Active || localRole == SdpDtlsSetupRole.Passive;
        }
        if (!knownExpectation)
        {
            return;
        }
        int index = 0;
        while (index < channels.Count)
        {
            bool even = channels[index].ChannelId % 2 == 0;
            if (even != expectEven)
            {
                AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationDataChannel, sources[index], mediaIndex, occurrences[index], expectEven ? "The endpoint that owns this dcmap stream identifier is the DTLS client and must use an even identifier." : "The endpoint that owns this dcmap stream identifier is the DTLS server and must use an odd identifier.");
            }
            index = index + 1;
        }
    }

    internal static SdpDtlsSetupRole ReadLocalDtlsRole(SdpSessionDescription description, SdpMediaDescription media)
    {
        SdpSetupAttribute? setup = SdpDtlsValidationCore.ReadEffectiveSetup(description.Attributes, media.Attributes);
        return setup == null ? SdpDtlsSetupRole.Unknown : setup.Role;
    }

    internal static void ValidateAnswer(SdpSessionDescription description, SdpMediaDescription media, int mediaIndex, List<SdpDataChannelAttribute> channels, List<SdpAttribute> sources, List<int> occurrences, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (result.AppliedOptions.NegotiationRole != SdpNegotiationRole.Answerer || result.AppliedOptions.OfferDescription == null)
        {
            return;
        }
        SdpSessionDescription offer = result.AppliedOptions.OfferDescription;
        if (mediaIndex >= offer.MediaDescriptions.Count)
        {
            AddMediaIssue(result, limiter, media, mediaIndex, "An SCTP answer media description has no corresponding offered media description.");
            return;
        }
        SdpMediaDescription offeredMedia = offer.MediaDescriptions[mediaIndex];
        if ((IsModernSctpProtocol(media.Protocol) || IsModernSctpProtocol(offeredMedia.Protocol)) && !string.Equals(media.Protocol, offeredMedia.Protocol, StringComparison.OrdinalIgnoreCase))
        {
            AddMediaIssue(result, limiter, media, mediaIndex, "An SCTP answer must retain the transport protocol from the corresponding offer.");
        }

        SdpSctpDescription offeredSctp = SdpDataChannelServiceCore.ResolveMediaSctp(offeredMedia);
        SdpSctpDescription answeredSctp = SdpDataChannelServiceCore.ResolveMediaSctp(media);
        if (offeredSctp.PortAttribute != null && offeredSctp.Port == 0 && answeredSctp.PortAttribute != null && answeredSctp.Port != 0)
        {
            AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationDataChannel, answeredSctp.PortAttribute.SourceAttribute, mediaIndex, 0, "An answer to a zero SCTP port offer must also use a zero SCTP port.");
        }
        if (media.Port == 0 && answeredSctp.PortAttribute != null && answeredSctp.Port != 0)
        {
            AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationDataChannel, answeredSctp.PortAttribute.SourceAttribute, mediaIndex, 0, "A rejected SCTP answer must use a zero SCTP port.");
        }

        List<SdpDataChannelAttribute> offeredChannels = SdpDataChannelServiceCore.ResolveDataChannels(offeredMedia);
        int index = 0;
        while (index < channels.Count)
        {
            SdpDataChannelAttribute? offered = FindChannel(offeredChannels, channels[index].ChannelId);
            if (offered == null)
            {
                AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationDataChannel, sources[index], mediaIndex, occurrences[index], "An SDP answer must not introduce a dcmap stream identifier that was absent from the offer.");
            }
            else if (!ReliabilityParametersMatch(offered, channels[index]))
            {
                AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationDataChannel, sources[index], mediaIndex, occurrences[index], "An SDP answer must retain the offered max-retr and max-time values for a data channel.");
            }
            index = index + 1;
        }
    }

    internal static bool ReliabilityParametersMatch(SdpDataChannelAttribute offered, SdpDataChannelAttribute answered)
    {
        SdpDataChannelParameter? offeredRetransmissions = FindParameter(offered.Parameters, SdpDataChannelTokens.MaximumRetransmissions);
        SdpDataChannelParameter? answeredRetransmissions = FindParameter(answered.Parameters, SdpDataChannelTokens.MaximumRetransmissions);
        SdpDataChannelParameter? offeredTime = FindParameter(offered.Parameters, SdpDataChannelTokens.MaximumRetransmissionTime);
        SdpDataChannelParameter? answeredTime = FindParameter(answered.Parameters, SdpDataChannelTokens.MaximumRetransmissionTime);
        return ParameterValuesMatch(offeredRetransmissions, answeredRetransmissions) && ParameterValuesMatch(offeredTime, answeredTime);
    }

    internal static bool ParameterValuesMatch(SdpDataChannelParameter? first, SdpDataChannelParameter? second)
    {
        if (first == null || second == null)
        {
            return first == null && second == null;
        }
        return first.HasValue == second.HasValue && first.Value == second.Value;
    }

    internal static SdpDataChannelParameter? FindParameter(List<SdpDataChannelParameter> parameters, string name)
    {
        int index = 0;
        while (index < parameters.Count)
        {
            if (SdpRtpValidationCore.NameEquals(parameters[index].Name, name))
            {
                return parameters[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static SdpDataChannelAttribute? FindChannel(List<SdpDataChannelAttribute> channels, int channelId)
    {
        int index = 0;
        while (index < channels.Count)
        {
            if (channels[index].ChannelId == channelId)
            {
                return channels[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static bool IsKnownParameterName(string name)
    {
        return SdpRtpValidationCore.NameEquals(name, SdpDataChannelTokens.Subprotocol) || SdpRtpValidationCore.NameEquals(name, SdpDataChannelTokens.Label) || SdpRtpValidationCore.NameEquals(name, SdpDataChannelTokens.MaximumRetransmissions) || SdpRtpValidationCore.NameEquals(name, SdpDataChannelTokens.MaximumRetransmissionTime) || SdpRtpValidationCore.NameEquals(name, SdpDataChannelTokens.Ordered) || SdpRtpValidationCore.NameEquals(name, SdpDataChannelTokens.Priority);
    }

    internal static bool IsDataChannelAttributeName(string name)
    {
        return SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.SctpPort) || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.MaximumMessageSize) || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.SctpMap) || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.DataChannelMap) || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.DataChannelSubprotocol);
    }

    internal static bool IsSingletonAttributeName(string name)
    {
        return SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.SctpPort) || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.MaximumMessageSize) || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.SctpMap);
    }

    internal static bool IsModernSctpProtocol(string protocol)
    {
        return string.Equals(protocol, SdpTransportProtocols.UdpDtlsSctp, StringComparison.OrdinalIgnoreCase) || string.Equals(protocol, SdpTransportProtocols.TcpDtlsSctp, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsLegacySctpProtocol(string protocol)
    {
        return string.Equals(protocol, "DTLS/SCTP", StringComparison.OrdinalIgnoreCase) || string.Equals(protocol, "SCTP/DTLS", StringComparison.OrdinalIgnoreCase) || string.Equals(protocol, "SCTP", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool ContainsFormat(List<string> formats, string expected)
    {
        int index = 0;
        while (index < formats.Count)
        {
            if (string.Equals(formats[index], expected, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static void AddAttributeIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, string code, SdpAttribute? attribute, int mediaIndex, int occurrenceIndex, string detail)
    {
        if (attribute == null)
        {
            return;
        }
        SdpValidationCore.AddValidationIssue(result, limiter, code, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, detail, attribute.Location);
    }

    internal static void AddMediaIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, SdpMediaDescription media, int mediaIndex, string detail)
    {
        SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationDataChannel, SdpValidationSeverity.Error, SdpScope.Media, 'm', mediaIndex, 0, detail, media.Location);
    }
}
