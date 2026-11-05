using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpBundleServiceCore
{
    internal static List<SdpBundleGroup> ResolveBundleGroups(SdpSessionDescription description)
    {
        List<SdpBundleGroup> bundles = new List<SdpBundleGroup>();
        List<SdpGroupAttribute> groups = SdpGroupServiceCore.ResolveGroups(description);
        int index = 0;
        while (index < groups.Count)
        {
            SdpGroupAttribute group = groups[index];
            if (group.Semantic == SdpGroupSemantic.Bundle)
            {
                bundles.Add(CreateBundleGroup(group));
            }
            index = index + 1;
        }
        return bundles;
    }

    internal static SdpBundleGroup CreateBundleGroup(SdpGroupAttribute group)
    {
        SdpBundleGroup bundle = new SdpBundleGroup();
        bundle.SourceGroup = group;
        int index = 0;
        while (index < group.IdentificationTags.Count)
        {
            bundle.MediaIds.Add(group.IdentificationTags[index]);
            index = index + 1;
        }
        if (bundle.MediaIds.Count > 0)
        {
            bundle.TaggedMediaId = bundle.MediaIds[0];
        }
        return bundle;
    }

    internal static SdpBundleGroup? FindBundleGroupForMedia(SdpSessionDescription description, int mediaIndex)
    {
        SdpMediaIdAttribute? mediaId = SdpGroupServiceCore.ResolveMediaId(description.MediaDescriptions[mediaIndex]);
        if (mediaId == null)
        {
            return null;
        }
        List<SdpBundleGroup> bundles = ResolveBundleGroups(description);
        int index = 0;
        while (index < bundles.Count)
        {
            if (ContainsMediaId(bundles[index].MediaIds, mediaId.Id))
            {
                return bundles[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static int ResolveBundleTaggedMediaIndex(SdpSessionDescription description, int mediaIndex)
    {
        SdpBundleGroup? bundle = FindBundleGroupForMedia(description, mediaIndex);
        if (bundle == null || bundle.TaggedMediaId.Length == 0)
        {
            return mediaIndex;
        }
        int taggedMediaIndex = SdpGroupServiceCore.FindMediaIndex(description, bundle.TaggedMediaId);
        if (taggedMediaIndex < 0)
        {
            return mediaIndex;
        }
        return taggedMediaIndex;
    }

    internal static SdpBundleTransport? ResolveBundleTransport(SdpSessionDescription description, int mediaIndex)
    {
        SdpBundleGroup? bundle = FindBundleGroupForMedia(description, mediaIndex);
        if (bundle == null || bundle.TaggedMediaId.Length == 0)
        {
            return null;
        }
        int taggedMediaIndex = SdpGroupServiceCore.FindMediaIndex(description, bundle.TaggedMediaId);
        if (taggedMediaIndex < 0)
        {
            return null;
        }

        SdpMediaDescription taggedMedia = description.MediaDescriptions[taggedMediaIndex];
        SdpBundleTransport transport = new SdpBundleTransport();
        transport.TaggedMediaId = bundle.TaggedMediaId;
        SdpConnectionData? connection = SdpIceValidationCore.ReadEffectiveConnection(description, taggedMedia);
        if (connection != null)
        {
            transport.Connection = CopyConnection(connection);
        }
        if (HasIceSignaling(description, taggedMedia))
        {
            transport.Ice = SdpIceServiceCore.ResolveMediaIce(description, taggedMediaIndex);
        }
        if (HasDtlsSignaling(description, taggedMedia))
        {
            transport.Dtls = SdpDtlsServiceCore.ResolveMediaDtls(description, taggedMediaIndex);
        }
        transport.HeaderExtensions = SdpExtMapServiceCore.ResolveMediaExtMaps(description, taggedMediaIndex);
        transport.SharedAttributes = ResolveSharedBundleAttributes(taggedMedia);
        transport.MediaAttributes = ResolveNormalBundleAttributes(description.MediaDescriptions[mediaIndex]);
        transport.Bandwidths = ResolveBundleBandwidths(description, bundle, mediaIndex);
        transport.RtcpMux = SdpRtcpValidationCore.ContainsValidAttribute(taggedMedia.Attributes, SdpAttributeNames.RtcpMux);
        return transport;
    }

    internal static List<SdpAttribute> ResolveSharedBundleAttributes(SdpMediaDescription taggedMedia)
    {
        List<SdpAttribute> result = new List<SdpAttribute>();
        int index = 0;
        while (index < taggedMedia.Attributes.Count)
        {
            SdpAttribute attribute = taggedMedia.Attributes[index];
            SdpAttributeRegistration registration = SdpRegistryLookup.FindAttributeRegistration(attribute.Name);
            if (registration.MuxCategory == SdpMuxCategory.Identical || registration.MuxCategory == SdpMuxCategory.Transport)
            {
                result.Add(CopyBundleAttribute(attribute));
            }
            index = index + 1;
        }
        return result;
    }

    internal static List<SdpAttribute> ResolveNormalBundleAttributes(SdpMediaDescription media)
    {
        List<SdpAttribute> result = new List<SdpAttribute>();
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            SdpAttributeRegistration registration = SdpRegistryLookup.FindAttributeRegistration(attribute.Name);
            if (registration.MuxCategory == SdpMuxCategory.Normal)
            {
                result.Add(CopyBundleAttribute(attribute));
            }
            index = index + 1;
        }
        return result;
    }

    internal static List<SdpBandwidth> ResolveBundleBandwidths(SdpSessionDescription description, SdpBundleGroup bundle, int mediaIndex)
    {
        List<SdpBandwidth> result = new List<SdpBandwidth>();
        List<SdpBandwidth> selectedBandwidths = description.MediaDescriptions[mediaIndex].Bandwidths;
        int bandwidthIndex = 0;
        while (bandwidthIndex < selectedBandwidths.Count)
        {
            SdpBandwidth bandwidth = selectedBandwidths[bandwidthIndex];
            SdpRegisteredToken registration = SdpRegistryLookup.FindSdpNameRegistration("sdp-parameters-3", bandwidth.Type, false, false);
            if (registration.MuxCategory == SdpMuxCategory.Normal)
            {
                result.Add(CopyBundleBandwidth(bandwidth));
            }
            bandwidthIndex = bandwidthIndex + 1;
        }

        List<int> visitedMediaIndexes = new List<int>();
        int groupIndex = 0;
        while (groupIndex < bundle.MediaIds.Count)
        {
            int bundledMediaIndex = SdpGroupServiceCore.FindMediaIndex(description, bundle.MediaIds[groupIndex]);
            if (bundledMediaIndex >= 0 && !visitedMediaIndexes.Contains(bundledMediaIndex))
            {
                visitedMediaIndexes.Add(bundledMediaIndex);
                AddSummedBundleBandwidths(result, description.MediaDescriptions[bundledMediaIndex].Bandwidths);
            }
            groupIndex = groupIndex + 1;
        }
        return result;
    }

    internal static void AddSummedBundleBandwidths(List<SdpBandwidth> result, List<SdpBandwidth> bandwidths)
    {
        int index = 0;
        while (index < bandwidths.Count)
        {
            SdpBandwidth bandwidth = bandwidths[index];
            SdpRegisteredToken registration = SdpRegistryLookup.FindSdpNameRegistration("sdp-parameters-3", bandwidth.Type, false, false);
            if (registration.MuxCategory == SdpMuxCategory.Sum)
            {
                SdpBandwidth? aggregate = FindBundleBandwidth(result, bandwidth.Type);
                if (aggregate == null)
                {
                    aggregate = CopyBundleBandwidth(bandwidth);
                    SetAggregateBandwidthText(aggregate);
                    result.Add(aggregate);
                }
                else
                {
                    AddBundleBandwidthValue(aggregate, bandwidth.Value);
                }
            }
            index = index + 1;
        }
    }

    internal static SdpBandwidth? FindBundleBandwidth(List<SdpBandwidth> bandwidths, string type)
    {
        int index = 0;
        while (index < bandwidths.Count)
        {
            if (string.Equals(bandwidths[index].Type, type, StringComparison.Ordinal))
            {
                return bandwidths[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static void AddBundleBandwidthValue(SdpBandwidth aggregate, long value)
    {
        if (value > 0 && aggregate.Value > long.MaxValue - value)
        {
            aggregate.Value = long.MaxValue;
        }
        else if (value < 0 && aggregate.Value < long.MinValue - value)
        {
            aggregate.Value = long.MinValue;
        }
        else
        {
            aggregate.Value = aggregate.Value + value;
        }
        SetAggregateBandwidthText(aggregate);
    }

    internal static void SetAggregateBandwidthText(SdpBandwidth bandwidth)
    {
        bandwidth.RawValue = bandwidth.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        bandwidth.RawText = bandwidth.Type + ":" + bandwidth.RawValue;
    }

    internal static SdpAttribute CopyBundleAttribute(SdpAttribute attribute)
    {
        SdpAttribute result = new SdpAttribute();
        result.Kind = attribute.Kind;
        result.Name = attribute.Name;
        result.Value = attribute.Value;
        result.RawText = attribute.RawText;
        result.Location = SdpDiagnosticBuilder.CopySourceLocation(attribute.Location);
        return result;
    }

    internal static SdpBandwidth CopyBundleBandwidth(SdpBandwidth bandwidth)
    {
        SdpBandwidth result = new SdpBandwidth();
        result.Type = bandwidth.Type;
        result.Value = bandwidth.Value;
        result.RawValue = bandwidth.RawValue;
        result.RawText = bandwidth.RawText;
        result.Location = SdpDiagnosticBuilder.CopySourceLocation(bandwidth.Location);
        return result;
    }

    internal static SdpBundleGroup CreateInitialBundleOffer(SdpSessionDescription description, List<int> mediaIndexes, int taggedMediaIndex, List<int> bundleOnlyMediaIndexes)
    {
        ValidateInitialBundleIndexes(description, mediaIndexes, taggedMediaIndex, bundleOnlyMediaIndexes);
        if (ResolveBundleGroups(description).Count > 0)
        {
            throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
        }

        List<string> mediaIds = PrepareMediaIds(description);
        ValidateFallbackTransports(description, mediaIndexes, bundleOnlyMediaIndexes);
        List<string> groupIds = OrderBundleMediaIds(mediaIndexes, taggedMediaIndex, mediaIds);
        SdpGroupAttribute group = SdpGroupServiceCore.CreateGroup(SdpGroupSemantic.Bundle, string.Empty, groupIds);
        List<SdpAttribute?> mediaIdAttributes = EncodeMissingMediaIds(description, mediaIds);
        ApplyInitialBundleOffer(description, mediaIndexes, bundleOnlyMediaIndexes, mediaIdAttributes, group);
        return CreateBundleGroup(group);
    }

    internal static void ValidateInitialBundleIndexes(SdpSessionDescription description, List<int> mediaIndexes, int taggedMediaIndex, List<int> bundleOnlyMediaIndexes)
    {
        if (mediaIndexes.Count == 0)
        {
            throw new ArgumentException("At least one media section is required for a BUNDLE group.", "mediaIndexes");
        }
        int index = 0;
        while (index < mediaIndexes.Count)
        {
            int mediaIndex = mediaIndexes[index];
            if (mediaIndex < 0 || mediaIndex >= description.MediaDescriptions.Count)
            {
                throw new ArgumentOutOfRangeException("mediaIndexes");
            }
            if (ContainsEarlierIndex(mediaIndexes, index))
            {
                throw new ArgumentException("BUNDLE media indexes must be distinct.", "mediaIndexes");
            }
            index = index + 1;
        }
        if (taggedMediaIndex < 0 || taggedMediaIndex >= description.MediaDescriptions.Count)
        {
            throw new ArgumentOutOfRangeException("taggedMediaIndex");
        }
        if (!mediaIndexes.Contains(taggedMediaIndex))
        {
            throw new ArgumentException("The tagged media section must be a member of the BUNDLE group.", "taggedMediaIndex");
        }

        index = 0;
        while (index < bundleOnlyMediaIndexes.Count)
        {
            int mediaIndex = bundleOnlyMediaIndexes[index];
            if (mediaIndex < 0 || mediaIndex >= description.MediaDescriptions.Count)
            {
                throw new ArgumentOutOfRangeException("bundleOnlyMediaIndexes");
            }
            if (!mediaIndexes.Contains(mediaIndex) || ContainsEarlierIndex(bundleOnlyMediaIndexes, index))
            {
                throw new ArgumentException("Bundle-only indexes must be distinct members of the BUNDLE group.", "bundleOnlyMediaIndexes");
            }
            if (mediaIndex == taggedMediaIndex)
            {
                throw new ArgumentException("The tagged media section must not be bundle-only.", "taggedMediaIndex");
            }
            index = index + 1;
        }
    }

    internal static bool ContainsEarlierIndex(List<int> indexes, int targetIndex)
    {
        int index = 0;
        while (index < targetIndex)
        {
            if (indexes[index] == indexes[targetIndex])
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static List<string> PrepareMediaIds(SdpSessionDescription description)
    {
        List<string> mediaIds = new List<string>();
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaIdAttribute? existing = ReadSingleMediaId(description.MediaDescriptions[mediaIndex]);
            string mediaId = string.Empty;
            if (existing != null)
            {
                mediaId = existing.Id;
                if (mediaIds.Contains(mediaId))
                {
                    throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
                }
            }
            mediaIds.Add(mediaId);
            mediaIndex = mediaIndex + 1;
        }

        mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            if (mediaIds[mediaIndex].Length == 0)
            {
                mediaIds[mediaIndex] = AllocateMediaId(description.MediaDescriptions[mediaIndex], mediaIds);
            }
            mediaIndex = mediaIndex + 1;
        }
        return mediaIds;
    }

    internal static SdpMediaIdAttribute? ReadSingleMediaId(SdpMediaDescription media)
    {
        SdpMediaIdAttribute? result = null;
        int count = 0;
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.MediaId))
            {
                count = count + 1;
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success || decoded.MediaIdAttribute == null || count > 1)
                {
                    throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
                }
                result = decoded.MediaIdAttribute;
            }
            index = index + 1;
        }
        return result;
    }

    internal static string AllocateMediaId(SdpMediaDescription media, List<string> reservedMediaIds)
    {
        string stem = media.MediaType;
        if (!SdpCommonAttributeCodecCore.IsTokenValue(stem))
        {
            stem = "media";
        }
        if (!reservedMediaIds.Contains(stem))
        {
            return stem;
        }
        int suffix = 2;
        string candidate = stem + "-" + suffix;
        while (reservedMediaIds.Contains(candidate))
        {
            suffix = suffix + 1;
            candidate = stem + "-" + suffix;
        }
        return candidate;
    }

    internal static void ValidateFallbackTransports(SdpSessionDescription description, List<int> mediaIndexes, List<int> bundleOnlyMediaIndexes)
    {
        int index = 0;
        while (index < mediaIndexes.Count)
        {
            int mediaIndex = mediaIndexes[index];
            if (!bundleOnlyMediaIndexes.Contains(mediaIndex))
            {
                SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
                SdpConnectionData? connection = SdpIceValidationCore.ReadEffectiveConnection(description, media);
                int portCount = media.HasPortCount ? media.PortCount : 1;
                if (media.Port <= 0 || media.Port > 65535 || portCount <= 0 || portCount > 65536 - media.Port || connection == null || connection.NetworkType.Length == 0 || connection.AddressType.Length == 0 || connection.Address.Length == 0)
                {
                    throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
                }
                int earlierIndex = 0;
                while (earlierIndex < index)
                {
                    int earlierMediaIndex = mediaIndexes[earlierIndex];
                    if (!bundleOnlyMediaIndexes.Contains(earlierMediaIndex) && FallbackTransportsEqual(description, earlierMediaIndex, mediaIndex))
                    {
                        throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
                    }
                    earlierIndex = earlierIndex + 1;
                }
            }
            index = index + 1;
        }
    }

    internal static bool FallbackTransportsEqual(SdpSessionDescription description, int firstMediaIndex, int secondMediaIndex)
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
        int firstEnd = firstMedia.Port + firstCount - 1;
        int secondEnd = secondMedia.Port + secondCount - 1;
        bool portsOverlap = firstMedia.Port <= secondEnd && secondMedia.Port <= firstEnd;
        return portsOverlap && string.Equals(first.NetworkType, second.NetworkType, StringComparison.OrdinalIgnoreCase)
            && string.Equals(first.AddressType, second.AddressType, StringComparison.OrdinalIgnoreCase)
            && string.Equals(first.Address, second.Address, StringComparison.OrdinalIgnoreCase);
    }

    internal static List<string> OrderBundleMediaIds(List<int> mediaIndexes, int taggedMediaIndex, List<string> mediaIds)
    {
        List<string> result = new List<string>();
        result.Add(mediaIds[taggedMediaIndex]);
        int index = 0;
        while (index < mediaIndexes.Count)
        {
            if (mediaIndexes[index] != taggedMediaIndex)
            {
                result.Add(mediaIds[mediaIndexes[index]]);
            }
            index = index + 1;
        }
        return result;
    }

    internal static List<SdpAttribute?> EncodeMissingMediaIds(SdpSessionDescription description, List<string> mediaIds)
    {
        List<SdpAttribute?> attributes = new List<SdpAttribute?>();
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaIdAttribute? existing = SdpGroupServiceCore.ResolveMediaId(description.MediaDescriptions[mediaIndex]);
            SdpAttribute? attribute = null;
            if (existing == null)
            {
                SdpMediaIdAttribute projection = new SdpMediaIdAttribute();
                projection.Id = mediaIds[mediaIndex];
                SdpAttributeEncodeResult encoded = SdpGroupAttributeCodecCore.EncodeMediaId(projection);
                if (!encoded.Success || encoded.Attribute == null)
                {
                    throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
                }
                attribute = encoded.Attribute;
            }
            attributes.Add(attribute);
            mediaIndex = mediaIndex + 1;
        }
        return attributes;
    }

    internal static void ApplyInitialBundleOffer(SdpSessionDescription description, List<int> mediaIndexes, List<int> bundleOnlyMediaIndexes, List<SdpAttribute?> mediaIdAttributes, SdpGroupAttribute group)
    {
        int mediaIndex = 0;
        while (mediaIndex < mediaIdAttributes.Count)
        {
            SdpAttribute? mediaIdAttribute = mediaIdAttributes[mediaIndex];
            if (mediaIdAttribute != null)
            {
                description.MediaDescriptions[mediaIndex].Attributes.Add(mediaIdAttribute);
            }
            mediaIndex = mediaIndex + 1;
        }

        int index = 0;
        while (index < mediaIndexes.Count)
        {
            int bundledMediaIndex = mediaIndexes[index];
            SdpMediaDescription media = description.MediaDescriptions[bundledMediaIndex];
            RemoveAttributes(media.Attributes, SdpAttributeNames.BundleOnly);
            if (bundleOnlyMediaIndexes.Contains(bundledMediaIndex))
            {
                media.Port = 0;
                media.Attributes.Add(CreatePropertyAttribute(SdpAttributeNames.BundleOnly));
            }
            index = index + 1;
        }
        SdpGroupServiceCore.AddGroup(description, group);
    }

    internal static SdpBundleGroup? CreateInitialBundleAnswer(SdpSessionDescription offer, SdpSessionDescription answer, int offeredBundleGroupIndex, List<int> acceptedMediaIndexes)
    {
        if (object.ReferenceEquals(offer, answer))
        {
            throw new ArgumentException("The offer and answer must be distinct descriptions.", "answer");
        }
        if (offer.MediaDescriptions.Count != answer.MediaDescriptions.Count)
        {
            throw new ArgumentException("The offer and answer must contain the same number of media sections.", "answer");
        }

        List<SdpBundleGroup> offeredGroups = ResolveBundleGroups(offer);
        if (offeredBundleGroupIndex < 0 || offeredBundleGroupIndex >= offeredGroups.Count)
        {
            throw new ArgumentOutOfRangeException("offeredBundleGroupIndex");
        }
        SdpBundleGroup offeredGroup = offeredGroups[offeredBundleGroupIndex];
        List<int> offeredMediaIndexes = ResolveOfferedBundleMediaIndexes(offer, offeredGroup);
        ValidateAcceptedBundleMedia(offer, answer, offeredMediaIndexes, acceptedMediaIndexes);
        ValidateAnswerBundleAvailability(answer, offeredGroup);
        List<SdpAttribute?> mediaIdAttributes = PrepareAnswerMediaIdAttributes(answer, offeredGroup, offeredMediaIndexes);

        int taggedMediaIndex = SelectInitialAnswerTaggedMediaIndex(offer, offeredMediaIndexes, acceptedMediaIndexes);
        SdpGroupAttribute? answerGroup = null;
        if (acceptedMediaIndexes.Count > 0)
        {
            if (taggedMediaIndex < 0)
            {
                throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
            }
            ValidateTaggedBundleTransport(answer, taggedMediaIndex);
            List<string> answerGroupIds = OrderInitialAnswerMediaIds(offeredGroup, offeredMediaIndexes, acceptedMediaIndexes, taggedMediaIndex);
            answerGroup = SdpGroupServiceCore.CreateGroup(SdpGroupSemantic.Bundle, string.Empty, answerGroupIds);
        }

        ApplyInitialBundleAnswer(offer, answer, offeredMediaIndexes, acceptedMediaIndexes, taggedMediaIndex, mediaIdAttributes, answerGroup);
        if (answerGroup == null)
        {
            return null;
        }
        return CreateBundleGroup(answerGroup);
    }

    internal static List<int> ResolveOfferedBundleMediaIndexes(SdpSessionDescription offer, SdpBundleGroup offeredGroup)
    {
        if (offeredGroup.MediaIds.Count == 0)
        {
            throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
        }
        List<int> result = new List<int>();
        int groupIndex = 0;
        while (groupIndex < offeredGroup.MediaIds.Count)
        {
            int mediaIndex = FindUniqueMediaIndexById(offer, offeredGroup.MediaIds[groupIndex]);
            if (mediaIndex < 0 || result.Contains(mediaIndex))
            {
                throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
            }
            result.Add(mediaIndex);
            groupIndex = groupIndex + 1;
        }
        return result;
    }

    internal static int FindUniqueMediaIndexById(SdpSessionDescription description, string mediaId)
    {
        int result = -1;
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaIdAttribute? candidate = ReadSingleMediaId(description.MediaDescriptions[mediaIndex]);
            if (candidate != null && candidate.Id == mediaId)
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

    internal static void ValidateAcceptedBundleMedia(SdpSessionDescription offer, SdpSessionDescription answer, List<int> offeredMediaIndexes, List<int> acceptedMediaIndexes)
    {
        int index = 0;
        while (index < acceptedMediaIndexes.Count)
        {
            int mediaIndex = acceptedMediaIndexes[index];
            if (mediaIndex < 0 || mediaIndex >= answer.MediaDescriptions.Count)
            {
                throw new ArgumentOutOfRangeException("acceptedMediaIndexes");
            }
            if (ContainsEarlierIndex(acceptedMediaIndexes, index))
            {
                throw new ArgumentException("Accepted BUNDLE media indexes must be distinct.", "acceptedMediaIndexes");
            }
            if (!offeredMediaIndexes.Contains(mediaIndex))
            {
                throw new ArgumentException("Every accepted media index must belong to the offered BUNDLE group.", "acceptedMediaIndexes");
            }
            SdpMediaDescription offeredMedia = offer.MediaDescriptions[mediaIndex];
            if (offeredMedia.Port == 0 && !IsBundleOnlyMedia(offeredMedia))
            {
                throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
            }
            index = index + 1;
        }
    }

    internal static void ValidateAnswerBundleAvailability(SdpSessionDescription answer, SdpBundleGroup offeredGroup)
    {
        List<SdpBundleGroup> answerGroups = ResolveBundleGroups(answer);
        int answerGroupIndex = 0;
        while (answerGroupIndex < answerGroups.Count)
        {
            SdpBundleGroup answerGroup = answerGroups[answerGroupIndex];
            int mediaIdIndex = 0;
            while (mediaIdIndex < offeredGroup.MediaIds.Count)
            {
                if (answerGroup.MediaIds.Contains(offeredGroup.MediaIds[mediaIdIndex]))
                {
                    throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
                }
                mediaIdIndex = mediaIdIndex + 1;
            }
            answerGroupIndex = answerGroupIndex + 1;
        }
    }

    internal static List<SdpAttribute?> PrepareAnswerMediaIdAttributes(SdpSessionDescription answer, SdpBundleGroup offeredGroup, List<int> offeredMediaIndexes)
    {
        List<string> answerMediaIds = new List<string>();
        int mediaIndex = 0;
        while (mediaIndex < answer.MediaDescriptions.Count)
        {
            SdpMediaIdAttribute? existing = ReadSingleMediaId(answer.MediaDescriptions[mediaIndex]);
            string mediaId = existing == null ? string.Empty : existing.Id;
            if (mediaId.Length > 0 && answerMediaIds.Contains(mediaId))
            {
                throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
            }
            answerMediaIds.Add(mediaId);
            mediaIndex = mediaIndex + 1;
        }

        List<SdpAttribute?> result = new List<SdpAttribute?>();
        mediaIndex = 0;
        while (mediaIndex < answer.MediaDescriptions.Count)
        {
            result.Add(null);
            mediaIndex = mediaIndex + 1;
        }
        int groupIndex = 0;
        while (groupIndex < offeredMediaIndexes.Count)
        {
            mediaIndex = offeredMediaIndexes[groupIndex];
            string expectedMediaId = offeredGroup.MediaIds[groupIndex];
            string existingMediaId = answerMediaIds[mediaIndex];
            if (existingMediaId.Length > 0 && existingMediaId != expectedMediaId)
            {
                throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
            }
            if (existingMediaId.Length == 0)
            {
                if (answerMediaIds.Contains(expectedMediaId))
                {
                    throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
                }
                SdpMediaIdAttribute mediaId = new SdpMediaIdAttribute();
                mediaId.Id = expectedMediaId;
                SdpAttributeEncodeResult encoded = SdpGroupAttributeCodecCore.EncodeMediaId(mediaId);
                if (!encoded.Success || encoded.Attribute == null)
                {
                    throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
                }
                result[mediaIndex] = encoded.Attribute;
                answerMediaIds[mediaIndex] = expectedMediaId;
            }
            groupIndex = groupIndex + 1;
        }
        return result;
    }

    internal static int SelectInitialAnswerTaggedMediaIndex(SdpSessionDescription offer, List<int> offeredMediaIndexes, List<int> acceptedMediaIndexes)
    {
        int groupIndex = 0;
        while (groupIndex < offeredMediaIndexes.Count)
        {
            int mediaIndex = offeredMediaIndexes[groupIndex];
            SdpMediaDescription offeredMedia = offer.MediaDescriptions[mediaIndex];
            if (acceptedMediaIndexes.Contains(mediaIndex) && offeredMedia.Port != 0 && !IsBundleOnlyMedia(offeredMedia))
            {
                return mediaIndex;
            }
            groupIndex = groupIndex + 1;
        }
        return -1;
    }

    internal static void ValidateTaggedBundleTransport(SdpSessionDescription answer, int taggedMediaIndex)
    {
        SdpMediaDescription taggedMedia = answer.MediaDescriptions[taggedMediaIndex];
        int portCount = taggedMedia.HasPortCount ? taggedMedia.PortCount : 1;
        SdpConnectionData? connection = SdpIceValidationCore.ReadEffectiveConnection(answer, taggedMedia);
        if (taggedMedia.Port <= 0 || taggedMedia.Port > 65535 || portCount <= 0 || portCount > 65536 - taggedMedia.Port || connection == null || connection.NetworkType.Length == 0 || connection.AddressType.Length == 0 || connection.Address.Length == 0)
        {
            throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
        }
    }

    internal static List<string> OrderInitialAnswerMediaIds(SdpBundleGroup offeredGroup, List<int> offeredMediaIndexes, List<int> acceptedMediaIndexes, int taggedMediaIndex)
    {
        List<string> result = new List<string>();
        int taggedGroupIndex = offeredMediaIndexes.IndexOf(taggedMediaIndex);
        result.Add(offeredGroup.MediaIds[taggedGroupIndex]);
        int groupIndex = 0;
        while (groupIndex < offeredMediaIndexes.Count)
        {
            int mediaIndex = offeredMediaIndexes[groupIndex];
            if (mediaIndex != taggedMediaIndex && acceptedMediaIndexes.Contains(mediaIndex))
            {
                result.Add(offeredGroup.MediaIds[groupIndex]);
            }
            groupIndex = groupIndex + 1;
        }
        return result;
    }

    internal static void ApplyInitialBundleAnswer(SdpSessionDescription offer, SdpSessionDescription answer, List<int> offeredMediaIndexes, List<int> acceptedMediaIndexes, int taggedMediaIndex, List<SdpAttribute?> mediaIdAttributes, SdpGroupAttribute? answerGroup)
    {
        int mediaIndex = 0;
        while (mediaIndex < mediaIdAttributes.Count)
        {
            SdpAttribute? mediaIdAttribute = mediaIdAttributes[mediaIndex];
            if (mediaIdAttribute != null)
            {
                answer.MediaDescriptions[mediaIndex].Attributes.Add(mediaIdAttribute);
            }
            mediaIndex = mediaIndex + 1;
        }

        int groupIndex = 0;
        while (groupIndex < offeredMediaIndexes.Count)
        {
            mediaIndex = offeredMediaIndexes[groupIndex];
            SdpMediaDescription answerMedia = answer.MediaDescriptions[mediaIndex];
            RemoveAttributes(answerMedia.Attributes, SdpAttributeNames.BundleOnly);
            if (acceptedMediaIndexes.Contains(mediaIndex))
            {
                if (mediaIndex != taggedMediaIndex)
                {
                    CopyTaggedBundleEndpoint(answer, taggedMediaIndex, mediaIndex);
                    RemoveUntaggedBundleAttributes(answerMedia.Attributes);
                }
            }
            else if (IsBundleOnlyMedia(offer.MediaDescriptions[mediaIndex]))
            {
                answerMedia.Port = 0;
                answerMedia.HasPortCount = false;
                answerMedia.PortCount = 1;
            }
            groupIndex = groupIndex + 1;
        }
        if (answerGroup != null)
        {
            SdpGroupServiceCore.AddGroup(answer, answerGroup);
        }
    }

    internal static void CopyTaggedBundleEndpoint(SdpSessionDescription answer, int taggedMediaIndex, int targetMediaIndex)
    {
        SdpMediaDescription taggedMedia = answer.MediaDescriptions[taggedMediaIndex];
        SdpMediaDescription targetMedia = answer.MediaDescriptions[targetMediaIndex];
        targetMedia.Port = taggedMedia.Port;
        targetMedia.HasPortCount = taggedMedia.HasPortCount;
        targetMedia.PortCount = taggedMedia.PortCount;
        targetMedia.Connections.Clear();
        int connectionIndex = 0;
        while (connectionIndex < taggedMedia.Connections.Count)
        {
            targetMedia.Connections.Add(CopyConnection(taggedMedia.Connections[connectionIndex]));
            connectionIndex = connectionIndex + 1;
        }
    }

    internal static void RemoveUntaggedBundleAttributes(List<SdpAttribute> attributes)
    {
        int index = attributes.Count - 1;
        while (index >= 0)
        {
            if (IsBundleTransportAttribute(attributes[index]))
            {
                attributes.RemoveAt(index);
            }
            index = index - 1;
        }
    }

    internal static void CopyMissingBundleAttributes(List<SdpAttribute> source, List<SdpAttribute> target)
    {
        int originalTargetCount = target.Count;
        int sourceIndex = 0;
        while (sourceIndex < source.Count)
        {
            SdpAttribute attribute = source[sourceIndex];
            if (IsBundleTransportAttribute(attribute) && !ContainsOriginalAttributeName(target, originalTargetCount, attribute.Name))
            {
                SdpAttribute copy = new SdpAttribute();
                copy.Kind = attribute.Kind;
                copy.Name = attribute.Name;
                copy.Value = attribute.Value;
                copy.RawText = attribute.RawText;
                copy.Location = SdpDiagnosticBuilder.CopySourceLocation(attribute.Location);
                target.Add(copy);
            }
            sourceIndex = sourceIndex + 1;
        }
    }

    internal static bool IsBundleTransportAttribute(SdpAttribute attribute)
    {
        SdpAttributeRegistration registration = SdpRegistryLookup.FindAttributeRegistration(attribute.Name);
        return registration.MuxCategory == SdpMuxCategory.Identical || registration.MuxCategory == SdpMuxCategory.Transport;
    }

    internal static bool ContainsOriginalAttributeName(List<SdpAttribute> attributes, int originalCount, string name)
    {
        int index = 0;
        while (index < originalCount)
        {
            if (SdpRtpValidationCore.NameEquals(attributes[index].Name, name))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool IsBundleOnlyMedia(SdpMediaDescription media)
    {
        int index = 0;
        while (index < media.Attributes.Count)
        {
            if (SdpRtpValidationCore.NameEquals(media.Attributes[index].Name, SdpAttributeNames.BundleOnly))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static SdpBundleGroup? UpdateSubsequentBundleOffer(SdpSessionDescription offer, int bundleGroupIndex, List<int> bundledMediaIndexes, int taggedMediaIndex, List<int> disabledMediaIndexes)
    {
        List<SdpBundleGroup> groups = ResolveBundleGroups(offer);
        if (bundleGroupIndex < 0 || bundleGroupIndex >= groups.Count)
        {
            throw new ArgumentOutOfRangeException("bundleGroupIndex");
        }
        SdpBundleGroup currentGroup = groups[bundleGroupIndex];
        if (currentGroup.SourceGroup == null)
        {
            throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
        }
        List<int> previousMediaIndexes = ResolveOfferedBundleMediaIndexes(offer, currentGroup);
        ValidateSubsequentOfferSelection(offer, groups, bundleGroupIndex, previousMediaIndexes, bundledMediaIndexes, taggedMediaIndex, disabledMediaIndexes);
        List<string> mediaIds = PrepareMediaIds(offer);
        List<int> movedMediaIndexes = CollectMovedBundleMediaIndexes(previousMediaIndexes, bundledMediaIndexes, disabledMediaIndexes);
        List<int> fallbackMediaIndexes = new List<int>();
        if (bundledMediaIndexes.Count > 0)
        {
            ValidateTaggedBundleTransport(offer, taggedMediaIndex);
            fallbackMediaIndexes.Add(taggedMediaIndex);
        }
        int movedIndex = 0;
        while (movedIndex < movedMediaIndexes.Count)
        {
            fallbackMediaIndexes.Add(movedMediaIndexes[movedIndex]);
            movedIndex = movedIndex + 1;
        }
        if (fallbackMediaIndexes.Count > 0)
        {
            ValidateFallbackTransports(offer, fallbackMediaIndexes, new List<int>());
        }

        List<SdpAttribute?> mediaIdAttributes = EncodeMissingMediaIds(offer, mediaIds);
        SdpGroupAttribute? replacement = null;
        if (bundledMediaIndexes.Count > 0)
        {
            List<string> groupIds = OrderBundleMediaIds(bundledMediaIndexes, taggedMediaIndex, mediaIds);
            replacement = SdpGroupServiceCore.CreateGroup(SdpGroupSemantic.Bundle, string.Empty, groupIds);
        }
        ApplySubsequentBundleOffer(offer, currentGroup.SourceGroup, previousMediaIndexes, bundledMediaIndexes, disabledMediaIndexes, taggedMediaIndex, mediaIdAttributes, replacement);
        if (replacement == null)
        {
            return null;
        }
        return CreateBundleGroup(currentGroup.SourceGroup);
    }

    internal static void ValidateSubsequentOfferSelection(SdpSessionDescription offer, List<SdpBundleGroup> groups, int bundleGroupIndex, List<int> previousMediaIndexes, List<int> bundledMediaIndexes, int taggedMediaIndex, List<int> disabledMediaIndexes)
    {
        int index = 0;
        while (index < bundledMediaIndexes.Count)
        {
            int mediaIndex = bundledMediaIndexes[index];
            if (mediaIndex < 0 || mediaIndex >= offer.MediaDescriptions.Count)
            {
                throw new ArgumentOutOfRangeException("bundledMediaIndexes");
            }
            if (ContainsEarlierIndex(bundledMediaIndexes, index))
            {
                throw new ArgumentException("Bundled media indexes must be distinct.", "bundledMediaIndexes");
            }
            ValidateOtherBundleMembership(offer, groups, bundleGroupIndex, mediaIndex);
            index = index + 1;
        }

        if (bundledMediaIndexes.Count == 0)
        {
            if (taggedMediaIndex != -1)
            {
                throw new ArgumentException("A dissolved BUNDLE group must not select a tagged media section.", "taggedMediaIndex");
            }
        }
        else
        {
            if (taggedMediaIndex < 0 || taggedMediaIndex >= offer.MediaDescriptions.Count)
            {
                throw new ArgumentOutOfRangeException("taggedMediaIndex");
            }
            if (!bundledMediaIndexes.Contains(taggedMediaIndex))
            {
                throw new ArgumentException("The tagged media section must remain in the BUNDLE group.", "taggedMediaIndex");
            }
        }

        index = 0;
        while (index < disabledMediaIndexes.Count)
        {
            int mediaIndex = disabledMediaIndexes[index];
            if (mediaIndex < 0 || mediaIndex >= offer.MediaDescriptions.Count)
            {
                throw new ArgumentOutOfRangeException("disabledMediaIndexes");
            }
            if (ContainsEarlierIndex(disabledMediaIndexes, index))
            {
                throw new ArgumentException("Disabled media indexes must be distinct.", "disabledMediaIndexes");
            }
            if (!previousMediaIndexes.Contains(mediaIndex) || bundledMediaIndexes.Contains(mediaIndex))
            {
                throw new ArgumentException("Disabled media must be previous members removed from the BUNDLE group.", "disabledMediaIndexes");
            }
            index = index + 1;
        }
    }

    internal static void ValidateOtherBundleMembership(SdpSessionDescription offer, List<SdpBundleGroup> groups, int currentGroupIndex, int mediaIndex)
    {
        SdpMediaIdAttribute? mediaId = ReadSingleMediaId(offer.MediaDescriptions[mediaIndex]);
        if (mediaId == null)
        {
            return;
        }
        int groupIndex = 0;
        while (groupIndex < groups.Count)
        {
            if (groupIndex != currentGroupIndex && groups[groupIndex].MediaIds.Contains(mediaId.Id))
            {
                throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
            }
            groupIndex = groupIndex + 1;
        }
    }

    internal static List<int> CollectMovedBundleMediaIndexes(List<int> previousMediaIndexes, List<int> bundledMediaIndexes, List<int> disabledMediaIndexes)
    {
        List<int> result = new List<int>();
        int index = 0;
        while (index < previousMediaIndexes.Count)
        {
            int mediaIndex = previousMediaIndexes[index];
            if (!bundledMediaIndexes.Contains(mediaIndex) && !disabledMediaIndexes.Contains(mediaIndex))
            {
                result.Add(mediaIndex);
            }
            index = index + 1;
        }
        return result;
    }

    internal static void ApplySubsequentBundleOffer(SdpSessionDescription offer, SdpGroupAttribute currentGroup, List<int> previousMediaIndexes, List<int> bundledMediaIndexes, List<int> disabledMediaIndexes, int taggedMediaIndex, List<SdpAttribute?> mediaIdAttributes, SdpGroupAttribute? replacement)
    {
        int mediaIndex = 0;
        while (mediaIndex < mediaIdAttributes.Count)
        {
            SdpAttribute? mediaIdAttribute = mediaIdAttributes[mediaIndex];
            if (mediaIdAttribute != null)
            {
                offer.MediaDescriptions[mediaIndex].Attributes.Add(mediaIdAttribute);
            }
            mediaIndex = mediaIndex + 1;
        }

        int previousIndex = 0;
        while (previousIndex < previousMediaIndexes.Count)
        {
            RemoveAttributes(offer.MediaDescriptions[previousMediaIndexes[previousIndex]].Attributes, SdpAttributeNames.BundleOnly);
            previousIndex = previousIndex + 1;
        }
        if (bundledMediaIndexes.Count > 0 && previousMediaIndexes.Count > 0 && previousMediaIndexes[0] != taggedMediaIndex)
        {
            CopyMissingBundleAttributes(offer.MediaDescriptions[previousMediaIndexes[0]].Attributes, offer.MediaDescriptions[taggedMediaIndex].Attributes);
        }
        int bundledIndex = 0;
        while (bundledIndex < bundledMediaIndexes.Count)
        {
            mediaIndex = bundledMediaIndexes[bundledIndex];
            SdpMediaDescription media = offer.MediaDescriptions[mediaIndex];
            RemoveAttributes(media.Attributes, SdpAttributeNames.BundleOnly);
            if (mediaIndex != taggedMediaIndex)
            {
                CopyTaggedBundleEndpoint(offer, taggedMediaIndex, mediaIndex);
                RemoveUntaggedBundleAttributes(media.Attributes);
            }
            bundledIndex = bundledIndex + 1;
        }
        int disabledIndex = 0;
        while (disabledIndex < disabledMediaIndexes.Count)
        {
            SdpMediaDescription disabledMedia = offer.MediaDescriptions[disabledMediaIndexes[disabledIndex]];
            disabledMedia.Port = 0;
            disabledMedia.HasPortCount = false;
            disabledMedia.PortCount = 1;
            disabledIndex = disabledIndex + 1;
        }

        if (replacement == null)
        {
            SdpGroupServiceCore.RemoveGroup(offer, currentGroup);
        }
        else
        {
            SdpGroupServiceCore.UpdateGroup(currentGroup, replacement);
        }
    }

    internal static SdpBundleGroup UpdateSubsequentBundleAnswer(SdpSessionDescription offer, SdpSessionDescription answer, int offeredBundleGroupIndex, List<int> acceptedMediaIndexes)
    {
        if (object.ReferenceEquals(offer, answer))
        {
            throw new ArgumentException("The offer and answer must be distinct descriptions.", "answer");
        }
        if (offer.MediaDescriptions.Count != answer.MediaDescriptions.Count)
        {
            throw new ArgumentException("The offer and answer must contain the same number of media sections.", "answer");
        }
        List<SdpBundleGroup> offeredGroups = ResolveBundleGroups(offer);
        if (offeredBundleGroupIndex < 0 || offeredBundleGroupIndex >= offeredGroups.Count)
        {
            throw new ArgumentOutOfRangeException("offeredBundleGroupIndex");
        }
        SdpBundleGroup offeredGroup = offeredGroups[offeredBundleGroupIndex];
        List<int> offeredMediaIndexes = ResolveOfferedBundleMediaIndexes(offer, offeredGroup);
        ValidateAcceptedBundleMedia(offer, answer, offeredMediaIndexes, acceptedMediaIndexes);
        int taggedMediaIndex = offeredMediaIndexes[0];
        if (!acceptedMediaIndexes.Contains(taggedMediaIndex) || offer.MediaDescriptions[taggedMediaIndex].Port == 0 || IsBundleOnlyMedia(offer.MediaDescriptions[taggedMediaIndex]))
        {
            throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
        }
        ValidateTaggedBundleTransport(answer, taggedMediaIndex);
        SdpGroupAttribute? existingAnswerGroup = FindExistingSubsequentAnswerGroup(answer, offeredGroup);
        List<SdpAttribute?> mediaIdAttributes = PrepareAnswerMediaIdAttributes(answer, offeredGroup, offeredMediaIndexes);
        List<string> groupIds = OrderInitialAnswerMediaIds(offeredGroup, offeredMediaIndexes, acceptedMediaIndexes, taggedMediaIndex);
        SdpGroupAttribute replacement = SdpGroupServiceCore.CreateGroup(SdpGroupSemantic.Bundle, string.Empty, groupIds);
        ApplySubsequentBundleAnswer(answer, offeredMediaIndexes, acceptedMediaIndexes, taggedMediaIndex, mediaIdAttributes, existingAnswerGroup, replacement);
        if (existingAnswerGroup != null)
        {
            return CreateBundleGroup(existingAnswerGroup);
        }
        return CreateBundleGroup(replacement);
    }

    internal static SdpGroupAttribute? FindExistingSubsequentAnswerGroup(SdpSessionDescription answer, SdpBundleGroup offeredGroup)
    {
        SdpGroupAttribute? result = null;
        List<SdpBundleGroup> answerGroups = ResolveBundleGroups(answer);
        int groupIndex = 0;
        while (groupIndex < answerGroups.Count)
        {
            SdpBundleGroup candidate = answerGroups[groupIndex];
            bool overlaps = false;
            int mediaIdIndex = 0;
            while (mediaIdIndex < candidate.MediaIds.Count)
            {
                if (offeredGroup.MediaIds.Contains(candidate.MediaIds[mediaIdIndex]))
                {
                    overlaps = true;
                }
                else if (overlaps)
                {
                    throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
                }
                mediaIdIndex = mediaIdIndex + 1;
            }
            if (overlaps)
            {
                if (result != null || candidate.SourceGroup == null)
                {
                    throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
                }
                mediaIdIndex = 0;
                while (mediaIdIndex < candidate.MediaIds.Count)
                {
                    if (!offeredGroup.MediaIds.Contains(candidate.MediaIds[mediaIdIndex]))
                    {
                        throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationBundleIncompatible, null);
                    }
                    mediaIdIndex = mediaIdIndex + 1;
                }
                result = candidate.SourceGroup;
            }
            groupIndex = groupIndex + 1;
        }
        return result;
    }

    internal static void ApplySubsequentBundleAnswer(SdpSessionDescription answer, List<int> offeredMediaIndexes, List<int> acceptedMediaIndexes, int taggedMediaIndex, List<SdpAttribute?> mediaIdAttributes, SdpGroupAttribute? existingAnswerGroup, SdpGroupAttribute replacement)
    {
        int mediaIndex = 0;
        while (mediaIndex < mediaIdAttributes.Count)
        {
            SdpAttribute? mediaIdAttribute = mediaIdAttributes[mediaIndex];
            if (mediaIdAttribute != null)
            {
                answer.MediaDescriptions[mediaIndex].Attributes.Add(mediaIdAttribute);
            }
            mediaIndex = mediaIndex + 1;
        }
        int groupIndex = 0;
        while (groupIndex < offeredMediaIndexes.Count)
        {
            mediaIndex = offeredMediaIndexes[groupIndex];
            SdpMediaDescription media = answer.MediaDescriptions[mediaIndex];
            RemoveAttributes(media.Attributes, SdpAttributeNames.BundleOnly);
            if (acceptedMediaIndexes.Contains(mediaIndex))
            {
                if (mediaIndex != taggedMediaIndex)
                {
                    CopyTaggedBundleEndpoint(answer, taggedMediaIndex, mediaIndex);
                    RemoveUntaggedBundleAttributes(media.Attributes);
                }
            }
            else
            {
                media.Port = 0;
                media.HasPortCount = false;
                media.PortCount = 1;
            }
            groupIndex = groupIndex + 1;
        }
        if (existingAnswerGroup == null)
        {
            SdpGroupServiceCore.AddGroup(answer, replacement);
        }
        else
        {
            SdpGroupServiceCore.UpdateGroup(existingAnswerGroup, replacement);
        }
    }

    internal static void RemoveAttributes(List<SdpAttribute> attributes, string name)
    {
        int index = attributes.Count - 1;
        while (index >= 0)
        {
            if (SdpRtpValidationCore.NameEquals(attributes[index].Name, name))
            {
                attributes.RemoveAt(index);
            }
            index = index - 1;
        }
    }

    internal static SdpAttribute CreatePropertyAttribute(string name)
    {
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Property;
        attribute.Name = name;
        attribute.RawText = name;
        return attribute;
    }

    internal static bool ContainsMediaId(List<string> mediaIds, string mediaId)
    {
        int index = 0;
        while (index < mediaIds.Count)
        {
            if (mediaIds[index] == mediaId)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool HasIceSignaling(SdpSessionDescription description, SdpMediaDescription media)
    {
        return ContainsAnyAttribute(description.Attributes, SdpAttributeNames.IceUsernameFragment, SdpAttributeNames.IcePassword, SdpAttributeNames.IceOptions, SdpAttributeNames.IceLite)
            || ContainsAnyAttribute(media.Attributes, SdpAttributeNames.IceUsernameFragment, SdpAttributeNames.IcePassword, SdpAttributeNames.IceOptions, SdpAttributeNames.IceMismatch)
            || ContainsAnyAttribute(media.Attributes, SdpAttributeNames.Candidate, SdpAttributeNames.RemoteCandidates, SdpAttributeNames.EndOfCandidates, string.Empty);
    }

    internal static bool HasDtlsSignaling(SdpSessionDescription description, SdpMediaDescription media)
    {
        return ContainsAnyAttribute(description.Attributes, SdpAttributeNames.Fingerprint, SdpAttributeNames.Setup, SdpAttributeNames.Connection, SdpAttributeNames.Crypto)
            || ContainsAnyAttribute(media.Attributes, SdpAttributeNames.Fingerprint, SdpAttributeNames.Setup, SdpAttributeNames.Connection, SdpAttributeNames.TlsId)
            || ContainsAnyAttribute(media.Attributes, SdpAttributeNames.Crypto, string.Empty, string.Empty, string.Empty);
    }

    internal static bool ContainsAnyAttribute(List<SdpAttribute> attributes, string first, string second, string third, string fourth)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            string name = attributes[index].Name;
            if (SdpRtpValidationCore.NameEquals(name, first)
                || second.Length > 0 && SdpRtpValidationCore.NameEquals(name, second)
                || third.Length > 0 && SdpRtpValidationCore.NameEquals(name, third)
                || fourth.Length > 0 && SdpRtpValidationCore.NameEquals(name, fourth))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static SdpConnectionData CopyConnection(SdpConnectionData source)
    {
        SdpConnectionData result = new SdpConnectionData();
        result.NetworkType = source.NetworkType;
        result.AddressType = source.AddressType;
        result.Address = source.Address;
        result.AddressKind = source.AddressKind;
        result.HasTimeToLive = source.HasTimeToLive;
        result.TimeToLive = source.TimeToLive;
        result.HasAddressCount = source.HasAddressCount;
        result.AddressCount = source.AddressCount;
        result.RawAddress = source.RawAddress;
        result.RawText = source.RawText;
        result.Location = SdpDiagnosticBuilder.CopySourceLocation(source.Location);
        return result;
    }
}
