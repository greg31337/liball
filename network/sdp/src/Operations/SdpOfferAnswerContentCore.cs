using System;
using System.Collections.Generic;
using System.Globalization;

namespace sdp;

internal static class SdpOfferAnswerContentCore
{
    internal static SdpRejectionReason SelectDataChannelRejectionReason(SdpMediaDescription offeredMedia, SdpMediaCapability capability, SdpEndpointCapabilities endpoint)
    {
        bool sctp = SdpDataChannelValidationCore.IsModernSctpProtocol(offeredMedia.Protocol) || SdpDataChannelValidationCore.IsLegacySctpProtocol(offeredMedia.Protocol);
        if (!sctp)
        {
            return SdpRejectionReason.Unknown;
        }
        if (!endpoint.SupportsDataChannels || capability.Sctp == null)
        {
            return SdpRejectionReason.DataChannelRequirementsNotMet;
        }
        SdpSctpDescription offered = SdpDataChannelServiceCore.ResolveMediaSctp(offeredMedia);
        SdpSctpDescription local = NormalizeLocalSctp(capability.Sctp, offeredMedia.Protocol);
        bool offeredSignaling = offered.PortAttribute != null || offered.SctpMapAttribute != null;
        if (!offeredSignaling || local.Port < 0 || local.Port > 65535 || SdpDataChannelServiceCore.IntersectSctpCapabilities(offered, local) == null)
        {
            return SdpRejectionReason.DataChannelRequirementsNotMet;
        }
        return SdpRejectionReason.Unknown;
    }

    internal static SdpSctpDescription NormalizeLocalSctp(SdpSctpDescription source, string protocol)
    {
        SdpSctpDescription result = SdpDataChannelServiceCore.CopySctpDescription(source);
        if (result.Format.Length == 0)
        {
            result.Format = SdpDataChannelValidationCore.IsModernSctpProtocol(protocol) ? SdpDataChannelTokens.WebRtcDataChannel : string.Empty;
        }
        return result;
    }

    internal static SdpSctpDescription? ApplyAnswerContent(SdpMediaDescription offeredMedia, SdpMediaDescription answerMedia, SdpMediaCapability capability, SdpEndpointCapabilities endpoint, SdpDirection answerDirection, List<SdpPayloadMapping> payloadMappings)
    {
        if (SdpRtpValidationCore.IsRtpProtocol(offeredMedia.Protocol))
        {
            ApplyAnswerStreams(offeredMedia, answerMedia, capability, answerDirection, payloadMappings);
            return null;
        }
        bool sctp = SdpDataChannelValidationCore.IsModernSctpProtocol(offeredMedia.Protocol) || SdpDataChannelValidationCore.IsLegacySctpProtocol(offeredMedia.Protocol);
        if (sctp && endpoint.SupportsDataChannels && capability.Sctp != null)
        {
            return ApplyAnswerSctp(offeredMedia, answerMedia, capability);
        }
        return null;
    }

    internal static void ApplyAnswerStreams(SdpMediaDescription offeredMedia, SdpMediaDescription answerMedia, SdpMediaCapability capability, SdpDirection answerDirection, List<SdpPayloadMapping> payloadMappings)
    {
        bool sends = answerDirection == SdpDirection.SendOnly || answerDirection == SdpDirection.SendReceive;
        if (sends)
        {
            AddLocalStreamAttributes(answerMedia, capability);
        }
        if (HasRtpStreamIdExtension(answerMedia))
        {
            ApplyAnswerRids(offeredMedia, answerMedia, capability, payloadMappings);
        }
        ApplyAnswerImageAttributes(offeredMedia, answerMedia, capability, answerDirection, payloadMappings);
    }

    internal static void AddLocalStreamAttributes(SdpMediaDescription answerMedia, SdpMediaCapability capability)
    {
        int index = 0;
        while (index < capability.MediaStreamIds.Count)
        {
            SdpSemanticComparisonCore.AddEncodedAttribute(answerMedia.Attributes, capability.MediaStreamIds[index]);
            index = index + 1;
        }
        index = 0;
        while (index < capability.SourceAttributes.Count)
        {
            SdpSemanticComparisonCore.AddEncodedAttribute(answerMedia.Attributes, capability.SourceAttributes[index]);
            index = index + 1;
        }
        index = 0;
        while (index < capability.SourceGroups.Count)
        {
            SdpSemanticComparisonCore.AddEncodedAttribute(answerMedia.Attributes, capability.SourceGroups[index]);
            index = index + 1;
        }
    }

