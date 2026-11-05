using System;
using System.Collections.Generic;
using System.Globalization;

namespace sdp;

internal static class SdpSubsequentOfferCore
{
    internal static void ValidateArguments(SdpSessionDescription previousOffer, SdpNegotiationOptions options)
    {
        SdpOfferAnswerCore.ValidateOfferedDescription(previousOffer);
        if (options.Role != SdpNegotiationRole.Unknown && options.Role != SdpNegotiationRole.Offerer)
        {
            throw new ArgumentException("Subsequent offer creation requires the offerer role.", "options");
        }
        SdpSessionDescription template = CreateTemplate(previousOffer);
        SdpOfferAnswerCore.ValidateInitialOfferArguments(template, options);
    }

    internal static SdpNegotiationResult CreateOffer(SdpSessionDescription previousOffer, SdpNegotiationOptions options)
    {
        SdpSessionDescription template = CreateTemplate(previousOffer);
        SdpNegotiationResult generated = SdpOfferAnswerCore.CreateInitialOffer(template, options);
        SdpSessionDescription current = generated.Description == null ? new SdpSessionDescription() : generated.Description;
        SdpEndpointCapabilities capabilities = options.LocalCapabilities == null ? new SdpEndpointCapabilities() : options.LocalCapabilities;
        List<int> oldForNew = MatchMedia(previousOffer, current, options);
        List<int> desiredOutputIndexes = new List<int>();
        SdpSessionDescription resultDescription = SdpCloneCore.CopyDescription(current);
        resultDescription.MediaDescriptions.Clear();

        if (options.PreserveMediaOrder)
        {
            AddInPreviousOrder(previousOffer, current, capabilities, oldForNew, options, resultDescription, desiredOutputIndexes);
        }
        else
        {
            AddInCapabilityOrder(previousOffer, current, capabilities, oldForNew, options, resultDescription, desiredOutputIndexes);
        }
        RebuildBundleState(previousOffer, resultDescription, capabilities, desiredOutputIndexes);
        AlignBundleExtMaps(resultDescription);
        ApplyOriginVersion(previousOffer, resultDescription, options);
        SdpSemanticComparisonCore.ClearRawFormatting(resultDescription);
        return CreateResult(previousOffer, resultDescription, options, desiredOutputIndexes);
    }

    internal static SdpSessionDescription CreateTemplate(SdpSessionDescription previousOffer)
    {
        SdpSessionDescription template = SdpCloneCore.CopyDescription(previousOffer);
        template.MediaDescriptions.Clear();
        RemoveBundleGroupAttributes(template.Attributes);
        return template;
    }

