using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpGroupValidationCore
{
    internal static void ValidateGroupDescription(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool hasValidGroup = ValidateSessionAttributes(description.Attributes, result, limiter);
        List<string> mediaIds = ValidateMediaIdentifiers(description, result, limiter);
        if (hasValidGroup)
        {
            ValidateRequiredMediaIdentifiers(description, mediaIds, result, limiter);
        }
        ValidateGroups(description, mediaIds, result, limiter);
    }

    internal static bool ValidateSessionAttributes(List<SdpAttribute> attributes, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool hasValidGroup = false;
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(attributes, index);
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.Group))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success || decoded.GroupAttribute == null)
                {
                    AddDecodeIssue(decoded, attribute, SdpScope.Session, -1, occurrenceIndex, result, limiter);
                }
                else
                {
                    hasValidGroup = true;
                }
            }
            else if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.MediaId))
            {
                AddGroupIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpScope.Session, attribute, -1, occurrenceIndex, "The mid attribute is permitted only at media scope.");
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success)
                {
                    AddDecodeIssue(decoded, attribute, SdpScope.Session, -1, occurrenceIndex, result, limiter);
                }
            }
            index = index + 1;
        }
        return hasValidGroup;
    }

    internal static List<string> ValidateMediaIdentifiers(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<string> mediaIds = new List<string>();
        List<string> usedIds = new List<string>();
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            string resolvedId = string.Empty;
            int midCount = 0;
            int attributeIndex = 0;
            while (attributeIndex < media.Attributes.Count)
            {
                SdpAttribute attribute = media.Attributes[attributeIndex];
                int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, attributeIndex);
                if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.MediaId))
                {
                    midCount = midCount + 1;
                    SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                    if (!decoded.Success || decoded.MediaIdAttribute == null)
                    {
                        AddDecodeIssue(decoded, attribute, SdpScope.Media, mediaIndex, occurrenceIndex, result, limiter);
                    }
                    else if (resolvedId.Length == 0)
                    {
                        resolvedId = decoded.MediaIdAttribute.Id;
                        if (usedIds.Contains(resolvedId))
                        {
                            AddGroupIssue(result, limiter, SdpDiagnosticCodes.ValidationGroup, SdpScope.Media, attribute, mediaIndex, occurrenceIndex, "Every mid identification tag must be unique within the SDP session description.");
                        }
                        else
                        {
                            usedIds.Add(resolvedId);
                        }
                    }
                    if (midCount > 1)
                    {
                        AddGroupIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, SdpScope.Media, attribute, mediaIndex, occurrenceIndex, "A media section may contain at most one mid attribute.");
                    }
                }
                else if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.Group))
                {
                    AddGroupIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpScope.Media, attribute, mediaIndex, occurrenceIndex, "The group attribute is permitted only at session scope.");
                    SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                    if (!decoded.Success)
                    {
                        AddDecodeIssue(decoded, attribute, SdpScope.Media, mediaIndex, occurrenceIndex, result, limiter);
                    }
                }
                attributeIndex = attributeIndex + 1;
            }
            mediaIds.Add(resolvedId);
            mediaIndex = mediaIndex + 1;
        }
        return mediaIds;
    }

    internal static void ValidateRequiredMediaIdentifiers(SdpSessionDescription description, List<string> mediaIds, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int mediaIndex = 0;
        while (mediaIndex < mediaIds.Count)
        {
            if (mediaIds[mediaIndex].Length == 0 && !ContainsAttribute(description.MediaDescriptions[mediaIndex].Attributes, SdpAttributeNames.MediaId))
            {
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationGroup, SdpValidationSeverity.Error, SdpScope.Media, SdpAttributeNames.MediaId, mediaIndex, 0, "Every media section must contain a mid attribute when the session uses media grouping.", description.MediaDescriptions[mediaIndex].Location);
            }
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void ValidateGroups(SdpSessionDescription description, List<string> mediaIds, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<string> bundledMediaIds = new List<string>();
        int attributeIndex = 0;
        while (attributeIndex < description.Attributes.Count)
        {
            SdpAttribute attribute = description.Attributes[attributeIndex];
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.Group))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.GroupAttribute != null)
                {
                    int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(description.Attributes, attributeIndex);
                    ValidateSemanticMetadata(decoded.GroupAttribute, attribute, occurrenceIndex, result, limiter);
                    ValidateGroupTags(description, decoded.GroupAttribute, mediaIds, bundledMediaIds, attribute, occurrenceIndex, result, limiter);
                }
            }
            attributeIndex = attributeIndex + 1;
        }
    }

    internal static void ValidateSemanticMetadata(SdpGroupAttribute group, SdpAttribute attribute, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool registered = SdpRegistryData.GroupSemantics.Contains(group.SemanticToken);
        bool recognized = group.Semantic != SdpGroupSemantic.Unknown;
        if (registered != recognized)
        {
            AddGroupIssue(result, limiter, SdpDiagnosticCodes.ValidationGroup, SdpScope.Session, attribute, -1, occurrenceIndex, "The group semantic token is inconsistent with the registered semantic metadata.");
        }
    }

    internal static void ValidateGroupTags(SdpSessionDescription description, SdpGroupAttribute group, List<string> mediaIds, List<string> bundledMediaIds, SdpAttribute attribute, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<string> groupMediaIds = new List<string>();
        int tagIndex = 0;
        while (tagIndex < group.IdentificationTags.Count)
        {
            string mediaId = group.IdentificationTags[tagIndex];
            if (groupMediaIds.Contains(mediaId))
            {
                AddGroupIssue(result, limiter, SdpDiagnosticCodes.ValidationGroup, SdpScope.Session, attribute, -1, occurrenceIndex, "A group identification tag must not be repeated within the same group attribute.");
            }
            else
            {
                groupMediaIds.Add(mediaId);
                int mediaIndex = FindMediaIndex(mediaIds, mediaId);
                if (mediaIndex < 0)
                {
                    AddGroupIssue(result, limiter, SdpDiagnosticCodes.ValidationGroup, SdpScope.Session, attribute, -1, occurrenceIndex, "A group identification tag does not identify an existing media section.");
                }
                else if (group.Semantic != SdpGroupSemantic.Bundle && description.MediaDescriptions[mediaIndex].Port == 0)
                {
                    AddGroupIssue(result, limiter, SdpDiagnosticCodes.ValidationGroup, SdpScope.Session, attribute, -1, occurrenceIndex, "A generic media group must not include a rejected media section whose port is zero.");
                }

                if (group.Semantic == SdpGroupSemantic.Bundle)
                {
                    if (bundledMediaIds.Contains(mediaId))
                    {
                        AddGroupIssue(result, limiter, SdpDiagnosticCodes.ValidationGroup, SdpScope.Session, attribute, -1, occurrenceIndex, "A media section must not be a member of more than one BUNDLE group.");
                    }
                    else
                    {
                        bundledMediaIds.Add(mediaId);
                    }
                }
            }
            tagIndex = tagIndex + 1;
        }
    }

    internal static int FindMediaIndex(List<string> mediaIds, string mediaId)
    {
        int index = 0;
        while (index < mediaIds.Count)
        {
            if (mediaIds[index] == mediaId)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static bool ContainsAttribute(List<SdpAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (SdpRtpValidationCore.NameEquals(attributes[index].Name, name))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static void AddDecodeIssue(SdpAttributeDecodeResult decoded, SdpAttribute attribute, SdpScope scope, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        string detail = "The grouping-related attribute does not satisfy its registered syntax.";
        if (decoded.Diagnostics.Count > 0)
        {
            detail = decoded.Diagnostics[0].Detail;
        }
        AddGroupIssue(result, limiter, SdpDiagnosticCodes.ValidationGroup, scope, attribute, mediaIndex, occurrenceIndex, detail);
    }

    internal static void AddGroupIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, string code, SdpScope scope, SdpAttribute attribute, int mediaIndex, int occurrenceIndex, string detail)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, code, SdpValidationSeverity.Error, scope, attribute.Name, mediaIndex, occurrenceIndex, detail, attribute.Location);
    }
}
