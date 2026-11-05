using System;
using System.Collections.Generic;
using System.Globalization;

namespace sdp;

internal static class SdpOfferAnswerCore
{
    internal static void ValidateInitialOfferArguments(SdpSessionDescription sessionTemplate, SdpNegotiationOptions options)
    {
        if (sessionTemplate.MediaDescriptions == null || sessionTemplate.MediaDescriptions.Count != 0)
        {
            throw new ArgumentException("An initial-offer session template must not contain media descriptions.", "sessionTemplate");
        }
        if (sessionTemplate.Origin == null || sessionTemplate.Timings == null || sessionTemplate.Timings.Count == 0 || !sessionTemplate.HasSessionName)
        {
            throw new ArgumentException("An initial-offer session template requires origin, session-name, and timing fields.", "sessionTemplate");
        }
        SdpSessionFactoryCore.ValidateOrigin(sessionTemplate.Origin);
        SdpSessionFactoryCore.ValidateSessionName(sessionTemplate.SessionName);
        int timingIndex = 0;
        while (timingIndex < sessionTemplate.Timings.Count)
        {
            SdpSessionFactoryCore.ValidateTiming(sessionTemplate.Timings[timingIndex]);
            timingIndex = timingIndex + 1;
        }
        if (sessionTemplate.Connection != null)
        {
            SdpSessionFactoryCore.ValidateConnection(sessionTemplate.Connection);
        }
        if (options.Role != SdpNegotiationRole.Unknown && options.Role != SdpNegotiationRole.Offerer)
        {
            throw new ArgumentException("Initial offer creation requires the offerer role.", "options");
        }
        if (options.CodecMatchMode != SdpCodecMatchMode.Strict && options.CodecMatchMode != SdpCodecMatchMode.Compatible)
        {
            throw new ArgumentOutOfRangeException("options");
        }
        if (options.MaximumDiagnosticCount <= 0)
        {
            throw new ArgumentOutOfRangeException("options");
        }
        SdpEndpointCapabilities? capabilities = options.LocalCapabilities;
        if (capabilities == null)
        {
            throw new ArgumentException("Initial offer creation requires local endpoint capabilities.", "options");
        }
        ValidateEndpointCapabilities(capabilities, sessionTemplate);
        if (options.RequireIce && !capabilities.SupportsIce)
        {
            throw new ArgumentException("The required ICE policy is not supported by the local capabilities.", "options");
        }
        if (options.RequireDtls && !capabilities.SupportsDtls)
        {
            throw new ArgumentException("The required DTLS policy is not supported by the local capabilities.", "options");
        }
        if (options.RequireBundle && !capabilities.SupportsBundle)
        {
            throw new ArgumentException("The required BUNDLE policy is not supported by the local capabilities.", "options");
        }
        ValidateRequiredInitialPolicies(options, capabilities);
    }

    internal static void ValidateEndpointCapabilities(SdpEndpointCapabilities capabilities, SdpSessionDescription sessionTemplate)
    {
        if (capabilities.Media == null || capabilities.HeaderExtensions == null || capabilities.Fingerprints == null)
        {
            throw new ArgumentException("Endpoint capability collections must be initialized.", "options");
        }
        if (capabilities.Media.Count == 0)
        {
            throw new ArgumentException("At least one media capability is required.", "options");
        }
        if (capabilities.Connection != null)
        {
            SdpSessionFactoryCore.ValidateConnection(capabilities.Connection);
        }
        if (sessionTemplate.Connection == null && capabilities.Connection == null)
        {
            throw new ArgumentException("The session template or endpoint capabilities must provide connection data.", "options");
        }
        ValidateHeaderExtensionCapabilities(capabilities.HeaderExtensions);
        int mediaIndex = 0;
        while (mediaIndex < capabilities.Media.Count)
        {
            SdpMediaCapability? media = capabilities.Media[mediaIndex];
            if (media == null)
            {
                throw new ArgumentException("Media capability collections must not contain null entries.", "options");
            }
            ValidateMediaCapability(media);
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void ValidateMediaCapability(SdpMediaCapability media)
    {
        if (!SdpCommonAttributeCodecCore.IsTokenValue(media.MediaType))
        {
            throw new ArgumentException("Every media capability requires a valid media type.", "options");
        }
        if (media.MediaId.Length > 0 && !SdpCommonAttributeCodecCore.IsTokenValue(media.MediaId))
        {
            throw new ArgumentException("Preferred media identifiers must use token syntax.", "options");
        }
        if (media.Port <= 0 || media.Port > 65535 || media.HasPortCount && (media.PortCount <= 0 || media.PortCount > 65536 - media.Port))
        {
            throw new ArgumentOutOfRangeException("options");
        }
        if (media.Protocols == null || media.Formats == null || media.Codecs == null || media.HeaderExtensions == null || media.MediaStreamIds == null || media.SourceAttributes == null || media.SourceGroups == null || media.Rids == null || media.ImageAttributes == null || media.DataChannels == null)
        {
            throw new ArgumentException("Media capability collections must be initialized.", "options");
        }
        if (media.Protocols.Count == 0 || !SdpCoreValidationCore.IsValidProtocolToken(media.Protocols[0]))
        {
            throw new ArgumentException("Every media capability requires a preferred transport protocol.", "options");
        }
        if (!IsDefinedDirection(media.Direction))
        {
            throw new ArgumentOutOfRangeException("options");
        }
        if (media.Connection != null)
        {
            SdpSessionFactoryCore.ValidateConnection(media.Connection);
        }
        ValidateHeaderExtensionCapabilities(media.HeaderExtensions);
        ValidateCodecCapabilities(media);
        bool rtp = SdpRtpValidationCore.IsRtpProtocol(media.Protocols[0]);
        bool sctp = SdpDataChannelValidationCore.IsModernSctpProtocol(media.Protocols[0]) || SdpDataChannelValidationCore.IsLegacySctpProtocol(media.Protocols[0]);
        if (rtp && media.Codecs.Count == 0)
        {
            throw new ArgumentException("RTP media capabilities require at least one codec.", "options");
        }
        if (sctp && media.Sctp == null)
        {
            throw new ArgumentException("SCTP media capabilities require SCTP signaling.", "options");
        }
        if (!rtp && !sctp && media.Formats.Count == 0)
        {
            throw new ArgumentException("Non-RTP media capabilities require at least one explicit format token.", "options");
        }
    }

    internal static void ValidateCodecCapabilities(SdpMediaCapability media)
    {
        List<int> preferredPayloadTypes = new List<int>();
        int codecIndex = 0;
        while (codecIndex < media.Codecs.Count)
        {
            SdpCodecCapability? codec = media.Codecs[codecIndex];
            if (codec == null || codec.FormatParameters == null || codec.RtcpFeedbackTypes == null || codec.RtcpFeedback == null)
            {
                throw new ArgumentException("Codec capabilities and their collections must be initialized.", "options");
            }
            if (!SdpCommonAttributeCodecCore.IsTokenValue(codec.EncodingName) || codec.ClockRate <= 0 || codec.HasChannelCount && codec.ChannelCount <= 0)
            {
                throw new ArgumentException("Codec capabilities require a valid encoding name, clock rate, and channel count.", "options");
            }
            if (codec.HasPreferredPayloadType)
            {
                if (codec.PreferredPayloadType < 0 || codec.PreferredPayloadType > 127)
                {
                    throw new ArgumentOutOfRangeException("options");
                }
                if (preferredPayloadTypes.Contains(codec.PreferredPayloadType))
                {
                    throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationPayloadAllocationFailed, null);
                }
                preferredPayloadTypes.Add(codec.PreferredPayloadType);
            }
            codecIndex = codecIndex + 1;
        }
    }

    internal static void ValidateHeaderExtensionCapabilities(List<SdpHeaderExtension> extensions)
    {
        List<int> preferredIds = new List<int>();
        int index = 0;
        while (index < extensions.Count)
        {
            SdpHeaderExtension? extension = extensions[index];
            if (extension == null || extension.Uri == null || extension.Uri.Length == 0 || extension.ExtensionAttributes == null)
            {
                throw new ArgumentException("Header-extension capabilities must contain URI and attribute values.", "options");
            }
            if (!IsDefinedDirection(extension.Direction))
            {
                throw new ArgumentOutOfRangeException("options");
            }
            if (extension.HasPreferredId)
            {
                if (!SdpExtMapServiceCore.IsUsableIdentifier(extension.PreferredId) || preferredIds.Contains(extension.PreferredId))
                {
                    throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationExtMapAllocationFailed, null);
                }
                preferredIds.Add(extension.PreferredId);
            }
            index = index + 1;
        }
    }

