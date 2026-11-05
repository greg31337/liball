using System.Collections.Generic;

namespace sdp;

internal static class SdpMsidValidationCore
{
    internal static void ValidateMsidDescription(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<SdpMediaStreamSemanticAttribute> semantics = new List<SdpMediaStreamSemanticAttribute>();
        List<SdpMediaStreamId> associations = new List<SdpMediaStreamId>();
        List<int> mediaIndexes = new List<int>();
        ReadSessionMsidAttributes(description, semantics, result, limiter);
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            ReadMediaMsidAttributes(description.MediaDescriptions[mediaIndex], mediaIndex, associations, mediaIndexes, result, limiter);
            mediaIndex = mediaIndex + 1;
        }
        ValidateRepeatedAssociations(description, associations, mediaIndexes, result, limiter);
        ValidateSemanticDeclarations(description, semantics, associations, mediaIndexes, result, limiter);
    }

    internal static void ReadSessionMsidAttributes(SdpSessionDescription description, List<SdpMediaStreamSemanticAttribute> semantics, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < description.Attributes.Count)
        {
            SdpAttribute attribute = description.Attributes[index];
            int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(description.Attributes, index);
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.MediaStreamId))
            {
                AddMsidIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpScope.Session, attribute, -1, occurrenceIndex, "The msid attribute is permitted only at media scope.");
                ValidateDecodedMsidAttribute(attribute, SdpScope.Session, -1, occurrenceIndex, result, limiter);
            }
            else if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.MediaStreamIdSemantic))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success || decoded.MediaStreamSemanticAttribute == null)
                {
                    AddDecodeIssue(decoded, attribute, SdpScope.Session, -1, occurrenceIndex, result, limiter);
                }
                else
                {
                    semantics.Add(decoded.MediaStreamSemanticAttribute);
                }
            }
            index = index + 1;
        }
    }

    internal static void ReadMediaMsidAttributes(SdpMediaDescription media, int mediaIndex, List<SdpMediaStreamId> associations, List<int> mediaIndexes, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<SdpMediaStreamId> mediaAssociations = new List<SdpMediaStreamId>();
        bool rtpProtocol = SdpRtpValidationCore.IsRtpProtocol(media.Protocol);
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, index);
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.MediaStreamIdSemantic))
            {
                AddMsidIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpScope.Media, attribute, mediaIndex, occurrenceIndex, "The msid-semantic attribute is permitted only at session scope.");
                ValidateDecodedMsidAttribute(attribute, SdpScope.Media, mediaIndex, occurrenceIndex, result, limiter);
            }
            else if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.MediaStreamId))
            {
                if (!rtpProtocol)
                {
                    AddMsidIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpScope.Media, attribute, mediaIndex, occurrenceIndex, "The msid attribute requires an RTP transport protocol.");
                }
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success || decoded.MediaStreamId == null)
                {
                    AddDecodeIssue(decoded, attribute, SdpScope.Media, mediaIndex, occurrenceIndex, result, limiter);
                }
                else if (rtpProtocol)
                {
                    ValidateMediaAssociation(media, mediaIndex, mediaAssociations, decoded.MediaStreamId, result, limiter);
                    mediaAssociations.Add(decoded.MediaStreamId);
                    associations.Add(decoded.MediaStreamId);
                    mediaIndexes.Add(mediaIndex);
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateDecodedMsidAttribute(SdpAttribute attribute, SdpScope scope, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
        if (!decoded.Success)
        {
            AddDecodeIssue(decoded, attribute, scope, mediaIndex, occurrenceIndex, result, limiter);
        }
    }

    internal static void ValidateMediaAssociation(SdpMediaDescription media, int mediaIndex, List<SdpMediaStreamId> mediaAssociations, SdpMediaStreamId current, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < mediaAssociations.Count)
        {
            SdpMediaStreamId previous = mediaAssociations[index];
            if (previous.HasTrackId != current.HasTrackId || previous.TrackId != current.TrackId)
            {
                AddAssociationIssue(media, mediaIndex, current, SdpDiagnosticCodes.ValidationMsid, "Every msid occurrence in one media section must use identical appdata.", result, limiter);
                return;
            }
            if (previous.StreamId == current.StreamId)
            {
                AddAssociationIssue(media, mediaIndex, current, SdpDiagnosticCodes.ValidationAttributeCardinality, "The same msid stream and appdata association must not be repeated within one media section.", result, limiter);
                return;
            }
            index = index + 1;
        }
    }

    internal static void ValidateRepeatedAssociations(SdpSessionDescription description, List<SdpMediaStreamId> associations, List<int> mediaIndexes, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < associations.Count)
        {
            SdpMediaStreamId current = associations[index];
            int previousIndex = 0;
            while (previousIndex < index)
            {
                SdpMediaStreamId previous = associations[previousIndex];
                if (mediaIndexes[previousIndex] != mediaIndexes[index] && AssociationsEqual(previous, current))
                {
                    SdpAttribute? attribute = current.SourceAttribute;
                    if (attribute != null)
                    {
                        SdpMediaDescription media = description.MediaDescriptions[mediaIndexes[index]];
                        int attributeIndex = media.Attributes.IndexOf(attribute);
                        int occurrenceIndex = 0;
                        if (attributeIndex >= 0)
                        {
                            occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, attributeIndex);
                        }
                        SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationMsid, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndexes[index], occurrenceIndex, "The same msid stream and appdata association must not appear in multiple media sections.", attribute.Location);
                    }
                    previousIndex = index;
                }
                else
                {
                    previousIndex = previousIndex + 1;
                }
            }
            index = index + 1;
        }
    }

    internal static bool AssociationsEqual(SdpMediaStreamId first, SdpMediaStreamId second)
    {
        return first.StreamId == second.StreamId && first.HasTrackId == second.HasTrackId && first.TrackId == second.TrackId;
    }

    internal static void ValidateSemanticDeclarations(SdpSessionDescription description, List<SdpMediaStreamSemanticAttribute> semantics, List<SdpMediaStreamId> associations, List<int> mediaIndexes, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<string> declaredSemantics = new List<string>();
        List<string> declaredStreamIds = new List<string>();
        bool wildcard = false;
        int index = 0;
        while (index < semantics.Count)
        {
            SdpMediaStreamSemanticAttribute semantic = semantics[index];
            SdpAttribute? attribute = semantic.SourceAttribute;
            if (declaredSemantics.Contains(semantic.Semantic) && attribute != null)
            {
                AddSessionSemanticIssue(description, attribute, SdpDiagnosticCodes.ValidationAttributeCardinality, "An msid semantic token must not be declared more than once.", result, limiter);
            }
            else
            {
                declaredSemantics.Add(semantic.Semantic);
            }
            ValidateSemanticStreamIds(description, semantic, associations, declaredStreamIds, result, limiter);
            if (semantic.StreamIds.Contains("*"))
            {
                wildcard = true;
            }
            index = index + 1;
        }
        if (semantics.Count > 0 && !wildcard)
        {
            ValidateDeclaredAssociations(description, associations, mediaIndexes, declaredStreamIds, result, limiter);
        }
    }

    internal static void ValidateSemanticStreamIds(SdpSessionDescription description, SdpMediaStreamSemanticAttribute semantic, List<SdpMediaStreamId> associations, List<string> declaredStreamIds, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<string> localIds = new List<string>();
        int index = 0;
        while (index < semantic.StreamIds.Count)
        {
            string streamId = semantic.StreamIds[index];
            if (streamId != "*")
            {
                if (localIds.Contains(streamId) || declaredStreamIds.Contains(streamId))
                {
                    if (semantic.SourceAttribute != null)
                    {
                        AddSessionSemanticIssue(description, semantic.SourceAttribute, SdpDiagnosticCodes.ValidationMsid, "An msid stream identifier must appear in only one semantic declaration.", result, limiter);
                    }
                }
                else
                {
                    localIds.Add(streamId);
                    declaredStreamIds.Add(streamId);
                    if (!ContainsAssociationStreamId(associations, streamId) && semantic.SourceAttribute != null)
                    {
                        AddSessionSemanticIssue(description, semantic.SourceAttribute, SdpDiagnosticCodes.ValidationMsid, "An msid semantic declaration refers to a stream identifier that is absent from all media sections.", result, limiter);
                    }
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateDeclaredAssociations(SdpSessionDescription description, List<SdpMediaStreamId> associations, List<int> mediaIndexes, List<string> declaredStreamIds, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<string> checkedIds = new List<string>();
        int index = 0;
        while (index < associations.Count)
        {
            SdpMediaStreamId association = associations[index];
            if (!checkedIds.Contains(association.StreamId))
            {
                checkedIds.Add(association.StreamId);
                if (!declaredStreamIds.Contains(association.StreamId) && association.SourceAttribute != null)
                {
                    SdpMediaDescription media = description.MediaDescriptions[mediaIndexes[index]];
                    AddAssociationIssue(media, mediaIndexes[index], association, SdpDiagnosticCodes.ValidationMsid, "An msid stream identifier is absent from the session semantic declarations.", result, limiter);
                }
            }
            index = index + 1;
        }
    }

    internal static bool ContainsAssociationStreamId(List<SdpMediaStreamId> associations, string streamId)
    {
        int index = 0;
        while (index < associations.Count)
        {
            if (associations[index].StreamId == streamId)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static void AddSessionSemanticIssue(SdpSessionDescription description, SdpAttribute attribute, string code, string detail, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int attributeIndex = description.Attributes.IndexOf(attribute);
        int occurrenceIndex = 0;
        if (attributeIndex >= 0)
        {
            occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(description.Attributes, attributeIndex);
        }
        AddMsidIssue(result, limiter, code, SdpScope.Session, attribute, -1, occurrenceIndex, detail);
    }

    internal static void AddAssociationIssue(SdpMediaDescription media, int mediaIndex, SdpMediaStreamId association, string code, string detail, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpAttribute? attribute = association.SourceAttribute;
        if (attribute != null)
        {
            int attributeIndex = media.Attributes.IndexOf(attribute);
            int occurrenceIndex = 0;
            if (attributeIndex >= 0)
            {
                occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, attributeIndex);
            }
            AddMsidIssue(result, limiter, code, SdpScope.Media, attribute, mediaIndex, occurrenceIndex, detail);
        }
    }

    internal static void AddDecodeIssue(SdpAttributeDecodeResult decoded, SdpAttribute attribute, SdpScope scope, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        string code = SdpDiagnosticCodes.ValidationAttribute;
        string detail = "The MSID-related attribute does not satisfy its registered syntax.";
        if (decoded.Diagnostics.Count > 0)
        {
            code = decoded.Diagnostics[0].Code;
            detail = decoded.Diagnostics[0].Detail;
        }
        AddMsidIssue(result, limiter, code, scope, attribute, mediaIndex, occurrenceIndex, detail);
    }

    internal static void AddMsidIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, string code, SdpScope scope, SdpAttribute attribute, int mediaIndex, int occurrenceIndex, string detail)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, code, SdpValidationSeverity.Error, scope, attribute.Name, mediaIndex, occurrenceIndex, detail, attribute.Location);
    }
}
