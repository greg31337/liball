using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpGroupServiceCore
{
    internal static List<SdpGroupAttribute> ResolveGroups(SdpSessionDescription description)
    {
        List<SdpGroupAttribute> groups = new List<SdpGroupAttribute>();
        int index = 0;
        while (index < description.Attributes.Count)
        {
            SdpAttribute attribute = description.Attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.Group, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.GroupAttribute != null)
                {
                    groups.Add(CopyGroup(decoded.GroupAttribute));
                }
            }
            index = index + 1;
        }
        return groups;
    }

    internal static SdpMediaIdAttribute? ResolveMediaId(SdpMediaDescription media)
    {
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.MediaId, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.MediaIdAttribute != null)
                {
                    return CopyMediaId(decoded.MediaIdAttribute);
                }
            }
            index = index + 1;
        }
        return null;
    }

    internal static int FindMediaIndex(SdpSessionDescription description, string mediaId)
    {
        int index = 0;
        while (index < description.MediaDescriptions.Count)
        {
            SdpMediaIdAttribute? resolved = ResolveMediaId(description.MediaDescriptions[index]);
            if (resolved != null && resolved.Id == mediaId)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static void ValidateMediaId(string mediaId)
    {
        if (!SdpCommonAttributeCodecCore.IsTokenValue(mediaId))
        {
            throw new ArgumentException("The media identifier must use non-empty token syntax.", "mediaId");
        }
    }

    internal static SdpGroupAttribute CreateGroup(SdpGroupSemantic semantic, string semanticToken, List<string> identificationTags)
    {
        ValidateSemantic(semantic);
        if (semantic == SdpGroupSemantic.Unknown && SdpGroupAttributeCodecCore.ReadGroupSemantic(semanticToken) != SdpGroupSemantic.Unknown)
        {
            throw new ArgumentException("A registered group semantic token must use its corresponding semantic value.", "semanticToken");
        }
        SdpGroupAttribute group = new SdpGroupAttribute();
        group.Semantic = semantic;
        group.SemanticToken = semanticToken;
        int index = 0;
        while (index < identificationTags.Count)
        {
            if (!SdpCommonAttributeCodecCore.IsTokenValue(identificationTags[index]))
            {
                throw new ArgumentException("Every group identification tag must use non-empty token syntax.", "identificationTags");
            }
            group.IdentificationTags.Add(identificationTags[index]);
            index = index + 1;
        }
        if (SdpGroupAttributeCodecCore.ReadGroupSemanticToken(group).Length == 0)
        {
            throw new ArgumentException("The group semantic kind and semantic token are invalid or inconsistent.", "semanticToken");
        }
        SdpAttributeEncodeResult encoded = SdpGroupAttributeCodecCore.EncodeGroup(group);
        if (!encoded.Success || encoded.Attribute == null)
        {
            throw new ArgumentException("The group cannot be represented as an SDP group attribute.", "semanticToken");
        }
        group.SemanticToken = ReadEncodedSemanticToken(encoded.Attribute.Value);
        group.SourceAttribute = encoded.Attribute;
        return group;
    }

    internal static void AddGroup(SdpSessionDescription description, SdpGroupAttribute group)
    {
        if (group.SourceAttribute != null)
        {
            description.Attributes.Add(group.SourceAttribute);
        }
    }

    internal static void UpdateGroup(SdpGroupAttribute group, SdpGroupAttribute replacement)
    {
        SdpAttribute? source = group.SourceAttribute;
        group.Semantic = replacement.Semantic;
        group.SemanticToken = replacement.SemanticToken;
        group.IdentificationTags.Clear();
        int index = 0;
        while (index < replacement.IdentificationTags.Count)
        {
            group.IdentificationTags.Add(replacement.IdentificationTags[index]);
            index = index + 1;
        }
        if (source != null && replacement.SourceAttribute != null)
        {
            source.Kind = replacement.SourceAttribute.Kind;
            source.Name = replacement.SourceAttribute.Name;
            source.Value = replacement.SourceAttribute.Value;
            source.RawText = replacement.SourceAttribute.RawText;
            group.SourceAttribute = source;
        }
    }

    internal static bool RemoveGroup(SdpSessionDescription description, SdpGroupAttribute group)
    {
        if (group.SourceAttribute == null)
        {
            return false;
        }
        int index = 0;
        while (index < description.Attributes.Count)
        {
            if (object.ReferenceEquals(description.Attributes[index], group.SourceAttribute))
            {
                description.Attributes.RemoveAt(index);
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static SdpGroupAttribute CopyGroup(SdpGroupAttribute source)
    {
        SdpGroupAttribute result = new SdpGroupAttribute();
        result.SourceAttribute = source.SourceAttribute;
        result.Semantic = source.Semantic;
        result.SemanticToken = source.SemanticToken;
        int index = 0;
        while (index < source.IdentificationTags.Count)
        {
            result.IdentificationTags.Add(source.IdentificationTags[index]);
            index = index + 1;
        }
        return result;
    }

    internal static SdpMediaIdAttribute CopyMediaId(SdpMediaIdAttribute source)
    {
        SdpMediaIdAttribute result = new SdpMediaIdAttribute();
        result.SourceAttribute = source.SourceAttribute;
        result.Id = source.Id;
        return result;
    }

    internal static string ReadEncodedSemanticToken(string value)
    {
        int end = value.IndexOf(' ');
        if (end < 0)
        {
            return value;
        }
        return value.Substring(0, end);
    }

    internal static void ValidateSemantic(SdpGroupSemantic semantic)
    {
        if (semantic != SdpGroupSemantic.Unknown && semantic != SdpGroupSemantic.LipSynchronization && semantic != SdpGroupSemantic.FlowIdentification && semantic != SdpGroupSemantic.SingleReservationFlow && semantic != SdpGroupSemantic.AlternativeNetworkAddressTypes && semantic != SdpGroupSemantic.ForwardErrorCorrection && semantic != SdpGroupSemantic.DecodingDependency && semantic != SdpGroupSemantic.Bundle && semantic != SdpGroupSemantic.ForwardErrorCorrectionFramework && semantic != SdpGroupSemantic.CompositeSession && semantic != SdpGroupSemantic.Duplication && semantic != SdpGroupSemantic.Splice && semantic != SdpGroupSemantic.Clue && semantic != SdpGroupSemantic.VolumetricVideo)
        {
            throw new ArgumentOutOfRangeException("semantic");
        }
    }
}
