using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpBundleValidationCore
{
    internal static void ValidateBundleDescription(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<int> bundledMediaIndexes = new List<int>();
        int attributeIndex = 0;
        while (attributeIndex < description.Attributes.Count)
        {
            SdpAttribute attribute = description.Attributes[attributeIndex];
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.Group))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.GroupAttribute != null && decoded.GroupAttribute.Semantic == SdpGroupSemantic.Bundle)
                {
                    List<int> groupMediaIndexes = ResolveBundleValidationMediaIndexes(description, decoded.GroupAttribute);
                    AddDistinctBundleIndexes(bundledMediaIndexes, groupMediaIndexes);
                    ValidateBundleGroup(description, decoded.GroupAttribute, groupMediaIndexes, result, limiter);
                }
            }
            attributeIndex = attributeIndex + 1;
        }
        ValidateBundleOnlyAttributes(description, bundledMediaIndexes, result, limiter);
        if (bundledMediaIndexes.Count > 0)
        {
            ValidateBundleCautionAttributes(description, bundledMediaIndexes, result, limiter);
        }
    }

    internal static List<int> ResolveBundleValidationMediaIndexes(SdpSessionDescription description, SdpGroupAttribute group)
    {
        List<int> result = new List<int>();
        int tagIndex = 0;
        while (tagIndex < group.IdentificationTags.Count)
        {
            int mediaIndex = FindUniqueBundleMediaIndex(description, group.IdentificationTags[tagIndex]);
            if (mediaIndex < 0 || result.Contains(mediaIndex))
            {
                result.Clear();
                return result;
            }
            result.Add(mediaIndex);
            tagIndex = tagIndex + 1;
        }
        return result;
    }

    internal static int FindUniqueBundleMediaIndex(SdpSessionDescription description, string mediaId)
    {
        int result = -1;
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaIdAttribute? resolved = SdpGroupServiceCore.ResolveMediaId(description.MediaDescriptions[mediaIndex]);
            if (resolved != null && resolved.Id == mediaId)
            {
                if (result >= 0)
                {
                    return -1;
                }
                result = mediaIndex;
            }
            mediaIndex = mediaIndex + 1;
        }
        return result;
    }

    internal static void AddDistinctBundleIndexes(List<int> target, List<int> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            if (!target.Contains(source[index]))
            {
                target.Add(source[index]);
            }
            index = index + 1;
        }
    }

    internal static void ValidateBundleOnlyAttributes(SdpSessionDescription description, List<int> bundledMediaIndexes, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            int attributeIndex = 0;
            while (attributeIndex < media.Attributes.Count)
            {
                SdpAttribute attribute = media.Attributes[attributeIndex];
                if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.BundleOnly))
                {
                    int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, attributeIndex);
                    if (attribute.Kind != SdpAttributeKind.Property)
                    {
                        AddBundleAttributeIssue(result, limiter, mediaIndex, attribute, occurrenceIndex, "The bundle-only attribute must use property syntax without a value.");
                    }
                    if (!bundledMediaIndexes.Contains(mediaIndex))
                    {
                        AddBundleAttributeIssue(result, limiter, mediaIndex, attribute, occurrenceIndex, "The bundle-only attribute is permitted only in a media section listed in a BUNDLE group.");
                    }
                    if (media.Port != 0)
                    {
                        AddBundleAttributeIssue(result, limiter, mediaIndex, attribute, occurrenceIndex, "The bundle-only attribute requires a zero media port.");
                    }
                    if (result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Answerer)
                    {
                        AddBundleAttributeIssue(result, limiter, mediaIndex, attribute, occurrenceIndex, "A BUNDLE answer must not retain the bundle-only attribute.");
                    }
                }
                attributeIndex = attributeIndex + 1;
            }
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void ValidateBundleGroup(SdpSessionDescription description, SdpGroupAttribute group, List<int> mediaIndexes, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (mediaIndexes.Count == 0)
        {
            return;
        }
        int taggedMediaIndex = mediaIndexes[0];
        bool initialOffer = IsInitialBundleOffer(result);
        ValidateBundlePortsAndConnections(description, mediaIndexes, taggedMediaIndex, initialOffer, result, limiter);
        ValidateBundleProtocols(description, mediaIndexes, result, limiter);
        ValidateBundleRtcp(description, mediaIndexes, taggedMediaIndex, initialOffer, result, limiter);
        ValidateBundleIce(description, group, mediaIndexes, taggedMediaIndex, initialOffer, result, limiter);
        ValidateBundleDtls(description, group, mediaIndexes, taggedMediaIndex, result, limiter);
        ValidateBundleMidExtensions(description, mediaIndexes, result, limiter);
        ValidateBundleAttributePlacement(description, mediaIndexes, taggedMediaIndex, initialOffer, result, limiter);
    }

    internal static void ValidateBundlePortsAndConnections(SdpSessionDescription description, List<int> mediaIndexes, int taggedMediaIndex, bool initialOffer, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        string addressType = string.Empty;
        int index = 0;
        while (index < mediaIndexes.Count)
        {
            int mediaIndex = mediaIndexes[index];
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            int portCount = media.HasPortCount ? media.PortCount : 1;
            bool bundleOnly = ContainsValidPropertyAttribute(media.Attributes, SdpAttributeNames.BundleOnly);
            if (media.Port < 0 || media.Port > 65535 || portCount <= 0 || media.Port > 0 && portCount > 65536 - media.Port)
            {
                AddBundleFieldIssue(result, limiter, mediaIndex, SdpFieldNames.Media, "A bundled media port and port count must identify a valid transport range.", media.Location);
            }
            if (mediaIndex == taggedMediaIndex && media.Port == 0)
            {
                AddBundleFieldIssue(result, limiter, mediaIndex, SdpFieldNames.Media, "The tagged BUNDLE media section must use a nonzero port.", media.Location);
            }
            else if (media.Port == 0 && !bundleOnly && !initialOffer)
            {
                AddBundleFieldIssue(result, limiter, mediaIndex, SdpFieldNames.Media, "A zero-port media section without bundle-only must not remain in a BUNDLE group.", media.Location);
            }

            SdpConnectionData? connection = SdpIceValidationCore.ReadEffectiveConnection(description, media);
            if (connection != null)
            {
                if (!string.Equals(connection.NetworkType, SdpNetworkTypes.Internet, StringComparison.OrdinalIgnoreCase))
                {
                    AddBundleFieldIssue(result, limiter, mediaIndex, SdpFieldNames.Connection, "Bundled connection data must use the IN network type.", connection.Location);
                }
                bool ip4 = string.Equals(connection.AddressType, SdpAddressTypes.Ip4, StringComparison.OrdinalIgnoreCase);
                bool ip6 = string.Equals(connection.AddressType, SdpAddressTypes.Ip6, StringComparison.OrdinalIgnoreCase);
                if (!ip4 && !ip6)
                {
                    AddBundleFieldIssue(result, limiter, mediaIndex, SdpFieldNames.Connection, "Bundled connection data must use the IP4 or IP6 address type.", connection.Location);
                }
                else if (addressType.Length == 0)
                {
                    addressType = connection.AddressType;
                }
                else if (!string.Equals(addressType, connection.AddressType, StringComparison.OrdinalIgnoreCase))
                {
                    AddBundleFieldIssue(result, limiter, mediaIndex, SdpFieldNames.Connection, "Every media section in a BUNDLE group must use the same connection address type.", connection.Location);
                }
            }
            index = index + 1;
        }
        ValidateBundleEndpointMode(description, mediaIndexes, taggedMediaIndex, initialOffer, result, limiter);
    }

    internal static void ValidateBundleEndpointMode(SdpSessionDescription description, List<int> mediaIndexes, int taggedMediaIndex, bool initialOffer, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpNegotiationRole role = result.AppliedOptions.NegotiationRole;
        if (role == SdpNegotiationRole.Unknown)
        {
            return;
        }
        int index = 0;
        while (index < mediaIndexes.Count)
        {
            int mediaIndex = mediaIndexes[index];
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            if (media.Port > 0 && mediaIndex != taggedMediaIndex)
            {
                if (initialOffer)
                {
                    int earlierIndex = 0;
                    while (earlierIndex < index)
                    {
                        int earlierMediaIndex = mediaIndexes[earlierIndex];
                        SdpMediaDescription earlierMedia = description.MediaDescriptions[earlierMediaIndex];
                        if (earlierMedia.Port > 0 && BundleEndpointsEqual(description, earlierMediaIndex, mediaIndex) && !IsBundlePlaceholder(description, earlierMediaIndex) && !IsBundlePlaceholder(description, mediaIndex))
                        {
                            AddBundleFieldIssue(result, limiter, mediaIndex, SdpFieldNames.Media, "Active media sections in an initial BUNDLE offer must use unique fallback address and port combinations.", media.Location);
                        }
                        earlierIndex = earlierIndex + 1;
                    }
                }
                else if (!BundleEndpointsEqual(description, taggedMediaIndex, mediaIndex))
                {
                    AddBundleFieldIssue(result, limiter, mediaIndex, SdpFieldNames.Media, "Every active media section in an established BUNDLE group must use the tagged address and port.", media.Location);
                }
            }
            index = index + 1;
        }
    }

    internal static bool BundleEndpointsEqual(SdpSessionDescription description, int firstMediaIndex, int secondMediaIndex)
    {
        SdpMediaDescription firstMedia = description.MediaDescriptions[firstMediaIndex];
        SdpMediaDescription secondMedia = description.MediaDescriptions[secondMediaIndex];
        SdpConnectionData? first = SdpIceValidationCore.ReadEffectiveConnection(description, firstMedia);
        SdpConnectionData? second = SdpIceValidationCore.ReadEffectiveConnection(description, secondMedia);
        if (first == null || second == null)
        {
            return false;
        }
        int firstCount = firstMedia.HasPortCount ? firstMedia.PortCount : 1;
        int secondCount = secondMedia.HasPortCount ? secondMedia.PortCount : 1;
        return firstMedia.Port == secondMedia.Port && firstCount == secondCount
            && string.Equals(first.NetworkType, second.NetworkType, StringComparison.OrdinalIgnoreCase)
            && string.Equals(first.AddressType, second.AddressType, StringComparison.OrdinalIgnoreCase)
            && string.Equals(first.Address, second.Address, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsBundlePlaceholder(SdpSessionDescription description, int mediaIndex)
    {
        SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
        SdpConnectionData? connection = SdpIceValidationCore.ReadEffectiveConnection(description, media);
        return SdpIceValidationCore.IsPlaceholderDefault(connection, media.Port);
    }

    internal static void ValidateBundleProtocols(SdpSessionDescription description, List<int> mediaIndexes, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        string transportLayer = string.Empty;
        string rtpProtocol = string.Empty;
        int index = 0;
        while (index < mediaIndexes.Count)
        {
            int mediaIndex = mediaIndexes[index];
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            string currentTransportLayer = ReadBundleTransportLayer(media.Protocol);
            if (transportLayer.Length == 0)
            {
                transportLayer = currentTransportLayer;
            }
            else if (currentTransportLayer.Length > 0 && !string.Equals(transportLayer, currentTransportLayer, StringComparison.OrdinalIgnoreCase))
            {
                AddBundleFieldIssue(result, limiter, mediaIndex, SdpFieldNames.Media, "Every media section in a BUNDLE group must use the same transport-layer protocol.", media.Location);
            }
            if (SdpRtpValidationCore.IsRtpProtocol(media.Protocol))
            {
                if (rtpProtocol.Length == 0)
                {
                    rtpProtocol = media.Protocol;
                }
                else if (!string.Equals(rtpProtocol, media.Protocol, StringComparison.OrdinalIgnoreCase))
                {
                    AddBundleFieldIssue(result, limiter, mediaIndex, SdpFieldNames.Media, "Every RTP media section in one BUNDLE group must use the same RTP transport profile.", media.Location);
                }
            }
            index = index + 1;
        }
    }

    internal static string ReadBundleTransportLayer(string protocol)
    {
        if (protocol.StartsWith("TCP", StringComparison.OrdinalIgnoreCase))
        {
            return SdpTransportProtocols.Tcp;
        }
        if (protocol.StartsWith("UDP", StringComparison.OrdinalIgnoreCase) || protocol.StartsWith("RTP/", StringComparison.OrdinalIgnoreCase))
        {
            return SdpTransportProtocols.Udp;
        }
        int separator = protocol.IndexOf('/');
        if (separator < 0)
        {
            return protocol;
        }
        return protocol.Substring(0, separator);
    }

    internal static void ValidateBundleRtcp(SdpSessionDescription description, List<int> mediaIndexes, int taggedMediaIndex, bool initialOffer, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool hasRtp = false;
        int index = 0;
        while (index < mediaIndexes.Count)
        {
            if (SdpRtpValidationCore.IsRtpProtocol(description.MediaDescriptions[mediaIndexes[index]].Protocol))
            {
                hasRtp = true;
            }
            index = index + 1;
        }
        if (!hasRtp)
        {
            return;
        }

        if (initialOffer)
        {
            index = 0;
            while (index < mediaIndexes.Count)
            {
                int mediaIndex = mediaIndexes[index];
                SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
                if (media.Port > 0 && !ContainsValidPropertyAttribute(media.Attributes, SdpAttributeNames.RtcpMux))
                {
                    AddBundleAttributeIssueAtMedia(result, limiter, mediaIndex, SdpAttributeNames.RtcpMux, "Every active media section in an initial RTP BUNDLE offer must include rtcp-mux.", media.Location);
                }
                index = index + 1;
            }
        }
        else if (!ContainsValidPropertyAttribute(description.MediaDescriptions[taggedMediaIndex].Attributes, SdpAttributeNames.RtcpMux))
        {
            AddBundleAttributeIssueAtMedia(result, limiter, taggedMediaIndex, SdpAttributeNames.RtcpMux, "The tagged media section of an RTP BUNDLE group must include rtcp-mux.", description.MediaDescriptions[taggedMediaIndex].Location);
        }

        index = 0;
        while (index < mediaIndexes.Count)
        {
            int mediaIndex = mediaIndexes[index];
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            SdpAttribute? muxOnly = FindNamedAttribute(media.Attributes, SdpAttributeNames.RtcpMuxOnly);
            if (muxOnly != null && !ContainsValidPropertyAttribute(media.Attributes, SdpAttributeNames.RtcpMux))
            {
                AddBundleAttributeIssue(result, limiter, mediaIndex, muxOnly, ReadAttributeOccurrence(media.Attributes, muxOnly), "The rtcp-mux-only attribute requires rtcp-mux on the same BUNDLE transport description.");
            }
            if (result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Answerer)
            {
                SdpAttribute? rtcp = FindNamedAttribute(media.Attributes, SdpAttributeNames.Rtcp);
                if (rtcp != null)
                {
                    AddBundleAttributeIssue(result, limiter, mediaIndex, rtcp, ReadAttributeOccurrence(media.Attributes, rtcp), "A bundled media section in an answer must not include a separate RTCP transport attribute.");
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateBundleIce(SdpSessionDescription description, SdpGroupAttribute group, List<int> mediaIndexes, int taggedMediaIndex, bool initialOffer, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool usesIce = DescriptionGroupUsesIce(description, mediaIndexes);
        if (!usesIce && result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Answerer && result.AppliedOptions.OfferDescription != null)
        {
            usesIce = RelatedOfferGroupUsesIce(result.AppliedOptions.OfferDescription, group);
        }
        if (!usesIce)
        {
            return;
        }

        SdpIceDescription taggedIce = SdpIceServiceCore.ResolveMediaIce(description, taggedMediaIndex);
        if (taggedIce.Credentials == null || taggedIce.Credentials.UsernameFragment.Length == 0 || taggedIce.Credentials.Password.Length == 0)
        {
            AddBundleAttributeIssueAtMedia(result, limiter, taggedMediaIndex, SdpAttributeNames.IceUsernameFragment, "The tagged BUNDLE transport must provide complete ICE credentials when ICE is used.", description.MediaDescriptions[taggedMediaIndex].Location);
        }
        if (initialOffer)
        {
            ValidateInitialIceUniqueness(description, mediaIndexes, result, limiter);
        }

        bool taggedOnly = result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Answerer || result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Offerer && !initialOffer;
        int index = 0;
        while (index < mediaIndexes.Count)
        {
            int mediaIndex = mediaIndexes[index];
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            bool forbidden = ContainsValidPropertyAttribute(media.Attributes, SdpAttributeNames.BundleOnly) || taggedOnly && mediaIndex != taggedMediaIndex;
            if (forbidden)
            {
                int attributeIndex = 0;
                while (attributeIndex < media.Attributes.Count)
                {
                    SdpAttribute attribute = media.Attributes[attributeIndex];
                    if (IsIceMediaAttribute(attribute.Name))
                    {
                        AddBundleAttributeIssue(result, limiter, mediaIndex, attribute, SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, attributeIndex), "ICE media attributes are permitted only on the active tagged BUNDLE transport in this description.");
                    }
                    attributeIndex = attributeIndex + 1;
                }
            }
            index = index + 1;
        }
    }

    internal static bool RelatedOfferGroupUsesIce(SdpSessionDescription offer, SdpGroupAttribute group)
    {
        if (FindNamedAttribute(offer.Attributes, SdpAttributeNames.IceUsernameFragment) != null || FindNamedAttribute(offer.Attributes, SdpAttributeNames.IcePassword) != null)
        {
            return true;
        }
        int tagIndex = 0;
        while (tagIndex < group.IdentificationTags.Count)
        {
            int mediaIndex = SdpDtlsServiceCore.FindMediaIndex(offer, group.IdentificationTags[tagIndex]);
            if (mediaIndex >= 0 && MediaHasIceSignaling(offer.MediaDescriptions[mediaIndex]))
            {
                return true;
            }
            tagIndex = tagIndex + 1;
        }
        return false;
    }

    internal static bool DescriptionGroupUsesIce(SdpSessionDescription description, List<int> mediaIndexes)
    {
        if (FindNamedAttribute(description.Attributes, SdpAttributeNames.IceUsernameFragment) != null || FindNamedAttribute(description.Attributes, SdpAttributeNames.IcePassword) != null)
        {
            return true;
        }
        int index = 0;
        while (index < mediaIndexes.Count)
        {
            if (MediaHasIceSignaling(description.MediaDescriptions[mediaIndexes[index]]))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool MediaHasIceSignaling(SdpMediaDescription media)
    {
        int index = 0;
        while (index < media.Attributes.Count)
        {
            if (IsIceMediaAttribute(media.Attributes[index].Name))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool IsIceMediaAttribute(string name)
    {
        return SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.Candidate)
            || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.RemoteCandidates)
            || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.IceMismatch)
            || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.IceUsernameFragment)
            || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.IcePassword)
            || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.EndOfCandidates)
            || SdpRtpValidationCore.NameEquals(name, "ice-pacing");
    }

    internal static void ValidateInitialIceUniqueness(SdpSessionDescription description, List<int> mediaIndexes, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < mediaIndexes.Count)
        {
            int mediaIndex = mediaIndexes[index];
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            if (media.Port > 0)
            {
                SdpIceDescription ice = SdpIceServiceCore.ResolveMediaIce(description, mediaIndex);
                if (ice.Credentials == null || ice.Credentials.UsernameFragment.Length == 0 || ice.Credentials.Password.Length == 0)
                {
                    AddBundleAttributeIssueAtMedia(result, limiter, mediaIndex, SdpAttributeNames.IceUsernameFragment, "Every active media section in an initial BUNDLE offer must provide complete ICE credentials when ICE is used.", media.Location);
                }
                else
                {
                    int earlierIndex = 0;
                    while (earlierIndex < index)
                    {
                        int earlierMediaIndex = mediaIndexes[earlierIndex];
                        SdpMediaDescription earlierMedia = description.MediaDescriptions[earlierMediaIndex];
                        SdpIceDescription earlierIce = SdpIceServiceCore.ResolveMediaIce(description, earlierMediaIndex);
                        if (earlierMedia.Port > 0 && earlierIce.Credentials != null && earlierIce.Credentials.UsernameFragment == ice.Credentials.UsernameFragment && earlierIce.Credentials.Password == ice.Credentials.Password)
                        {
                            AddBundleAttributeIssueAtMedia(result, limiter, mediaIndex, SdpAttributeNames.IceUsernameFragment, "Active media sections in an initial BUNDLE offer must use unique ICE credentials.", media.Location);
                        }
                        earlierIndex = earlierIndex + 1;
                    }
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateBundleDtls(SdpSessionDescription description, SdpGroupAttribute group, List<int> mediaIndexes, int taggedMediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool usesDtls = false;
        int index = 0;
        while (index < mediaIndexes.Count)
        {
            if (SdpDtlsValidationCore.IsTlsTransport(description.MediaDescriptions[mediaIndexes[index]].Protocol))
            {
                usesDtls = true;
            }
            index = index + 1;
        }
        if (!usesDtls && result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Answerer && result.AppliedOptions.OfferDescription != null)
        {
            int tagIndex = 0;
            while (tagIndex < group.IdentificationTags.Count)
            {
                int mediaIndex = SdpDtlsServiceCore.FindMediaIndex(result.AppliedOptions.OfferDescription, group.IdentificationTags[tagIndex]);
                if (mediaIndex >= 0 && SdpDtlsValidationCore.IsTlsTransport(result.AppliedOptions.OfferDescription.MediaDescriptions[mediaIndex].Protocol))
                {
                    usesDtls = true;
                }
                tagIndex = tagIndex + 1;
            }
        }
        if (!usesDtls)
        {
            return;
        }
        SdpDtlsDescription dtls = SdpDtlsServiceCore.ResolveMediaDtls(description, taggedMediaIndex);
        if (dtls.Fingerprints.Count == 0)
        {
            AddBundleAttributeIssueAtMedia(result, limiter, taggedMediaIndex, SdpAttributeNames.Fingerprint, "The tagged BUNDLE transport must provide a certificate fingerprint when DTLS or TLS is used.", description.MediaDescriptions[taggedMediaIndex].Location);
        }
        if (dtls.SetupRole == SdpDtlsSetupRole.Unknown)
        {
            AddBundleAttributeIssueAtMedia(result, limiter, taggedMediaIndex, SdpAttributeNames.Setup, "The tagged BUNDLE transport must provide a valid setup role when DTLS or TLS is used.", description.MediaDescriptions[taggedMediaIndex].Location);
        }
    }

    internal static void ValidateBundleMidExtensions(SdpSessionDescription description, List<int> mediaIndexes, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpExtMapAttribute? established = null;
        int index = 0;
        while (index < mediaIndexes.Count)
        {
            int mediaIndex = mediaIndexes[index];
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            if (SdpRtpValidationCore.IsRtpProtocol(media.Protocol))
            {
                SdpExtMapAttribute? mapping = ReadMidExtension(media);
                if (mapping == null)
                {
                    AddBundleAttributeIssueAtMedia(result, limiter, mediaIndex, SdpAttributeNames.ExtMap, "Every RTP media section in a BUNDLE group must include the RTP MID header extension.", media.Location);
                }
                else if (established == null)
                {
                    established = mapping;
                }
                else if (established.Id != mapping.Id || established.Encrypted != mapping.Encrypted || established.ExtensionAttributes != mapping.ExtensionAttributes)
                {
                    SdpSourceLocation location = mapping.SourceAttribute == null ? media.Location : mapping.SourceAttribute.Location;
                    AddBundleAttributeIssueAtMedia(result, limiter, mediaIndex, SdpAttributeNames.ExtMap, "The RTP MID header extension must use one consistent mapping throughout a BUNDLE group.", location);
                }
            }
            index = index + 1;
        }
    }

    internal static SdpExtMapAttribute? ReadMidExtension(SdpMediaDescription media)
    {
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.ExtMap))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.ExtMapAttribute != null && decoded.ExtMapAttribute.Uri == SdpHeaderExtensionUris.MediaId)
                {
                    return decoded.ExtMapAttribute;
                }
            }
            index = index + 1;
        }
        return null;
    }

    internal static void ValidateBundleAttributePlacement(SdpSessionDescription description, List<int> mediaIndexes, int taggedMediaIndex, bool initialOffer, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool taggedOnly = result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Answerer || result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Offerer && !initialOffer;
        int index = 0;
        while (index < mediaIndexes.Count)
        {
            int mediaIndex = mediaIndexes[index];
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            bool bundleOnly = ContainsValidPropertyAttribute(media.Attributes, SdpAttributeNames.BundleOnly);
            if (bundleOnly || taggedOnly && mediaIndex != taggedMediaIndex)
            {
                int attributeIndex = 0;
                while (attributeIndex < media.Attributes.Count)
                {
                    SdpAttribute attribute = media.Attributes[attributeIndex];
                    if (!IsIceMediaAttribute(attribute.Name) && IsBundleTransportCategoryAttribute(attribute))
                    {
                        AddBundleAttributeIssue(result, limiter, mediaIndex, attribute, SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, attributeIndex), "IDENTICAL and TRANSPORT category attributes are permitted only on the tagged BUNDLE media section in this description.");
                    }
                    attributeIndex = attributeIndex + 1;
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateBundleCautionAttributes(SdpSessionDescription description, List<int> mediaIndexes, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int attributeIndex = 0;
        while (attributeIndex < description.Attributes.Count)
        {
            SdpAttribute attribute = description.Attributes[attributeIndex];
            if (IsBundleCautionAttribute(attribute))
            {
                int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(description.Attributes, attributeIndex);
                AddBundleCautionIssue(result, limiter, SdpScope.Session, -1, attribute, occurrenceIndex);
            }
            attributeIndex = attributeIndex + 1;
        }

        int index = 0;
        while (index < mediaIndexes.Count)
        {
            int mediaIndex = mediaIndexes[index];
            List<SdpAttribute> attributes = description.MediaDescriptions[mediaIndex].Attributes;
            attributeIndex = 0;
            while (attributeIndex < attributes.Count)
            {
                SdpAttribute attribute = attributes[attributeIndex];
                if (IsBundleCautionAttribute(attribute))
                {
                    int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(attributes, attributeIndex);
                    AddBundleCautionIssue(result, limiter, SdpScope.Media, mediaIndex, attribute, occurrenceIndex);
                }
                attributeIndex = attributeIndex + 1;
            }
            index = index + 1;
        }
    }

    internal static bool IsBundleCautionAttribute(SdpAttribute attribute)
    {
        SdpAttributeRegistration registration = SdpRegistryLookup.FindAttributeRegistration(attribute.Name);
        return registration.MuxCategory == SdpMuxCategory.Caution;
    }

    internal static void AddBundleCautionIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, SdpScope scope, int mediaIndex, SdpAttribute attribute, int occurrenceIndex)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationBundle, SdpValidationSeverity.Warning, scope, attribute.Name, mediaIndex, occurrenceIndex, "The SDP attribute registry classifies this attribute as CAUTION; verify its specification-specific behavior before multiplexing it in BUNDLE.", attribute.Location);
    }

    internal static bool IsInitialBundleOffer(SdpValidationResult result)
    {
        return result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Offerer && result.AppliedOptions.OfferDescription == null;
    }

    internal static bool IsBundleTransportCategoryAttribute(SdpAttribute attribute)
    {
        SdpAttributeRegistration registration = SdpRegistryLookup.FindAttributeRegistration(attribute.Name);
        return registration.MuxCategory == SdpMuxCategory.Identical || registration.MuxCategory == SdpMuxCategory.Transport;
    }

    internal static bool ContainsValidPropertyAttribute(List<SdpAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (SdpRtpValidationCore.NameEquals(attributes[index].Name, name) && attributes[index].Kind == SdpAttributeKind.Property)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static SdpAttribute? FindNamedAttribute(List<SdpAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (SdpRtpValidationCore.NameEquals(attributes[index].Name, name))
            {
                return attributes[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static int ReadAttributeOccurrence(List<SdpAttribute> attributes, SdpAttribute target)
    {
        int occurrenceIndex = 0;
        int index = 0;
        while (index < attributes.Count)
        {
            if (object.ReferenceEquals(attributes[index], target))
            {
                return occurrenceIndex;
            }
            if (SdpRtpValidationCore.NameEquals(attributes[index].Name, target.Name))
            {
                occurrenceIndex = occurrenceIndex + 1;
            }
            index = index + 1;
        }
        return 0;
    }

    internal static void AddBundleAttributeIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, int mediaIndex, SdpAttribute attribute, int occurrenceIndex, string detail)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationBundle, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, detail, attribute.Location);
    }

    internal static void AddBundleAttributeIssueAtMedia(SdpValidationResult result, SdpValidationIssueLimiter limiter, int mediaIndex, string attributeName, string detail, SdpSourceLocation location)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationBundle, SdpValidationSeverity.Error, SdpScope.Media, attributeName, mediaIndex, 0, detail, location);
    }

    internal static void AddBundleFieldIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, int mediaIndex, char fieldName, string detail, SdpSourceLocation location)
    {
        SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationBundle, SdpValidationSeverity.Error, SdpScope.Media, fieldName, mediaIndex, 0, detail, location);
    }
}
