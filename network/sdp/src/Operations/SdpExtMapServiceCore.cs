using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpExtMapServiceCore
{
    internal static List<SdpExtMapAttribute> ResolveMediaExtMaps(SdpSessionDescription description, int mediaIndex)
    {
        SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
        List<SdpExtMapAttribute> result = new List<SdpExtMapAttribute>();
        if (!SdpRtpValidationCore.IsRtpProtocol(media.Protocol))
        {
            return result;
        }

        List<SdpAttribute> attributes = description.Attributes;
        bool sessionScope = true;
        if (ContainsAttribute(media.Attributes, SdpAttributeNames.ExtMap))
        {
            attributes = media.Attributes;
            sessionScope = false;
        }
        SdpDirection inheritedDirection = ReadInheritedDirection(description.Attributes, media);
        bool secureMedia = SdpExtMapValidationCore.IsSecureRtpProtocol(media.Protocol);
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.ExtMap, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.ExtMapAttribute != null && (!sessionScope || !decoded.ExtMapAttribute.Encrypted || secureMedia))
                {
                    result.Add(CopyEffectiveMapping(decoded.ExtMapAttribute, inheritedDirection));
                }
            }
            index = index + 1;
        }
        return result;
    }

    internal static bool ResolveMediaExtMapAllowMixed(SdpSessionDescription description, int mediaIndex)
    {
        SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
        if (!SdpRtpValidationCore.IsRtpProtocol(media.Protocol))
        {
            return false;
        }
        return ContainsValidAllowMixed(description.Attributes) || ContainsValidAllowMixed(media.Attributes);
    }

    internal static string? FindUriById(SdpSessionDescription description, int mediaIndex, int id)
    {
        List<SdpExtMapAttribute> mappings = ResolveMediaExtMaps(description, mediaIndex);
        int index = 0;
        while (index < mappings.Count)
        {
            if (mappings[index].Id == id)
            {
                return mappings[index].Uri;
            }
            index = index + 1;
        }
        return null;
    }

    internal static int FindIdByUri(SdpSessionDescription description, int mediaIndex, string uri)
    {
        List<SdpExtMapAttribute> mappings = ResolveMediaExtMaps(description, mediaIndex);
        int index = 0;
        while (index < mappings.Count)
        {
            if (string.Equals(mappings[index].Uri, uri, StringComparison.Ordinal))
            {
                return mappings[index].Id;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static List<SdpExtMapAttribute> IntersectExtMaps(List<SdpExtMapAttribute> offeredMappings, List<SdpHeaderExtension> localCapabilities)
    {
        List<SdpExtMapAttribute> result = new List<SdpExtMapAttribute>();
        List<int> preferences = new List<int>();
        int offeredIndex = 0;
        while (offeredIndex < offeredMappings.Count)
        {
            SdpExtMapAttribute offered = offeredMappings[offeredIndex];
            SdpHeaderExtension? capability = FindBestCapability(offered, localCapabilities);
            if (capability != null)
            {
                SdpDirection answerDirection = SelectAnswerDirection(offered.Direction, capability.Direction);
                SdpExtMapAttribute answer = CopyAnswerMapping(offered, answerDirection);
                int existingIndex = -1;
                if (IsNegotiationIdentifier(offered.Id))
                {
                    existingIndex = FindMappingIndexByIdentifier(result, offered.Id);
                }
                if (existingIndex < 0)
                {
                    result.Add(answer);
                    preferences.Add(capability.Preference);
                }
                else if (capability.Preference < preferences[existingIndex])
                {
                    result[existingIndex] = answer;
                    preferences[existingIndex] = capability.Preference;
                }
            }
            offeredIndex = offeredIndex + 1;
        }
        return result;
    }

    internal static SdpHeaderExtension? FindBestCapability(SdpExtMapAttribute offered, List<SdpHeaderExtension> capabilities)
    {
        if (!IsValidMappingData(offered))
        {
            return null;
        }
        SdpHeaderExtension? result = null;
        int index = 0;
        while (index < capabilities.Count)
        {
            SdpHeaderExtension capability = capabilities[index];
            bool validDirection = IsDefinedDirection(capability.Direction);
            bool uriMatches = offered.Uri != null && capability.Uri != null && string.Equals(offered.Uri, capability.Uri, StringComparison.Ordinal);
            bool encryptionMatches = !offered.Encrypted || capability.SupportsEncryption;
            bool attributesMatch = capability.ExtensionAttributes.Length == 0 || offered.ExtensionAttributes == capability.ExtensionAttributes;
            if (validDirection && uriMatches && encryptionMatches && attributesMatch && (result == null || capability.Preference < result.Preference))
            {
                result = capability;
            }
            index = index + 1;
        }
        return result;
    }

    internal static SdpDirection SelectAnswerDirection(SdpDirection offeredDirection, SdpDirection localDirection)
    {
        bool send = CanReceive(offeredDirection) && CanSend(localDirection);
        bool receive = CanSend(offeredDirection) && CanReceive(localDirection);
        if (send && receive)
        {
            return SdpDirection.SendReceive;
        }
        if (send)
        {
            return SdpDirection.SendOnly;
        }
        if (receive)
        {
            return SdpDirection.ReceiveOnly;
        }
        return SdpDirection.Inactive;
    }

    internal static bool CanSend(SdpDirection direction)
    {
        return direction == SdpDirection.SendReceive || direction == SdpDirection.SendOnly;
    }

    internal static bool CanReceive(SdpDirection direction)
    {
        return direction == SdpDirection.SendReceive || direction == SdpDirection.ReceiveOnly;
    }

    internal static SdpExtMapAttribute CopyAnswerMapping(SdpExtMapAttribute offered, SdpDirection answerDirection)
    {
        SdpExtMapAttribute result = new SdpExtMapAttribute();
        result.SourceAttribute = offered.SourceAttribute;
        result.Id = offered.Id;
        result.HasDirection = offered.HasDirection || answerDirection != SdpDirection.SendReceive;
        result.Direction = answerDirection;
        result.Encrypted = offered.Encrypted;
        result.Uri = offered.Uri;
        result.ExtensionAttributes = offered.ExtensionAttributes;
        return result;
    }

    internal static int FindMappingIndexByIdentifier(List<SdpExtMapAttribute> mappings, int identifier)
    {
        int index = 0;
        while (index < mappings.Count)
        {
            if (mappings[index].Id == identifier)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static List<SdpHeaderExtensionMapping> AllocateExtMapIds(List<SdpExtMapAttribute> mappings)
    {
        List<int> offeredIdentifiers = new List<int>();
        List<int> usedIdentifiers = new List<int>();
        List<SdpExtMapAttribute> acceptedMappings = new List<SdpExtMapAttribute>();
        int index = 0;
        while (index < mappings.Count)
        {
            SdpExtMapAttribute mapping = mappings[index];
            if (!IsUsableIdentifier(mapping.Id) && !IsNegotiationIdentifier(mapping.Id))
            {
                throw new ArgumentOutOfRangeException("mappings");
            }
            if (!IsValidMappingData(mapping))
            {
                throw new ArgumentException("Every extmap must contain a valid URI, direction, and extension-attribute value.", "mappings");
            }
            if (offeredIdentifiers.Contains(mapping.Id))
            {
                throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationExtMapAllocationFailed, null);
            }
            if (ContainsMappingIdentity(acceptedMappings, mapping))
            {
                throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationExtMapAllocationFailed, null);
            }
            offeredIdentifiers.Add(mapping.Id);
            acceptedMappings.Add(mapping);
            if (IsUsableIdentifier(mapping.Id))
            {
                usedIdentifiers.Add(mapping.Id);
            }
            index = index + 1;
        }

        List<SdpHeaderExtensionMapping> result = new List<SdpHeaderExtensionMapping>();
        index = 0;
        while (index < mappings.Count)
        {
            SdpExtMapAttribute extMap = mappings[index];
            int negotiatedIdentifier = extMap.Id;
            if (IsNegotiationIdentifier(extMap.Id))
            {
                negotiatedIdentifier = FindFreeIdentifier(usedIdentifiers);
                if (negotiatedIdentifier < 0)
                {
                    throw new SdpNegotiationException(SdpDiagnosticCodes.NegotiationExtMapAllocationFailed, null);
                }
                usedIdentifiers.Add(negotiatedIdentifier);
            }
            SdpHeaderExtensionMapping mapping = new SdpHeaderExtensionMapping();
            mapping.OfferedId = extMap.Id;
            mapping.NegotiatedId = negotiatedIdentifier;
            mapping.Uri = extMap.Uri;
            mapping.Direction = extMap.Direction;
            mapping.Encrypted = extMap.Encrypted;
            mapping.ExtensionAttributes = extMap.ExtensionAttributes;
            result.Add(mapping);
            index = index + 1;
        }
        return result;
    }

    internal static bool ContainsMappingIdentity(List<SdpExtMapAttribute> mappings, SdpExtMapAttribute target)
    {
        int index = 0;
        while (index < mappings.Count)
        {
            SdpExtMapAttribute mapping = mappings[index];
            if (mapping.Encrypted == target.Encrypted && mapping.Uri == target.Uri && mapping.ExtensionAttributes == target.ExtensionAttributes)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static int FindFreeIdentifier(List<int> usedIdentifiers)
    {
        int identifier = 1;
        while (identifier <= 256)
        {
            if (!usedIdentifiers.Contains(identifier))
            {
                return identifier;
            }
            identifier = identifier + 1;
        }
        return -1;
    }

    internal static bool IsUsableIdentifier(int identifier)
    {
        return identifier >= 1 && identifier <= 256;
    }

    internal static bool IsNegotiationIdentifier(int identifier)
    {
        return identifier >= 4096 && identifier <= 4351;
    }

    internal static bool IsValidMappingData(SdpExtMapAttribute mapping)
    {
        if (mapping.Uri == null || !SdpExtMapAttributeCodecCore.IsAbsoluteUri(mapping.Uri) || !IsDefinedDirection(mapping.Direction) || mapping.ExtensionAttributes == null)
        {
            return false;
        }
        return mapping.ExtensionAttributes.Length == 0 || SdpExtMapAttributeCodecCore.IsSafeExtensionAttributes(mapping.ExtensionAttributes);
    }

    internal static bool IsDefinedDirection(SdpDirection direction)
    {
        return direction == SdpDirection.SendReceive || direction == SdpDirection.SendOnly || direction == SdpDirection.ReceiveOnly || direction == SdpDirection.Inactive;
    }

    internal static void ValidateExtMapRemap(SdpEffectiveMedia media, List<SdpHeaderExtensionMapping> mappings)
    {
        List<int> offeredIdentifiers = new List<int>();
        List<int> negotiatedIdentifiers = new List<int>();
        int index = 0;
        while (index < mappings.Count)
        {
            SdpHeaderExtensionMapping mapping = mappings[index];
            if (mapping == null)
            {
                throw new ArgumentException("An extmap identifier mapping collection must not contain null entries.", "mappings");
            }
            bool offeredValid = IsUsableIdentifier(mapping.OfferedId) || IsNegotiationIdentifier(mapping.OfferedId);
            bool attributesValid = mapping.ExtensionAttributes != null && (mapping.ExtensionAttributes.Length == 0 || SdpExtMapAttributeCodecCore.IsSafeExtensionAttributes(mapping.ExtensionAttributes));
            if (!offeredValid || !IsUsableIdentifier(mapping.NegotiatedId) || mapping.Uri == null || !SdpExtMapAttributeCodecCore.IsAbsoluteUri(mapping.Uri) || !IsDefinedDirection(mapping.Direction) || !attributesValid)
            {
                throw new ArgumentException("Every extmap identifier mapping must contain valid offered, negotiated, URI, direction, and extension-attribute values.", "mappings");
            }
            if (offeredIdentifiers.Contains(mapping.OfferedId) || negotiatedIdentifiers.Contains(mapping.NegotiatedId))
            {
                throw new ArgumentException("Extmap identifier mappings must have unique offered and negotiated assignments.", "mappings");
            }
            if (HasEarlierIdentifierMappingIdentity(mappings, index))
            {
                throw new ArgumentException("Extmap identifier mappings must identify unique extension configurations.", "mappings");
            }
            offeredIdentifiers.Add(mapping.OfferedId);
            negotiatedIdentifiers.Add(mapping.NegotiatedId);
            index = index + 1;
        }

        ValidateHeaderExtensionGraph(media.HeaderExtensions, mappings);
        if (media.BundleTransport != null)
        {
            ValidateHeaderExtensionGraph(media.BundleTransport.HeaderExtensions, mappings);
        }
    }

    internal static bool HasEarlierIdentifierMappingIdentity(List<SdpHeaderExtensionMapping> mappings, int targetIndex)
    {
        SdpHeaderExtensionMapping target = mappings[targetIndex];
        int index = 0;
        while (index < targetIndex)
        {
            SdpHeaderExtensionMapping mapping = mappings[index];
            if (mapping != null && mapping.Encrypted == target.Encrypted && mapping.Uri == target.Uri && mapping.ExtensionAttributes == target.ExtensionAttributes)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static void ValidateHeaderExtensionGraph(List<SdpExtMapAttribute> extensions, List<SdpHeaderExtensionMapping> mappings)
    {
        int index = 0;
        while (index < extensions.Count)
        {
            SdpExtMapAttribute extension = extensions[index];
            if (extension == null)
            {
                throw new ArgumentException("An effective header-extension collection must not contain null entries.", "media");
            }
            if (FindHeaderExtensionMapping(extension, mappings) == null)
            {
                throw new ArgumentException("Every effective header extension must have a complete identifier mapping.", "mappings");
            }
            index = index + 1;
        }
    }

    internal static SdpHeaderExtensionMapping? FindHeaderExtensionMapping(SdpExtMapAttribute extension, List<SdpHeaderExtensionMapping> mappings)
    {
        int index = 0;
        while (index < mappings.Count)
        {
            SdpHeaderExtensionMapping mapping = mappings[index];
            bool identifierMatches = extension.Id == mapping.OfferedId || extension.Id == mapping.NegotiatedId;
            if (identifierMatches && extension.Encrypted == mapping.Encrypted && extension.Uri == mapping.Uri && extension.ExtensionAttributes == mapping.ExtensionAttributes)
            {
                return mapping;
            }
            index = index + 1;
        }
        return null;
    }

    internal static List<SdpExtMapAttribute> PrepareBundleExtMapRemap(SdpSessionDescription description, SdpGroupAttribute bundleGroup, List<SdpHeaderExtensionMapping> mappings)
    {
        if (bundleGroup.Semantic != SdpGroupSemantic.Bundle || bundleGroup.IdentificationTags.Count == 0)
        {
            throw new ArgumentException("A BUNDLE group with at least one media identifier is required.", "bundleGroup");
        }

        List<int> negotiatedIdentifiers = new List<int>();
        int mappingIndex = 0;
        while (mappingIndex < mappings.Count)
        {
            SdpHeaderExtensionMapping mapping = mappings[mappingIndex];
            if (mapping == null)
            {
                throw new ArgumentException("An extmap identifier mapping collection must not contain null entries.", "mappings");
            }
            bool offeredValid = IsUsableIdentifier(mapping.OfferedId) || IsNegotiationIdentifier(mapping.OfferedId);
            bool attributesValid = mapping.ExtensionAttributes != null && (mapping.ExtensionAttributes.Length == 0 || SdpExtMapAttributeCodecCore.IsSafeExtensionAttributes(mapping.ExtensionAttributes));
            if (!offeredValid || !IsUsableIdentifier(mapping.NegotiatedId) || mapping.Uri == null || !SdpExtMapAttributeCodecCore.IsAbsoluteUri(mapping.Uri) || !IsDefinedDirection(mapping.Direction) || !attributesValid)
            {
                throw new ArgumentException("Every extmap identifier mapping must contain valid offered, negotiated, URI, direction, and extension-attribute values.", "mappings");
            }
            if (negotiatedIdentifiers.Contains(mapping.NegotiatedId))
            {
                throw new ArgumentException("BUNDLE extmap mappings must have unique negotiated identifier assignments.", "mappings");
            }
            if (HasEarlierIdentifierMappingIdentity(mappings, mappingIndex))
            {
                throw new ArgumentException("BUNDLE extmap mappings must identify unique extension configurations.", "mappings");
            }
            negotiatedIdentifiers.Add(mapping.NegotiatedId);
            mappingIndex = mappingIndex + 1;
        }

        List<int> mediaIndexes = new List<int>();
        List<SdpExtMapAttribute> extensions = new List<SdpExtMapAttribute>();
        int tagIndex = 0;
        while (tagIndex < bundleGroup.IdentificationTags.Count)
        {
            int mediaIndex = FindUniqueBundleMediaIndex(description, bundleGroup.IdentificationTags[tagIndex]);
            if (mediaIndex < 0 || mediaIndexes.Contains(mediaIndex))
            {
                throw new ArgumentException("Every BUNDLE group identifier must resolve to one distinct media section.", "bundleGroup");
            }
            mediaIndexes.Add(mediaIndex);
            List<SdpExtMapAttribute> mediaExtensions = ResolveMediaExtMaps(description, mediaIndex);
            int extensionIndex = 0;
            while (extensionIndex < mediaExtensions.Count)
            {
                SdpExtMapAttribute extension = mediaExtensions[extensionIndex];
                if (FindBundleIdentifierMapping(extension, mappings) == null)
                {
                    throw new ArgumentException("Every bundled header extension must have an identifier mapping for its exact extension configuration.", "mappings");
                }
                extensions.Add(extension);
                extensionIndex = extensionIndex + 1;
            }
            tagIndex = tagIndex + 1;
        }
        return extensions;
    }

    internal static int FindUniqueBundleMediaIndex(SdpSessionDescription description, string mediaId)
    {
        int foundIndex = -1;
        int index = 0;
        while (index < description.MediaDescriptions.Count)
        {
            if (SdpDtlsServiceCore.ReadMediaId(description.MediaDescriptions[index].Attributes) == mediaId)
            {
                if (foundIndex >= 0)
                {
                    return -1;
                }
                foundIndex = index;
            }
            index = index + 1;
        }
        return foundIndex;
    }

    internal static SdpHeaderExtensionMapping? FindBundleIdentifierMapping(SdpExtMapAttribute extension, List<SdpHeaderExtensionMapping> mappings)
    {
        int index = 0;
        while (index < mappings.Count)
        {
            SdpHeaderExtensionMapping mapping = mappings[index];
            if (mapping.Encrypted == extension.Encrypted && mapping.Uri == extension.Uri && mapping.ExtensionAttributes == extension.ExtensionAttributes)
            {
                return mapping;
            }
            index = index + 1;
        }
        return null;
    }

    internal static void RemapBundleExtMapIds(List<SdpExtMapAttribute> extensions, List<SdpHeaderExtensionMapping> mappings)
    {
        int index = 0;
        while (index < extensions.Count)
        {
            SdpExtMapAttribute extension = extensions[index];
            SdpHeaderExtensionMapping? mapping = FindBundleIdentifierMapping(extension, mappings);
            if (mapping != null)
            {
                extension.Id = mapping.NegotiatedId;
                SynchronizeExtMapSource(extension);
            }
            index = index + 1;
        }
    }

    internal static void RemapExtMapIds(SdpEffectiveMedia media, List<SdpHeaderExtensionMapping> mappings)
    {
        RemapHeaderExtensionGraph(media.HeaderExtensions, mappings);
        if (media.BundleTransport != null)
        {
            RemapHeaderExtensionGraph(media.BundleTransport.HeaderExtensions, mappings);
        }
    }

    internal static void RemapHeaderExtensionGraph(List<SdpExtMapAttribute> extensions, List<SdpHeaderExtensionMapping> mappings)
    {
        int index = 0;
        while (index < extensions.Count)
        {
            SdpExtMapAttribute extension = extensions[index];
            SdpHeaderExtensionMapping? mapping = FindHeaderExtensionMapping(extension, mappings);
            if (mapping != null)
            {
                extension.Id = mapping.NegotiatedId;
                extension.HasDirection = extension.HasDirection || mapping.Direction != SdpDirection.SendReceive;
                extension.Direction = mapping.Direction;
                SynchronizeExtMapSource(extension);
            }
            index = index + 1;
        }
    }

    internal static void SynchronizeExtMapSource(SdpExtMapAttribute extension)
    {
        if (extension.SourceAttribute == null)
        {
            return;
        }
        SdpAttributeEncodeResult encoded = SdpExtMapAttributeCodecCore.EncodeExtMap(extension);
        if (encoded.Success && encoded.Attribute != null)
        {
            extension.SourceAttribute.Kind = encoded.Attribute.Kind;
            extension.SourceAttribute.Name = encoded.Attribute.Name;
            extension.SourceAttribute.Value = encoded.Attribute.Value;
            extension.SourceAttribute.RawText = encoded.Attribute.RawText;
        }
    }

    internal static SdpExtMapAttribute CopyEffectiveMapping(SdpExtMapAttribute mapping, SdpDirection inheritedDirection)
    {
        SdpExtMapAttribute result = new SdpExtMapAttribute();
        result.SourceAttribute = mapping.SourceAttribute;
        result.Id = mapping.Id;
        result.HasDirection = mapping.HasDirection;
        result.Direction = mapping.HasDirection ? mapping.Direction : inheritedDirection;
        result.Encrypted = mapping.Encrypted;
        result.Uri = mapping.Uri;
        result.ExtensionAttributes = mapping.ExtensionAttributes;
        return result;
    }

    internal static SdpDirection ReadInheritedDirection(List<SdpAttribute> sessionAttributes, SdpMediaDescription media)
    {
        if (media.Port == 0)
        {
            return SdpDirection.SendReceive;
        }
        SdpDirection direction = ReadDirection(media.Attributes);
        if (direction == SdpDirection.Unknown)
        {
            direction = ReadDirection(sessionAttributes);
        }
        if (direction == SdpDirection.SendOnly || direction == SdpDirection.ReceiveOnly)
        {
            return direction;
        }
        return SdpDirection.SendReceive;
    }

    internal static SdpDirection ReadDirection(List<SdpAttribute> attributes)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (attribute.Kind == SdpAttributeKind.Property)
            {
                string canonicalName = SdpCommonAttributeCodecCore.ReadCanonicalName(attribute.Name);
                SdpDirection direction = SdpCommonAttributeCodecCore.ReadDirection(canonicalName);
                if (direction != SdpDirection.Unknown)
                {
                    return direction;
                }
            }
            index = index + 1;
        }
        return SdpDirection.Unknown;
    }

    internal static bool ContainsValidAllowMixed(List<SdpAttribute> attributes)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.ExtMapAllowMixed, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success)
                {
                    return true;
                }
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool ContainsAttribute(List<SdpAttribute> attributes, string name)
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
}
