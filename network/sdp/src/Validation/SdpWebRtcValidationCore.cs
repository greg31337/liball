using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpWebRtcValidationCore
{
    internal static void ValidateWebRtcDescription(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<int> activeMediaIndexes = new List<int>();
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            ValidateWebRtcMediaIdentity(media, mediaIndex, result, limiter);
            if (media.Port != 0)
            {
                bool rtp = SdpRtpValidationCore.IsRtpProtocol(media.Protocol);
                bool dataChannel = IsWebRtcDataChannelShape(media);
                if (rtp || dataChannel)
                {
                    activeMediaIndexes.Add(mediaIndex);
                    ValidateRequiredIceAndDtls(description, media, mediaIndex, result, limiter);
                }
                if (rtp)
                {
                    ValidateWebRtcRtpMedia(description, media, mediaIndex, result, limiter);
                }
                else if (dataChannel)
                {
                    ValidateWebRtcDataChannelMedia(media, mediaIndex, result, limiter);
                }
                else
                {
                    AddWebRtcMediaIssue(media, mediaIndex, SdpDiagnosticCodes.ValidationMedia, "An active WebRTC media description must use DTLS-SRTP or a modern SCTP-over-DTLS data-channel transport.", result, limiter);
                }
            }
            mediaIndex = mediaIndex + 1;
        }
        ValidateWebRtcBundle(description, activeMediaIndexes, result, limiter);
    }

    internal static void ValidateWebRtcMediaIdentity(SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!ContainsNamedAttribute(media.Attributes, SdpAttributeNames.MediaId))
        {
            AddWebRtcMediaIdIssue(media, mediaIndex, result, limiter);
        }
    }

    internal static void ValidateWebRtcRtpMedia(SdpSessionDescription description, SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!IsAllowedWebRtcRtpProtocol(media.Protocol))
        {
            AddWebRtcMediaIssue(media, mediaIndex, SdpDiagnosticCodes.ValidationMedia, "Active WebRTC RTP media must use the UDP/TLS/RTP/SAVPF secure feedback transport.", result, limiter);
        }
        if (!ContainsValidPropertyAttribute(media.Attributes, SdpAttributeNames.RtcpMux))
        {
            AddWebRtcAttributeIssue(media, mediaIndex, SdpDiagnosticCodes.ValidationAttribute, SdpAttributeNames.RtcpMux, "Active WebRTC RTP media requires media-level RTP/RTCP multiplexing.", result, limiter);
        }
        SdpDirection direction = SdpQueryServiceCore.ResolveEffectiveDirection(description, mediaIndex);
        bool sendsMedia = direction == SdpDirection.SendOnly || direction == SdpDirection.SendReceive;
        bool mediaStreamKind = media.MediaKind == SdpMediaKind.Audio || media.MediaKind == SdpMediaKind.Video;
        if (sendsMedia && mediaStreamKind && !ContainsNamedAttribute(media.Attributes, SdpAttributeNames.MediaStreamId))
        {
            AddWebRtcAttributeIssue(media, mediaIndex, SdpDiagnosticCodes.ValidationMsid, SdpAttributeNames.MediaStreamId, "A sending WebRTC audio or video description requires a media-level msid association.", result, limiter);
        }
    }

    internal static bool IsAllowedWebRtcRtpProtocol(string protocol)
    {
        return string.Equals(protocol, SdpTransportProtocols.UdpTlsRtpSavpf, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsWebRtcDataChannelShape(SdpMediaDescription media)
    {
        return SdpDtlsValidationCore.ContainsProtocolComponent(media.Protocol, "SCTP") || ContainsFormat(media.Formats, SdpDataChannelTokens.WebRtcDataChannel) || ContainsNamedAttribute(media.Attributes, SdpAttributeNames.SctpPort);
    }

    internal static void ValidateWebRtcDataChannelMedia(SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool allowedProtocol = string.Equals(media.Protocol, SdpTransportProtocols.UdpDtlsSctp, StringComparison.OrdinalIgnoreCase) || string.Equals(media.Protocol, SdpTransportProtocols.TcpDtlsSctp, StringComparison.OrdinalIgnoreCase);
        bool application = string.Equals(media.MediaType, SdpMediaTypes.Application, StringComparison.OrdinalIgnoreCase);
        bool exactFormat = media.Formats.Count == 1 && string.Equals(media.Formats[0], SdpDataChannelTokens.WebRtcDataChannel, StringComparison.OrdinalIgnoreCase);
        if (!allowedProtocol || !application || !exactFormat)
        {
            AddWebRtcMediaIssue(media, mediaIndex, SdpDiagnosticCodes.ValidationDataChannel, "A WebRTC data channel requires application media, a UDP/DTLS/SCTP or TCP/DTLS/SCTP transport, and exactly the webrtc-datachannel format.", result, limiter);
        }
        if (!ContainsNamedAttribute(media.Attributes, SdpAttributeNames.SctpPort))
        {
            AddWebRtcAttributeIssue(media, mediaIndex, SdpDiagnosticCodes.ValidationDataChannel, SdpAttributeNames.SctpPort, "An active WebRTC data channel requires a media-level sctp-port attribute.", result, limiter);
        }
        if (string.Equals(media.Protocol, SdpTransportProtocols.TcpDtlsSctp, StringComparison.OrdinalIgnoreCase) && !ContainsNamedAttribute(media.Attributes, SdpAttributeNames.Connection))
        {
            AddWebRtcAttributeIssue(media, mediaIndex, SdpDiagnosticCodes.ValidationDataChannel, SdpAttributeNames.Connection, "A TCP WebRTC data channel requires a media-level connection attribute.", result, limiter);
        }
    }

    internal static void ValidateRequiredIceAndDtls(SdpSessionDescription description, SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int iceTransportIndex = SdpIceServiceCore.ResolveTransportMediaIndex(description, mediaIndex);
        SdpMediaDescription iceTransport = description.MediaDescriptions[iceTransportIndex];
        bool username = ContainsNamedAttribute(iceTransport.Attributes, SdpAttributeNames.IceUsernameFragment) || ContainsNamedAttribute(description.Attributes, SdpAttributeNames.IceUsernameFragment);
        bool password = ContainsNamedAttribute(iceTransport.Attributes, SdpAttributeNames.IcePassword) || ContainsNamedAttribute(description.Attributes, SdpAttributeNames.IcePassword);
        if (!username)
        {
            AddWebRtcAttributeIssue(media, mediaIndex, SdpDiagnosticCodes.ValidationIce, SdpAttributeNames.IceUsernameFragment, "Every active WebRTC transport requires an effective ICE username fragment.", result, limiter);
        }
        if (!password)
        {
            AddWebRtcAttributeIssue(media, mediaIndex, SdpDiagnosticCodes.ValidationIce, SdpAttributeNames.IcePassword, "Every active WebRTC transport requires an effective ICE password.", result, limiter);
        }

        int dtlsTransportIndex = SdpDtlsServiceCore.ResolveTransportMediaIndex(description, mediaIndex);
        SdpMediaDescription dtlsTransport = description.MediaDescriptions[dtlsTransportIndex];
        if (!SdpDtlsValidationCore.HasEffectiveFingerprint(description.Attributes, dtlsTransport.Attributes))
        {
            AddWebRtcAttributeIssue(media, mediaIndex, SdpDiagnosticCodes.ValidationDtls, SdpAttributeNames.Fingerprint, "Every active WebRTC transport requires an effective certificate fingerprint.", result, limiter);
        }
        if (!SdpDtlsValidationCore.HasEffectiveAttribute(description.Attributes, dtlsTransport.Attributes, SdpAttributeNames.Setup))
        {
            AddWebRtcAttributeIssue(media, mediaIndex, SdpDiagnosticCodes.ValidationDtls, SdpAttributeNames.Setup, "Every active WebRTC transport requires an effective DTLS setup role.", result, limiter);
        }
    }

    internal static void ValidateWebRtcBundle(SdpSessionDescription description, List<int> activeMediaIndexes, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (activeMediaIndexes.Count < 2)
        {
            return;
        }
        List<SdpGroupAttribute> groups = SdpGroupServiceCore.ResolveGroups(description);
        SdpGroupAttribute? bundle = null;
        int bundleCount = 0;
        int groupIndex = 0;
        while (groupIndex < groups.Count)
        {
            if (groups[groupIndex].Semantic == SdpGroupSemantic.Bundle)
            {
                bundleCount = bundleCount + 1;
                if (bundle == null)
                {
                    bundle = groups[groupIndex];
                }
            }
            groupIndex = groupIndex + 1;
        }
        if (bundle == null)
        {
            if (!ContainsBundleDeclaration(description.Attributes))
            {
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationBundle, SdpValidationSeverity.Error, SdpScope.Session, SdpAttributeNames.Group, -1, -1, "A WebRTC description with multiple active transports requires a BUNDLE group.", new SdpSourceLocation());
            }
            return;
        }
        if (bundleCount > 1)
        {
            SdpSourceLocation location = bundle.SourceAttribute == null ? new SdpSourceLocation() : bundle.SourceAttribute.Location;
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationBundle, SdpValidationSeverity.Error, SdpScope.Session, SdpAttributeNames.Group, -1, -1, "A WebRTC description must place its active transports in one BUNDLE group.", location);
        }
        int activeIndex = 0;
        while (activeIndex < activeMediaIndexes.Count)
        {
            int mediaIndex = activeMediaIndexes[activeIndex];
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            SdpMediaIdAttribute? mediaId = SdpGroupServiceCore.ResolveMediaId(media);
            if (mediaId != null && !bundle.IdentificationTags.Contains(mediaId.Id))
            {
                AddWebRtcAttributeIssue(media, mediaIndex, SdpDiagnosticCodes.ValidationBundle, SdpAttributeNames.MediaId, "Every active WebRTC transport must be a member of the session's BUNDLE group.", result, limiter);
            }
            activeIndex = activeIndex + 1;
        }
    }

    internal static bool ContainsBundleDeclaration(List<SdpAttribute> attributes)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.Group, StringComparison.OrdinalIgnoreCase))
            {
                string value = SdpBfcpAttributeCodecCore.TrimHorizontalWhitespace(attribute.Value);
                if (value == "BUNDLE" || value.StartsWith("BUNDLE ", StringComparison.Ordinal))
                {
                    return true;
                }
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool ContainsValidPropertyAttribute(List<SdpAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase) && attribute.Kind == SdpAttributeKind.Property)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool ContainsNamedAttribute(List<SdpAttribute> attributes, string name)
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

    internal static bool ContainsFormat(List<string> formats, string value)
    {
        int index = 0;
        while (index < formats.Count)
        {
            if (string.Equals(formats[index], value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static void AddWebRtcAttributeIssue(SdpMediaDescription media, int mediaIndex, string code, string attributeName, string detail, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, code, SdpValidationSeverity.Error, SdpScope.Media, attributeName, mediaIndex, -1, detail, media.Location);
    }

    internal static void AddWebRtcMediaIdIssue(SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationGroup, SdpValidationSeverity.Error, SdpScope.Media, SdpAttributeNames.MediaId, mediaIndex, 0, "Every WebRTC media description, including a rejected one, requires a media-level mid attribute.", media.Location);
    }

    internal static void AddWebRtcMediaIssue(SdpMediaDescription media, int mediaIndex, string code, string detail, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpValidationCore.AddFieldValidationIssue(result, limiter, code, SdpValidationSeverity.Error, SdpScope.Media, 'm', mediaIndex, 0, detail, media.Location);
    }
}
