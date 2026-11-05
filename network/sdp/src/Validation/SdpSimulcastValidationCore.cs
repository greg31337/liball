using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpSimulcastValidationCore
{
    internal static void ValidateSimulcastDescription(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        ValidateSessionSimulcastAttributes(description, result, limiter);
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            ValidateMediaSimulcastAttributes(description, mediaIndex, result, limiter);
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void ValidateSessionSimulcastAttributes(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < description.Attributes.Count)
        {
            SdpAttribute attribute = description.Attributes[index];
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.Simulcast))
            {
                int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(description.Attributes, index);
                AddSimulcastIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpScope.Session, attribute, -1, occurrenceIndex, "The simulcast attribute is permitted only at media scope.");
                ValidateDecodedSimulcast(attribute, SdpScope.Session, -1, occurrenceIndex, result, limiter);
            }
            index = index + 1;
        }
    }

    internal static void ValidateMediaSimulcastAttributes(SdpSessionDescription description, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
        List<SdpRidAttribute> rids = ReadRids(media);
        bool rtpProtocol = SdpRtpValidationCore.IsRtpProtocol(media.Protocol);
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.Simulcast))
            {
                int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, index);
                if (occurrenceIndex > 0)
                {
                    AddSimulcastIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, SdpScope.Media, attribute, mediaIndex, occurrenceIndex, "A media section may contain at most one simulcast attribute.");
                }
                if (!rtpProtocol)
                {
                    AddSimulcastIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpScope.Media, attribute, mediaIndex, occurrenceIndex, "The simulcast attribute requires an RTP transport protocol.");
                }
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success || decoded.SimulcastAttribute == null)
                {
                    AddDecodeIssue(decoded, attribute, SdpScope.Media, mediaIndex, occurrenceIndex, result, limiter);
                }
                else if (rtpProtocol && !decoded.SimulcastAttribute.LegacySyntax)
                {
                    ValidateModernSimulcast(description, media, mediaIndex, decoded.SimulcastAttribute, rids, result, limiter);
                    ValidateAnswerSimulcast(description, mediaIndex, decoded.SimulcastAttribute, result, limiter);
                }
            }
            index = index + 1;
        }
    }

    internal static List<SdpRidAttribute> ReadRids(SdpMediaDescription media)
    {
        List<SdpRidAttribute> rids = new List<SdpRidAttribute>();
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.Rid))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.RidAttribute != null)
                {
                    rids.Add(decoded.RidAttribute);
                }
            }
            index = index + 1;
        }
        return rids;
    }

    internal static void ValidateDecodedSimulcast(SdpAttribute attribute, SdpScope scope, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
        if (!decoded.Success)
        {
            AddDecodeIssue(decoded, attribute, scope, mediaIndex, occurrenceIndex, result, limiter);
        }
    }

    internal static void ValidateModernSimulcast(SdpSessionDescription description, SdpMediaDescription media, int mediaIndex, SdpSimulcastAttribute simulcast, List<SdpRidAttribute> rids, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<string> usedIdentifiers = new List<string>();
        ValidateStreamList(media, mediaIndex, simulcast, simulcast.SendStreams, SdpDirection.SendOnly, rids, usedIdentifiers, result, limiter);
        ValidateStreamList(media, mediaIndex, simulcast, simulcast.ReceiveStreams, SdpDirection.ReceiveOnly, rids, usedIdentifiers, result, limiter);
        if (!HasRtpStreamIdExtension(description, mediaIndex))
        {
            AddProjectionIssue(media, mediaIndex, simulcast, SdpDiagnosticCodes.ValidationSimulcast, "Simulcast signaling requires an effective RtpStreamId extmap mapping.", result, limiter);
        }
    }

    internal static void ValidateStreamList(SdpMediaDescription media, int mediaIndex, SdpSimulcastAttribute simulcast, List<SdpSimulcastStream> streams, SdpDirection direction, List<SdpRidAttribute> rids, List<string> usedIdentifiers, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int streamIndex = 0;
        while (streamIndex < streams.Count)
        {
            SdpSimulcastStream stream = streams[streamIndex];
            int alternativeIndex = 0;
            while (alternativeIndex < stream.Alternatives.Count)
            {
                SdpSimulcastAlternative alternative = stream.Alternatives[alternativeIndex];
                if (usedIdentifiers.Contains(alternative.RidId))
                {
                    AddProjectionIssue(media, mediaIndex, simulcast, SdpDiagnosticCodes.ValidationSimulcast, "A rid identifier must not occur more than once in one simulcast attribute.", result, limiter);
                }
                else
                {
                    usedIdentifiers.Add(alternative.RidId);
                }
                SdpRidAttribute? rid = FindUniqueRid(rids, alternative.RidId);
                if (rid == null)
                {
                    AddProjectionIssue(media, mediaIndex, simulcast, SdpDiagnosticCodes.ValidationSimulcast, "Every simulcast alternative must unambiguously identify an existing rid in the same media section.", result, limiter);
                }
                else if (rid.Direction != direction)
                {
                    AddProjectionIssue(media, mediaIndex, simulcast, SdpDiagnosticCodes.ValidationSimulcast, "A simulcast alternative direction must match its referenced rid direction.", result, limiter);
                }
                else if (alternative.Paused && !HasPauseCapability(media, rid))
                {
                    AddProjectionIssue(media, mediaIndex, simulcast, SdpDiagnosticCodes.ValidationSimulcast, "Every initially paused simulcast alternative requires pause feedback capability for all related payload types.", result, limiter);
                }
                alternativeIndex = alternativeIndex + 1;
            }
            streamIndex = streamIndex + 1;
        }
    }

    internal static SdpRidAttribute? FindUniqueRid(List<SdpRidAttribute> rids, string identifier)
    {
        SdpRidAttribute? result = null;
        int index = 0;
        while (index < rids.Count)
        {
            if (rids[index].Id == identifier)
            {
                if (result != null)
                {
                    return null;
                }
                result = rids[index];
            }
            index = index + 1;
        }
        return result;
    }

    internal static bool HasPauseCapability(SdpMediaDescription media, SdpRidAttribute rid)
    {
        if (rid.PayloadTypes.Count > 0)
        {
            int payloadIndex = 0;
            while (payloadIndex < rid.PayloadTypes.Count)
            {
                if (!HasPauseFeedback(media.Attributes, rid.PayloadTypes[payloadIndex]))
                {
                    return false;
                }
                payloadIndex = payloadIndex + 1;
            }
            return true;
        }
        bool foundPayload = false;
        int formatIndex = 0;
        while (formatIndex < media.Formats.Count)
        {
            string format = media.Formats[formatIndex];
            if (SdpCommonAttributeCodecCore.IsZeroBasedIntegerValue(format))
            {
                SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(format.AsSpan());
                if (payload.Success)
                {
                    foundPayload = true;
                    if (!HasPauseFeedback(media.Attributes, payload.Int32Value))
                    {
                        return false;
                    }
                }
            }
            formatIndex = formatIndex + 1;
        }
        return foundPayload;
    }

    internal static bool HasPauseFeedback(List<SdpAttribute> attributes, int payloadType)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.RtcpFeedback))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                SdpRtcpFeedbackAttribute? feedback = decoded.RtcpFeedbackAttribute;
                if (decoded.Success && feedback != null && feedback.Type == SdpRtcpFeedbackTypes.CodecControlMessage && FeedbackIsPause(feedback) && FeedbackAppliesToPayload(feedback, payloadType))
                {
                    return true;
                }
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool FeedbackIsPause(SdpRtcpFeedbackAttribute feedback)
    {
        if (!feedback.HasParameter)
        {
            return false;
        }
        string[] tokens = SdpRtcpAttributeCodecCore.ReadWhitespaceTokens(feedback.Parameter);
        return tokens.Length > 0 && tokens[0] == "pause";
    }

    internal static bool FeedbackAppliesToPayload(SdpRtcpFeedbackAttribute feedback, int payloadType)
    {
        if (feedback.PayloadType == "*")
        {
            return true;
        }
        SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(feedback.PayloadType.AsSpan());
        return payload.Success && payload.Int32Value == payloadType;
    }

    internal static bool HasRtpStreamIdExtension(SdpSessionDescription description, int mediaIndex)
    {
        List<SdpExtMapAttribute> mappings = SdpExtMapServiceCore.ResolveMediaExtMaps(description, mediaIndex);
        int index = 0;
        while (index < mappings.Count)
        {
            if (mappings[index].Uri == SdpHeaderExtensionUris.RtpStreamId)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static void ValidateAnswerSimulcast(SdpSessionDescription description, int mediaIndex, SdpSimulcastAttribute answer, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpSessionDescription? offer = result.AppliedOptions.OfferDescription;
        if (result.AppliedOptions.NegotiationRole != SdpNegotiationRole.Answerer || offer == null || mediaIndex >= offer.MediaDescriptions.Count)
        {
            return;
        }
        SdpSimulcastAttribute? offered = ReadModernSimulcast(offer.MediaDescriptions[mediaIndex]);
        if (offered == null)
        {
            AddProjectionIssue(description.MediaDescriptions[mediaIndex], mediaIndex, answer, SdpDiagnosticCodes.ValidationSimulcast, "An SDP answer must not add simulcast signaling that was absent from the offer.", result, limiter);
            return;
        }
        bool sendValid = StreamsAreOrderedSubset(answer.SendStreams, offered.ReceiveStreams);
        bool receiveValid = StreamsAreOrderedSubset(answer.ReceiveStreams, offered.SendStreams);
        if (!sendValid || !receiveValid)
        {
            AddProjectionIssue(description.MediaDescriptions[mediaIndex], mediaIndex, answer, SdpDiagnosticCodes.ValidationSimulcast, "An SDP answer must reverse directions and retain only the offered stream and alternative order.", result, limiter);
        }
    }

    internal static SdpSimulcastAttribute? ReadModernSimulcast(SdpMediaDescription media)
    {
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.Simulcast))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.SimulcastAttribute != null && !decoded.SimulcastAttribute.LegacySyntax)
                {
                    return decoded.SimulcastAttribute;
                }
            }
            index = index + 1;
        }
        return null;
    }

    internal static bool StreamsAreOrderedSubset(List<SdpSimulcastStream> answer, List<SdpSimulcastStream> offer)
    {
        int offeredIndex = 0;
        int answerIndex = 0;
        while (answerIndex < answer.Count)
        {
            bool matched = false;
            while (offeredIndex < offer.Count && !matched)
            {
                if (AlternativesAreOrderedSubset(answer[answerIndex].Alternatives, offer[offeredIndex].Alternatives))
                {
                    matched = true;
                }
                offeredIndex = offeredIndex + 1;
            }
            if (!matched)
            {
                return false;
            }
            answerIndex = answerIndex + 1;
        }
        return true;
    }

    internal static bool AlternativesAreOrderedSubset(List<SdpSimulcastAlternative> answer, List<SdpSimulcastAlternative> offer)
    {
        int offeredIndex = 0;
        int answerIndex = 0;
        while (answerIndex < answer.Count)
        {
            bool matched = false;
            while (offeredIndex < offer.Count && !matched)
            {
                if (answer[answerIndex].RidId == offer[offeredIndex].RidId)
                {
                    matched = true;
                }
                offeredIndex = offeredIndex + 1;
            }
            if (!matched)
            {
                return false;
            }
            answerIndex = answerIndex + 1;
        }
        return true;
    }

    internal static void AddProjectionIssue(SdpMediaDescription media, int mediaIndex, SdpSimulcastAttribute simulcast, string code, string detail, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpAttribute? attribute = simulcast.SourceAttribute;
        if (attribute != null)
        {
            int attributeIndex = media.Attributes.IndexOf(attribute);
            int occurrenceIndex = 0;
            if (attributeIndex >= 0)
            {
                occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, attributeIndex);
            }
            AddSimulcastIssue(result, limiter, code, SdpScope.Media, attribute, mediaIndex, occurrenceIndex, detail);
        }
    }

    internal static void AddDecodeIssue(SdpAttributeDecodeResult decoded, SdpAttribute attribute, SdpScope scope, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        string code = SdpDiagnosticCodes.ValidationAttribute;
        string detail = "The simulcast attribute does not satisfy its registered syntax.";
        if (decoded.Diagnostics.Count > 0)
        {
            code = decoded.Diagnostics[0].Code;
            detail = decoded.Diagnostics[0].Detail;
        }
        AddSimulcastIssue(result, limiter, code, scope, attribute, mediaIndex, occurrenceIndex, detail);
    }

    internal static void AddSimulcastIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, string code, SdpScope scope, SdpAttribute attribute, int mediaIndex, int occurrenceIndex, string detail)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, code, SdpValidationSeverity.Error, scope, attribute.Name, mediaIndex, occurrenceIndex, detail, attribute.Location);
    }
}