    internal static void ValidateRequiredInitialPolicies(SdpNegotiationOptions options, SdpEndpointCapabilities capabilities)
    {
        int mediaIndex = 0;
        while (mediaIndex < capabilities.Media.Count)
        {
            SdpMediaCapability media = capabilities.Media[mediaIndex];
            bool rtp = SdpRtpValidationCore.IsRtpProtocol(media.Protocols[0]);
            bool sctp = SdpDataChannelValidationCore.IsModernSctpProtocol(media.Protocols[0]) || SdpDataChannelValidationCore.IsLegacySctpProtocol(media.Protocols[0]);
            if (options.RequireRtcpMux && rtp && !media.SupportsRtcpMux && !media.SupportsRtcpMuxOnly)
            {
                throw new ArgumentException("Every RTP media capability must support required RTCP multiplexing.", "options");
            }
            SdpIceDescription? ice = media.Ice == null ? capabilities.Ice : media.Ice;
            if (options.RequireIce && (ice == null || ice.Credentials == null))
            {
                throw new ArgumentException("Every media capability must supply required ICE credentials.", "options");
            }
            SdpDtlsDescription? dtls = media.Dtls == null ? capabilities.Dtls : media.Dtls;
            bool hasFingerprint = dtls != null && dtls.Fingerprints.Count > 0 || media.Dtls == null && capabilities.Dtls == null && capabilities.Fingerprints.Count > 0;
            if (options.RequireDtls && !hasFingerprint)
            {
                throw new ArgumentException("Every media capability must supply a required DTLS fingerprint.", "options");
            }
            if (sctp && !capabilities.SupportsDataChannels)
            {
                throw new ArgumentException("SCTP media requires endpoint data-channel support.", "options");
            }
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static bool IsDefinedDirection(SdpDirection direction)
    {
        return direction == SdpDirection.SendReceive || direction == SdpDirection.SendOnly || direction == SdpDirection.ReceiveOnly || direction == SdpDirection.Inactive;
    }

    internal static void ValidateAnswerArguments(SdpSessionDescription offer, SdpSessionDescription sessionTemplate, SdpNegotiationOptions options)
    {
        if (sessionTemplate.MediaDescriptions == null || sessionTemplate.MediaDescriptions.Count != 0)
        {
            throw new ArgumentException("An answer session template must not contain media descriptions.", "sessionTemplate");
        }
        if (sessionTemplate.Origin == null || sessionTemplate.Timings == null || sessionTemplate.Timings.Count == 0 || !sessionTemplate.HasSessionName)
        {
            throw new ArgumentException("An answer session template requires origin, session-name, and timing fields.", "sessionTemplate");
        }
        SdpSessionFactoryCore.ValidateOrigin(sessionTemplate.Origin);
        SdpSessionFactoryCore.ValidateSessionName(sessionTemplate.SessionName);
        int timingIndex = 0;
        while (timingIndex < sessionTemplate.Timings.Count)
        {
            SdpSessionFactoryCore.ValidateTiming(sessionTemplate.Timings[timingIndex]);
            timingIndex = timingIndex + 1;
        }
        if (sessionTemplate.Connection != null)
        {
            SdpSessionFactoryCore.ValidateConnection(sessionTemplate.Connection);
        }
        if (options.Role != SdpNegotiationRole.Unknown && options.Role != SdpNegotiationRole.Answerer)
        {
            throw new ArgumentException("Answer creation requires the answerer role.", "options");
        }
        if (options.CodecMatchMode != SdpCodecMatchMode.Strict && options.CodecMatchMode != SdpCodecMatchMode.Compatible)
        {
            throw new ArgumentOutOfRangeException("options");
        }
        if (options.MaximumDiagnosticCount <= 0)
        {
            throw new ArgumentOutOfRangeException("options");
        }
        SdpEndpointCapabilities? capabilities = options.LocalCapabilities;
        if (capabilities == null)
        {
            throw new ArgumentException("Answer creation requires local endpoint capabilities.", "options");
        }
        ValidateEndpointCapabilities(capabilities, sessionTemplate);
        if (options.RequireBundle && !capabilities.SupportsBundle)
        {
            throw new ArgumentException("The required BUNDLE policy is not supported by the local capabilities.", "options");
        }
        ValidateOfferedDescription(offer);
    }

    internal static void ValidateOfferedDescription(SdpSessionDescription offer)
    {
        if (offer.Attributes == null || offer.MediaDescriptions == null || offer.MediaDescriptions.Count == 0)
        {
            throw new ArgumentException("An offer must contain initialized attributes and at least one media description.", "offer");
        }
        int mediaIndex = 0;
        while (mediaIndex < offer.MediaDescriptions.Count)
        {
            SdpMediaDescription? media = offer.MediaDescriptions[mediaIndex];
            if (media == null || media.Formats == null || media.Connections == null || media.Attributes == null)
            {
                throw new ArgumentException("Every offered media description and its collections must be initialized.", "offer");
            }
            if (!SdpCommonAttributeCodecCore.IsTokenValue(media.MediaType) || !SdpCoreValidationCore.IsValidProtocolToken(media.Protocol) || media.Port < 0 || media.Port > 65535)
            {
                throw new ArgumentException("Every offered media description requires a valid media type, port, and protocol.", "offer");
            }
            SdpSessionFactoryCore.ValidateFormats(media.Formats);
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static SdpNegotiationResult CreateInitialOffer(SdpSessionDescription sessionTemplate, SdpNegotiationOptions options)
    {
        SdpEndpointCapabilities capabilities = options.LocalCapabilities == null ? new SdpEndpointCapabilities() : options.LocalCapabilities;
        SdpSessionDescription description = SdpCloneCore.CopyDescription(sessionTemplate);
        SdpSemanticComparisonCore.ClearRawFormatting(description);
        description.LineEnding = SdpLineEnding.CrLf;
        SdpNegotiationResult result = new SdpNegotiationResult();
        result.AppliedOptions = CopyInitialOfferOptions(options);
        List<int> bundleMediaIndexes = new List<int>();
        List<int> bundleOnlyMediaIndexes = new List<int>();
        List<string> mediaIds = AllocateInitialMediaIds(capabilities.Media);
        int taggedMediaIndex = -1;
        int mediaIndex = 0;
        while (mediaIndex < capabilities.Media.Count)
        {
            SdpMediaCapability capability = capabilities.Media[mediaIndex];
            List<int> payloadTypes = AllocateInitialPayloadTypes(capability.Codecs);
            SdpMediaDescription media = CreateOfferMedia(capability, capabilities, mediaIds[mediaIndex], payloadTypes);
            description.MediaDescriptions.Add(media);
            if (capabilities.SupportsBundle)
            {
                bundleMediaIndexes.Add(mediaIndex);
                if (capability.BundleOnly)
                {
                    bundleOnlyMediaIndexes.Add(mediaIndex);
                }
                else if (taggedMediaIndex < 0)
                {
                    taggedMediaIndex = mediaIndex;
                }
            }
            result.Media.Add(CreateInitialMediaResult(description, capability, mediaIndex, mediaIds[mediaIndex], payloadTypes));
            mediaIndex = mediaIndex + 1;
        }
        if (capabilities.SupportsBundle)
        {
            if (taggedMediaIndex < 0)
            {
                throw new ArgumentException("A BUNDLE offer requires at least one non-bundle-only media capability.", "options");
            }
            SdpBundleGroup group = SdpBundleServiceCore.CreateInitialBundleOffer(description, bundleMediaIndexes, taggedMediaIndex, bundleOnlyMediaIndexes);
            result.BundleGroups.Add(group);
            SdpBundleTransport? transport = SdpBundleServiceCore.ResolveBundleTransport(description, taggedMediaIndex);
            if (transport != null)
            {
                result.BundleTransports.Add(transport);
            }
        }
        RefreshInitialMediaResults(description, result);
        result.Description = description;
        result.Success = true;
        return result;
    }

    internal static SdpNegotiationResult CreateAnswer(SdpSessionDescription offer, SdpSessionDescription sessionTemplate, SdpNegotiationOptions options)
    {
        SdpEndpointCapabilities capabilities = options.LocalCapabilities == null ? new SdpEndpointCapabilities() : options.LocalCapabilities;
        SdpSessionDescription answer = SdpCloneCore.CopyDescription(sessionTemplate);
        answer.Timings.Clear();
        SdpCloneCore.CopyTimingList(offer.Timings, answer.Timings);
        SdpSemanticComparisonCore.ClearRawFormatting(answer);
        answer.LineEnding = SdpLineEnding.CrLf;
        SdpNegotiationResult result = new SdpNegotiationResult();
        result.AppliedOptions = CopyAnswerOptions(options);
        List<SdpRejectionReason> rejectionReasons = SelectAnswerRejectionReasons(offer, capabilities, options);
        int mediaIndex = 0;
        while (mediaIndex < offer.MediaDescriptions.Count)
        {
            SdpMediaDescription offeredMedia = offer.MediaDescriptions[mediaIndex];
            SdpDirection offeredDirection = SdpQueryServiceCore.ResolveEffectiveDirection(offer, mediaIndex);
            SdpMediaCapability? capability = mediaIndex < capabilities.Media.Count ? capabilities.Media[mediaIndex] : null;
            SdpRejectionReason reason = rejectionReasons[mediaIndex];
            List<SdpCodec> selectedCodecs = new List<SdpCodec>();
            if (reason == SdpRejectionReason.Unknown && capability != null && SdpCodecServiceCore.IsRtpProtocol(offeredMedia.Protocol))
            {
                selectedCodecs = SelectAnswerCodecs(offeredMedia, capability, options);
            }
            EnsureAnswerRejectionAllowed(reason, options);
            SdpDirection answerDirection = reason == SdpRejectionReason.Unknown && capability != null ? SdpExtMapServiceCore.SelectAnswerDirection(offeredDirection, capability.Direction) : SdpDirection.Inactive;
            SdpMediaDescription answerMedia = CreatePositionalAnswerMedia(offeredMedia, capability, capabilities, answerDirection, reason);
            AddAnswerMediaId(answerMedia, offeredMedia);
            List<SdpPayloadMapping> payloadMappings = new List<SdpPayloadMapping>();
            if (reason == SdpRejectionReason.Unknown && selectedCodecs.Count > 0 && capability != null)
            {
                payloadMappings = ApplyAnswerCodecs(answerMedia, selectedCodecs, capability, mediaIndex, options);
            }
            List<SdpHeaderExtensionMapping> extensionMappings = new List<SdpHeaderExtensionMapping>();
            if (reason == SdpRejectionReason.Unknown && capability != null)
            {
                int transportMediaIndex = SdpBundleServiceCore.IsBundleOnlyMedia(offeredMedia) ? FindAnswerBundleTaggedMediaIndex(offer, mediaIndex, rejectionReasons) : mediaIndex;
                SdpOfferAnswerTransportCore.ApplyAnswerAttributes(answer, offer, mediaIndex, transportMediaIndex, answerMedia, capability, capabilities, options, extensionMappings);
            }
            SdpSctpDescription? negotiatedSctp = null;
            if (reason == SdpRejectionReason.Unknown && capability != null)
            {
                negotiatedSctp = SdpOfferAnswerContentCore.ApplyAnswerContent(offeredMedia, answerMedia, capability, capabilities, answerDirection, payloadMappings);
            }
            answer.MediaDescriptions.Add(answerMedia);
            SdpMediaNegotiation mediaResult = CreatePositionalAnswerResult(answer, offeredMedia, mediaIndex, offeredDirection, answerDirection, reason);
            PopulateAnswerCodecResult(mediaResult, answerMedia, payloadMappings);
            PopulateAnswerExtensionResult(mediaResult, extensionMappings);
            if (negotiatedSctp != null)
            {
                mediaResult.Sctp = negotiatedSctp;
            }
            result.Media.Add(mediaResult);
            if (mediaResult.Rejection != null)
            {
                result.Rejections.Add(mediaResult.Rejection);
            }
            mediaIndex = mediaIndex + 1;
        }
        ApplyAnswerGroups(offer, answer, result, capabilities);
        RefreshInitialMediaResults(answer, result);
        result.Description = answer;
        PopulateRejectionDiagnostics(result, offer, options.MaximumDiagnosticCount);
        result.Success = true;
        return result;
    }

    internal static List<SdpRejectionReason> SelectAnswerRejectionReasons(SdpSessionDescription offer, SdpEndpointCapabilities capabilities, SdpNegotiationOptions options)
    {
        List<SdpRejectionReason> reasons = new List<SdpRejectionReason>();
        int mediaIndex = 0;
        while (mediaIndex < offer.MediaDescriptions.Count)
        {
            SdpMediaDescription offeredMedia = offer.MediaDescriptions[mediaIndex];
            SdpMediaCapability? capability = mediaIndex < capabilities.Media.Count ? capabilities.Media[mediaIndex] : null;
            bool bundledBundleOnly = capabilities.SupportsBundle && SdpBundleServiceCore.IsBundleOnlyMedia(offeredMedia) && SdpBundleServiceCore.FindBundleGroupForMedia(offer, mediaIndex) != null;
            SdpRejectionReason reason = SelectPositionalRejectionReason(offeredMedia, capability, bundledBundleOnly);
            if (reason == SdpRejectionReason.Unknown && capability != null && SdpCodecServiceCore.IsRtpProtocol(offeredMedia.Protocol) && SelectAnswerCodecs(offeredMedia, capability, options).Count == 0)
            {
                reason = SdpRejectionReason.NoCompatibleCodec;
            }
            reasons.Add(reason);
            mediaIndex = mediaIndex + 1;
        }
        mediaIndex = 0;
        while (mediaIndex < offer.MediaDescriptions.Count)
        {
            SdpMediaCapability? capability = mediaIndex < capabilities.Media.Count ? capabilities.Media[mediaIndex] : null;
            if (reasons[mediaIndex] == SdpRejectionReason.Unknown && capability != null && !SdpBundleServiceCore.IsBundleOnlyMedia(offer.MediaDescriptions[mediaIndex]))
            {
                reasons[mediaIndex] = SdpOfferAnswerTransportCore.SelectRejectionReason(offer, mediaIndex, mediaIndex, capability, capabilities, options);
                if (reasons[mediaIndex] == SdpRejectionReason.Unknown)
                {
                    reasons[mediaIndex] = SdpOfferAnswerContentCore.SelectDataChannelRejectionReason(offer.MediaDescriptions[mediaIndex], capability, capabilities);
                }
            }
            mediaIndex = mediaIndex + 1;
        }
        mediaIndex = 0;
        while (mediaIndex < offer.MediaDescriptions.Count)
        {
            SdpMediaCapability? capability = mediaIndex < capabilities.Media.Count ? capabilities.Media[mediaIndex] : null;
            if (reasons[mediaIndex] == SdpRejectionReason.Unknown && capability != null && SdpBundleServiceCore.IsBundleOnlyMedia(offer.MediaDescriptions[mediaIndex]))
            {
                int transportMediaIndex = FindAnswerBundleTaggedMediaIndex(offer, mediaIndex, reasons);
                if (transportMediaIndex >= 0)
                {
                    reasons[mediaIndex] = SdpOfferAnswerTransportCore.SelectRejectionReason(offer, mediaIndex, transportMediaIndex, capability, capabilities, options);
                    if (reasons[mediaIndex] == SdpRejectionReason.Unknown)
                    {
                        reasons[mediaIndex] = SdpOfferAnswerContentCore.SelectDataChannelRejectionReason(offer.MediaDescriptions[mediaIndex], capability, capabilities);
                    }
                }
            }
            mediaIndex = mediaIndex + 1;
        }
        ApplyAnswerBundlePolicy(offer, capabilities, options, reasons);
        return reasons;
    }

    internal static SdpRejectionReason SelectPositionalRejectionReason(SdpMediaDescription offeredMedia, SdpMediaCapability? capability, bool bundledBundleOnly)
    {
        if (offeredMedia.Port == 0 && !bundledBundleOnly)
        {
            return SdpRejectionReason.OfferedAsRejected;
        }
        if (capability == null || !string.Equals(offeredMedia.MediaType, capability.MediaType, StringComparison.OrdinalIgnoreCase))
        {
            return SdpRejectionReason.UnsupportedMedia;
        }
        if (!ContainsCompatibleProtocol(capability.Protocols, offeredMedia.Protocol))
        {
            return SdpRejectionReason.UnsupportedProtocol;
        }
        return SdpRejectionReason.Unknown;
    }

    internal static int FindAnswerBundleTaggedMediaIndex(SdpSessionDescription offer, int mediaIndex, List<SdpRejectionReason> reasons)
    {
        SdpBundleGroup? group = SdpBundleServiceCore.FindBundleGroupForMedia(offer, mediaIndex);
        if (group == null)
        {
            return -1;
        }
        List<int> offeredMediaIndexes = SdpBundleServiceCore.ResolveOfferedBundleMediaIndexes(offer, group);
        int memberIndex = 0;
        while (memberIndex < offeredMediaIndexes.Count)
        {
            int memberMediaIndex = offeredMediaIndexes[memberIndex];
            SdpMediaDescription offeredMedia = offer.MediaDescriptions[memberMediaIndex];
            if (reasons[memberMediaIndex] == SdpRejectionReason.Unknown && offeredMedia.Port != 0 && !SdpBundleServiceCore.IsBundleOnlyMedia(offeredMedia))
            {
                return memberMediaIndex;
            }
            memberIndex = memberIndex + 1;
        }
        return -1;
    }

    internal static void ApplyAnswerBundlePolicy(SdpSessionDescription offer, SdpEndpointCapabilities capabilities, SdpNegotiationOptions options, List<SdpRejectionReason> reasons)
    {
        List<int> bundledMediaIndexes = new List<int>();
        List<SdpBundleGroup> groups = SdpBundleServiceCore.ResolveBundleGroups(offer);
        int groupIndex = 0;
        while (groupIndex < groups.Count)
        {
            List<int> offeredMediaIndexes = SdpBundleServiceCore.ResolveOfferedBundleMediaIndexes(offer, groups[groupIndex]);
            int acceptedCount = 0;
            bool hasTaggedMedia = false;
            int memberIndex = 0;
            while (memberIndex < offeredMediaIndexes.Count)
            {
                int memberMediaIndex = offeredMediaIndexes[memberIndex];
                if (!bundledMediaIndexes.Contains(memberMediaIndex))
                {
                    bundledMediaIndexes.Add(memberMediaIndex);
                }
                if (reasons[memberMediaIndex] == SdpRejectionReason.Unknown)
                {
                    acceptedCount = acceptedCount + 1;
                    SdpMediaDescription offeredMedia = offer.MediaDescriptions[memberMediaIndex];
                    if (offeredMedia.Port != 0 && !SdpBundleServiceCore.IsBundleOnlyMedia(offeredMedia))
                    {
                        hasTaggedMedia = true;
                    }
                }
                memberIndex = memberIndex + 1;
            }
            bool rejectAcceptedMembers = capabilities.SupportsBundle && acceptedCount > 0 && (!hasTaggedMedia || !options.AllowBundleSubset && acceptedCount != offeredMediaIndexes.Count);
            if (rejectAcceptedMembers)
            {
                memberIndex = 0;
                while (memberIndex < offeredMediaIndexes.Count)
                {
                    int memberMediaIndex = offeredMediaIndexes[memberIndex];
                    if (reasons[memberMediaIndex] == SdpRejectionReason.Unknown)
                    {
                        reasons[memberMediaIndex] = SdpRejectionReason.BundleRequirementsNotMet;
                    }
                    memberIndex = memberIndex + 1;
                }
            }
            groupIndex = groupIndex + 1;
        }

        int mediaIndex = 0;
        while (mediaIndex < offer.MediaDescriptions.Count)
        {
            bool unavailableBundleOnly = SdpBundleServiceCore.IsBundleOnlyMedia(offer.MediaDescriptions[mediaIndex]) && (!capabilities.SupportsBundle || !bundledMediaIndexes.Contains(mediaIndex));
            if (unavailableBundleOnly && reasons[mediaIndex] == SdpRejectionReason.OfferedAsRejected)
            {
                reasons[mediaIndex] = SdpRejectionReason.BundleRequirementsNotMet;
            }
            if (options.RequireBundle && reasons[mediaIndex] == SdpRejectionReason.Unknown && !bundledMediaIndexes.Contains(mediaIndex))
            {
                reasons[mediaIndex] = SdpRejectionReason.BundleRequirementsNotMet;
            }
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void AddAnswerMediaId(SdpMediaDescription answerMedia, SdpMediaDescription offeredMedia)
    {
        SdpMediaIdAttribute? offeredMediaId = SdpGroupServiceCore.ResolveMediaId(offeredMedia);
        if (offeredMediaId == null)
        {
            return;
        }
        SdpMediaIdAttribute mediaId = new SdpMediaIdAttribute();
        mediaId.Id = offeredMediaId.Id;
        SdpAttributeEncodeResult encoded = SdpGroupAttributeCodecCore.EncodeMediaId(mediaId);
        if (!encoded.Success || encoded.Attribute == null)
        {
            throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
        }
        answerMedia.Attributes.Add(encoded.Attribute);
    }

    internal static void ApplyAnswerGroups(SdpSessionDescription offer, SdpSessionDescription answer, SdpNegotiationResult result, SdpEndpointCapabilities capabilities)
    {
        List<SdpGroupAttribute> offeredGroups = SdpGroupServiceCore.ResolveGroups(offer);
        int offeredBundleGroupIndex = 0;
        int groupIndex = 0;
        while (groupIndex < offeredGroups.Count)
        {
            SdpGroupAttribute offeredGroup = offeredGroups[groupIndex];
            List<int> acceptedMediaIndexes = FindAcceptedGroupMediaIndexes(offer, result, offeredGroup);
            if (offeredGroup.Semantic == SdpGroupSemantic.Bundle)
            {
                if (capabilities.SupportsBundle && acceptedMediaIndexes.Count > 0)
                {
                    SdpBundleGroup? answerGroup = SdpBundleServiceCore.CreateInitialBundleAnswer(offer, answer, offeredBundleGroupIndex, acceptedMediaIndexes);
                    if (answerGroup != null)
                    {
                        result.BundleGroups.Add(answerGroup);
                        int taggedMediaIndex = SdpGroupServiceCore.FindMediaIndex(answer, answerGroup.TaggedMediaId);
                        SdpBundleTransport? transport = taggedMediaIndex < 0 ? null : SdpBundleServiceCore.ResolveBundleTransport(answer, taggedMediaIndex);
                        if (transport != null)
                        {
                            result.BundleTransports.Add(transport);
                        }
                    }
                }
                offeredBundleGroupIndex = offeredBundleGroupIndex + 1;
            }
            else if (offeredGroup.Semantic != SdpGroupSemantic.Unknown)
            {
                List<string> acceptedMediaIds = new List<string>();
                int acceptedIndex = 0;
                while (acceptedIndex < acceptedMediaIndexes.Count)
                {
                    int mediaIndex = acceptedMediaIndexes[acceptedIndex];
                    SdpMediaIdAttribute? mediaId = SdpGroupServiceCore.ResolveMediaId(offer.MediaDescriptions[mediaIndex]);
                    if (mediaId != null)
                    {
                        acceptedMediaIds.Add(mediaId.Id);
                    }
                    acceptedIndex = acceptedIndex + 1;
                }
                SdpGroupAttribute answerGroup = SdpGroupServiceCore.CreateGroup(offeredGroup.Semantic, offeredGroup.SemanticToken, acceptedMediaIds);
                SdpGroupServiceCore.AddGroup(answer, answerGroup);
            }
            groupIndex = groupIndex + 1;
        }
    }

    internal static List<int> FindAcceptedGroupMediaIndexes(SdpSessionDescription offer, SdpNegotiationResult result, SdpGroupAttribute group)
    {
        List<int> accepted = new List<int>();
        int tagIndex = 0;
        while (tagIndex < group.IdentificationTags.Count)
        {
            int mediaIndex = SdpBundleServiceCore.FindUniqueMediaIndexById(offer, group.IdentificationTags[tagIndex]);
            if (mediaIndex >= 0 && result.Media[mediaIndex].Accepted && !accepted.Contains(mediaIndex))
            {
                accepted.Add(mediaIndex);
            }
            tagIndex = tagIndex + 1;
        }
        return accepted;
    }

    internal static bool ContainsCompatibleProtocol(List<string> protocols, string offeredProtocol)
    {
        int protocolIndex = 0;
        while (protocolIndex < protocols.Count)
        {
            if (string.Equals(protocols[protocolIndex], offeredProtocol, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            protocolIndex = protocolIndex + 1;
        }
        return false;
    }

    internal static List<SdpCodec> SelectAnswerCodecs(SdpMediaDescription offeredMedia, SdpMediaCapability capability, SdpNegotiationOptions options)
    {
        SdpMediaDescription sourceMedia = SdpCloneCore.CopyMedia(offeredMedia);
        List<SdpCodec> offeredCodecs = SdpCodecServiceCore.ResolveMediaCodecs(sourceMedia);
        List<SdpCodec> selected = new List<SdpCodec>();
        int codecIndex = 0;
        while (codecIndex < offeredCodecs.Count)
        {
            SdpCodec codec = offeredCodecs[codecIndex];
            if (FindAnswerCodecCapability(codec, capability.Codecs, options.CodecMatchMode) != null)
            {
                selected.Add(codec);
            }
            codecIndex = codecIndex + 1;
        }
        PruneAnswerAssociatedCodecs(selected);
        if (options.PreferLocalCodecOrder)
        {
            selected = OrderAnswerCodecs(selected, capability.Codecs, options.CodecMatchMode);
        }
        return selected;
    }

    internal static SdpCodecCapability? FindAnswerCodecCapability(SdpCodec codec, List<SdpCodecCapability> capabilities, SdpCodecMatchMode matchMode)
    {
        SdpCodecCapability? result = null;
        int resultPreference = 0;
        int capabilityIndex = 0;
        while (capabilityIndex < capabilities.Count)
        {
            SdpCodecCapability capability = capabilities[capabilityIndex];
            SdpCodecCapability comparable = CreateAnswerFormatCapability(codec, capability);
            if (SdpCodecServiceCore.IsCodecMatch(codec, comparable, matchMode) && (result == null || capability.Preference < resultPreference))
            {
                result = capability;
                resultPreference = capability.Preference;
            }
            capabilityIndex = capabilityIndex + 1;
        }
        return result;
    }

    internal static SdpCodecCapability CreateAnswerFormatCapability(SdpCodec codec, SdpCodecCapability source)
    {
        SdpCodecCapability result = new SdpCodecCapability();
        result.MediaKind = source.MediaKind;
        result.MediaType = source.MediaType;
        result.EncodingName = source.EncodingName;
        result.ClockRate = source.ClockRate;
        result.HasPreferredPayloadType = source.HasPreferredPayloadType;
        result.PreferredPayloadType = source.PreferredPayloadType;
        result.HasChannelCount = source.HasChannelCount;
        result.ChannelCount = source.ChannelCount;
        result.Preference = source.Preference;
        int parameterIndex = 0;
        while (parameterIndex < source.FormatParameters.Count)
        {
            result.FormatParameters.Add(source.FormatParameters[parameterIndex]);
            parameterIndex = parameterIndex + 1;
        }
        int feedbackIndex = 0;
        while (feedbackIndex < codec.RtcpFeedback.Count)
        {
            SdpRtcpFeedbackAttribute feedback = codec.RtcpFeedback[feedbackIndex];
            string feedbackValue = feedback.HasParameter ? feedback.Type + " " + feedback.Parameter : feedback.Type;
            result.RtcpFeedbackTypes.Add(feedbackValue);
            feedbackIndex = feedbackIndex + 1;
        }
        return result;
    }

    internal static List<SdpCodec> OrderAnswerCodecs(List<SdpCodec> codecs, List<SdpCodecCapability> capabilities, SdpCodecMatchMode matchMode)
    {
        List<SdpCodec> result = new List<SdpCodec>();
        List<int> preferences = new List<int>();
        List<int> capabilityIndexes = new List<int>();
        int codecIndex = 0;
        while (codecIndex < codecs.Count)
        {
            SdpCodec codec = codecs[codecIndex];
            SdpCodecCapability? matched = FindAnswerCodecCapability(codec, capabilities, matchMode);
            int preference = matched == null ? 0 : matched.Preference;
            int matchedIndex = matched == null ? capabilities.Count : capabilities.IndexOf(matched);
            int insertionIndex = 0;
            while (insertionIndex < result.Count && (preferences[insertionIndex] < preference || preferences[insertionIndex] == preference && capabilityIndexes[insertionIndex] <= matchedIndex))
            {
                insertionIndex = insertionIndex + 1;
            }
            result.Insert(insertionIndex, codec);
            preferences.Insert(insertionIndex, preference);
            capabilityIndexes.Insert(insertionIndex, matchedIndex);
            codecIndex = codecIndex + 1;
        }
        return result;
    }

    internal static void PruneAnswerAssociatedCodecs(List<SdpCodec> codecs)
    {
        bool changed = true;
        while (changed)
        {
            changed = false;
            int codecIndex = 0;
            while (codecIndex < codecs.Count)
            {
                if (!AreAnswerCodecReferencesSelected(codecs[codecIndex], codecs))
                {
                    codecs.RemoveAt(codecIndex);
                    changed = true;
                }
                else
                {
                    codecIndex = codecIndex + 1;
                }
            }
        }
    }

    internal static bool AreAnswerCodecReferencesSelected(SdpCodec codec, List<SdpCodec> codecs)
    {
        if (SdpAdditionalCodecCore.IsRetransmissionEncoding(codec.EncodingName))
        {
            int associatedPayloadType = SdpAdditionalCodecCore.ReadIntegerParameter(SdpCodecServiceCore.CollectCodecParameters(codec), "apt", -1);
            return associatedPayloadType >= 0 && ContainsAnswerCodecPayload(codecs, associatedPayloadType);
        }
        if (SdpAdditionalCodecCore.IsRedEncoding(codec.EncodingName) && codec.FormatParameters.Count > 0)
        {
            string[] payloads = SdpAdditionalCodecCore.ReadRawCodecFormat(codec).Split('/');
            int payloadIndex = 0;
            while (payloadIndex < payloads.Length)
            {
                SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(payloads[payloadIndex].AsSpan());
                if (!payload.Success || payload.Int32Value == codec.PayloadType || !ContainsAnswerCodecPayload(codecs, payload.Int32Value))
                {
                    return false;
                }
                payloadIndex = payloadIndex + 1;
            }
        }
        return true;
    }

    internal static bool ContainsAnswerCodecPayload(List<SdpCodec> codecs, int payloadType)
    {
        int codecIndex = 0;
        while (codecIndex < codecs.Count)
        {
            if (codecs[codecIndex].PayloadType == payloadType)
            {
                return true;
            }
            codecIndex = codecIndex + 1;
        }
        return false;
    }

    internal static List<SdpPayloadMapping> ApplyAnswerCodecs(SdpMediaDescription answerMedia, List<SdpCodec> codecs, SdpMediaCapability mediaCapability, int mediaIndex, SdpNegotiationOptions options)
    {
        int codecIndex = 0;
        while (codecIndex < codecs.Count)
        {
            SdpCodecCapability? capability = FindAnswerCodecCapability(codecs[codecIndex], mediaCapability.Codecs, options.CodecMatchMode);
            FilterAnswerCodecFeedback(codecs[codecIndex], capability);
            codecIndex = codecIndex + 1;
        }
        NormalizeAnswerWildcardFeedback(codecs);
        List<SdpPayloadMapping> mappings = SdpCodecServiceCore.AllocatePayloadTypes(codecs, mediaIndex, options.PreservePayloadTypes);
        SdpEffectiveMedia graph = new SdpEffectiveMedia();
        graph.MediaIndex = mediaIndex;
        graph.Codecs = codecs;
        SdpCodecServiceCore.ValidatePayloadRemap(graph, mappings);
        SdpCodecServiceCore.RemapPayloadTypes(graph, mappings);
        answerMedia.Formats.Clear();
        codecIndex = 0;
        while (codecIndex < codecs.Count)
        {
            answerMedia.Formats.Add(codecs[codecIndex].PayloadType.ToString(CultureInfo.InvariantCulture));
            AddAnswerCodecAttributes(answerMedia, codecs[codecIndex]);
            codecIndex = codecIndex + 1;
        }
        SdpCommonAttribute? packetTime = codecs[0].PacketTime;
        if (packetTime != null)
        {
            AddUniqueAnswerAttribute(answerMedia.Attributes, packetTime);
        }
        SdpCommonAttribute? maximumPacketTime = codecs[0].MaximumPacketTime;
        if (maximumPacketTime != null)
        {
            AddUniqueAnswerAttribute(answerMedia.Attributes, maximumPacketTime);
        }
        return mappings;
    }

    internal static void FilterAnswerCodecFeedback(SdpCodec codec, SdpCodecCapability? capability)
    {
        int feedbackIndex = codec.RtcpFeedback.Count - 1;
        while (feedbackIndex >= 0)
        {
            if (capability == null || !AnswerCapabilitySupportsFeedback(capability, codec.RtcpFeedback[feedbackIndex]))
            {
                codec.RtcpFeedback.RemoveAt(feedbackIndex);
            }
            feedbackIndex = feedbackIndex - 1;
        }
    }

    internal static bool AnswerCapabilitySupportsFeedback(SdpCodecCapability capability, SdpRtcpFeedbackAttribute offered)
    {
        string value = offered.HasParameter ? offered.Type + " " + offered.Parameter : offered.Type;
        int typeIndex = 0;
        while (typeIndex < capability.RtcpFeedbackTypes.Count)
        {
            if (capability.RtcpFeedbackTypes[typeIndex] == value)
            {
                return true;
            }
            typeIndex = typeIndex + 1;
        }
        int feedbackIndex = 0;
        while (feedbackIndex < capability.RtcpFeedback.Count)
        {
            SdpRtcpFeedbackAttribute supported = capability.RtcpFeedback[feedbackIndex];
            if (supported.Type == offered.Type && supported.HasParameter == offered.HasParameter && (!offered.HasParameter || supported.Parameter == offered.Parameter))
            {
                return true;
            }
            feedbackIndex = feedbackIndex + 1;
        }
        return false;
    }

    internal static void NormalizeAnswerWildcardFeedback(List<SdpCodec> codecs)
    {
        int codecIndex = 0;
        while (codecIndex < codecs.Count)
        {
            SdpCodec codec = codecs[codecIndex];
            int feedbackIndex = 0;
            while (feedbackIndex < codec.RtcpFeedback.Count)
            {
                SdpRtcpFeedbackAttribute feedback = codec.RtcpFeedback[feedbackIndex];
                if (feedback.PayloadType == "*" && !AllAnswerCodecsSupportWildcardFeedback(codecs, feedback))
                {
                    feedback.PayloadType = codec.PayloadType.ToString(CultureInfo.InvariantCulture);
                }
                feedbackIndex = feedbackIndex + 1;
            }
            codecIndex = codecIndex + 1;
        }
    }

    internal static bool AllAnswerCodecsSupportWildcardFeedback(List<SdpCodec> codecs, SdpRtcpFeedbackAttribute expected)
    {
        int codecIndex = 0;
        while (codecIndex < codecs.Count)
        {
            bool found = false;
            int feedbackIndex = 0;
            while (feedbackIndex < codecs[codecIndex].RtcpFeedback.Count && !found)
            {
                SdpRtcpFeedbackAttribute candidate = codecs[codecIndex].RtcpFeedback[feedbackIndex];
                found = candidate.PayloadType == "*" && candidate.Type == expected.Type && candidate.HasParameter == expected.HasParameter && (!expected.HasParameter || candidate.Parameter == expected.Parameter);
                feedbackIndex = feedbackIndex + 1;
            }
            if (!found)
            {
                return false;
            }
            codecIndex = codecIndex + 1;
        }
        return true;
    }

    internal static void AddAnswerCodecAttributes(SdpMediaDescription answerMedia, SdpCodec codec)
    {
        if (codec.RtpMap != null)
        {
            AddUniqueAnswerAttribute(answerMedia.Attributes, codec.RtpMap);
        }
        int formatIndex = 0;
        while (formatIndex < codec.FormatParameters.Count)
        {
            AddUniqueAnswerAttribute(answerMedia.Attributes, codec.FormatParameters[formatIndex]);
            formatIndex = formatIndex + 1;
        }
        int feedbackIndex = 0;
        while (feedbackIndex < codec.RtcpFeedback.Count)
        {
            AddUniqueAnswerAttribute(answerMedia.Attributes, codec.RtcpFeedback[feedbackIndex]);
            feedbackIndex = feedbackIndex + 1;
        }
    }

    internal static void AddUniqueAnswerAttribute(List<SdpAttribute> attributes, object value)
    {
        SdpAttributeEncodeResult encoded = SdpAttributeEncoderCore.EncodeAttribute(value);
        if (!encoded.Success || encoded.Attribute == null)
        {
            throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationNoCompatibleCodec, null);
        }
        int attributeIndex = 0;
        while (attributeIndex < attributes.Count)
        {
            SdpAttribute existing = attributes[attributeIndex];
            if (existing.Kind == encoded.Attribute.Kind && string.Equals(existing.Name, encoded.Attribute.Name, StringComparison.OrdinalIgnoreCase) && existing.Value == encoded.Attribute.Value)
            {
                return;
            }
            attributeIndex = attributeIndex + 1;
        }
        encoded.Attribute.RawText = string.Empty;
        encoded.Attribute.Location = new SdpSourceLocation();
        attributes.Add(encoded.Attribute);
    }

    internal static void PopulateAnswerCodecResult(SdpMediaNegotiation result, SdpMediaDescription answerMedia, List<SdpPayloadMapping> mappings)
    {
        if (!result.Accepted || !SdpCodecServiceCore.IsRtpProtocol(answerMedia.Protocol))
        {
            return;
        }
        result.Codecs = SdpCodecServiceCore.ResolveMediaCodecs(answerMedia);
        int mappingIndex = 0;
        while (mappingIndex < mappings.Count)
        {
            result.PayloadMappings.Add(mappings[mappingIndex]);
            mappingIndex = mappingIndex + 1;
        }
        if (result.EffectiveMedia != null)
        {
            result.EffectiveMedia.Codecs = result.Codecs;
        }
    }

    internal static void PopulateAnswerExtensionResult(SdpMediaNegotiation result, List<SdpHeaderExtensionMapping> mappings)
    {
        if (!result.Accepted)
        {
            return;
        }
        int mappingIndex = 0;
        while (mappingIndex < mappings.Count)
        {
            result.HeaderExtensionMappings.Add(mappings[mappingIndex]);
            mappingIndex = mappingIndex + 1;
        }
    }

    internal static void EnsureAnswerRejectionAllowed(SdpRejectionReason reason, SdpNegotiationOptions options)
    {
        if (reason == SdpRejectionReason.Unknown || reason == SdpRejectionReason.OfferedAsRejected || options.AllowMediaRejection)
        {
            return;
        }
        string code = SdpDiagnosticCodes.NegotiationUnsupportedMedia;
        if (reason == SdpRejectionReason.UnsupportedProtocol)
        {
            code = SdpDiagnosticCodes.NegotiationUnsupportedProtocol;
        }
        else if (reason == SdpRejectionReason.NoCompatibleCodec)
        {
            code = SdpDiagnosticCodes.NegotiationNoCompatibleCodec;
        }
        else if (reason == SdpRejectionReason.IncompatibleDirection)
        {
            code = SdpDiagnosticCodes.NegotiationIncompatibleDirection;
        }
        else if (reason == SdpRejectionReason.IceRequirementsNotMet)
        {
            code = SdpDiagnosticCodes.NegotiationIceIncompatible;
        }
        else if (reason == SdpRejectionReason.DtlsRequirementsNotMet)
        {
            code = SdpDiagnosticCodes.NegotiationDtlsIncompatible;
        }
        else if (reason == SdpRejectionReason.BundleRequirementsNotMet)
        {
            code = SdpDiagnosticCodes.NegotiationBundleIncompatible;
        }
        else if (reason == SdpRejectionReason.DataChannelRequirementsNotMet)
        {
            code = SdpDiagnosticCodes.NegotiationDataChannelIncompatible;
        }
        else if (reason == SdpRejectionReason.RtcpMuxRequired)
        {
            code = SdpDiagnosticCodes.NegotiationRtcpMuxIncompatible;
        }
        else if (reason == SdpRejectionReason.RejectedByPolicy)
        {
            code = SdpDiagnosticCodes.NegotiationRejectedByPolicy;
        }
        else if (reason == SdpRejectionReason.InvalidOffer)
        {
            code = SdpDiagnosticCodes.NegotiationInvalidOffer;
        }
        throw new SdpNegotiationException(code, null);
    }

    internal static SdpMediaDescription CreatePositionalAnswerMedia(SdpMediaDescription offeredMedia, SdpMediaCapability? capability, SdpEndpointCapabilities endpoint, SdpDirection direction, SdpRejectionReason reason)
    {
        List<string> formats = new List<string>();
        int formatIndex = 0;
        while (formatIndex < offeredMedia.Formats.Count)
        {
            formats.Add(offeredMedia.Formats[formatIndex]);
            formatIndex = formatIndex + 1;
        }
        int port = reason == SdpRejectionReason.Unknown && capability != null ? capability.Port : 0;
        SdpMediaDescription result = SdpSessionFactoryCore.CreateMedia(offeredMedia.MediaType, port, offeredMedia.Protocol, formats);
        result.MediaKind = offeredMedia.MediaKind == SdpMediaKind.Unknown ? SdpMediaLineParser.ReadMediaKind(offeredMedia.MediaType) : offeredMedia.MediaKind;
        if (reason == SdpRejectionReason.Unknown && capability != null)
        {
            result.HasPortCount = capability.HasPortCount;
            result.PortCount = capability.PortCount;
            SdpConnectionData? connection = capability.Connection == null ? endpoint.Connection : capability.Connection;
            if (connection != null)
            {
                result.Connections.Add(SdpBundleServiceCore.CopyConnection(connection));
            }
        }
        AddDirectionAttribute(result, direction);
        return result;
    }

    internal static SdpMediaNegotiation CreatePositionalAnswerResult(SdpSessionDescription answer, SdpMediaDescription offeredMedia, int mediaIndex, SdpDirection offeredDirection, SdpDirection answerDirection, SdpRejectionReason reason)
    {
        SdpMediaIdAttribute? offeredMediaId = SdpGroupServiceCore.ResolveMediaId(offeredMedia);
        string mediaId = offeredMediaId == null ? string.Empty : offeredMediaId.Id;
        SdpMediaNegotiation result = new SdpMediaNegotiation();
        result.MediaIndex = mediaIndex;
        result.MediaId = mediaId;
        result.MediaKind = offeredMedia.MediaKind == SdpMediaKind.Unknown ? SdpMediaLineParser.ReadMediaKind(offeredMedia.MediaType) : offeredMedia.MediaKind;
        result.MediaType = offeredMedia.MediaType;
        result.Protocol = offeredMedia.Protocol;
        result.TransportMediaIndex = mediaIndex;
        result.Accepted = reason == SdpRejectionReason.Unknown;
        result.OfferedDirection = offeredDirection;
        result.Direction = answerDirection;
        if (reason != SdpRejectionReason.Unknown)
        {
            result.Rejection = CreatePositionalRejection(mediaIndex, mediaId, reason);
        }
        result.EffectiveMedia = SdpQueryServiceCore.ResolveEffectiveMedia(answer, mediaIndex);
        return result;
    }

    internal static SdpRejection CreatePositionalRejection(int mediaIndex, string mediaId, SdpRejectionReason reason)
    {
        SdpRejection result = new SdpRejection();
        result.MediaIndex = mediaIndex;
        result.MediaId = mediaId;
        result.Reason = reason;
        result.Path.Scope = SdpScope.Media;
        result.Path.MediaIndex = mediaIndex;
        result.Path.MediaId = mediaId;
        result.Path.FieldName = 'm';
        if (reason == SdpRejectionReason.Unknown)
        {
            result.ReasonToken = "unknown";
            result.Detail = "The media description was rejected for an unknown or extension-defined reason.";
        }
        else if (reason == SdpRejectionReason.OfferedAsRejected)
        {
            result.ReasonToken = "offered-as-rejected";
            result.Detail = "The offered media description already has a zero port.";
        }
        else if (reason == SdpRejectionReason.UnsupportedMedia)
        {
            result.ReasonToken = "unsupported-media";
            result.Detail = "No positionally corresponding local media capability is available.";
        }
        else if (reason == SdpRejectionReason.UnsupportedProtocol)
        {
            result.ReasonToken = "unsupported-protocol";
            result.Detail = "The offered transport protocol is not supported at this media position.";
        }
        else if (reason == SdpRejectionReason.NoCompatibleCodec)
        {
            result.ReasonToken = "no-compatible-codec";
            result.Detail = "No offered RTP codec is compatible with local capabilities.";
        }
        else if (reason == SdpRejectionReason.IncompatibleDirection)
        {
            result.ReasonToken = "incompatible-direction";
            result.Path.FieldName = 'a';
            result.Path.AttributeName = SdpAttributeNames.SendReceive;
            result.Detail = "The offered and local media directions are incompatible.";
        }
        else if (reason == SdpRejectionReason.IceRequirementsNotMet)
        {
            result.ReasonToken = "ice-requirements-not-met";
            result.Path.FieldName = 'a';
            result.Path.AttributeName = SdpAttributeNames.IceUsernameFragment;
            result.Detail = "The offered and local ICE capabilities cannot establish this media transport.";
        }
        else if (reason == SdpRejectionReason.DtlsRequirementsNotMet)
        {
            result.ReasonToken = "dtls-requirements-not-met";
            result.Path.FieldName = 'a';
            result.Path.AttributeName = SdpAttributeNames.Fingerprint;
            result.Detail = "The secure media transport does not have compatible TLS or DTLS signaling.";
        }
        else if (reason == SdpRejectionReason.RtcpMuxRequired)
        {
            result.ReasonToken = "rtcp-mux-required";
            result.Path.FieldName = 'a';
            result.Path.AttributeName = SdpAttributeNames.RtcpMux;
            result.Detail = "The offered and local RTCP multiplexing policies are incompatible.";
        }
        else if (reason == SdpRejectionReason.BundleRequirementsNotMet)
        {
            result.ReasonToken = "bundle-requirements-not-met";
            result.Path.FieldName = 'a';
            result.Path.AttributeName = SdpAttributeNames.BundleOnly;
            result.Detail = "The offered media description cannot satisfy the local BUNDLE policy.";
        }
        else if (reason == SdpRejectionReason.DataChannelRequirementsNotMet)
        {
            result.ReasonToken = "data-channel-requirements-not-met";
            result.Path.FieldName = 'a';
            result.Path.AttributeName = SdpAttributeNames.SctpPort;
            result.Detail = "The offered SCTP or data-channel configuration is incompatible with local capabilities.";
        }
        else if (reason == SdpRejectionReason.InvalidOffer)
        {
            result.ReasonToken = "invalid-offer";
            result.Detail = "The offered media description is invalid and cannot be negotiated.";
        }
        else
        {
            result.ReasonToken = "rejected-by-policy";
            result.Path.FieldName = 'a';
            result.Path.AttributeName = SdpAttributeNames.ExtMap;
            result.Detail = "The media description was rejected by local policy.";
        }
        return result;
    }

    internal static void PopulateRejectionDiagnostics(SdpNegotiationResult result, SdpSessionDescription source, int maximumDiagnosticCount)
    {
        SdpDiagnosticLimiter limiter = new SdpDiagnosticLimiter(result.Diagnostics, maximumDiagnosticCount);
        int index = 0;
        while (index < result.Rejections.Count)
        {
            SdpRejection rejection = result.Rejections[index];
            SdpDiagnostic diagnostic = SdpDiagnosticBuilder.CreateDiagnostic(
                ReadRejectionDiagnosticCode(rejection.Reason),
                SdpValidationSeverity.Warning,
                rejection.Path.Scope,
                rejection.Path.FieldName,
                rejection.Path.AttributeName,
                rejection.MediaIndex,
                rejection.Detail,
                SdpDiagnosticBuilder.CopySourcePath(rejection.Path),
                ResolveRejectionLocation(source, rejection));
            limiter.AddDiagnostic(diagnostic);
            index = index + 1;
        }
        result.DiagnosticLimitReached = limiter.DiagnosticLimitReached;
    }

    internal static string ReadRejectionDiagnosticCode(SdpRejectionReason reason)
    {
        if (reason == SdpRejectionReason.OfferedAsRejected)
        {
            return SdpDiagnosticCodes.NegotiationMediaAlreadyRejected;
        }
        if (reason == SdpRejectionReason.UnsupportedProtocol)
        {
            return SdpDiagnosticCodes.NegotiationUnsupportedProtocol;
        }
        if (reason == SdpRejectionReason.NoCompatibleCodec)
        {
            return SdpDiagnosticCodes.NegotiationNoCompatibleCodec;
        }
        if (reason == SdpRejectionReason.IncompatibleDirection)
        {
            return SdpDiagnosticCodes.NegotiationIncompatibleDirection;
        }
        if (reason == SdpRejectionReason.IceRequirementsNotMet)
        {
            return SdpDiagnosticCodes.NegotiationIceIncompatible;
        }
        if (reason == SdpRejectionReason.DtlsRequirementsNotMet)
        {
            return SdpDiagnosticCodes.NegotiationDtlsIncompatible;
        }
        if (reason == SdpRejectionReason.RtcpMuxRequired)
        {
            return SdpDiagnosticCodes.NegotiationRtcpMuxIncompatible;
        }
        if (reason == SdpRejectionReason.BundleRequirementsNotMet)
        {
            return SdpDiagnosticCodes.NegotiationBundleIncompatible;
        }
        if (reason == SdpRejectionReason.DataChannelRequirementsNotMet)
        {
            return SdpDiagnosticCodes.NegotiationDataChannelIncompatible;
        }
        if (reason == SdpRejectionReason.InvalidOffer)
        {
            return SdpDiagnosticCodes.NegotiationInvalidOffer;
        }
        if (reason == SdpRejectionReason.RejectedByPolicy)
        {
            return SdpDiagnosticCodes.NegotiationRejectedByPolicy;
        }
        if (reason == SdpRejectionReason.UnsupportedMedia)
        {
            return SdpDiagnosticCodes.NegotiationUnsupportedMedia;
        }
        return SdpDiagnosticCodes.NegotiationRejectedByPolicy;
    }

    internal static SdpSourceLocation ResolveRejectionLocation(SdpSessionDescription source, SdpRejection rejection)
    {
        int sourceMediaIndex = rejection.MediaId.Length == 0 ? -1 : SdpGroupServiceCore.FindMediaIndex(source, rejection.MediaId);
        if (sourceMediaIndex < 0)
        {
            sourceMediaIndex = rejection.MediaIndex;
        }
        if (sourceMediaIndex < 0 || sourceMediaIndex >= source.MediaDescriptions.Count)
        {
            return new SdpSourceLocation();
        }
        SdpMediaDescription media = source.MediaDescriptions[sourceMediaIndex];
        if (rejection.Path.AttributeName.Length > 0)
        {
            int index = 0;
            while (index < media.Attributes.Count)
            {
                SdpAttribute attribute = media.Attributes[index];
                if (string.Equals(attribute.Name, rejection.Path.AttributeName, StringComparison.OrdinalIgnoreCase))
                {
                    return SdpDiagnosticBuilder.CopySourceLocation(attribute.Location);
                }
                index = index + 1;
            }
        }
        return SdpDiagnosticBuilder.CopySourceLocation(media.Location);
    }

    internal static SdpNegotiationOptions CopyAnswerOptions(SdpNegotiationOptions source)
    {
        SdpNegotiationOptions result = new SdpNegotiationOptions();
        result.Role = SdpNegotiationRole.Answerer;
        result.CodecMatchMode = source.CodecMatchMode;
        result.LocalCapabilities = source.LocalCapabilities;
        result.AllowMediaRejection = source.AllowMediaRejection;
        result.PreservePayloadTypes = source.PreservePayloadTypes;
        result.PreferLocalCodecOrder = source.PreferLocalCodecOrder;
        result.AllowHeaderExtensionIdRemapping = source.AllowHeaderExtensionIdRemapping;
        result.AllowBundleSubset = source.AllowBundleSubset;
        result.AllowIceRestart = source.AllowIceRestart;
        result.PreserveMediaOrder = source.PreserveMediaOrder;
        result.PreserveMediaIds = source.PreserveMediaIds;
        result.PreserveRejectedMedia = source.PreserveRejectedMedia;
        result.IncrementOriginSessionVersion = source.IncrementOriginSessionVersion;
        result.RequireIce = source.RequireIce;
        result.RequireDtls = source.RequireDtls;
        result.RequireRtcpMux = source.RequireRtcpMux;
        result.RequireBundle = source.RequireBundle;
        result.MaximumDiagnosticCount = source.MaximumDiagnosticCount;
        return result;
    }

    internal static SdpNegotiationOptions CopyInitialOfferOptions(SdpNegotiationOptions source)
    {
        SdpNegotiationOptions result = new SdpNegotiationOptions();
        result.Role = SdpNegotiationRole.Offerer;
        result.CodecMatchMode = source.CodecMatchMode;
        result.LocalCapabilities = source.LocalCapabilities;
        result.AllowMediaRejection = source.AllowMediaRejection;
        result.PreservePayloadTypes = source.PreservePayloadTypes;
        result.PreferLocalCodecOrder = source.PreferLocalCodecOrder;
        result.AllowHeaderExtensionIdRemapping = source.AllowHeaderExtensionIdRemapping;
        result.AllowBundleSubset = source.AllowBundleSubset;
        result.AllowIceRestart = source.AllowIceRestart;
        result.PreserveMediaOrder = source.PreserveMediaOrder;
        result.PreserveMediaIds = source.PreserveMediaIds;
        result.PreserveRejectedMedia = source.PreserveRejectedMedia;
        result.IncrementOriginSessionVersion = source.IncrementOriginSessionVersion;
        result.RequireIce = source.RequireIce;
        result.RequireDtls = source.RequireDtls;
        result.RequireRtcpMux = source.RequireRtcpMux;
        result.RequireBundle = source.RequireBundle;
        result.MaximumDiagnosticCount = source.MaximumDiagnosticCount;
        return result;
    }

    internal static List<int> AllocateInitialPayloadTypes(List<SdpCodecCapability> codecs)
    {
        List<int> result = new List<int>();
        int codecIndex = 0;
        while (codecIndex < codecs.Count)
        {
            SdpCodecCapability codec = codecs[codecIndex];
            int payloadType = codec.HasPreferredPayloadType ? codec.PreferredPayloadType : -1;
            if (payloadType >= 0 && result.Contains(payloadType))
            {
                throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationPayloadAllocationFailed, null);
            }
            result.Add(payloadType);
            codecIndex = codecIndex + 1;
        }
        codecIndex = 0;
        while (codecIndex < codecs.Count)
        {
            if (result[codecIndex] < 0)
            {
                result[codecIndex] = FindFreeDynamicPayloadType(result);
            }
            codecIndex = codecIndex + 1;
        }
        return result;
    }

    internal static List<string> AllocateInitialMediaIds(List<SdpMediaCapability> mediaCapabilities)
    {
        List<string> result = new List<string>();
        int mediaIndex = 0;
        while (mediaIndex < mediaCapabilities.Count)
        {
            string mediaId = mediaCapabilities[mediaIndex].MediaId;
            if (mediaId.Length > 0 && result.Contains(mediaId))
            {
                throw new ArgumentException("Preferred media identifiers must be unique.", "options");
            }
            result.Add(mediaId);
            mediaIndex = mediaIndex + 1;
        }
        mediaIndex = 0;
        while (mediaIndex < mediaCapabilities.Count)
        {
            if (result[mediaIndex].Length == 0)
            {
                result[mediaIndex] = FindFreeInitialMediaId(mediaIndex, result);
            }
            mediaIndex = mediaIndex + 1;
        }
        return result;
    }

    internal static string FindFreeInitialMediaId(int mediaIndex, List<string> reservedMediaIds)
    {
        int candidateIndex = mediaIndex;
        string candidate = candidateIndex.ToString(CultureInfo.InvariantCulture);
        while (reservedMediaIds.Contains(candidate))
        {
            candidateIndex = candidateIndex + 1;
            candidate = candidateIndex.ToString(CultureInfo.InvariantCulture);
        }
        return candidate;
    }

    internal static int FindFreeDynamicPayloadType(List<int> usedPayloadTypes)
    {
        int payloadType = SdpStaticPayloadTypes.DynamicMinimum;
        while (payloadType <= SdpStaticPayloadTypes.DynamicMaximum)
        {
            if (!usedPayloadTypes.Contains(payloadType))
            {
                return payloadType;
            }
            payloadType = payloadType + 1;
        }
        throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationPayloadAllocationFailed, null);
    }

    internal static SdpMediaDescription CreateOfferMedia(SdpMediaCapability capability, SdpEndpointCapabilities endpoint, string mediaId, List<int> payloadTypes)
    {
        List<string> formats = CreateOfferFormats(capability, payloadTypes);
        SdpMediaDescription media = SdpSessionFactoryCore.CreateMedia(capability.MediaType, capability.Port, capability.Protocols[0], formats);
        media.MediaKind = capability.MediaKind == SdpMediaKind.Unknown ? SdpMediaLineParser.ReadMediaKind(capability.MediaType) : capability.MediaKind;
        media.HasPortCount = capability.HasPortCount;
        media.PortCount = capability.PortCount;
        SdpConnectionData? connection = capability.Connection == null ? endpoint.Connection : capability.Connection;
        if (connection != null)
        {
            media.Connections.Add(SdpBundleServiceCore.CopyConnection(connection));
        }
        AddMediaIdAttribute(media, mediaId);
        AddDirectionAttribute(media, capability.Direction);
        AddCodecAttributes(media, capability, payloadTypes);
        AddRtcpAttributes(media, capability);
        AddHeaderExtensionAttributes(media, capability, endpoint);
        AddTransportAttributes(media, capability, endpoint);
        AddStreamAttributes(media, capability);
        AddSctpAttributes(media, capability);
        return media;
    }

    internal static List<string> CreateOfferFormats(SdpMediaCapability capability, List<int> payloadTypes)
    {
        List<string> result = new List<string>();
        if (SdpRtpValidationCore.IsRtpProtocol(capability.Protocols[0]))
        {
            int payloadIndex = 0;
            while (payloadIndex < payloadTypes.Count)
            {
                result.Add(payloadTypes[payloadIndex].ToString(CultureInfo.InvariantCulture));
                payloadIndex = payloadIndex + 1;
            }
            return result;
        }
        if (capability.Sctp != null)
        {
            if (SdpDataChannelValidationCore.IsLegacySctpProtocol(capability.Protocols[0]))
            {
                result.Add(capability.Sctp.Port.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                string format = capability.Sctp.Format.Length == 0 ? SdpDataChannelTokens.WebRtcDataChannel : capability.Sctp.Format;
                result.Add(format);
            }
            return result;
        }
        int index = 0;
        while (index < capability.Formats.Count)
        {
            result.Add(capability.Formats[index]);
            index = index + 1;
        }
        return result;
    }

    internal static void AddMediaIdAttribute(SdpMediaDescription media, string identifier)
    {
        SdpMediaIdAttribute mediaId = new SdpMediaIdAttribute();
        mediaId.Id = identifier;
        SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, mediaId);
    }

    internal static void AddDirectionAttribute(SdpMediaDescription media, SdpDirection direction)
    {
        string name = SdpSemanticComparisonCore.ReadDirectionName(direction);
        media.Attributes.Add(SdpSemanticComparisonCore.CreatePropertyAttribute(name));
    }

    internal static void AddCodecAttributes(SdpMediaDescription media, SdpMediaCapability capability, List<int> payloadTypes)
    {
        int codecIndex = 0;
        while (codecIndex < capability.Codecs.Count)
        {
            SdpCodecCapability codec = capability.Codecs[codecIndex];
            int payloadType = payloadTypes[codecIndex];
            SdpRtpMapAttribute mapping = new SdpRtpMapAttribute();
            mapping.PayloadType = payloadType;
            mapping.EncodingName = codec.EncodingName;
            mapping.ClockRate = codec.ClockRate;
            mapping.HasEncodingParameters = codec.HasChannelCount;
            mapping.EncodingParameters = codec.HasChannelCount ? codec.ChannelCount.ToString(CultureInfo.InvariantCulture) : string.Empty;
            mapping.HasChannelCount = codec.HasChannelCount;
            mapping.ChannelCount = codec.ChannelCount;
            SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, mapping);
            if (codec.FormatParameters.Count > 0)
            {
                SdpFormatParametersAttribute format = new SdpFormatParametersAttribute();
                format.PayloadType = payloadType;
                int parameterIndex = 0;
                while (parameterIndex < codec.FormatParameters.Count)
                {
                    SdpFormatParameter source = codec.FormatParameters[parameterIndex];
                    SdpFormatParameter parameter = new SdpFormatParameter();
                    parameter.Name = source.Name;
                    parameter.HasValue = source.HasValue;
                    parameter.Value = source.Value;
                    parameter.RawText = source.RawText;
                    format.Parameters.Add(parameter);
                    parameterIndex = parameterIndex + 1;
                }
                SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, format);
            }
            AddCodecFeedbackAttributes(media, codec, payloadType);
            codecIndex = codecIndex + 1;
        }
    }

    internal static void AddCodecFeedbackAttributes(SdpMediaDescription media, SdpCodecCapability codec, int payloadType)
    {
        int feedbackIndex = 0;
        while (feedbackIndex < codec.RtcpFeedbackTypes.Count)
        {
            SdpRtcpFeedbackAttribute feedback = new SdpRtcpFeedbackAttribute();
            feedback.PayloadType = payloadType.ToString(CultureInfo.InvariantCulture);
            feedback.Type = codec.RtcpFeedbackTypes[feedbackIndex];
            SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, feedback);
            feedbackIndex = feedbackIndex + 1;
        }
        feedbackIndex = 0;
        while (feedbackIndex < codec.RtcpFeedback.Count)
        {
            SdpRtcpFeedbackAttribute source = codec.RtcpFeedback[feedbackIndex];
            SdpRtcpFeedbackAttribute feedback = new SdpRtcpFeedbackAttribute();
            feedback.PayloadType = payloadType.ToString(CultureInfo.InvariantCulture);
            feedback.Type = source.Type;
            feedback.HasParameter = source.HasParameter;
            feedback.Parameter = source.Parameter;
            SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, feedback);
            feedbackIndex = feedbackIndex + 1;
        }
    }

    internal static void AddRtcpAttributes(SdpMediaDescription media, SdpMediaCapability capability)
    {
        if (capability.SupportsRtcpMux || capability.SupportsRtcpMuxOnly)
        {
            media.Attributes.Add(SdpSemanticComparisonCore.CreatePropertyAttribute(SdpAttributeNames.RtcpMux));
        }
        if (capability.SupportsRtcpMuxOnly)
        {
            media.Attributes.Add(SdpSemanticComparisonCore.CreatePropertyAttribute(SdpAttributeNames.RtcpMuxOnly));
        }
        if (capability.SupportsReducedSizeRtcp)
        {
            media.Attributes.Add(SdpSemanticComparisonCore.CreatePropertyAttribute(SdpAttributeNames.RtcpReducedSize));
        }
    }

    internal static void AddHeaderExtensionAttributes(SdpMediaDescription media, SdpMediaCapability capability, SdpEndpointCapabilities endpoint)
    {
        List<SdpHeaderExtension> extensions = capability.HeaderExtensions.Count > 0 ? capability.HeaderExtensions : endpoint.HeaderExtensions;
        List<int> usedIds = new List<int>();
        int extensionIndex = 0;
        while (extensionIndex < extensions.Count)
        {
            SdpHeaderExtension extension = extensions[extensionIndex];
            int identifier = extension.HasPreferredId ? extension.PreferredId : FindFreeHeaderExtensionId(usedIds);
            if (usedIds.Contains(identifier))
            {
                throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationExtMapAllocationFailed, null);
            }
            usedIds.Add(identifier);
            SdpExtMapAttribute mapping = new SdpExtMapAttribute();
            mapping.Id = identifier;
            mapping.HasDirection = extension.Direction != SdpDirection.SendReceive;
            mapping.Direction = extension.Direction;
            mapping.Encrypted = extension.SupportsEncryption;
            mapping.Uri = extension.Uri;
            mapping.ExtensionAttributes = extension.ExtensionAttributes;
            SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, mapping);
            extensionIndex = extensionIndex + 1;
        }
    }

    internal static int FindFreeHeaderExtensionId(List<int> usedIds)
    {
        int identifier = 1;
        while (identifier <= 255)
        {
            if (identifier != 15 && !usedIds.Contains(identifier))
            {
                return identifier;
            }
            identifier = identifier + 1;
        }
        throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationExtMapAllocationFailed, null);
    }

    internal static void AddTransportAttributes(SdpMediaDescription media, SdpMediaCapability capability, SdpEndpointCapabilities endpoint)
    {
        if (endpoint.SupportsIce)
        {
            SdpIceDescription? ice = capability.Ice == null ? endpoint.Ice : capability.Ice;
            if (ice != null)
            {
                SdpSemanticComparisonCore.AddEffectiveIce(media.Attributes, ice, false);
            }
        }
        if (endpoint.SupportsDtls)
        {
            SdpDtlsDescription? dtls = capability.Dtls == null ? endpoint.Dtls : capability.Dtls;
            if (dtls != null)
            {
                SdpSemanticComparisonCore.AddEffectiveDtls(media.Attributes, dtls);
                if (dtls.SetupRole == SdpDtlsSetupRole.Unknown && dtls.SetupRoleToken.Length == 0)
                {
                    AddDefaultOfferSetupAttribute(media);
                }
            }
            else if (endpoint.Fingerprints.Count > 0)
            {
                int fingerprintIndex = 0;
                while (fingerprintIndex < endpoint.Fingerprints.Count)
                {
                    SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, endpoint.Fingerprints[fingerprintIndex]);
                    fingerprintIndex = fingerprintIndex + 1;
                }
                AddDefaultOfferSetupAttribute(media);
            }
        }
    }

    internal static void AddDefaultOfferSetupAttribute(SdpMediaDescription media)
    {
        SdpSetupAttribute setup = new SdpSetupAttribute();
        setup.Role = SdpDtlsSetupRole.ActPass;
        setup.RoleToken = "actpass";
        SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, setup);
    }

    internal static void AddStreamAttributes(SdpMediaDescription media, SdpMediaCapability capability)
    {
        int index = 0;
        while (index < capability.MediaStreamIds.Count)
        {
            SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, capability.MediaStreamIds[index]);
            index = index + 1;
        }
        index = 0;
        while (index < capability.SourceAttributes.Count)
        {
            SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, capability.SourceAttributes[index]);
            index = index + 1;
        }
        index = 0;
        while (index < capability.SourceGroups.Count)
        {
            SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, capability.SourceGroups[index]);
            index = index + 1;
        }
        index = 0;
        while (index < capability.Rids.Count)
        {
            SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, capability.Rids[index]);
            index = index + 1;
        }
        if (capability.Simulcast != null)
        {
            SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, capability.Simulcast);
        }
        index = 0;
        while (index < capability.ImageAttributes.Count)
        {
            SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, capability.ImageAttributes[index]);
            index = index + 1;
        }
    }

    internal static void AddSctpAttributes(SdpMediaDescription media, SdpMediaCapability capability)
    {
        if (capability.Sctp != null)
        {
            SdpSctpDescription sctp = capability.Sctp;
            if (sctp.LegacySctpMap)
            {
                SdpSctpMapAttribute mapping = sctp.SctpMapAttribute == null ? new SdpSctpMapAttribute() : sctp.SctpMapAttribute;
                if (sctp.SctpMapAttribute == null)
                {
                    mapping.Port = sctp.Port;
                    mapping.Format = sctp.Format;
                    mapping.HasMaximumStreams = sctp.HasMaximumStreams;
                    mapping.MaximumStreams = sctp.MaximumStreams;
                }
                SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, mapping);
            }
            else
            {
                SdpSctpPortAttribute port = sctp.PortAttribute == null ? new SdpSctpPortAttribute() : sctp.PortAttribute;
                if (sctp.PortAttribute == null)
                {
                    port.Port = sctp.Port;
                }
                SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, port);
            }
            if (sctp.HasMaximumMessageSize || sctp.MaximumMessageSizeAttribute != null)
            {
                SdpMaximumMessageSizeAttribute maximum = sctp.MaximumMessageSizeAttribute == null ? new SdpMaximumMessageSizeAttribute() : sctp.MaximumMessageSizeAttribute;
                if (sctp.MaximumMessageSizeAttribute == null)
                {
                    maximum.MaximumMessageSize = sctp.MaximumMessageSize;
                }
                SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, maximum);
            }
        }
        int channelIndex = 0;
        while (channelIndex < capability.DataChannels.Count)
        {
            SdpDataChannelAttribute channel = capability.DataChannels[channelIndex];
            SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, channel);
            int subprotocolIndex = 0;
            while (subprotocolIndex < channel.SubprotocolAttributes.Count)
            {
                SdpDataChannelSubprotocolAttribute subprotocol = new SdpDataChannelSubprotocolAttribute();
                subprotocol.ChannelId = channel.ChannelId;
                subprotocol.EmbeddedAttribute = channel.SubprotocolAttributes[subprotocolIndex];
                SdpSemanticComparisonCore.AddEncodedAttribute(media.Attributes, subprotocol);
                subprotocolIndex = subprotocolIndex + 1;
            }
            channelIndex = channelIndex + 1;
        }
    }

    internal static SdpMediaNegotiation CreateInitialMediaResult(SdpSessionDescription description, SdpMediaCapability capability, int mediaIndex, string mediaId, List<int> payloadTypes)
    {
        SdpMediaNegotiation result = new SdpMediaNegotiation();
        result.MediaIndex = mediaIndex;
        result.MediaId = mediaId;
        result.MediaKind = capability.MediaKind == SdpMediaKind.Unknown ? SdpMediaLineParser.ReadMediaKind(capability.MediaType) : capability.MediaKind;
        result.MediaType = capability.MediaType;
        result.Protocol = capability.Protocols[0];
        result.TransportMediaIndex = mediaIndex;
        result.Accepted = true;
        result.OfferedDirection = capability.Direction;
        result.Direction = capability.Direction;
        result.Codecs = SdpCodecServiceCore.ResolveMediaCodecs(description.MediaDescriptions[mediaIndex]);
        int payloadIndex = 0;
        while (payloadIndex < payloadTypes.Count)
        {
            SdpPayloadMapping mapping = new SdpPayloadMapping();
            mapping.MediaIndex = mediaIndex;
            mapping.OfferedPayloadType = payloadTypes[payloadIndex];
            mapping.NegotiatedPayloadType = payloadTypes[payloadIndex];
            mapping.EncodingName = capability.Codecs[payloadIndex].EncodingName;
            result.PayloadMappings.Add(mapping);
            payloadIndex = payloadIndex + 1;
        }
        AddHeaderExtensionResults(result, description.MediaDescriptions[mediaIndex]);
        if (capability.Sctp != null)
        {
            result.Sctp = SdpDataChannelServiceCore.CopySctpDescription(capability.Sctp);
        }
        return result;
    }

    internal static void AddHeaderExtensionResults(SdpMediaNegotiation result, SdpMediaDescription media)
    {
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.ExtMap, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.ExtMapAttribute != null)
                {
                    SdpExtMapAttribute extension = decoded.ExtMapAttribute;
                    SdpHeaderExtensionMapping mapping = new SdpHeaderExtensionMapping();
                    mapping.OfferedId = extension.Id;
                    mapping.NegotiatedId = extension.Id;
                    mapping.Uri = extension.Uri;
                    mapping.Direction = extension.Direction;
                    mapping.Encrypted = extension.Encrypted;
                    mapping.ExtensionAttributes = extension.ExtensionAttributes;
                    result.HeaderExtensionMappings.Add(mapping);
                }
            }
            index = index + 1;
        }
    }

    internal static void RefreshInitialMediaResults(SdpSessionDescription description, SdpNegotiationResult result)
    {
        int mediaIndex = 0;
        while (mediaIndex < result.Media.Count)
        {
            SdpMediaNegotiation mediaResult = result.Media[mediaIndex];
            mediaResult.TransportMediaIndex = SdpBundleServiceCore.ResolveBundleTaggedMediaIndex(description, mediaIndex);
            mediaResult.BundleGroupIndex = FindInitialBundleGroupIndex(description, mediaIndex);
            mediaResult.EffectiveMedia = SdpQueryServiceCore.ResolveEffectiveMedia(description, mediaIndex);
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static int FindInitialBundleGroupIndex(SdpSessionDescription description, int mediaIndex)
    {
        List<SdpBundleGroup> groups = SdpBundleServiceCore.ResolveBundleGroups(description);
        SdpMediaIdAttribute? resolvedMediaId = SdpGroupServiceCore.ResolveMediaId(description.MediaDescriptions[mediaIndex]);
        string mediaId = resolvedMediaId == null ? string.Empty : resolvedMediaId.Id;
        int groupIndex = 0;
        while (groupIndex < groups.Count)
        {
            if (groups[groupIndex].MediaIds.Contains(mediaId))
            {
                return groupIndex;
            }
            groupIndex = groupIndex + 1;
        }
        return -1;
    }
}
