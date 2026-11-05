using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpIceValidationCore
{
    internal static void ValidateIceDescription(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        ValidateIceScopeAttributes(description.Attributes, SdpScope.Session, -1, result, limiter);
        ValidateIceSingletons(description.Attributes, SdpScope.Session, -1, result, limiter);
        ValidateIceAttributeValues(description.Attributes, SdpScope.Session, -1, result, limiter);

        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            ValidateIceScopeAttributes(media.Attributes, SdpScope.Media, mediaIndex, result, limiter);
            ValidateIceSingletons(media.Attributes, SdpScope.Media, mediaIndex, result, limiter);
            ValidateIceAttributeValues(media.Attributes, SdpScope.Media, mediaIndex, result, limiter);
            ValidateEffectiveIceCredentials(description, media, mediaIndex, result, limiter);
            ValidateMediaIceConsistency(description, media, mediaIndex, result, limiter);
            mediaIndex = mediaIndex + 1;
        }
        ValidateTrickleOptionCoverage(description, result, limiter);
        ValidateReusedIceCredentials(description, result, limiter);
    }

    internal static void ValidateMediaIceConsistency(SdpSessionDescription description, SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!MediaUsesIce(description, media))
        {
            return;
        }

        bool sessionTrickle = ContainsIceOption(description.Attributes, SdpIceTokens.Trickle);
        bool mediaTrickle = ContainsIceOption(media.Attributes, SdpIceTokens.Trickle);
        bool trickle = sessionTrickle || mediaTrickle;
        bool endOfCandidates = ContainsIceAttribute(media.Attributes, SdpAttributeNames.EndOfCandidates);
        bool mismatch = ContainsIceAttribute(media.Attributes, SdpAttributeNames.IceMismatch);
        bool lite = ContainsIceAttribute(description.Attributes, SdpAttributeNames.IceLite);
        bool rtp = IsRtpMedia(media);
        bool rtcpMux = ContainsIceAttribute(media.Attributes, SdpAttributeNames.RtcpMux);
        bool rtcpMuxOnly = ContainsIceAttribute(media.Attributes, SdpAttributeNames.RtcpMuxOnly);
        List<SdpIceCandidate> candidates = ReadValidCandidates(media.Attributes);

        ValidateCandidateOrdering(media, mediaIndex, result, limiter);
        if (endOfCandidates && !trickle)
        {
            AddMediaIceIssue(result, limiter, mediaIndex, SdpAttributeNames.EndOfCandidates, "The end-of-candidates marker requires the effective trickle ICE option.", media.Location);
        }
        if (rtcpMuxOnly && !rtcpMux)
        {
            AddMediaIceIssue(result, limiter, mediaIndex, SdpAttributeNames.RtcpMuxOnly, "The RTCP-mux-only attribute requires the RTCP-mux attribute.", media.Location);
        }
        if (mismatch && candidates.Count > 0)
        {
            AddMediaIceIssue(result, limiter, mediaIndex, SdpAttributeNames.IceMismatch, "An ICE-mismatch media description must omit candidate attributes.", media.Location);
        }

        ValidateLiteCandidates(candidates, mediaIndex, lite, result, limiter);
        ValidateRtpCandidateComponents(candidates, mediaIndex, rtp, rtcpMuxOnly, result, limiter);

        if (mismatch || media.Port == 0)
        {
            return;
        }

        SdpConnectionData? connection = ReadEffectiveConnection(description, media);
        bool placeholder = IsPlaceholderDefault(connection, media.Port);
        bool partialGathering = trickle && !endOfCandidates && !lite;
        bool componentOne = ContainsCandidateComponent(candidates, 1);
        bool componentTwo = ContainsCandidateComponent(candidates, 2);
        bool needsComponentTwo = rtp && !rtcpMux && !rtcpMuxOnly && !IsRtcpDisabled(description, media);

        if (!placeholder && !partialGathering)
        {
            if (!componentOne)
            {
                AddMediaIceIssue(result, limiter, mediaIndex, SdpAttributeNames.Candidate, "The ICE candidate set does not cover component 1.", media.Location);
            }
            if (needsComponentTwo && !componentTwo)
            {
                AddMediaIceIssue(result, limiter, mediaIndex, SdpAttributeNames.Candidate, "A non-multiplexed RTP media description requires an ICE candidate for component 2.", media.Location);
            }
        }

        if (connection == null || placeholder || partialGathering)
        {
            return;
        }
        SdpAddressKind connectionAddressKind = SdpIceCandidateCodecCore.ReadCandidateAddressKind(connection.Address);
        if (componentOne && connectionAddressKind != SdpAddressKind.HostName && !CandidateMatchesDefault(candidates, 1, connection.Address, media.Port))
        {
            AddMediaIceIssue(result, limiter, mediaIndex, SdpAttributeNames.Candidate, "No component 1 candidate matches the effective default media destination.", media.Location);
        }
        if (needsComponentTwo && componentTwo)
        {
            string rtcpAddress = ReadRtcpDefaultAddress(media.Attributes, connection.Address);
            int rtcpPort = ReadRtcpDefaultPort(media.Attributes, media.Port);
            SdpAddressKind rtcpAddressKind = SdpIceCandidateCodecCore.ReadCandidateAddressKind(rtcpAddress);
            if (rtcpAddressKind != SdpAddressKind.HostName && !CandidateMatchesDefault(candidates, 2, rtcpAddress, rtcpPort))
            {
                AddMediaIceIssue(result, limiter, mediaIndex, SdpAttributeNames.Candidate, "No component 2 candidate matches the effective default RTCP destination.", media.Location);
            }
        }
    }

    internal static void ValidateTrickleOptionCoverage(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (ContainsIceOption(description.Attributes, SdpIceTokens.Trickle))
        {
            return;
        }
        bool mediaTrickle = false;
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            if (ContainsIceOption(description.MediaDescriptions[mediaIndex].Attributes, SdpIceTokens.Trickle))
            {
                mediaTrickle = true;
            }
            mediaIndex = mediaIndex + 1;
        }
        if (!mediaTrickle)
        {
            return;
        }
        mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            if (!ContainsIceOption(media.Attributes, SdpIceTokens.Trickle))
            {
                AddMediaIceIssue(result, limiter, mediaIndex, SdpAttributeNames.IceOptions, "When trickle ICE is signaled at media scope, every ICE media description must signal it.", media.Location);
            }
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void ValidateCandidateOrdering(SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool ended = false;
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.EndOfCandidates, StringComparison.OrdinalIgnoreCase))
            {
                ended = true;
            }
            else if (ended && string.Equals(attribute.Name, SdpAttributeNames.Candidate, StringComparison.OrdinalIgnoreCase))
            {
                int occurrenceIndex = ReadIceOccurrenceIndex(media.Attributes, index);
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationIce, SdpValidationSeverity.Error, SdpScope.Media, SdpAttributeNames.Candidate, mediaIndex, occurrenceIndex, "A candidate attribute must not appear after end-of-candidates.", attribute.Location);
            }
            index = index + 1;
        }
    }

    internal static void ValidateLiteCandidates(List<SdpIceCandidate> candidates, int mediaIndex, bool lite, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!lite)
        {
            return;
        }
        int index = 0;
        while (index < candidates.Count)
        {
            SdpIceCandidate candidate = candidates[index];
            if (candidate.CandidateType != SdpIceCandidateType.Host)
            {
                SdpSourceLocation location = new SdpSourceLocation();
                if (candidate.SourceAttribute != null)
                {
                    location = candidate.SourceAttribute.Location;
                }
                AddCandidateIssue(result, limiter, SdpScope.Media, mediaIndex, index, "An ICE-lite agent may advertise only host candidates.", location);
            }
            index = index + 1;
        }
    }

    internal static void ValidateRtpCandidateComponents(List<SdpIceCandidate> candidates, int mediaIndex, bool rtp, bool rtcpMuxOnly, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            SdpIceCandidate candidate = candidates[index];
            bool invalidRtpComponent = rtp && candidate.Component > 2 && candidate.Component <= 256;
            bool invalidMuxOnlyComponent = rtcpMuxOnly && candidate.Component == 2;
            if (invalidRtpComponent || invalidMuxOnlyComponent)
            {
                SdpSourceLocation location = new SdpSourceLocation();
                if (candidate.SourceAttribute != null)
                {
                    location = candidate.SourceAttribute.Location;
                }
                string detail = "RTP ICE candidates may use only component 1 or component 2.";
                if (invalidMuxOnlyComponent)
                {
                    detail = "An RTCP-mux-only media description must not contain a component 2 candidate.";
                }
                AddCandidateIssue(result, limiter, SdpScope.Media, mediaIndex, index, detail, location);
            }
            index = index + 1;
        }
    }

    internal static void ValidateIceScopeAttributes(List<SdpAttribute> attributes, SdpScope scope, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (IsIceAttributeName(attribute.Name) && !IsIceScopeAllowed(attribute.Name, scope))
            {
                int occurrenceIndex = ReadIceOccurrenceIndex(attributes, index);
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpValidationSeverity.Error, scope, attribute.Name, mediaIndex, occurrenceIndex, "The ICE attribute appears in a scope where it is not permitted.", attribute.Location);
            }
            index = index + 1;
        }
    }

    internal static void ValidateIceSingletons(List<SdpAttribute> attributes, SdpScope scope, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (IsSingletonIceAttributeName(attribute.Name))
            {
                int occurrenceIndex = ReadIceOccurrenceIndex(attributes, index);
                if (occurrenceIndex > 0)
                {
                    SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, SdpValidationSeverity.Error, scope, attribute.Name, mediaIndex, occurrenceIndex, "The ICE attribute may appear at most once in this scope.", attribute.Location);
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateIceAttributeValues(List<SdpAttribute> attributes, SdpScope scope, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (IsIceAttributeName(attribute.Name))
            {
                int occurrenceIndex = ReadIceOccurrenceIndex(attributes, index);
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success)
                {
                    string detail = "The ICE attribute value does not satisfy its registered syntax.";
                    if (decoded.Diagnostics.Count > 0)
                    {
                        detail = decoded.Diagnostics[0].Detail;
                    }
                    SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationIce, SdpValidationSeverity.Error, scope, attribute.Name, mediaIndex, occurrenceIndex, detail, attribute.Location);
                }
                else if (decoded.IceCandidate != null)
                {
                    ValidateCandidateSemantics(decoded.IceCandidate, attribute, scope, mediaIndex, occurrenceIndex, result, limiter);
                }
                else if (decoded.RemoteCandidatesAttribute != null)
                {
                    ValidateRemoteCandidateSemantics(decoded.RemoteCandidatesAttribute, attribute, scope, mediaIndex, occurrenceIndex, result, limiter);
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateEffectiveIceCredentials(SdpSessionDescription description, SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool sessionUsername = ContainsIceAttribute(description.Attributes, SdpAttributeNames.IceUsernameFragment);
        bool sessionPassword = ContainsIceAttribute(description.Attributes, SdpAttributeNames.IcePassword);
        bool mediaUsername = ContainsIceAttribute(media.Attributes, SdpAttributeNames.IceUsernameFragment);
        bool mediaPassword = ContainsIceAttribute(media.Attributes, SdpAttributeNames.IcePassword);
        bool effectiveUsername = mediaUsername || sessionUsername;
        bool effectivePassword = mediaPassword || sessionPassword;
        bool iceInUse = effectiveUsername || effectivePassword || ContainsIceAttribute(description.Attributes, SdpAttributeNames.IceOptions) || ContainsIceAttribute(description.Attributes, SdpAttributeNames.IceLite) || ContainsAnyMediaIceAttribute(media.Attributes);
        if (!iceInUse)
        {
            return;
        }
        if (!effectiveUsername)
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationIce, SdpValidationSeverity.Error, SdpScope.Media, SdpAttributeNames.IceUsernameFragment, mediaIndex, -1, "The media description does not have an effective ICE username fragment.", media.Location);
        }
        if (!effectivePassword)
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationIce, SdpValidationSeverity.Error, SdpScope.Media, SdpAttributeNames.IcePassword, mediaIndex, -1, "The media description does not have an effective ICE password.", media.Location);
        }
    }

    internal static void ValidateReusedIceCredentials(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            string username = ReadEffectiveCredentialValue(description.Attributes, media.Attributes, SdpAttributeNames.IceUsernameFragment, 4);
            string password = ReadEffectiveCredentialValue(description.Attributes, media.Attributes, SdpAttributeNames.IcePassword, 22);
            if (username.Length > 0 && password.Length > 0)
            {
                int earlierIndex = 0;
                bool mismatchReported = false;
                while (earlierIndex < mediaIndex && !mismatchReported)
                {
                    SdpMediaDescription earlier = description.MediaDescriptions[earlierIndex];
                    string earlierUsername = ReadEffectiveCredentialValue(description.Attributes, earlier.Attributes, SdpAttributeNames.IceUsernameFragment, 4);
                    if (earlierUsername == username)
                    {
                        string earlierPassword = ReadEffectiveCredentialValue(description.Attributes, earlier.Attributes, SdpAttributeNames.IcePassword, 22);
                        if (earlierPassword.Length > 0 && earlierPassword != password)
                        {
                            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationIce, SdpValidationSeverity.Error, SdpScope.Media, SdpAttributeNames.IcePassword, mediaIndex, -1, "Media descriptions that share an ICE username fragment must use the same ICE password.", media.Location);
                            mismatchReported = true;
                        }
                    }
                    earlierIndex = earlierIndex + 1;
                }
            }
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static string ReadEffectiveCredentialValue(List<SdpAttribute> sessionAttributes, List<SdpAttribute> mediaAttributes, string name, int minimumLength)
    {
        int index = 0;
        while (index < mediaAttributes.Count)
        {
            if (string.Equals(mediaAttributes[index].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                string value = mediaAttributes[index].Value;
                return SdpIceAttributeCodecCore.IsIceCredentialValue(value, minimumLength, 256) ? value : string.Empty;
            }
            index = index + 1;
        }
        index = 0;
        while (index < sessionAttributes.Count)
        {
            if (string.Equals(sessionAttributes[index].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                string value = sessionAttributes[index].Value;
                return SdpIceAttributeCodecCore.IsIceCredentialValue(value, minimumLength, 256) ? value : string.Empty;
            }
            index = index + 1;
        }
        return string.Empty;
    }

    internal static void ValidateCandidateSemantics(SdpIceCandidate candidate, SdpAttribute attribute, SdpScope scope, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (candidate.Component < 1 || candidate.Component > 256)
        {
            AddCandidateIssue(result, limiter, scope, mediaIndex, occurrenceIndex, "The ICE candidate component identifier must be from 1 through 256.", attribute.Location);
        }
        if (candidate.Priority < 1u || candidate.Priority > 2147483647u)
        {
            AddCandidateIssue(result, limiter, scope, mediaIndex, occurrenceIndex, "The ICE candidate priority must be from 1 through 2147483647.", attribute.Location);
        }
        if (candidate.Port < 1 || candidate.Port > 65535)
        {
            AddCandidateIssue(result, limiter, scope, mediaIndex, occurrenceIndex, "The ICE candidate port must be from 1 through 65535.", attribute.Location);
        }
        ValidateCandidateAddressKind(candidate.AddressKind, result, limiter, scope, SdpAttributeNames.Candidate, mediaIndex, occurrenceIndex, "candidate", attribute.Location);

        bool hasTcpType = candidate.TcpTypeToken.Length > 0;
        if (candidate.TransportProtocol == SdpIceTransportProtocol.Tcp)
        {
            if (!hasTcpType || candidate.TcpType == SdpIceTcpType.Unknown)
            {
                AddCandidateIssue(result, limiter, scope, mediaIndex, occurrenceIndex, "A TCP ICE candidate requires an active, passive, or simultaneous-open TCP type.", attribute.Location);
            }
            if (candidate.TcpType == SdpIceTcpType.Active && candidate.Port != 9)
            {
                AddCandidateIssue(result, limiter, scope, mediaIndex, occurrenceIndex, "An active TCP ICE candidate must use the discard port 9.", attribute.Location);
            }
        }
        else if (hasTcpType)
        {
            AddCandidateIssue(result, limiter, scope, mediaIndex, occurrenceIndex, "Only a TCP ICE candidate may contain a TCP type extension.", attribute.Location);
        }

        bool hasRelatedEndpoint = candidate.HasRelatedAddress || candidate.HasRelatedPort;
        if (candidate.CandidateType == SdpIceCandidateType.Host && hasRelatedEndpoint)
        {
            AddCandidateIssue(result, limiter, scope, mediaIndex, occurrenceIndex, "A host ICE candidate must not contain a related endpoint.", attribute.Location);
        }
        bool requiresRelatedEndpoint = candidate.CandidateType == SdpIceCandidateType.ServerReflexive || candidate.CandidateType == SdpIceCandidateType.PeerReflexive || candidate.CandidateType == SdpIceCandidateType.Relayed;
        if (requiresRelatedEndpoint && (!candidate.HasRelatedAddress || !candidate.HasRelatedPort))
        {
            AddCandidateIssue(result, limiter, scope, mediaIndex, occurrenceIndex, "A reflexive or relayed ICE candidate requires both a related address and related port.", attribute.Location);
        }
        if (candidate.HasRelatedAddress)
        {
            SdpAddressKind relatedKind = SdpIceCandidateCodecCore.ReadCandidateAddressKind(candidate.RelatedAddress);
            ValidateCandidateAddressKind(relatedKind, result, limiter, scope, SdpAttributeNames.Candidate, mediaIndex, occurrenceIndex, "related candidate", attribute.Location);
            if (!AddressFamiliesMatch(candidate.AddressKind, relatedKind))
            {
                AddCandidateIssue(result, limiter, scope, mediaIndex, occurrenceIndex, "The ICE candidate and related address must use the same IP address family.", attribute.Location);
            }
        }
        if (candidate.HasRelatedPort && (candidate.RelatedPort < 1 || candidate.RelatedPort > 65535))
        {
            AddCandidateIssue(result, limiter, scope, mediaIndex, occurrenceIndex, "The ICE candidate related port must be from 1 through 65535.", attribute.Location);
        }
    }

    internal static void ValidateRemoteCandidateSemantics(SdpRemoteCandidatesAttribute projection, SdpAttribute attribute, SdpScope scope, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < projection.Candidates.Count)
        {
            SdpRemoteCandidate candidate = projection.Candidates[index];
            if (candidate.Component < 1 || candidate.Component > 256)
            {
                AddRemoteCandidateIssue(result, limiter, scope, mediaIndex, occurrenceIndex, "Every remote ICE component identifier must be from 1 through 256.", attribute.Location);
            }
            if (candidate.Port < 1 || candidate.Port > 65535)
            {
                AddRemoteCandidateIssue(result, limiter, scope, mediaIndex, occurrenceIndex, "Every remote ICE candidate port must be from 1 through 65535.", attribute.Location);
            }
            ValidateCandidateAddressKind(candidate.AddressKind, result, limiter, scope, SdpAttributeNames.RemoteCandidates, mediaIndex, occurrenceIndex, "remote candidate", attribute.Location);
            index = index + 1;
        }
    }

    internal static void ValidateCandidateAddressKind(SdpAddressKind kind, SdpValidationResult result, SdpValidationIssueLimiter limiter, SdpScope scope, string attributeName, int mediaIndex, int occurrenceIndex, string endpointName, SdpSourceLocation location)
    {
        if (kind == SdpAddressKind.Ipv4Multicast || kind == SdpAddressKind.Ipv6Multicast || kind == SdpAddressKind.Unknown)
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationIce, SdpValidationSeverity.Error, scope, attributeName, mediaIndex, occurrenceIndex, "The ICE " + endpointName + " address must be a valid unicast IP address or an extension-supported host name.", location);
        }
        else if (kind == SdpAddressKind.HostName)
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationIce, SdpValidationSeverity.Warning, scope, attributeName, mediaIndex, occurrenceIndex, "The ICE " + endpointName + " uses a host name that requires extension support.", location);
        }
    }

    internal static bool AddressFamiliesMatch(SdpAddressKind first, SdpAddressKind second)
    {
        bool firstIpv4 = first == SdpAddressKind.Ipv4 || first == SdpAddressKind.Ipv4Multicast;
        bool secondIpv4 = second == SdpAddressKind.Ipv4 || second == SdpAddressKind.Ipv4Multicast;
        bool firstIpv6 = first == SdpAddressKind.Ipv6 || first == SdpAddressKind.Ipv6Multicast;
        bool secondIpv6 = second == SdpAddressKind.Ipv6 || second == SdpAddressKind.Ipv6Multicast;
        if (firstIpv4 && secondIpv4 || firstIpv6 && secondIpv6)
        {
            return true;
        }
        return first == SdpAddressKind.HostName || second == SdpAddressKind.HostName || first == SdpAddressKind.Unknown || second == SdpAddressKind.Unknown;
    }

    internal static void AddCandidateIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, SdpScope scope, int mediaIndex, int occurrenceIndex, string detail, SdpSourceLocation location)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationIce, SdpValidationSeverity.Error, scope, SdpAttributeNames.Candidate, mediaIndex, occurrenceIndex, detail, location);
    }

    internal static void AddRemoteCandidateIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, SdpScope scope, int mediaIndex, int occurrenceIndex, string detail, SdpSourceLocation location)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationIce, SdpValidationSeverity.Error, scope, SdpAttributeNames.RemoteCandidates, mediaIndex, occurrenceIndex, detail, location);
    }

    internal static bool ContainsAnyMediaIceAttribute(List<SdpAttribute> attributes)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (IsIceAttributeName(attributes[index].Name))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool ContainsIceAttribute(List<SdpAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (string.Equals(attributes[index].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool ContainsIceOption(List<SdpAttribute> attributes, string option)
    {
        int attributeIndex = 0;
        while (attributeIndex < attributes.Count)
        {
            SdpAttribute attribute = attributes[attributeIndex];
            if (string.Equals(attribute.Name, SdpAttributeNames.IceOptions, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.IceOptionsAttribute != null)
                {
                    int optionIndex = 0;
                    while (optionIndex < decoded.IceOptionsAttribute.Options.Count)
                    {
                        if (string.Equals(decoded.IceOptionsAttribute.Options[optionIndex], option, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                        optionIndex = optionIndex + 1;
                    }
                }
            }
            attributeIndex = attributeIndex + 1;
        }
        return false;
    }

    internal static bool MediaUsesIce(SdpSessionDescription description, SdpMediaDescription media)
    {
        return ContainsIceAttribute(description.Attributes, SdpAttributeNames.IceUsernameFragment) || ContainsIceAttribute(description.Attributes, SdpAttributeNames.IcePassword) || ContainsIceAttribute(description.Attributes, SdpAttributeNames.IceOptions) || ContainsIceAttribute(description.Attributes, SdpAttributeNames.IceLite) || ContainsAnyMediaIceAttribute(media.Attributes);
    }

    internal static List<SdpIceCandidate> ReadValidCandidates(List<SdpAttribute> attributes)
    {
        List<SdpIceCandidate> candidates = new List<SdpIceCandidate>();
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.Candidate, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.IceCandidate != null)
                {
                    candidates.Add(decoded.IceCandidate);
                }
            }
            index = index + 1;
        }
        return candidates;
    }

    internal static bool ContainsCandidateComponent(List<SdpIceCandidate> candidates, int component)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            if (candidates[index].Component == component)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool CandidateMatchesDefault(List<SdpIceCandidate> candidates, int component, string address, int port)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            SdpIceCandidate candidate = candidates[index];
            if (candidate.Component == component && candidate.Port == port && string.Equals(candidate.Address, address, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static SdpConnectionData? ReadEffectiveConnection(SdpSessionDescription description, SdpMediaDescription media)
    {
        if (media.Connections.Count > 0)
        {
            return media.Connections[0];
        }
        return description.Connection;
    }

    internal static bool IsPlaceholderDefault(SdpConnectionData? connection, int port)
    {
        if (connection == null || port != 9)
        {
            return false;
        }
        return connection.Address == "0.0.0.0" || connection.Address == "::";
    }

    internal static bool IsRtpMedia(SdpMediaDescription media)
    {
        return media.Protocol.IndexOf("RTP", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    internal static bool IsRtcpDisabled(SdpSessionDescription description, SdpMediaDescription media)
    {
        return HasEffectiveZeroBandwidth(description.Bandwidths, media.Bandwidths, SdpBandwidthTypes.RtcpSender) && HasEffectiveZeroBandwidth(description.Bandwidths, media.Bandwidths, SdpBandwidthTypes.RtcpReceiver);
    }

    internal static bool HasEffectiveZeroBandwidth(List<SdpBandwidth> sessionBandwidths, List<SdpBandwidth> mediaBandwidths, string type)
    {
        int index = 0;
        while (index < mediaBandwidths.Count)
        {
            if (string.Equals(mediaBandwidths[index].Type, type, StringComparison.OrdinalIgnoreCase))
            {
                return mediaBandwidths[index].Value == 0;
            }
            index = index + 1;
        }
        index = 0;
        while (index < sessionBandwidths.Count)
        {
            if (string.Equals(sessionBandwidths[index].Type, type, StringComparison.OrdinalIgnoreCase))
            {
                return sessionBandwidths[index].Value == 0;
            }
            index = index + 1;
        }
        return false;
    }

    internal static string ReadRtcpDefaultAddress(List<SdpAttribute> attributes, string fallbackAddress)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.Rtcp, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.RtcpAttribute != null && decoded.RtcpAttribute.HasNetworkAddress)
                {
                    return decoded.RtcpAttribute.Address;
                }
            }
            index = index + 1;
        }
        return fallbackAddress;
    }

    internal static int ReadRtcpDefaultPort(List<SdpAttribute> attributes, int mediaPort)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.Rtcp, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.RtcpAttribute != null)
                {
                    return decoded.RtcpAttribute.Port;
                }
            }
            index = index + 1;
        }
        return mediaPort + 1;
    }

    internal static void AddMediaIceIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, int mediaIndex, string attributeName, string detail, SdpSourceLocation location)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationIce, SdpValidationSeverity.Error, SdpScope.Media, attributeName, mediaIndex, -1, detail, location);
    }

    internal static int ReadIceOccurrenceIndex(List<SdpAttribute> attributes, int attributeIndex)
    {
        string name = attributes[attributeIndex].Name;
        int occurrenceIndex = 0;
        int index = 0;
        while (index < attributeIndex)
        {
            if (string.Equals(attributes[index].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                occurrenceIndex = occurrenceIndex + 1;
            }
            index = index + 1;
        }
        return occurrenceIndex;
    }

    internal static bool IsIceScopeAllowed(string name, SdpScope scope)
    {
        if (string.Equals(name, SdpAttributeNames.IceUsernameFragment, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.IcePassword, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.IceOptions, StringComparison.OrdinalIgnoreCase))
        {
            return scope == SdpScope.Session || scope == SdpScope.Media;
        }
        if (string.Equals(name, SdpAttributeNames.IceLite, StringComparison.OrdinalIgnoreCase))
        {
            return scope == SdpScope.Session;
        }
        return scope == SdpScope.Media;
    }

    internal static bool IsSingletonIceAttributeName(string name)
    {
        return !string.Equals(name, SdpAttributeNames.Candidate, StringComparison.OrdinalIgnoreCase) && IsIceAttributeName(name);
    }

    internal static bool IsIceAttributeName(string name)
    {
        return string.Equals(name, SdpAttributeNames.IceUsernameFragment, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.IcePassword, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.IceOptions, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.IceLite, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.IceMismatch, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.Candidate, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.RemoteCandidates, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.EndOfCandidates, StringComparison.OrdinalIgnoreCase);
    }
}