    internal static void RemoveBundleGroupAttributes(List<SdpAttribute> attributes)
    {
        int index = attributes.Count - 1;
        while (index >= 0)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.Group, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.GroupAttribute != null && decoded.GroupAttribute.Semantic == SdpGroupSemantic.Bundle)
                {
                    attributes.RemoveAt(index);
                }
            }
            index = index - 1;
        }
    }

    internal static List<int> MatchMedia(SdpSessionDescription previousOffer, SdpSessionDescription generatedOffer, SdpNegotiationOptions options)
    {
        List<int> result = new List<int>();
        List<int> usedOldIndexes = new List<int>();
        int newIndex = 0;
        while (newIndex < generatedOffer.MediaDescriptions.Count)
        {
            SdpMediaDescription generatedMedia = generatedOffer.MediaDescriptions[newIndex];
            string generatedId = ReadMediaId(generatedMedia);
            int oldIndex = FindOldByMediaId(previousOffer, generatedId, generatedMedia.MediaType, usedOldIndexes);
            if (oldIndex < 0 && options.PreserveMediaOrder && newIndex < previousOffer.MediaDescriptions.Count)
            {
                SdpMediaDescription positional = previousOffer.MediaDescriptions[newIndex];
                string positionalId = ReadMediaId(positional);
                bool identifiersPermitMatch = generatedId.Length == 0 || positionalId.Length == 0 || generatedId == positionalId;
                if (!usedOldIndexes.Contains(newIndex) && identifiersPermitMatch && string.Equals(positional.MediaType, generatedMedia.MediaType, StringComparison.OrdinalIgnoreCase))
                {
                    oldIndex = newIndex;
                }
            }
            result.Add(oldIndex);
            if (oldIndex >= 0)
            {
                usedOldIndexes.Add(oldIndex);
            }
            newIndex = newIndex + 1;
        }
        return result;
    }

    internal static int FindOldByMediaId(SdpSessionDescription previousOffer, string mediaId, string mediaType, List<int> usedOldIndexes)
    {
        if (mediaId.Length == 0)
        {
            return -1;
        }
        int oldIndex = 0;
        while (oldIndex < previousOffer.MediaDescriptions.Count)
        {
            if (!usedOldIndexes.Contains(oldIndex) && ReadMediaId(previousOffer.MediaDescriptions[oldIndex]) == mediaId)
            {
                if (!string.Equals(previousOffer.MediaDescriptions[oldIndex].MediaType, mediaType, StringComparison.OrdinalIgnoreCase))
                {
                    throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationInvalidOffer, null);
                }
                return oldIndex;
            }
            oldIndex = oldIndex + 1;
        }
        return -1;
    }

    internal static void AddInPreviousOrder(SdpSessionDescription previousOffer, SdpSessionDescription generatedOffer, SdpEndpointCapabilities capabilities, List<int> oldForNew, SdpNegotiationOptions options, SdpSessionDescription result, List<int> desiredOutputIndexes)
    {
        List<int> usedNewIndexes = new List<int>();
        int oldIndex = 0;
        while (oldIndex < previousOffer.MediaDescriptions.Count)
        {
            int newIndex = FindNewIndex(oldForNew, oldIndex);
            if (newIndex >= 0)
            {
                if (CanContinueMedia(previousOffer.MediaDescriptions[oldIndex], capabilities.Media[newIndex]))
                {
                    SdpMediaDescription media = CreateContinuedMedia(previousOffer, oldIndex, generatedOffer.MediaDescriptions[newIndex], capabilities.Media[newIndex], capabilities, options);
                    result.MediaDescriptions.Add(media);
                    desiredOutputIndexes.Add(result.MediaDescriptions.Count - 1);
                }
                else
                {
                    SdpMediaDescription rejected = SdpCloneCore.CopyMedia(previousOffer.MediaDescriptions[oldIndex]);
                    rejected.Port = 0;
                    result.MediaDescriptions.Add(rejected);
                }
                usedNewIndexes.Add(newIndex);
            }
            else if (options.PreserveRejectedMedia)
            {
                SdpMediaDescription rejected = SdpCloneCore.CopyMedia(previousOffer.MediaDescriptions[oldIndex]);
                rejected.Port = 0;
                result.MediaDescriptions.Add(rejected);
            }
            oldIndex = oldIndex + 1;
        }

        int appendedIndex = 0;
        while (appendedIndex < generatedOffer.MediaDescriptions.Count)
        {
            if (!usedNewIndexes.Contains(appendedIndex))
            {
                result.MediaDescriptions.Add(SdpCloneCore.CopyMedia(generatedOffer.MediaDescriptions[appendedIndex]));
                desiredOutputIndexes.Add(result.MediaDescriptions.Count - 1);
            }
            appendedIndex = appendedIndex + 1;
        }
    }

    internal static void AddInCapabilityOrder(SdpSessionDescription previousOffer, SdpSessionDescription generatedOffer, SdpEndpointCapabilities capabilities, List<int> oldForNew, SdpNegotiationOptions options, SdpSessionDescription result, List<int> desiredOutputIndexes)
    {
        List<int> usedOldIndexes = new List<int>();
        int newIndex = 0;
        while (newIndex < generatedOffer.MediaDescriptions.Count)
        {
            int oldIndex = oldForNew[newIndex];
            SdpMediaDescription media = SdpCloneCore.CopyMedia(generatedOffer.MediaDescriptions[newIndex]);
            if (oldIndex >= 0)
            {
                if (CanContinueMedia(previousOffer.MediaDescriptions[oldIndex], capabilities.Media[newIndex]))
                {
                    media = CreateContinuedMedia(previousOffer, oldIndex, generatedOffer.MediaDescriptions[newIndex], capabilities.Media[newIndex], capabilities, options);
                }
                else
                {
                    media = SdpCloneCore.CopyMedia(previousOffer.MediaDescriptions[oldIndex]);
                    media.Port = 0;
                }
                usedOldIndexes.Add(oldIndex);
            }
            result.MediaDescriptions.Add(media);
            if (oldIndex < 0 || CanContinueMedia(previousOffer.MediaDescriptions[oldIndex], capabilities.Media[newIndex]))
            {
                desiredOutputIndexes.Add(result.MediaDescriptions.Count - 1);
            }
            newIndex = newIndex + 1;
        }
        if (options.PreserveRejectedMedia)
        {
            int oldIndex = 0;
            while (oldIndex < previousOffer.MediaDescriptions.Count)
            {
                if (!usedOldIndexes.Contains(oldIndex))
                {
                    SdpMediaDescription rejected = SdpCloneCore.CopyMedia(previousOffer.MediaDescriptions[oldIndex]);
                    rejected.Port = 0;
                    result.MediaDescriptions.Add(rejected);
                }
                oldIndex = oldIndex + 1;
            }
        }
    }

    internal static int FindNewIndex(List<int> oldForNew, int oldIndex)
    {
        int newIndex = 0;
        while (newIndex < oldForNew.Count)
        {
            if (oldForNew[newIndex] == oldIndex)
            {
                return newIndex;
            }
            newIndex = newIndex + 1;
        }
        return -1;
    }

    internal static bool CanContinueMedia(SdpMediaDescription oldMedia, SdpMediaCapability capability)
    {
        return string.Equals(oldMedia.MediaType, capability.MediaType, StringComparison.OrdinalIgnoreCase) && SdpOfferAnswerCore.ContainsCompatibleProtocol(capability.Protocols, oldMedia.Protocol);
    }

    internal static SdpMediaDescription CreateContinuedMedia(SdpSessionDescription previousOffer, int oldIndex, SdpMediaDescription generatedMedia, SdpMediaCapability capability, SdpEndpointCapabilities endpoint, SdpNegotiationOptions options)
    {
        SdpMediaDescription oldMedia = previousOffer.MediaDescriptions[oldIndex];
        SdpMediaDescription result = SdpCloneCore.CopyMedia(generatedMedia);
        result.Protocol = oldMedia.Protocol;
        if (options.PreserveMediaIds)
        {
            string oldMediaId = ReadMediaId(oldMedia);
            if (oldMediaId.Length > 0)
            {
                ReplaceMediaId(result, oldMediaId);
            }
        }
        StabilizePayloadTypes(oldMedia, result, capability, options);
        StabilizeExtMapIds(oldMedia, result);
        PreserveIceContinuity(previousOffer, oldIndex, result, capability, endpoint, options);
        PreserveDtlsContinuity(previousOffer, oldIndex, result);
        PreserveMediaMetadata(oldMedia, result);
        PreserveUnknownAttributes(oldMedia, result);
        if (capability.BundleOnly)
        {
            result.Port = 0;
            if (FindAttribute(result.Attributes, SdpAttributeNames.BundleOnly) == null)
            {
                result.Attributes.Add(SdpSemanticComparisonCore.CreatePropertyAttribute(SdpAttributeNames.BundleOnly));
            }
        }
        return result;
    }

    internal static void StabilizePayloadTypes(SdpMediaDescription oldMedia, SdpMediaDescription currentMedia, SdpMediaCapability capability, SdpNegotiationOptions options)
    {
        if (!SdpRtpValidationCore.IsRtpProtocol(currentMedia.Protocol))
        {
            return;
        }
        List<SdpCodec> oldCodecs = SdpCodecServiceCore.ResolveMediaCodecs(oldMedia);
        List<SdpCodec> currentCodecs = SdpCodecServiceCore.ResolveMediaCodecs(currentMedia);
        List<int> usedOldCodecIndexes = new List<int>();
        List<int> negotiatedPayloadTypes = new List<int>();
        int codecIndex = 0;
        while (codecIndex < currentCodecs.Count)
        {
            int oldCodecIndex = FindMatchingOldCodec(oldCodecs, capability.Codecs[codecIndex], options.CodecMatchMode, usedOldCodecIndexes);
            int payloadType = -1;
            if (oldCodecIndex >= 0)
            {
                payloadType = oldCodecs[oldCodecIndex].PayloadType;
                usedOldCodecIndexes.Add(oldCodecIndex);
            }
            negotiatedPayloadTypes.Add(payloadType);
            codecIndex = codecIndex + 1;
        }

        List<int> reservedPayloadTypes = new List<int>();
        int reservationIndex = 0;
        while (reservationIndex < oldCodecs.Count)
        {
            if (!reservedPayloadTypes.Contains(oldCodecs[reservationIndex].PayloadType))
            {
                reservedPayloadTypes.Add(oldCodecs[reservationIndex].PayloadType);
            }
            reservationIndex = reservationIndex + 1;
        }
        List<int> usedPayloadTypes = new List<int>();
        codecIndex = 0;
        while (codecIndex < negotiatedPayloadTypes.Count)
        {
            if (negotiatedPayloadTypes[codecIndex] >= 0)
            {
                if (usedPayloadTypes.Contains(negotiatedPayloadTypes[codecIndex]))
                {
                    throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationPayloadAllocationFailed, null);
                }
                usedPayloadTypes.Add(negotiatedPayloadTypes[codecIndex]);
            }
            codecIndex = codecIndex + 1;
        }
        codecIndex = 0;
        while (codecIndex < negotiatedPayloadTypes.Count)
        {
            if (negotiatedPayloadTypes[codecIndex] < 0)
            {
                int generatedPayload = currentCodecs[codecIndex].PayloadType;
                List<int> unavailablePayloadTypes = CombinedIdentifiers(usedPayloadTypes, reservedPayloadTypes);
                int payloadType = unavailablePayloadTypes.Contains(generatedPayload) ? SdpOfferAnswerCore.FindFreeDynamicPayloadType(unavailablePayloadTypes) : generatedPayload;
                negotiatedPayloadTypes[codecIndex] = payloadType;
                usedPayloadTypes.Add(payloadType);
            }
            codecIndex = codecIndex + 1;
        }

        List<SdpPayloadMapping> mappings = new List<SdpPayloadMapping>();
        codecIndex = 0;
        while (codecIndex < currentCodecs.Count)
        {
            SdpPayloadMapping mapping = new SdpPayloadMapping();
            mapping.MediaIndex = 0;
            mapping.OfferedPayloadType = currentCodecs[codecIndex].PayloadType;
            mapping.NegotiatedPayloadType = negotiatedPayloadTypes[codecIndex];
            mapping.EncodingName = currentCodecs[codecIndex].EncodingName;
            mappings.Add(mapping);
            codecIndex = codecIndex + 1;
        }
        SdpEffectiveMedia graph = new SdpEffectiveMedia();
        graph.Codecs = currentCodecs;
        graph.Rids = SdpSimulcastValidationCore.ReadRids(currentMedia);
        SdpCodecServiceCore.ValidatePayloadRemap(graph, mappings);
        SdpCodecServiceCore.RemapPayloadTypes(graph, mappings);
        currentMedia.Formats.Clear();
        codecIndex = 0;
        while (codecIndex < currentCodecs.Count)
        {
            currentMedia.Formats.Add(currentCodecs[codecIndex].PayloadType.ToString(CultureInfo.InvariantCulture));
            codecIndex = codecIndex + 1;
        }
    }

    internal static int FindMatchingOldCodec(List<SdpCodec> oldCodecs, SdpCodecCapability capability, SdpCodecMatchMode matchMode, List<int> usedOldCodecIndexes)
    {
        int oldIndex = 0;
        while (oldIndex < oldCodecs.Count)
        {
            if (!usedOldCodecIndexes.Contains(oldIndex) && SdpCodecServiceCore.IsCodecMatch(oldCodecs[oldIndex], capability, matchMode))
            {
                return oldIndex;
            }
            oldIndex = oldIndex + 1;
        }
        return -1;
    }

    internal static void StabilizeExtMapIds(SdpMediaDescription oldMedia, SdpMediaDescription currentMedia)
    {
        List<SdpExtMapAttribute> oldMappings = ReadExtMaps(oldMedia);
        List<SdpExtMapAttribute> currentMappings = ReadExtMaps(currentMedia);
        List<int> assigned = new List<int>();
        List<int> reserved = new List<int>();
        int index = 0;
        while (index < currentMappings.Count)
        {
            int oldId = FindMatchingExtMapId(oldMappings, currentMappings[index], reserved);
            reserved.Add(oldId);
            index = index + 1;
        }
        index = 0;
        while (index < currentMappings.Count)
        {
            int identifier = reserved[index];
            if (identifier < 0)
            {
                identifier = currentMappings[index].Id;
                if (!SdpExtMapServiceCore.IsUsableIdentifier(identifier) || assigned.Contains(identifier) || reserved.Contains(identifier) || ContainsExtMapIdentifier(oldMappings, identifier))
                {
                    identifier = SdpOfferAnswerCore.FindFreeHeaderExtensionId(CombinedIdentifiers(CombinedIdentifiers(assigned, reserved), ReadExtMapIdentifiers(oldMappings)));
                }
            }
            currentMappings[index].Id = identifier;
            assigned.Add(identifier);
            SynchronizeExtMap(currentMappings[index]);
            index = index + 1;
        }
    }

    internal static List<SdpExtMapAttribute> ReadExtMaps(SdpMediaDescription media)
    {
        List<SdpExtMapAttribute> result = new List<SdpExtMapAttribute>();
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(media.Attributes[index]);
            if (decoded.Success && decoded.ExtMapAttribute != null)
            {
                result.Add(decoded.ExtMapAttribute);
            }
            index = index + 1;
        }
        return result;
    }

    internal static int FindMatchingExtMapId(List<SdpExtMapAttribute> oldMappings, SdpExtMapAttribute current, List<int> reserved)
    {
        int index = 0;
        while (index < oldMappings.Count)
        {
            SdpExtMapAttribute old = oldMappings[index];
            bool identity = old.Encrypted == current.Encrypted && old.Uri == current.Uri && old.ExtensionAttributes == current.ExtensionAttributes;
            bool usable = SdpExtMapServiceCore.IsUsableIdentifier(old.Id) || SdpExtMapServiceCore.IsNegotiationIdentifier(old.Id);
            if (identity && usable && !reserved.Contains(old.Id))
            {
                return old.Id;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static bool ContainsExtMapIdentifier(List<SdpExtMapAttribute> mappings, int identifier)
    {
        int index = 0;
        while (index < mappings.Count)
        {
            if (mappings[index].Id == identifier)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static List<int> ReadExtMapIdentifiers(List<SdpExtMapAttribute> mappings)
    {
        List<int> result = new List<int>();
        int index = 0;
        while (index < mappings.Count)
        {
            if (!result.Contains(mappings[index].Id))
            {
                result.Add(mappings[index].Id);
            }
            index = index + 1;
        }
        return result;
    }

    internal static List<int> CombinedIdentifiers(List<int> assigned, List<int> reserved)
    {
        List<int> result = new List<int>();
        int index = 0;
        while (index < assigned.Count)
        {
            result.Add(assigned[index]);
            index = index + 1;
        }
        index = 0;
        while (index < reserved.Count)
        {
            if (reserved[index] >= 0 && !result.Contains(reserved[index]))
            {
                result.Add(reserved[index]);
            }
            index = index + 1;
        }
        return result;
    }

    internal static void SynchronizeExtMap(SdpExtMapAttribute mapping)
    {
        SdpAttribute? source = mapping.SourceAttribute;
        SdpAttributeEncodeResult encoded = SdpExtMapAttributeCodecCore.EncodeExtMap(mapping);
        if (source == null || !encoded.Success || encoded.Attribute == null)
        {
            throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationExtMapAllocationFailed, null);
        }
        source.Kind = encoded.Attribute.Kind;
        source.Name = encoded.Attribute.Name;
        source.Value = encoded.Attribute.Value;
        source.RawText = encoded.Attribute.RawText;
    }

    internal static void PreserveIceContinuity(SdpSessionDescription previousOffer, int oldIndex, SdpMediaDescription currentMedia, SdpMediaCapability capability, SdpEndpointCapabilities endpoint, SdpNegotiationOptions options)
    {
        SdpIceDescription previousIce = SdpQueryServiceCore.ResolveEffectiveIce(previousOffer, oldIndex);
        SdpIceDescription? desiredIce = capability.Ice == null ? endpoint.Ice : capability.Ice;
        SdpIceCredentials? previousCredentials = previousIce.Credentials;
        SdpIceCredentials? desiredCredentials = desiredIce == null ? null : desiredIce.Credentials;
        if (previousCredentials == null)
        {
            return;
        }
        if (desiredIce == null)
        {
            ReplaceAttributeFamily(previousOffer.MediaDescriptions[oldIndex].Attributes, currentMedia.Attributes, true);
            return;
        }
        if (!options.AllowIceRestart || desiredCredentials == null)
        {
            ReplaceIceCredentials(currentMedia.Attributes, previousCredentials);
            return;
        }
        bool usernameChanged = previousCredentials.UsernameFragment != desiredCredentials.UsernameFragment;
        bool passwordChanged = previousCredentials.Password != desiredCredentials.Password;
        if (usernameChanged != passwordChanged)
        {
            throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationIceIncompatible, null);
        }
    }

    internal static void ReplaceIceCredentials(List<SdpAttribute> attributes, SdpIceCredentials credentials)
    {
        SdpMutationCore.RemoveAttributesByName(attributes, SdpAttributeNames.IceUsernameFragment);
        SdpMutationCore.RemoveAttributesByName(attributes, SdpAttributeNames.IcePassword);
        attributes.Add(SdpIceServiceCore.EncodeRestartUsername(credentials));
        attributes.Add(SdpIceServiceCore.EncodeRestartPassword(credentials));
    }

    internal static void PreserveDtlsContinuity(SdpSessionDescription previousOffer, int oldIndex, SdpMediaDescription currentMedia)
    {
        SdpMediaDescription oldMedia = previousOffer.MediaDescriptions[oldIndex];
        if (SdpDtlsValidationCore.IsTlsTransport(oldMedia.Protocol) && SdpDtlsValidationCore.IsTlsTransport(currentMedia.Protocol))
        {
            ReplaceAttributeFamily(oldMedia.Attributes, currentMedia.Attributes, false);
        }
    }

    internal static void ReplaceAttributeFamily(List<SdpAttribute> oldAttributes, List<SdpAttribute> currentAttributes, bool iceFamily)
    {
        int index = currentAttributes.Count - 1;
        while (index >= 0)
        {
            bool matches = iceFamily ? SdpIceValidationCore.IsIceAttributeName(currentAttributes[index].Name) : SdpDtlsValidationCore.IsBaseDtlsAttributeName(currentAttributes[index].Name);
            if (matches)
            {
                currentAttributes.RemoveAt(index);
            }
            index = index - 1;
        }
        index = 0;
        while (index < oldAttributes.Count)
        {
            bool matches = iceFamily ? SdpIceValidationCore.IsIceAttributeName(oldAttributes[index].Name) : SdpDtlsValidationCore.IsBaseDtlsAttributeName(oldAttributes[index].Name);
            if (matches)
            {
                currentAttributes.Add(SdpCloneCore.CopyAttribute(oldAttributes[index]));
            }
            index = index + 1;
        }
    }

    internal static void PreserveMediaMetadata(SdpMediaDescription oldMedia, SdpMediaDescription currentMedia)
    {
        SdpMediaDescription copy = SdpCloneCore.CopyMedia(oldMedia);
        currentMedia.Information = copy.Information;
        currentMedia.Bandwidths = copy.Bandwidths;
        currentMedia.EncryptionKey = copy.EncryptionKey;
    }

    internal static void PreserveUnknownAttributes(SdpMediaDescription oldMedia, SdpMediaDescription currentMedia)
    {
        int index = 0;
        while (index < oldMedia.Attributes.Count)
        {
            SdpAttribute attribute = oldMedia.Attributes[index];
            SdpAttributeRegistration registration = SdpRegistryLookup.FindAttributeRegistration(attribute.Name);
            if (!registration.Found && !ContainsAttributeValue(currentMedia.Attributes, attribute))
            {
                currentMedia.Attributes.Add(SdpCloneCore.CopyAttribute(attribute));
            }
            index = index + 1;
        }
    }

    internal static bool ContainsAttributeValue(List<SdpAttribute> attributes, SdpAttribute expected)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (string.Equals(attributes[index].Name, expected.Name, StringComparison.OrdinalIgnoreCase) && attributes[index].Kind == expected.Kind && attributes[index].Value == expected.Value)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static void RebuildBundleState(SdpSessionDescription previousOffer, SdpSessionDescription currentOffer, SdpEndpointCapabilities capabilities, List<int> desiredOutputIndexes)
    {
        List<SdpBundleGroup> previousGroups = SdpBundleServiceCore.ResolveBundleGroups(previousOffer);
        if (previousGroups.Count == 0)
        {
            if (!capabilities.SupportsBundle)
            {
                RemoveBundleGroupAttributes(currentOffer.Attributes);
                RemoveBundleOnlyAttributes(currentOffer);
            }
            return;
        }
        RemoveBundleGroupAttributes(currentOffer.Attributes);
        if (!capabilities.SupportsBundle)
        {
            RemoveBundleOnlyAttributes(currentOffer);
            return;
        }

        List<string> previousMemberIds = new List<string>();
        int groupIndex = 0;
        while (groupIndex < previousGroups.Count)
        {
            int memberIndex = 0;
            while (memberIndex < previousGroups[groupIndex].MediaIds.Count)
            {
                if (!previousMemberIds.Contains(previousGroups[groupIndex].MediaIds[memberIndex]))
                {
                    previousMemberIds.Add(previousGroups[groupIndex].MediaIds[memberIndex]);
                }
                memberIndex = memberIndex + 1;
            }
            groupIndex = groupIndex + 1;
        }

        groupIndex = 0;
        while (groupIndex < previousGroups.Count)
        {
            List<string> mediaIds = new List<string>();
            int memberIndex = 0;
            while (memberIndex < previousGroups[groupIndex].MediaIds.Count)
            {
                string mediaId = previousGroups[groupIndex].MediaIds[memberIndex];
                int outputIndex = FindDesiredMediaIndex(currentOffer, desiredOutputIndexes, mediaId);
                if (outputIndex >= 0)
                {
                    mediaIds.Add(mediaId);
                }
                memberIndex = memberIndex + 1;
            }
            if (groupIndex == 0)
            {
                int desiredIndex = 0;
                while (desiredIndex < desiredOutputIndexes.Count)
                {
                    string mediaId = ReadMediaId(currentOffer.MediaDescriptions[desiredOutputIndexes[desiredIndex]]);
                    if (mediaId.Length > 0 && !previousMemberIds.Contains(mediaId) && !mediaIds.Contains(mediaId))
                    {
                        mediaIds.Add(mediaId);
                    }
                    desiredIndex = desiredIndex + 1;
                }
            }
            if (mediaIds.Count > 0)
            {
                SdpGroupAttribute group = SdpGroupServiceCore.CreateGroup(SdpGroupSemantic.Bundle, string.Empty, mediaIds);
                SdpGroupServiceCore.AddGroup(currentOffer, group);
            }
            groupIndex = groupIndex + 1;
        }
    }

    internal static int FindDesiredMediaIndex(SdpSessionDescription description, List<int> desiredOutputIndexes, string mediaId)
    {
        int index = 0;
        while (index < desiredOutputIndexes.Count)
        {
            int mediaIndex = desiredOutputIndexes[index];
            if (ReadMediaId(description.MediaDescriptions[mediaIndex]) == mediaId)
            {
                return mediaIndex;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static void RemoveBundleOnlyAttributes(SdpSessionDescription description)
    {
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMutationCore.RemoveAttributesByName(description.MediaDescriptions[mediaIndex].Attributes, SdpAttributeNames.BundleOnly);
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void AlignBundleExtMaps(SdpSessionDescription description)
    {
        List<SdpBundleGroup> groups = SdpBundleServiceCore.ResolveBundleGroups(description);
        int groupIndex = 0;
        while (groupIndex < groups.Count)
        {
            SdpBundleGroup group = groups[groupIndex];
            if (group.MediaIds.Count > 0)
            {
                int taggedIndex = SdpGroupServiceCore.FindMediaIndex(description, group.MediaIds[0]);
                if (taggedIndex >= 0)
                {
                    List<SdpExtMapAttribute> taggedMappings = ReadExtMaps(description.MediaDescriptions[taggedIndex]);
                    int memberIndex = 1;
                    while (memberIndex < group.MediaIds.Count)
                    {
                        int mediaIndex = SdpGroupServiceCore.FindMediaIndex(description, group.MediaIds[memberIndex]);
                        if (mediaIndex >= 0)
                        {
                            AlignMediaExtMaps(taggedMappings, ReadExtMaps(description.MediaDescriptions[mediaIndex]));
                        }
                        memberIndex = memberIndex + 1;
                    }
                }
            }
            groupIndex = groupIndex + 1;
        }
    }

    internal static void AlignMediaExtMaps(List<SdpExtMapAttribute> taggedMappings, List<SdpExtMapAttribute> mediaMappings)
    {
        int mediaIndex = 0;
        while (mediaIndex < mediaMappings.Count)
        {
            int taggedIndex = 0;
            bool matched = false;
            while (taggedIndex < taggedMappings.Count && !matched)
            {
                SdpExtMapAttribute tagged = taggedMappings[taggedIndex];
                SdpExtMapAttribute media = mediaMappings[mediaIndex];
                bool identity = tagged.Encrypted == media.Encrypted && tagged.Uri == media.Uri && tagged.ExtensionAttributes == media.ExtensionAttributes;
                if (identity)
                {
                    if (media.Id != tagged.Id)
                    {
                        media.Id = tagged.Id;
                        SynchronizeExtMap(media);
                    }
                    matched = true;
                }
                taggedIndex = taggedIndex + 1;
            }
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void ApplyOriginVersion(SdpSessionDescription previousOffer, SdpSessionDescription currentOffer, SdpNegotiationOptions options)
    {
        if (!options.IncrementOriginSessionVersion)
        {
            return;
        }
        SdpComparisonOptions comparisonOptions = new SdpComparisonOptions();
        comparisonOptions.CompareSemanticValues = true;
        comparisonOptions.IgnoreOriginSessionVersion = true;
        comparisonOptions.IgnoreInsignificantOrdering = true;
        comparisonOptions.IgnoreRawFormatting = true;
        SdpComparisonResult comparison = SdpComparisonCore.CompareDescriptions(previousOffer, currentOffer, comparisonOptions);
        if (!comparison.Equal)
        {
            SdpMutationCore.IncrementOriginVersion(currentOffer);
        }
    }

    internal static SdpNegotiationResult CreateResult(SdpSessionDescription previousOffer, SdpSessionDescription description, SdpNegotiationOptions options, List<int> desiredOutputIndexes)
    {
        SdpNegotiationResult result = new SdpNegotiationResult();
        result.Success = true;
        result.Description = description;
        result.AppliedOptions = SdpOfferAnswerCore.CopyInitialOfferOptions(options);
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaNegotiation mediaResult = CreateMediaResult(previousOffer, description, mediaIndex, desiredOutputIndexes.Contains(mediaIndex));
            result.Media.Add(mediaResult);
            if (mediaResult.Rejection != null)
            {
                result.Rejections.Add(mediaResult.Rejection);
            }
            mediaIndex = mediaIndex + 1;
        }
        List<SdpBundleGroup> groups = SdpBundleServiceCore.ResolveBundleGroups(description);
        int groupIndex = 0;
        while (groupIndex < groups.Count)
        {
            result.BundleGroups.Add(groups[groupIndex]);
            int taggedMediaIndex = SdpBundleServiceCore.ResolveBundleTaggedMediaIndex(description, SdpGroupServiceCore.FindMediaIndex(description, groups[groupIndex].TaggedMediaId));
            SdpBundleTransport? transport = SdpBundleServiceCore.ResolveBundleTransport(description, taggedMediaIndex);
            if (transport != null)
            {
                result.BundleTransports.Add(transport);
            }
            groupIndex = groupIndex + 1;
        }
        SdpOfferAnswerCore.RefreshInitialMediaResults(description, result);
        SdpOfferAnswerCore.PopulateRejectionDiagnostics(result, previousOffer, options.MaximumDiagnosticCount);
        return result;
    }

    internal static SdpMediaNegotiation CreateMediaResult(SdpSessionDescription previousOffer, SdpSessionDescription description, int mediaIndex, bool desired)
    {
        SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
        SdpMediaNegotiation result = new SdpMediaNegotiation();
        result.MediaIndex = mediaIndex;
        result.MediaId = ReadMediaId(media);
        result.MediaKind = media.MediaKind;
        result.MediaType = media.MediaType;
        result.Protocol = media.Protocol;
        result.TransportMediaIndex = mediaIndex;
        result.Accepted = desired;
        result.OfferedDirection = SdpQueryServiceCore.ResolveEffectiveDirection(description, mediaIndex);
        result.Direction = result.OfferedDirection;
        result.Codecs = SdpCodecServiceCore.ResolveMediaCodecs(media);
        int codecIndex = 0;
        while (codecIndex < result.Codecs.Count)
        {
            SdpPayloadMapping mapping = new SdpPayloadMapping();
            mapping.MediaIndex = mediaIndex;
            mapping.OfferedPayloadType = result.Codecs[codecIndex].PayloadType;
            mapping.NegotiatedPayloadType = result.Codecs[codecIndex].PayloadType;
            mapping.EncodingName = result.Codecs[codecIndex].EncodingName;
            result.PayloadMappings.Add(mapping);
            codecIndex = codecIndex + 1;
        }
        SdpOfferAnswerCore.AddHeaderExtensionResults(result, media);
        if (SdpDataChannelValidationCore.IsModernSctpProtocol(media.Protocol) || SdpDataChannelValidationCore.IsLegacySctpProtocol(media.Protocol))
        {
            result.Sctp = SdpDataChannelServiceCore.ResolveMediaSctp(media);
        }
        if (!desired)
        {
            SdpRejectionReason reason = WasPreviouslyRejected(previousOffer, result.MediaId) ? SdpRejectionReason.OfferedAsRejected : SdpRejectionReason.RejectedByPolicy;
            result.Rejection = SdpOfferAnswerCore.CreatePositionalRejection(mediaIndex, result.MediaId, reason);
            if (reason == SdpRejectionReason.RejectedByPolicy)
            {
                result.Rejection.Path.FieldName = 'm';
                result.Rejection.Path.AttributeName = string.Empty;
                result.Rejection.Detail = "The previous media slot has no corresponding current local capability.";
            }
        }
        return result;
    }

    internal static bool WasPreviouslyRejected(SdpSessionDescription previousOffer, string mediaId)
    {
        int previousIndex = mediaId.Length == 0 ? -1 : SdpGroupServiceCore.FindMediaIndex(previousOffer, mediaId);
        return previousIndex >= 0 && previousOffer.MediaDescriptions[previousIndex].Port == 0;
    }

    internal static string ReadMediaId(SdpMediaDescription media)
    {
        SdpMediaIdAttribute? mediaId = SdpGroupServiceCore.ResolveMediaId(media);
        return mediaId == null ? string.Empty : mediaId.Id;
    }

    internal static void ReplaceMediaId(SdpMediaDescription media, string mediaId)
    {
        SdpAttribute? attribute = FindAttribute(media.Attributes, SdpAttributeNames.MediaId);
        if (attribute == null)
        {
            SdpOfferAnswerCore.AddMediaIdAttribute(media, mediaId);
            return;
        }
        SdpMediaIdAttribute projection = new SdpMediaIdAttribute();
        projection.Id = mediaId;
        SdpAttributeEncodeResult encoded = SdpGroupAttributeCodecCore.EncodeMediaId(projection);
        if (!encoded.Success || encoded.Attribute == null)
        {
            throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationInvalidOffer, null);
        }
        attribute.Kind = encoded.Attribute.Kind;
        attribute.Name = encoded.Attribute.Name;
        attribute.Value = encoded.Attribute.Value;
        attribute.RawText = encoded.Attribute.RawText;
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
}
