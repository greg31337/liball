using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpAnswerValidationCore
{
    internal static void ValidateAnswerDescription(SdpSessionDescription answer, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpSessionDescription? offer = result.AppliedOptions.OfferDescription;
        if (result.AppliedOptions.NegotiationRole != SdpNegotiationRole.Answerer || offer == null)
        {
            return;
        }

        if (answer.MediaDescriptions.Count != offer.MediaDescriptions.Count)
        {
            AddSessionMediaIssue(answer, result, limiter, "An SDP answer must contain exactly the offered number of media descriptions.");
        }
        ValidateTimings(answer, offer, result, limiter);

        int mediaCount = answer.MediaDescriptions.Count;
        if (offer.MediaDescriptions.Count < mediaCount)
        {
            mediaCount = offer.MediaDescriptions.Count;
        }
        int mediaIndex = 0;
        while (mediaIndex < mediaCount)
        {
            ValidateMedia(answer, offer, mediaIndex, result, limiter);
            mediaIndex = mediaIndex + 1;
        }
        ValidateGroups(answer, offer, result, limiter);
    }

    internal static void ValidateMedia(SdpSessionDescription answer, SdpSessionDescription offer, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpMediaDescription answerMedia = answer.MediaDescriptions[mediaIndex];
        SdpMediaDescription offerMedia = offer.MediaDescriptions[mediaIndex];
        if (!string.Equals(answerMedia.MediaType, offerMedia.MediaType, StringComparison.OrdinalIgnoreCase))
        {
            AddMediaFieldIssue(answerMedia, mediaIndex, result, limiter, "An SDP answer must preserve the offered media-description order and media type.");
        }
        if (!string.Equals(answerMedia.Protocol, offerMedia.Protocol, StringComparison.OrdinalIgnoreCase))
        {
            AddMediaFieldIssue(answerMedia, mediaIndex, result, limiter, "An SDP answer must retain the transport protocol from the corresponding offer.");
        }
        bool offeredBundleOnly = SdpBundleServiceCore.IsBundleOnlyMedia(offerMedia) && SdpBundleServiceCore.FindBundleGroupForMedia(offer, mediaIndex) != null;
        if (offerMedia.Port == 0 && answerMedia.Port != 0 && !offeredBundleOnly)
        {
            AddMediaFieldIssue(answerMedia, mediaIndex, result, limiter, "An SDP answer must not reactivate a media description rejected by the offer.");
        }

        ValidateDirection(answer, offer, answerMedia, offerMedia, mediaIndex, result, limiter);
        ValidateFormats(answer, offer, answerMedia, offerMedia, mediaIndex, result, limiter);
        ValidateMediaId(answerMedia, offerMedia, mediaIndex, result, limiter);
        ValidateMulticastAddress(answer, offer, answerMedia, offerMedia, mediaIndex, result, limiter);
        ValidateIce(answer, offer, mediaIndex, result, limiter);
        ValidateExtMaps(answer, offer, mediaIndex, result, limiter);
        ValidateRids(answerMedia, offerMedia, mediaIndex, result, limiter);
        ValidateSctp(answerMedia, offerMedia, mediaIndex, result, limiter);
    }

    internal static void ValidateDirection(SdpSessionDescription answer, SdpSessionDescription offer, SdpMediaDescription answerMedia, SdpMediaDescription offerMedia, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpDirection offered = SdpExtMapValidationCore.ReadEffectiveMediaDirection(offer, offerMedia);
        SdpDirection answered = SdpExtMapValidationCore.ReadEffectiveMediaDirection(answer, answerMedia);
        bool valid = IsAnswerDirection(offered, answered);
        if (!valid)
        {
            AddMediaAttributeIssue(answerMedia, mediaIndex, DirectionName(answered), result, limiter, "The answer media direction is incompatible with the direction of the corresponding offer.");
        }
    }

    internal static bool IsAnswerDirection(SdpDirection offered, SdpDirection answered)
    {
        if (answered == SdpDirection.Inactive)
        {
            return true;
        }
        if (offered == SdpDirection.SendReceive)
        {
            return answered == SdpDirection.SendReceive || answered == SdpDirection.SendOnly || answered == SdpDirection.ReceiveOnly;
        }
        if (offered == SdpDirection.SendOnly)
        {
            return answered == SdpDirection.ReceiveOnly;
        }
        if (offered == SdpDirection.ReceiveOnly)
        {
            return answered == SdpDirection.SendOnly;
        }
        return false;
    }

    internal static void ValidateTimings(SdpSessionDescription answer, SdpSessionDescription offer, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool same = answer.Timings.Count == offer.Timings.Count;
        int timingIndex = 0;
        while (same && timingIndex < answer.Timings.Count)
        {
            SdpTiming answerTiming = answer.Timings[timingIndex];
            SdpTiming offerTiming = offer.Timings[timingIndex];
            same = answerTiming.StartTime == offerTiming.StartTime && answerTiming.StopTime == offerTiming.StopTime;
            timingIndex = timingIndex + 1;
        }
        if (!same && !HasFieldIssue(result, SdpScope.Session, -1, 't'))
        {
            SdpSourceLocation location = answer.Timings.Count == 0 ? new SdpSourceLocation() : answer.Timings[0].Location;
            SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.NegotiationInvalidAnswer, SdpValidationSeverity.Error, SdpScope.Session, 't', -1, -1, "An SDP answer must retain the timing fields from its offer.", location);
        }
    }

    internal static string DirectionName(SdpDirection direction)
    {
        if (direction == SdpDirection.SendOnly)
        {
            return SdpAttributeNames.SendOnly;
        }
        if (direction == SdpDirection.ReceiveOnly)
        {
            return SdpAttributeNames.ReceiveOnly;
        }
        if (direction == SdpDirection.Inactive)
        {
            return SdpAttributeNames.Inactive;
        }
        return SdpAttributeNames.SendReceive;
    }

    internal static void ValidateFormats(SdpSessionDescription answerDescription, SdpSessionDescription offerDescription, SdpMediaDescription answer, SdpMediaDescription offer, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (answer.Port == 0)
        {
            return;
        }
        bool additionalFormatsAllowed = AllowsAdditionalFormats(answerDescription, offerDescription, mediaIndex);
        if (SdpRtpValidationCore.IsRtpProtocol(answer.Protocol) && SdpRtpValidationCore.IsRtpProtocol(offer.Protocol))
        {
            List<SdpCodec> answerCodecs = SdpCodecServiceCore.ResolveMediaCodecs(answer);
            List<SdpCodec> offerCodecs = SdpCodecServiceCore.ResolveMediaCodecs(offer);
            bool commonCodec = false;
            int codecIndex = 0;
            while (codecIndex < answerCodecs.Count)
            {
                if (ContainsCodec(offerCodecs, answerCodecs[codecIndex]))
                {
                    commonCodec = true;
                }
                else if (!additionalFormatsAllowed)
                {
                    AddMediaFieldIssue(answer, mediaIndex, result, limiter, "Every RTP codec in an SDP answer must correspond to a codec in the offer.");
                    return;
                }
                codecIndex = codecIndex + 1;
            }
            if (!commonCodec)
            {
                AddMediaFieldIssue(answer, mediaIndex, result, limiter, "An active SDP answer must contain at least one RTP codec from the corresponding offer.");
            }
            return;
        }

        bool commonFormat = false;
        int formatIndex = 0;
        while (formatIndex < answer.Formats.Count)
        {
            if (ContainsFormat(offer.Formats, answer.Formats[formatIndex]))
            {
                commonFormat = true;
            }
            else if (!additionalFormatsAllowed)
            {
                AddMediaFieldIssue(answer, mediaIndex, result, limiter, "Every answer media format must have been present in the corresponding offer.");
                return;
            }
            formatIndex = formatIndex + 1;
        }
        if (!commonFormat)
        {
            AddMediaFieldIssue(answer, mediaIndex, result, limiter, "An active SDP answer must contain at least one media format from the corresponding offer.");
        }
    }

    internal static bool AllowsAdditionalFormats(SdpSessionDescription answer, SdpSessionDescription offer, int mediaIndex)
    {
        SdpDirection answered = SdpExtMapValidationCore.ReadEffectiveMediaDirection(answer, answer.MediaDescriptions[mediaIndex]);
        if (answered == SdpDirection.ReceiveOnly || answered == SdpDirection.SendReceive)
        {
            return true;
        }
        if (answered != SdpDirection.Inactive)
        {
            return false;
        }
        SdpDirection offered = SdpExtMapValidationCore.ReadEffectiveMediaDirection(offer, offer.MediaDescriptions[mediaIndex]);
        return offered != SdpDirection.ReceiveOnly;
    }

    internal static bool ContainsCodec(List<SdpCodec> offered, SdpCodec answer)
    {
        int index = 0;
        while (index < offered.Count)
        {
            SdpCodec candidate = offered[index];
            bool sameEncoding = string.Equals(candidate.EncodingName, answer.EncodingName, StringComparison.OrdinalIgnoreCase);
            bool sameChannels = candidate.ChannelCount == answer.ChannelCount;
            if (sameEncoding && candidate.ClockRate == answer.ClockRate && sameChannels)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool ContainsFormat(List<string> formats, string format)
    {
        int index = 0;
        while (index < formats.Count)
        {
            if (string.Equals(formats[index], format, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static void ValidateMediaId(SdpMediaDescription answer, SdpMediaDescription offer, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpMediaIdAttribute? offered = SdpGroupServiceCore.ResolveMediaId(offer);
        SdpMediaIdAttribute? answered = SdpGroupServiceCore.ResolveMediaId(answer);
        if (offered == null && answered == null)
        {
            return;
        }
        if (offered == null || answered == null || offered.Id != answered.Id)
        {
            AddMediaAttributeIssue(answer, mediaIndex, SdpAttributeNames.MediaId, result, limiter, "An SDP answer must preserve the media identifier from the corresponding offer.");
        }
    }

    internal static void ValidateMulticastAddress(SdpSessionDescription answer, SdpSessionDescription offer, SdpMediaDescription answerMedia, SdpMediaDescription offerMedia, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpConnectionData? offered = SdpIceValidationCore.ReadEffectiveConnection(offer, offerMedia);
        if (answerMedia.Port == 0 || offered == null || offered.AddressKind != SdpAddressKind.Ipv4Multicast && offered.AddressKind != SdpAddressKind.Ipv6Multicast)
        {
            return;
        }
        SdpConnectionData? answered = SdpIceValidationCore.ReadEffectiveConnection(answer, answerMedia);
        bool same = answered != null && string.Equals(offered.NetworkType, answered.NetworkType, StringComparison.OrdinalIgnoreCase) && string.Equals(offered.AddressType, answered.AddressType, StringComparison.OrdinalIgnoreCase) && string.Equals(offered.Address, answered.Address, StringComparison.OrdinalIgnoreCase) && offered.HasTimeToLive == answered.HasTimeToLive && offered.TimeToLive == answered.TimeToLive && offered.HasAddressCount == answered.HasAddressCount && offered.AddressCount == answered.AddressCount && offerMedia.Port == answerMedia.Port;
        if (!same)
        {
            AddConnectionIssue(answerMedia, mediaIndex, result, limiter, "An answer to a multicast offer must retain the offered destination address and port.");
        }
    }

    internal static void ValidateIce(SdpSessionDescription answer, SdpSessionDescription offer, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int answerTransportIndex = SdpIceServiceCore.ResolveTransportMediaIndex(answer, mediaIndex);
        if (answerTransportIndex != mediaIndex)
        {
            return;
        }
        SdpIceDescription offered = SdpIceServiceCore.ResolveMediaIce(offer, mediaIndex);
        if (!UsesIce(offered))
        {
            int offerTransportIndex = SdpIceServiceCore.ResolveTransportMediaIndex(offer, mediaIndex);
            offered = SdpIceServiceCore.ResolveMediaIce(offer, offerTransportIndex);
        }
        SdpIceDescription answered = SdpIceServiceCore.ResolveMediaIce(answer, answerTransportIndex);
        bool offerUsesIce = UsesIce(offered);
        bool answerUsesIce = UsesIce(answered);
        if (answerUsesIce && !offerUsesIce)
        {
            AddMediaAttributeIssue(answer.MediaDescriptions[mediaIndex], mediaIndex, SdpAttributeNames.IceUsernameFragment, result, limiter, "An SDP answer must not introduce ICE when the corresponding offer did not use ICE.");
            return;
        }
        if (answer.MediaDescriptions[mediaIndex].Port != 0 && offerUsesIce && !answerUsesIce)
        {
            AddMediaAttributeIssue(answer.MediaDescriptions[mediaIndex], mediaIndex, SdpAttributeNames.IceUsernameFragment, result, limiter, "An accepted answer to an ICE offer must include ICE signaling.");
            return;
        }
        int optionIndex = 0;
        while (optionIndex < answered.Options.Count)
        {
            if (!ContainsFormat(offered.Options, answered.Options[optionIndex]))
            {
                AddMediaAttributeIssue(answer.MediaDescriptions[mediaIndex], mediaIndex, SdpAttributeNames.IceOptions, result, limiter, "An SDP answer must not introduce an ICE option that was absent from the offer.");
                return;
            }
            optionIndex = optionIndex + 1;
        }
    }

    internal static bool UsesIce(SdpIceDescription ice)
    {
        return ice.Credentials != null || ice.Options.Count > 0 || ice.Candidates.Count > 0 || ice.RemoteCandidates.Count > 0 || ice.IceMismatch || ice.EndOfCandidates;
    }

    internal static void ValidateExtMaps(SdpSessionDescription answer, SdpSessionDescription offer, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<SdpExtMapAttribute> offered = SdpExtMapServiceCore.ResolveMediaExtMaps(offer, mediaIndex);
        List<SdpExtMapAttribute> answered = SdpExtMapServiceCore.ResolveMediaExtMaps(answer, mediaIndex);
        int index = 0;
        while (index < answered.Count)
        {
            if (!ContainsExtMap(offered, answered[index]))
            {
                AddMediaAttributeIssue(answer.MediaDescriptions[mediaIndex], mediaIndex, SdpAttributeNames.ExtMap, result, limiter, "Every answer extmap must retain an offered extension identity, attributes, and compatible direction.");
                return;
            }
            index = index + 1;
        }
    }

    internal static bool ContainsExtMap(List<SdpExtMapAttribute> offered, SdpExtMapAttribute answer)
    {
        int index = 0;
        while (index < offered.Count)
        {
            SdpExtMapAttribute candidate = offered[index];
            bool identifier = candidate.Id == answer.Id || SdpExtMapServiceCore.IsNegotiationIdentifier(candidate.Id) && SdpExtMapServiceCore.IsUsableIdentifier(answer.Id);
            bool identity = candidate.Encrypted == answer.Encrypted && candidate.Uri == answer.Uri && candidate.ExtensionAttributes == answer.ExtensionAttributes;
            if (identifier && identity && IsAnswerDirection(candidate.Direction, answer.Direction))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static void ValidateGroups(SdpSessionDescription answer, SdpSessionDescription offer, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<SdpGroupAttribute> offered = SdpGroupServiceCore.ResolveGroups(offer);
        List<SdpGroupAttribute> answered = SdpGroupServiceCore.ResolveGroups(answer);
        List<int> matchedOfferIndexes = new List<int>();
        int groupIndex = 0;
        while (groupIndex < answered.Count)
        {
            int offeredIndex = FindGroupSubsetIndex(offered, answered[groupIndex], matchedOfferIndexes);
            if (offeredIndex < 0)
            {
                SdpAttribute? source = answered[groupIndex].SourceAttribute;
                SdpSourceLocation location = source == null ? new SdpSourceLocation() : source.Location;
                AddAttributeIssue(SdpScope.Session, -1, SdpAttributeNames.Group, location, result, limiter, "Every answer group must be an ordered subset of a group with the same semantic in the offer.");
            }
            else
            {
                matchedOfferIndexes.Add(offeredIndex);
            }
            groupIndex = groupIndex + 1;
        }
    }

    internal static int FindGroupSubsetIndex(List<SdpGroupAttribute> offered, SdpGroupAttribute answer, List<int> matchedOfferIndexes)
    {
        int index = 0;
        while (index < offered.Count)
        {
            SdpGroupAttribute candidate = offered[index];
            bool semantic = answer.Semantic != SdpGroupSemantic.Unknown && answer.Semantic == candidate.Semantic || answer.Semantic == SdpGroupSemantic.Unknown && candidate.Semantic == SdpGroupSemantic.Unknown && string.Equals(answer.SemanticToken, candidate.SemanticToken, StringComparison.OrdinalIgnoreCase);
            if (!matchedOfferIndexes.Contains(index) && semantic && IsOrderedSubset(answer.IdentificationTags, candidate.IdentificationTags))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static bool IsOrderedSubset(List<string> answer, List<string> offer)
    {
        int answerIndex = 0;
        int offerIndex = 0;
        while (answerIndex < answer.Count)
        {
            while (offerIndex < offer.Count && offer[offerIndex] != answer[answerIndex])
            {
                offerIndex = offerIndex + 1;
            }
            if (offerIndex >= offer.Count)
            {
                return false;
            }
            answerIndex = answerIndex + 1;
            offerIndex = offerIndex + 1;
        }
        return true;
    }

    internal static void ValidateRids(SdpMediaDescription answer, SdpMediaDescription offer, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<SdpRidAttribute> offered = SdpSimulcastValidationCore.ReadRids(offer);
        List<SdpRidAttribute> answered = SdpSimulcastValidationCore.ReadRids(answer);
        int index = 0;
        while (index < answered.Count)
        {
            if (!ContainsOppositeRid(offered, answered[index]))
            {
                SdpAttribute? source = answered[index].SourceAttribute;
                SdpSourceLocation location = source == null ? answer.Location : source.Location;
                AddAttributeIssue(SdpScope.Media, mediaIndex, SdpAttributeNames.Rid, location, result, limiter, "Every RID in an SDP answer must correspond to an oppositely directed RID in the offer.");
            }
            index = index + 1;
        }
    }

    internal static bool ContainsOppositeRid(List<SdpRidAttribute> offered, SdpRidAttribute answer)
    {
        int index = 0;
        while (index < offered.Count)
        {
            if (offered[index].Id == answer.Id && IsAnswerDirection(offered[index].Direction, answer.Direction))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static void ValidateSctp(SdpMediaDescription answer, SdpMediaDescription offer, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool answerSctp = SdpDataChannelValidationCore.IsModernSctpProtocol(answer.Protocol) || SdpDataChannelValidationCore.IsLegacySctpProtocol(answer.Protocol);
        bool offerSctp = SdpDataChannelValidationCore.IsModernSctpProtocol(offer.Protocol) || SdpDataChannelValidationCore.IsLegacySctpProtocol(offer.Protocol);
        if (!answerSctp && !offerSctp)
        {
            return;
        }
        SdpSctpDescription offered = SdpDataChannelServiceCore.ResolveMediaSctp(offer);
        SdpSctpDescription answered = SdpDataChannelServiceCore.ResolveMediaSctp(answer);
        if (answer.Port != 0 && (offered.Format.Length == 0 || !string.Equals(offered.Format, answered.Format, StringComparison.OrdinalIgnoreCase)))
        {
            AddMediaAttributeIssue(answer, mediaIndex, SdpAttributeNames.SctpPort, result, limiter, "An accepted SCTP answer must retain the application format from the corresponding offer.");
        }
    }

    internal static void AddSessionMediaIssue(SdpSessionDescription answer, SdpValidationResult result, SdpValidationIssueLimiter limiter, string detail)
    {
        SdpSourceLocation location = new SdpSourceLocation();
        if (answer.MediaDescriptions.Count > 0)
        {
            location = answer.MediaDescriptions[0].Location;
        }
        if (!HasFieldIssue(result, SdpScope.Session, -1, 'm'))
        {
            SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.NegotiationInvalidAnswer, SdpValidationSeverity.Error, SdpScope.Session, 'm', -1, -1, detail, location);
        }
    }

    internal static void AddMediaFieldIssue(SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter, string detail)
    {
        if (!HasFieldIssue(result, SdpScope.Media, mediaIndex, 'm'))
        {
            SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.NegotiationInvalidAnswer, SdpValidationSeverity.Error, SdpScope.Media, 'm', mediaIndex, 0, detail, media.Location);
        }
    }

    internal static void AddConnectionIssue(SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter, string detail)
    {
        if (!HasFieldIssue(result, SdpScope.Media, mediaIndex, 'c'))
        {
            SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.NegotiationInvalidAnswer, SdpValidationSeverity.Error, SdpScope.Media, 'c', mediaIndex, 0, detail, media.Location);
        }
    }

    internal static void AddMediaAttributeIssue(SdpMediaDescription media, int mediaIndex, string name, SdpValidationResult result, SdpValidationIssueLimiter limiter, string detail)
    {
        SdpAttribute? attribute = FindAttribute(media.Attributes, name);
        SdpSourceLocation location = attribute == null ? media.Location : attribute.Location;
        AddAttributeIssue(SdpScope.Media, mediaIndex, name, location, result, limiter, detail);
    }

    internal static void AddAttributeIssue(SdpScope scope, int mediaIndex, string name, SdpSourceLocation location, SdpValidationResult result, SdpValidationIssueLimiter limiter, string detail)
    {
        if (!HasAttributeIssue(result, scope, mediaIndex, name))
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.NegotiationInvalidAnswer, SdpValidationSeverity.Error, scope, name, mediaIndex, -1, detail, location);
        }
    }

    internal static SdpAttribute? FindAttribute(List<SdpAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (string.Equals(attributes[index].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return attributes[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static bool HasFieldIssue(SdpValidationResult result, SdpScope scope, int mediaIndex, char fieldName)
    {
        int index = 0;
        while (index < result.Issues.Count)
        {
            SdpValidationIssue issue = result.Issues[index];
            if (issue.Code == SdpDiagnosticCodes.NegotiationInvalidAnswer && issue.Scope == scope && issue.MediaIndex == mediaIndex && issue.FieldName == fieldName)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool HasAttributeIssue(SdpValidationResult result, SdpScope scope, int mediaIndex, string name)
    {
        int index = 0;
        while (index < result.Issues.Count)
        {
            SdpValidationIssue issue = result.Issues[index];
            if (issue.Code == SdpDiagnosticCodes.NegotiationInvalidAnswer && issue.Scope == scope && issue.MediaIndex == mediaIndex && string.Equals(issue.AttributeName, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }
}
