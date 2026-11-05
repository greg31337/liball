using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpExtMapValidationCore
{
    internal static void ValidateExtMapDescription(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool hasRtpMedia = HasRtpMedia(description);
        bool hasSecureRtpMedia = HasSecureRtpMedia(description);
        bool sessionMappings = ContainsAttribute(description.Attributes, SdpAttributeNames.ExtMap);
        ValidateAttributeSet(description, description.Attributes, SdpScope.Session, -1, hasRtpMedia, hasSecureRtpMedia, result, limiter);

        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            bool rtpMedia = SdpRtpValidationCore.IsRtpProtocol(media.Protocol);
            bool secureRtpMedia = IsSecureRtpProtocol(media.Protocol);
            ValidateAttributeSet(description, media.Attributes, SdpScope.Media, mediaIndex, rtpMedia, secureRtpMedia, result, limiter);
            if (sessionMappings && ContainsAttribute(media.Attributes, SdpAttributeNames.ExtMap))
            {
                AddMixedScopeIssues(media.Attributes, mediaIndex, result, limiter);
            }
            mediaIndex = mediaIndex + 1;
        }
        ValidateBundleCompatibility(description, result, limiter);
    }

    internal static void ValidateBundleCompatibility(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int attributeIndex = 0;
        while (attributeIndex < description.Attributes.Count)
        {
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(description.Attributes[attributeIndex]);
            if (decoded.Success && decoded.GroupAttribute != null && decoded.GroupAttribute.Semantic == SdpGroupSemantic.Bundle)
            {
                ValidateBundleGroup(description, decoded.GroupAttribute, result, limiter);
            }
            attributeIndex = attributeIndex + 1;
        }
    }

    internal static void ValidateBundleGroup(SdpSessionDescription description, SdpGroupAttribute group, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<SdpExtMapAttribute> established = new List<SdpExtMapAttribute>();
        int tagIndex = 0;
        while (tagIndex < group.IdentificationTags.Count)
        {
            int mediaIndex = SdpDtlsServiceCore.FindMediaIndex(description, group.IdentificationTags[tagIndex]);
            if (mediaIndex >= 0)
            {
                List<SdpExtMapAttribute> mappings = SdpExtMapServiceCore.ResolveMediaExtMaps(description, mediaIndex);
                int mappingIndex = 0;
                while (mappingIndex < mappings.Count)
                {
                    SdpExtMapAttribute mapping = mappings[mappingIndex];
                    SdpExtMapAttribute? sameIdentifier = FindIdentifier(established, mapping.Id);
                    SdpExtMapAttribute? sameExtension = FindExtension(established, mapping);
                    bool alternativeIdentifier = SdpExtMapServiceCore.IsNegotiationIdentifier(mapping.Id) && result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Offerer;
                    bool identifierConflict = sameIdentifier != null && !ExtensionsEqual(sameIdentifier, mapping) && !alternativeIdentifier;
                    bool extensionConflict = sameExtension != null && sameExtension.Id != mapping.Id;
                    if (identifierConflict || extensionConflict)
                    {
                        AddBundleCompatibilityIssue(description, mediaIndex, mapping, result, limiter);
                    }
                    else if (sameExtension == null && (sameIdentifier == null || alternativeIdentifier))
                    {
                        established.Add(mapping);
                    }
                    mappingIndex = mappingIndex + 1;
                }
            }
            tagIndex = tagIndex + 1;
        }
    }

    internal static bool ExtensionsEqual(SdpExtMapAttribute first, SdpExtMapAttribute second)
    {
        return first.Encrypted == second.Encrypted && first.Uri == second.Uri && first.ExtensionAttributes == second.ExtensionAttributes;
    }

    internal static void AddBundleCompatibilityIssue(SdpSessionDescription description, int mediaIndex, SdpExtMapAttribute mapping, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpAttribute? source = mapping.SourceAttribute;
        SdpSourceLocation location = description.MediaDescriptions[mediaIndex].Location;
        int occurrenceIndex = 0;
        if (source != null)
        {
            location = source.Location;
            occurrenceIndex = ReadAttributeOccurrence(description.MediaDescriptions[mediaIndex].Attributes, source);
        }
        SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationExtMap, SdpValidationSeverity.Error, SdpScope.Media, SdpAttributeNames.ExtMap, mediaIndex, occurrenceIndex, "Bundled RTP media must use one consistent identifier for each extmap URI, encryption form, and extension-attribute configuration, and one consistent extension configuration for each identifier.", location);
    }

    internal static int ReadAttributeOccurrence(List<SdpAttribute> attributes, SdpAttribute target)
    {
        int occurrenceIndex = 0;
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (object.ReferenceEquals(attribute, target))
            {
                return occurrenceIndex;
            }
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.ExtMap))
            {
                occurrenceIndex = occurrenceIndex + 1;
            }
            index = index + 1;
        }
        return 0;
    }

    internal static void ValidateAttributeSet(SdpSessionDescription description, List<SdpAttribute> attributes, SdpScope scope, int mediaIndex, bool rtpApplicable, bool secureRtpApplicable, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<SdpExtMapAttribute> mappings = new List<SdpExtMapAttribute>();
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(attributes, index);
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.ExtMap))
            {
                ValidateMapping(description, attribute, mappings, scope, mediaIndex, occurrenceIndex, rtpApplicable, secureRtpApplicable, result, limiter);
            }
            else if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.ExtMapAllowMixed))
            {
                ValidateAllowMixed(description, attribute, scope, mediaIndex, occurrenceIndex, rtpApplicable, result, limiter);
            }
            index = index + 1;
        }
    }

    internal static void ValidateMapping(SdpSessionDescription description, SdpAttribute attribute, List<SdpExtMapAttribute> mappings, SdpScope scope, int mediaIndex, int occurrenceIndex, bool rtpApplicable, bool secureRtpApplicable, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!rtpApplicable)
        {
            AddExtMapIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, scope, attribute, mediaIndex, occurrenceIndex, "The extmap attribute requires an RTP media stream in its applicable scope.");
        }

        SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
        SdpExtMapAttribute? mapping = decoded.ExtMapAttribute;
        if (!decoded.Success || mapping == null)
        {
            AddExtMapDecodeIssue(decoded, attribute, scope, mediaIndex, occurrenceIndex, result, limiter);
            return;
        }

        bool usableIdentifier = mapping.Id >= 1 && mapping.Id <= 256;
        bool negotiationIdentifier = mapping.Id >= 4096 && mapping.Id <= 4351;
        if (!usableIdentifier && !negotiationIdentifier)
        {
            AddExtMapIssue(result, limiter, SdpDiagnosticCodes.ValidationExtMap, scope, attribute, mediaIndex, occurrenceIndex, "The extmap identifier must be in the usable range 1 through 256 or the negotiation range 4096 through 4351.");
        }
        else if (negotiationIdentifier && result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Unknown)
        {
            AddExtMapIssue(result, limiter, SdpDiagnosticCodes.ValidationExtMap, scope, attribute, mediaIndex, occurrenceIndex, "An extmap identifier from 4096 through 4351 is permitted only in offer/answer negotiation.");
        }

        SdpExtMapAttribute? sameId = FindIdentifier(mappings, mapping.Id);
        if (sameId != null && (usableIdentifier || result.AppliedOptions.NegotiationRole != SdpNegotiationRole.Offerer))
        {
            AddExtMapIssue(result, limiter, SdpDiagnosticCodes.ValidationExtMap, scope, attribute, mediaIndex, occurrenceIndex, "An extmap identifier must not be assigned more than once in this mapping scope, except for negotiation alternatives in an offer.");
        }
        SdpExtMapAttribute? sameExtension = FindExtension(mappings, mapping);
        if (sameExtension != null)
        {
            AddExtMapIssue(result, limiter, SdpDiagnosticCodes.ValidationExtMap, scope, attribute, mediaIndex, occurrenceIndex, "The same extmap extension URI, encryption form, and extension attributes must not be repeated in one mapping scope.");
        }

        if (mapping.HasDirection && !DirectionIsCompatible(description, scope, mediaIndex, secureRtpApplicable && mapping.Encrypted, mapping.Direction))
        {
            AddExtMapIssue(result, limiter, SdpDiagnosticCodes.ValidationExtMap, scope, attribute, mediaIndex, occurrenceIndex, "The extmap direction is incompatible with the direction of an applicable media stream.");
        }
        if (mapping.Encrypted && !secureRtpApplicable)
        {
            AddExtMapIssue(result, limiter, SdpDiagnosticCodes.ValidationExtMap, scope, attribute, mediaIndex, occurrenceIndex, "An encrypted extmap requires an SRTP-based RTP profile in its applicable scope.");
        }
        if (mapping.Encrypted && mapping.Uri == SdpHeaderExtensionUris.Encryption)
        {
            AddExtMapIssue(result, limiter, SdpDiagnosticCodes.ValidationExtMap, scope, attribute, mediaIndex, occurrenceIndex, "The extmap encryption marker must not be recursively applied to itself.");
        }
        mappings.Add(mapping);
    }

    internal static void ValidateAllowMixed(SdpSessionDescription description, SdpAttribute attribute, SdpScope scope, int mediaIndex, int occurrenceIndex, bool rtpApplicable, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
        if (!decoded.Success)
        {
            AddExtMapDecodeIssue(decoded, attribute, scope, mediaIndex, occurrenceIndex, result, limiter);
        }
        if (occurrenceIndex > 0)
        {
            AddExtMapIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, scope, attribute, mediaIndex, occurrenceIndex, "The extmap-allow-mixed attribute may appear at most once in one session or media scope.");
        }
        if (!rtpApplicable)
        {
            AddExtMapIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, scope, attribute, mediaIndex, occurrenceIndex, "The extmap-allow-mixed attribute requires an RTP media stream in its applicable scope.");
        }
        if (decoded.Success && result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Answerer && result.AppliedOptions.OfferDescription != null && !OfferAllowsMixed(description, scope, mediaIndex, result.AppliedOptions.OfferDescription))
        {
            AddExtMapIssue(result, limiter, SdpDiagnosticCodes.ValidationExtMap, scope, attribute, mediaIndex, occurrenceIndex, "An SDP answer must not enable mixed one-byte and two-byte header extensions where the offer did not enable them.");
        }
    }

    internal static void AddMixedScopeIssues(List<SdpAttribute> attributes, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.ExtMap))
            {
                int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(attributes, index);
                AddExtMapIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpScope.Media, attribute, mediaIndex, occurrenceIndex, "Extmap declarations must be either all session level or all media level within one SDP description.");
            }
            index = index + 1;
        }
    }

    internal static bool DirectionIsCompatible(SdpSessionDescription description, SdpScope scope, int mediaIndex, bool secureOnly, SdpDirection extensionDirection)
    {
        if (scope == SdpScope.Media)
        {
            if (mediaIndex < 0 || mediaIndex >= description.MediaDescriptions.Count)
            {
                return false;
            }
            return MediaDirectionIsCompatible(description, description.MediaDescriptions[mediaIndex], secureOnly, extensionDirection);
        }

        int index = 0;
        while (index < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[index];
            if (SdpRtpValidationCore.IsRtpProtocol(media.Protocol) && (!secureOnly || IsSecureRtpProtocol(media.Protocol)) && !MediaDirectionIsCompatible(description, media, secureOnly, extensionDirection))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool MediaDirectionIsCompatible(SdpSessionDescription description, SdpMediaDescription media, bool secureOnly, SdpDirection extensionDirection)
    {
        if (!SdpRtpValidationCore.IsRtpProtocol(media.Protocol) || secureOnly && !IsSecureRtpProtocol(media.Protocol))
        {
            return true;
        }
        SdpDirection mediaDirection = ReadEffectiveMediaDirection(description, media);
        if (mediaDirection == SdpDirection.SendOnly)
        {
            return extensionDirection == SdpDirection.SendOnly || extensionDirection == SdpDirection.Inactive;
        }
        if (mediaDirection == SdpDirection.ReceiveOnly)
        {
            return extensionDirection == SdpDirection.ReceiveOnly || extensionDirection == SdpDirection.Inactive;
        }
        return true;
    }

    internal static SdpDirection ReadEffectiveMediaDirection(SdpSessionDescription description, SdpMediaDescription media)
    {
        bool activeBundleOnly = false;
        if (media.Port == 0 && SdpBundleServiceCore.IsBundleOnlyMedia(media))
        {
            int mediaIndex = description.MediaDescriptions.IndexOf(media);
            activeBundleOnly = mediaIndex >= 0 && SdpBundleServiceCore.FindBundleGroupForMedia(description, mediaIndex) != null;
        }
        if (media.Port == 0 && !activeBundleOnly)
        {
            return SdpDirection.Inactive;
        }
        SdpDirection direction = ReadDirection(media.Attributes);
        if (direction != SdpDirection.Unknown)
        {
            return direction;
        }
        direction = ReadDirection(description.Attributes);
        if (direction != SdpDirection.Unknown)
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

    internal static bool OfferAllowsMixed(SdpSessionDescription answer, SdpScope scope, int mediaIndex, SdpSessionDescription offer)
    {
        if (ContainsValidAllowMixed(offer.Attributes))
        {
            return true;
        }
        if (scope == SdpScope.Media)
        {
            if (mediaIndex < 0 || mediaIndex >= offer.MediaDescriptions.Count)
            {
                return false;
            }
            return ContainsValidAllowMixed(offer.MediaDescriptions[mediaIndex].Attributes);
        }

        int answerMediaIndex = 0;
        bool foundRtpMedia = false;
        while (answerMediaIndex < answer.MediaDescriptions.Count)
        {
            if (SdpRtpValidationCore.IsRtpProtocol(answer.MediaDescriptions[answerMediaIndex].Protocol))
            {
                foundRtpMedia = true;
                if (answerMediaIndex >= offer.MediaDescriptions.Count || !ContainsValidAllowMixed(offer.MediaDescriptions[answerMediaIndex].Attributes))
                {
                    return false;
                }
            }
            answerMediaIndex = answerMediaIndex + 1;
        }
        return foundRtpMedia;
    }

    internal static bool ContainsValidAllowMixed(List<SdpAttribute> attributes)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.ExtMapAllowMixed))
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

    internal static SdpExtMapAttribute? FindIdentifier(List<SdpExtMapAttribute> mappings, int identifier)
    {
        int index = 0;
        while (index < mappings.Count)
        {
            if (mappings[index].Id == identifier)
            {
                return mappings[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static SdpExtMapAttribute? FindExtension(List<SdpExtMapAttribute> mappings, SdpExtMapAttribute mapping)
    {
        int index = 0;
        while (index < mappings.Count)
        {
            SdpExtMapAttribute candidate = mappings[index];
            if (candidate.Encrypted == mapping.Encrypted && candidate.Uri == mapping.Uri && candidate.ExtensionAttributes == mapping.ExtensionAttributes)
            {
                return candidate;
            }
            index = index + 1;
        }
        return null;
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

    internal static bool HasRtpMedia(SdpSessionDescription description)
    {
        int index = 0;
        while (index < description.MediaDescriptions.Count)
        {
            if (SdpRtpValidationCore.IsRtpProtocol(description.MediaDescriptions[index].Protocol))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool HasSecureRtpMedia(SdpSessionDescription description)
    {
        int index = 0;
        while (index < description.MediaDescriptions.Count)
        {
            if (IsSecureRtpProtocol(description.MediaDescriptions[index].Protocol))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool IsSecureRtpProtocol(string protocol)
    {
        return protocol.IndexOf("RTP/SAVP", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    internal static void AddExtMapDecodeIssue(SdpAttributeDecodeResult decoded, SdpAttribute attribute, SdpScope scope, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        string detail = "The extmap-related attribute does not satisfy its registered syntax.";
        if (decoded.Diagnostics.Count > 0)
        {
            detail = decoded.Diagnostics[0].Detail;
        }
        AddExtMapIssue(result, limiter, SdpDiagnosticCodes.ValidationExtMap, scope, attribute, mediaIndex, occurrenceIndex, detail);
    }

    internal static void AddExtMapIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, string code, SdpScope scope, SdpAttribute attribute, int mediaIndex, int occurrenceIndex, string detail)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, code, SdpValidationSeverity.Error, scope, attribute.Name, mediaIndex, occurrenceIndex, detail, attribute.Location);
    }
}