    internal static bool HasRtpStreamIdExtension(SdpMediaDescription media)
    {
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.ExtMap, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.ExtMapAttribute != null && decoded.ExtMapAttribute.Uri == SdpHeaderExtensionUris.RtpStreamId)
                {
                    return true;
                }
            }
            index = index + 1;
        }
        return false;
    }

    internal static void ApplyAnswerRids(SdpMediaDescription offeredMedia, SdpMediaDescription answerMedia, SdpMediaCapability capability, List<SdpPayloadMapping> payloadMappings)
    {
        List<SdpRidAttribute> offeredRids = SdpStreamServiceCore.ResolveRids(offeredMedia);
        List<SdpRidAttribute> selectedRids = new List<SdpRidAttribute>();
        int offeredIndex = 0;
        while (offeredIndex < offeredRids.Count)
        {
            SdpRidAttribute offered = offeredRids[offeredIndex];
            SdpDirection answerDirection = ReverseRidDirection(offered.Direction);
            SdpRidAttribute? local = FindLocalRid(capability.Rids, offered.Id, answerDirection);
            if (local != null)
            {
                SdpRidAttribute selected = CreateAnswerRid(offered, local, answerDirection, payloadMappings);
                if (offered.PayloadTypes.Count == 0 && local.PayloadTypes.Count == 0 || selected.PayloadTypes.Count > 0)
                {
                    selectedRids.Add(selected);
                }
            }
            offeredIndex = offeredIndex + 1;
        }
        RemoveMissingRidDependencies(selectedRids);
        int selectedIndex = 0;
        while (selectedIndex < selectedRids.Count)
        {
            SdpSemanticComparisonCore.AddEncodedAttribute(answerMedia.Attributes, selectedRids[selectedIndex]);
            selectedIndex = selectedIndex + 1;
        }
        SdpSimulcastAttribute? answerSimulcast = CreateAnswerSimulcast(offeredMedia, capability.Simulcast, selectedRids);
        if (answerSimulcast != null)
        {
            SdpSemanticComparisonCore.AddEncodedAttribute(answerMedia.Attributes, answerSimulcast);
        }
    }

    internal static SdpDirection ReverseRidDirection(SdpDirection direction)
    {
        if (direction == SdpDirection.SendOnly)
        {
            return SdpDirection.ReceiveOnly;
        }
        if (direction == SdpDirection.ReceiveOnly)
        {
            return SdpDirection.SendOnly;
        }
        return SdpDirection.Unknown;
    }

    internal static SdpRidAttribute? FindLocalRid(List<SdpRidAttribute> localRids, string identifier, SdpDirection direction)
    {
        int index = 0;
        while (index < localRids.Count)
        {
            if (localRids[index].Id == identifier && localRids[index].Direction == direction)
            {
                return localRids[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static SdpRidAttribute CreateAnswerRid(SdpRidAttribute offered, SdpRidAttribute local, SdpDirection direction, List<SdpPayloadMapping> payloadMappings)
    {
        SdpRidAttribute result = new SdpRidAttribute();
        result.Id = offered.Id;
        result.Direction = direction;
        if (offered.PayloadTypes.Count == 0 && local.PayloadTypes.Count == 0)
        {
            return CopyAnswerRidRestrictions(result, local);
        }
        int mappingIndex = 0;
        while (mappingIndex < payloadMappings.Count)
        {
            SdpPayloadMapping mapping = payloadMappings[mappingIndex];
            bool offeredAllows = offered.PayloadTypes.Count == 0 || offered.PayloadTypes.Contains(mapping.OfferedPayloadType);
            bool localAllows = local.PayloadTypes.Count == 0 || local.PayloadTypes.Contains(mapping.OfferedPayloadType) || local.PayloadTypes.Contains(mapping.NegotiatedPayloadType);
            if (offeredAllows && localAllows && !result.PayloadTypes.Contains(mapping.NegotiatedPayloadType))
            {
                result.PayloadTypes.Add(mapping.NegotiatedPayloadType);
            }
            mappingIndex = mappingIndex + 1;
        }
        return CopyAnswerRidRestrictions(result, local);
    }

    internal static SdpRidAttribute CopyAnswerRidRestrictions(SdpRidAttribute result, SdpRidAttribute local)
    {
        int restrictionIndex = 0;
        while (restrictionIndex < local.Restrictions.Count)
        {
            SdpRidRestriction source = local.Restrictions[restrictionIndex];
            SdpRidRestriction restriction = new SdpRidRestriction();
            restriction.Name = source.Name;
            restriction.HasValue = source.HasValue;
            restriction.Value = source.Value;
            restriction.RawText = source.RawText;
            result.Restrictions.Add(restriction);
            restrictionIndex = restrictionIndex + 1;
        }
        return result;
    }

    internal static int FindNegotiatedPayload(List<SdpPayloadMapping> mappings, int offeredPayload)
    {
        int index = 0;
        while (index < mappings.Count)
        {
            if (mappings[index].OfferedPayloadType == offeredPayload)
            {
                return mappings[index].NegotiatedPayloadType;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static void RemoveMissingRidDependencies(List<SdpRidAttribute> rids)
    {
        List<string> identifiers = new List<string>();
        int index = 0;
        while (index < rids.Count)
        {
            identifiers.Add(rids[index].Id);
            index = index + 1;
        }
        index = 0;
        while (index < rids.Count)
        {
            int restrictionIndex = rids[index].Restrictions.Count - 1;
            while (restrictionIndex >= 0)
            {
                SdpRidRestriction restriction = rids[index].Restrictions[restrictionIndex];
                if (restriction.Name == "depend")
                {
                    List<string> retained = RetainRidIdentifiers(restriction.Value, identifiers);
                    if (retained.Count == 0)
                    {
                        rids[index].Restrictions.RemoveAt(restrictionIndex);
                    }
                    else
                    {
                        restriction.Value = JoinIdentifiers(retained);
                        restriction.RawText = restriction.Name + "=" + restriction.Value;
                    }
                }
                restrictionIndex = restrictionIndex - 1;
            }
            index = index + 1;
        }
    }

    internal static List<string> RetainRidIdentifiers(string value, List<string> available)
    {
        List<string> result = new List<string>();
        string[] identifiers = value.Split(',');
        int index = 0;
        while (index < identifiers.Length)
        {
            if (available.Contains(identifiers[index]) && !result.Contains(identifiers[index]))
            {
                result.Add(identifiers[index]);
            }
            index = index + 1;
        }
        return result;
    }

    internal static string JoinIdentifiers(List<string> identifiers)
    {
        string result = string.Empty;
        int index = 0;
        while (index < identifiers.Count)
        {
            if (index > 0)
            {
                result = result + ",";
            }
            result = result + identifiers[index];
            index = index + 1;
        }
        return result;
    }

    internal static SdpSimulcastAttribute? CreateAnswerSimulcast(SdpMediaDescription offeredMedia, SdpSimulcastAttribute? local, List<SdpRidAttribute> selectedRids)
    {
        SdpSimulcastAttribute? offered = SdpStreamServiceCore.ResolveSimulcast(offeredMedia);
        if (offered == null || offered.LegacySyntax || local == null || local.LegacySyntax)
        {
            return null;
        }
        List<string> selectedIds = new List<string>();
        int index = 0;
        while (index < selectedRids.Count)
        {
            selectedIds.Add(selectedRids[index].Id);
            index = index + 1;
        }
        SdpSimulcastAttribute result = new SdpSimulcastAttribute();
        result.SendFirst = local.SendFirst;
        CopyAnswerSimulcastStreams(offered.ReceiveStreams, local.SendStreams, selectedIds, result.SendStreams);
        CopyAnswerSimulcastStreams(offered.SendStreams, local.ReceiveStreams, selectedIds, result.ReceiveStreams);
        if (result.SendStreams.Count == 0 && result.ReceiveStreams.Count == 0)
        {
            return null;
        }
        return result;
    }

    internal static void CopyAnswerSimulcastStreams(List<SdpSimulcastStream> offered, List<SdpSimulcastStream> local, List<string> selectedIds, List<SdpSimulcastStream> destination)
    {
        List<string> localIds = new List<string>();
        AddSimulcastIdentifiers(local, localIds);
        int streamIndex = 0;
        while (streamIndex < offered.Count)
        {
            SdpSimulcastStream stream = new SdpSimulcastStream();
            int alternativeIndex = 0;
            while (alternativeIndex < offered[streamIndex].Alternatives.Count)
            {
                SdpSimulcastAlternative offeredAlternative = offered[streamIndex].Alternatives[alternativeIndex];
                if (selectedIds.Contains(offeredAlternative.RidId) && localIds.Contains(offeredAlternative.RidId))
                {
                    SdpSimulcastAlternative alternative = new SdpSimulcastAlternative();
                    alternative.RidId = offeredAlternative.RidId;
                    alternative.Paused = offeredAlternative.Paused;
                    stream.Alternatives.Add(alternative);
                }
                alternativeIndex = alternativeIndex + 1;
            }
            if (stream.Alternatives.Count > 0)
            {
                destination.Add(stream);
            }
            streamIndex = streamIndex + 1;
        }
    }

    internal static void AddSimulcastIdentifiers(List<SdpSimulcastStream> streams, List<string> identifiers)
    {
        int streamIndex = 0;
        while (streamIndex < streams.Count)
        {
            int alternativeIndex = 0;
            while (alternativeIndex < streams[streamIndex].Alternatives.Count)
            {
                string identifier = streams[streamIndex].Alternatives[alternativeIndex].RidId;
                if (!identifiers.Contains(identifier))
                {
                    identifiers.Add(identifier);
                }
                alternativeIndex = alternativeIndex + 1;
            }
            streamIndex = streamIndex + 1;
        }
    }

    internal static void ApplyAnswerImageAttributes(SdpMediaDescription offeredMedia, SdpMediaDescription answerMedia, SdpMediaCapability capability, SdpDirection answerDirection, List<SdpPayloadMapping> payloadMappings)
    {
        List<SdpImageAttribute> offeredImages = SdpStreamServiceCore.ResolveImageAttributes(offeredMedia);
        int offeredIndex = 0;
        while (offeredIndex < offeredImages.Count)
        {
            int localIndex = 0;
            while (localIndex < capability.ImageAttributes.Count)
            {
                SdpImageAttribute? answerImage = IntersectImageAttribute(offeredImages[offeredIndex], capability.ImageAttributes[localIndex], answerDirection, payloadMappings);
                if (answerImage != null)
                {
                    SdpSemanticComparisonCore.AddEncodedAttribute(answerMedia.Attributes, answerImage);
                    localIndex = capability.ImageAttributes.Count;
                }
                localIndex = localIndex + 1;
            }
            offeredIndex = offeredIndex + 1;
        }
    }

    internal static SdpImageAttribute? IntersectImageAttribute(SdpImageAttribute offered, SdpImageAttribute local, SdpDirection answerDirection, List<SdpPayloadMapping> payloadMappings)
    {
        string payload = IntersectImagePayload(offered.PayloadType, local.PayloadType, payloadMappings);
        if (payload.Length == 0)
        {
            return null;
        }
        SdpImageAttribute result = new SdpImageAttribute();
        result.PayloadType = payload;
        bool sends = answerDirection == SdpDirection.SendOnly || answerDirection == SdpDirection.SendReceive;
        bool receives = answerDirection == SdpDirection.ReceiveOnly || answerDirection == SdpDirection.SendReceive;
        if (sends && offered.HasReceiveConstraints && local.HasSendConstraints)
        {
            result.SendConstraints = IntersectImageConstraints(offered.ReceiveConstraints, local.SendConstraints);
            result.HasSendConstraints = result.SendConstraints.Length > 0;
        }
        if (receives && offered.HasSendConstraints && local.HasReceiveConstraints)
        {
            result.ReceiveConstraints = IntersectImageConstraints(offered.SendConstraints, local.ReceiveConstraints);
            result.HasReceiveConstraints = result.ReceiveConstraints.Length > 0;
        }
        if (!result.HasSendConstraints && !result.HasReceiveConstraints)
        {
            return null;
        }
        return result;
    }

    internal static string IntersectImagePayload(string offeredPayload, string localPayload, List<SdpPayloadMapping> payloadMappings)
    {
        if (offeredPayload == "*" && localPayload == "*")
        {
            return "*";
        }
        int offeredValue = ReadPayloadType(offeredPayload);
        if (offeredValue >= 0)
        {
            int negotiated = FindNegotiatedPayload(payloadMappings, offeredValue);
            if (negotiated < 0)
            {
                return string.Empty;
            }
            if (localPayload == "*" || localPayload == offeredPayload || localPayload == negotiated.ToString(CultureInfo.InvariantCulture))
            {
                return negotiated.ToString(CultureInfo.InvariantCulture);
            }
            return string.Empty;
        }
        int localValue = ReadPayloadType(localPayload);
        if (offeredPayload == "*" && localValue >= 0)
        {
            int mappingIndex = 0;
            while (mappingIndex < payloadMappings.Count)
            {
                if (payloadMappings[mappingIndex].OfferedPayloadType == localValue || payloadMappings[mappingIndex].NegotiatedPayloadType == localValue)
                {
                    return payloadMappings[mappingIndex].NegotiatedPayloadType.ToString(CultureInfo.InvariantCulture);
                }
                mappingIndex = mappingIndex + 1;
            }
        }
        return string.Empty;
    }

    internal static int ReadPayloadType(string value)
    {
        SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(value.AsSpan());
        return payload.Success ? payload.Int32Value : -1;
    }

    internal static string IntersectImageConstraints(string offered, string local)
    {
        if (offered == "*")
        {
            return local;
        }
        if (local == "*" || offered == local)
        {
            return offered;
        }
        return string.Empty;
    }

    internal static SdpSctpDescription ApplyAnswerSctp(SdpMediaDescription offeredMedia, SdpMediaDescription answerMedia, SdpMediaCapability capability)
    {
        SdpSctpDescription offered = SdpDataChannelServiceCore.ResolveMediaSctp(offeredMedia);
        SdpSctpDescription local = NormalizeLocalSctp(capability.Sctp == null ? new SdpSctpDescription() : capability.Sctp, offeredMedia.Protocol);
        SdpSctpDescription? selected = SdpDataChannelServiceCore.IntersectSctpCapabilities(offered, local);
        if (selected == null)
        {
            throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationDataChannelIncompatible, null);
        }
        SdpSctpDescription intersection = selected;
        intersection.Port = local.Port;
        intersection.PortAttribute = null;
        intersection.SctpMapAttribute = null;
        intersection.LegacySctpMap = offered.LegacySctpMap;
        intersection.HasMaximumStreams = local.HasMaximumStreams;
        intersection.MaximumStreams = local.MaximumStreams;
        if (intersection.LegacySctpMap)
        {
            answerMedia.Formats.Clear();
            answerMedia.Formats.Add(local.Port.ToString(CultureInfo.InvariantCulture));
        }
        List<string> supportedSubprotocols = ReadSupportedSubprotocols(capability.DataChannels);
        List<SdpDataChannelAttribute> selectedChannels = SdpDataChannelServiceCore.IntersectDataChannelCapabilities(SdpDataChannelServiceCore.ResolveDataChannels(offeredMedia), supportedSubprotocols);
        SdpMediaCapability signaling = new SdpMediaCapability();
        signaling.Sctp = intersection;
        int channelIndex = 0;
        while (channelIndex < selectedChannels.Count)
        {
            signaling.DataChannels.Add(selectedChannels[channelIndex]);
            channelIndex = channelIndex + 1;
        }
        SdpOfferAnswerCore.AddSctpAttributes(answerMedia, signaling);
        return intersection;
    }

    internal static List<string> ReadSupportedSubprotocols(List<SdpDataChannelAttribute> channels)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < channels.Count)
        {
            string subprotocol = SdpDataChannelServiceCore.ReadSubprotocol(channels[index]);
            if (!result.Contains(subprotocol))
            {
                result.Add(subprotocol);
            }
            index = index + 1;
        }
        return result;
    }
}
