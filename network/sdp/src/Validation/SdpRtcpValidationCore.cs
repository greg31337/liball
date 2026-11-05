using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpRtcpValidationCore
{
    internal static void ValidateRtcpDescription(SdpSessionDescription description, SdpMediaDescription media, int mediaIndex, bool feedbackProtocol, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpRtcpAttribute? rtcp = null;
        SdpAttribute? rtcpSource = null;
        SdpAttribute? muxSource = null;
        SdpAttribute? muxOnlySource = null;
        SdpAttribute? reducedSizeSource = null;
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (IsRtcpTransportAttribute(attribute.Name))
            {
                int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, index);
                if (occurrenceIndex > 0)
                {
                    AddRtcpIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, attribute, mediaIndex, occurrenceIndex, "The RTCP transport attribute may appear at most once in a media description.");
                }
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success)
                {
                    SdpRtpValidationCore.AddDecodeIssue(decoded, attribute, mediaIndex, occurrenceIndex, result, limiter);
                }
                if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.Rtcp))
                {
                    if (rtcpSource == null)
                    {
                        rtcpSource = attribute;
                        if (decoded.Success)
                        {
                            rtcp = decoded.RtcpAttribute;
                        }
                    }
                }
                else if (decoded.Success && SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.RtcpMux) && muxSource == null)
                {
                    muxSource = attribute;
                }
                else if (decoded.Success && SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.RtcpMuxOnly) && muxOnlySource == null)
                {
                    muxOnlySource = attribute;
                }
                else if (decoded.Success && SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.RtcpReducedSize) && reducedSizeSource == null)
                {
                    reducedSizeSource = attribute;
                }
            }
            index = index + 1;
        }

        if (muxOnlySource != null && muxSource == null && !SdpValidationCore.UsesIceProfile(result.AppliedOptions.Profile))
        {
            AddRtcpIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, muxOnlySource, mediaIndex, 0, "The rtcp-mux-only attribute requires the rtcp-mux attribute.");
        }
        if (muxOnlySource != null && result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Answerer)
        {
            AddRtcpIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, muxOnlySource, mediaIndex, 0, "The rtcp-mux-only attribute is forbidden in an SDP answer.");
        }
        if (muxOnlySource != null && rtcp != null && rtcpSource != null)
        {
            ValidateMuxOnlyRtcpEndpoint(description, media, rtcp, rtcpSource, mediaIndex, result, limiter);
        }
        if (result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Answerer && result.AppliedOptions.OfferDescription != null)
        {
            ValidateAnswerRtcpAttributes(media, mediaIndex, feedbackProtocol, muxSource, reducedSizeSource, result, limiter);
        }
    }

    internal static bool IsRtcpTransportAttribute(string name)
    {
        return SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.Rtcp)
            || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.RtcpMux)
            || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.RtcpMuxOnly)
            || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.RtcpReducedSize);
    }

    internal static void ValidateMuxOnlyRtcpEndpoint(SdpSessionDescription description, SdpMediaDescription media, SdpRtcpAttribute rtcp, SdpAttribute source, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (rtcp.Port != media.Port)
        {
            AddRtcpIssue(result, limiter, SdpDiagnosticCodes.ValidationPort, source, mediaIndex, 0, "An rtcp attribute used with rtcp-mux-only must use the RTP media port.");
        }
        if (!rtcp.HasNetworkAddress)
        {
            return;
        }
        SdpConnectionData? connection = SdpIceValidationCore.ReadEffectiveConnection(description, media);
        if (connection == null || !string.Equals(rtcp.NetworkType, connection.NetworkType, StringComparison.OrdinalIgnoreCase) || !string.Equals(rtcp.AddressType, connection.AddressType, StringComparison.OrdinalIgnoreCase) || !string.Equals(rtcp.Address, connection.Address, StringComparison.OrdinalIgnoreCase))
        {
            AddRtcpIssue(result, limiter, SdpDiagnosticCodes.ValidationConnection, source, mediaIndex, 0, "An rtcp attribute used with rtcp-mux-only must use the RTP media connection address.");
        }
    }

    internal static void ValidateAnswerRtcpAttributes(SdpMediaDescription media, int mediaIndex, bool feedbackProtocol, SdpAttribute? muxSource, SdpAttribute? reducedSizeSource, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpSessionDescription? offer = result.AppliedOptions.OfferDescription;
        if (offer == null || mediaIndex >= offer.MediaDescriptions.Count)
        {
            return;
        }
        SdpMediaDescription offeredMedia = offer.MediaDescriptions[mediaIndex];
        bool offeredMux = ContainsValidAttribute(offeredMedia.Attributes, SdpAttributeNames.RtcpMux);
        bool offeredMuxOnly = ContainsValidAttribute(offeredMedia.Attributes, SdpAttributeNames.RtcpMuxOnly);
        bool offeredReducedSize = ContainsValidAttribute(offeredMedia.Attributes, SdpAttributeNames.RtcpReducedSize);
        if (muxSource != null && !offeredMux)
        {
            AddRtcpIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, muxSource, mediaIndex, 0, "An SDP answer must not enable RTCP multiplexing when rtcp-mux was absent from the offer.");
        }
        if (reducedSizeSource != null && !offeredReducedSize)
        {
            AddRtcpIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, reducedSizeSource, mediaIndex, 0, "An SDP answer must not enable reduced-size RTCP when rtcp-rsize was absent from the offer.");
        }
        if (offeredMuxOnly && media.Port != 0 && muxSource == null)
        {
            AddRtcpMediaIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpAttributeNames.RtcpMux, media, mediaIndex, "A non-rejected answer to an rtcp-mux-only offer must include rtcp-mux.");
        }
        if (feedbackProtocol)
        {
            ValidateAnswerFeedback(media, offeredMedia, mediaIndex, result, limiter);
        }
    }

    internal static void ValidateAnswerFeedback(SdpMediaDescription media, SdpMediaDescription offeredMedia, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.RtcpFeedback))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.RtcpFeedbackAttribute != null && !FeedbackWasOffered(decoded.RtcpFeedbackAttribute, offeredMedia.Attributes))
                {
                    int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, index);
                    AddRtcpIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, attribute, mediaIndex, occurrenceIndex, "An SDP answer must not add or alter an RTCP feedback capability that was absent from the offer.");
                }
            }
            index = index + 1;
        }
    }

    internal static bool FeedbackWasOffered(SdpRtcpFeedbackAttribute answer, List<SdpAttribute> offeredAttributes)
    {
        int index = 0;
        while (index < offeredAttributes.Count)
        {
            SdpAttribute attribute = offeredAttributes[index];
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.RtcpFeedback))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                SdpRtcpFeedbackAttribute? offered = decoded.RtcpFeedbackAttribute;
                if (decoded.Success && offered != null && FeedbackValuesMatch(answer, offered))
                {
                    return true;
                }
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool FeedbackValuesMatch(SdpRtcpFeedbackAttribute answer, SdpRtcpFeedbackAttribute offered)
    {
        bool selectorMatches = answer.PayloadType == offered.PayloadType || answer.PayloadType != "*" && offered.PayloadType == "*";
        return selectorMatches && answer.Type == offered.Type && answer.HasParameter == offered.HasParameter && (!answer.HasParameter || answer.Parameter == offered.Parameter);
    }

    internal static bool ContainsValidAttribute(List<SdpAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (SdpRtpValidationCore.NameEquals(attributes[index].Name, name))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attributes[index]);
                if (decoded.Success)
                {
                    return true;
                }
            }
            index = index + 1;
        }
        return false;
    }

    internal static void AddRtcpIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, string code, SdpAttribute attribute, int mediaIndex, int occurrenceIndex, string detail)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, code, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, detail, attribute.Location);
    }

    internal static void AddRtcpMediaIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, string code, string attributeName, SdpMediaDescription media, int mediaIndex, string detail)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, code, SdpValidationSeverity.Error, SdpScope.Media, attributeName, mediaIndex, -1, detail, media.Location);
    }
}
