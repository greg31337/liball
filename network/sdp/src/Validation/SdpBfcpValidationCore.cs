using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpBfcpValidationCore
{
    internal static void ValidateBfcpDescription(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        ValidateSessionScope(description, result, limiter);
        if (result.AppliedOptions.Profile == SdpValidationProfile.Bfcp)
        {
            ValidateSessionConnectionAttributes(description, result, limiter);
        }
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            ValidateMedia(description, description.MediaDescriptions[mediaIndex], mediaIndex, result, limiter);
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void ValidateSessionScope(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < description.Attributes.Count)
        {
            SdpAttribute attribute = description.Attributes[index];
            if (IsBfcpAttributeName(attribute.Name))
            {
                int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(description.Attributes, index);
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpValidationSeverity.Error, SdpScope.Session, attribute.Name, -1, occurrenceIndex, "BFCP attributes are permitted only at media scope.", attribute.Location);
            }
            index = index + 1;
        }
    }

    internal static void ValidateSessionConnectionAttributes(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool setupApplicable = false;
        bool connectionApplicable = false;
        bool bfcpPresent = ContainsBfcpAttribute(description.Attributes);
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            string protocol = media.Protocol;
            bfcpPresent = bfcpPresent || IsBfcpProtocolShape(protocol) || ContainsBfcpAttribute(media.Attributes);
            setupApplicable = setupApplicable || UsesSetupAttribute(protocol);
            connectionApplicable = connectionApplicable || UsesConnectionAttribute(protocol);
            mediaIndex = mediaIndex + 1;
        }
        if (!bfcpPresent)
        {
            return;
        }
        ValidateConnectionAttributeList(description.Attributes, SdpScope.Session, -1, setupApplicable, connectionApplicable, result, limiter);
    }

    internal static void ValidateMedia(SdpSessionDescription description, SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool currentTransport = IsCurrentBfcpTransport(media.Protocol);
        bool bfcpLike = currentTransport || IsBfcpProtocolShape(media.Protocol) || ContainsBfcpAttribute(media.Attributes);
        ValidateMediaAttributes(media, mediaIndex, currentTransport, result, limiter);
        if (result.AppliedOptions.Profile == SdpValidationProfile.Bfcp && bfcpLike)
        {
            ValidateConnectionAttributeList(media.Attributes, SdpScope.Media, mediaIndex, UsesSetupAttribute(media.Protocol), UsesConnectionAttribute(media.Protocol), result, limiter);
        }
        if (!bfcpLike)
        {
            return;
        }
        if (!currentTransport)
        {
            AddMediaIssue(result, limiter, media, mediaIndex, "A BFCP media description must use a current TCP or UDP BFCP transport protocol.");
            return;
        }

        ValidateMediaLine(media, mediaIndex, result, limiter);
        ValidateRequiredAttributes(media, mediaIndex, result, limiter);
        ValidateServerAttributes(media, mediaIndex, result, limiter);
        ValidateFloorReferences(description, media, mediaIndex, result, limiter);
        ValidateOfferAnswer(description, media, mediaIndex, result, limiter);
    }

    internal static void ValidateMediaLine(SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!string.Equals(media.MediaType, SdpMediaTypes.Application, StringComparison.OrdinalIgnoreCase))
        {
            AddMediaIssue(result, limiter, media, mediaIndex, "A BFCP transport requires an application media description.");
        }
        if (media.Formats.Count != 1 || media.Formats[0] != "*")
        {
            AddMediaIssue(result, limiter, media, mediaIndex, "A BFCP media description must contain exactly one asterisk format token.");
        }
    }

    internal static void ValidateMediaAttributes(SdpMediaDescription media, int mediaIndex, bool currentTransport, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (IsBfcpAttributeName(attribute.Name))
            {
                int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, index);
                if (!currentTransport)
                {
                    AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, attribute, mediaIndex, occurrenceIndex, "The BFCP attribute is not applicable to this media transport.");
                }
                if (IsSingletonBfcpAttributeName(attribute.Name) && occurrenceIndex > 0)
                {
                    AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, attribute, mediaIndex, occurrenceIndex, "This BFCP attribute may appear at most once in a media description.");
                }
                ValidateBfcpAttributeValue(attribute, media.Attributes, index, mediaIndex, occurrenceIndex, result, limiter);
            }
            index = index + 1;
        }
    }

    internal static void ValidateBfcpAttributeValue(SdpAttribute attribute, List<SdpAttribute> attributes, int attributeIndex, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, attribute, mediaIndex, occurrenceIndex, "A BFCP attribute requires a colon-delimited value.");
            return;
        }
        string value = SdpBfcpAttributeCodecCore.TrimHorizontalWhitespace(attribute.Value);
        if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.FloorControl))
        {
            ValidateFloorControlValue(value, attribute, mediaIndex, occurrenceIndex, result, limiter);
        }
        else if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.ConferenceId))
        {
            ValidateDecimalValue(value, 4294967295UL, "The BFCP conference identifier must be an unsigned 32-bit decimal integer.", attribute, mediaIndex, occurrenceIndex, result, limiter);
        }
        else if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.UserId))
        {
            ValidateDecimalValue(value, 65535UL, "The BFCP user identifier must be an unsigned 16-bit decimal integer.", attribute, mediaIndex, occurrenceIndex, result, limiter);
        }
        else if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.FloorId))
        {
            ValidateFloorIdValue(value, attributes, attributeIndex, attribute, mediaIndex, occurrenceIndex, result, limiter);
        }
        else if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.BfcpVersion))
        {
            ValidateVersionValue(value, attribute, mediaIndex, occurrenceIndex, result, limiter);
        }
    }

    internal static void ValidateFloorControlValue(string value, SdpAttribute attribute, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<string> roles = SdpBfcpAttributeCodecCore.ReadTokens(value);
        if (roles.Count == 0 || !SdpBfcpAttributeCodecCore.AreFloorControlRoles(roles))
        {
            AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, attribute, mediaIndex, occurrenceIndex, "The floorctrl attribute must contain c-only, s-only, or the accepted legacy c-s role.");
            return;
        }
        int index = 0;
        while (index < roles.Count)
        {
            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (string.Equals(roles[index], roles[earlierIndex], StringComparison.OrdinalIgnoreCase))
                {
                    AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, attribute, mediaIndex, occurrenceIndex, "A floorctrl role must not be repeated within one attribute.");
                    return;
                }
                earlierIndex = earlierIndex + 1;
            }
            index = index + 1;
        }
    }

    internal static void ValidateDecimalValue(string value, ulong maximum, string detail, SdpAttribute attribute, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!IsBoundedDecimal(value, maximum))
        {
            AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, attribute, mediaIndex, occurrenceIndex, detail);
        }
    }

    internal static void ValidateFloorIdValue(string value, List<SdpAttribute> attributes, int attributeIndex, SdpAttribute attribute, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<string> tokens = SdpBfcpAttributeCodecCore.ReadTokens(value);
        if (!HasValidFloorIdSyntax(tokens) || !IsBoundedDecimal(tokens[0], 65535UL))
        {
            AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, attribute, mediaIndex, occurrenceIndex, "The floorid attribute must contain an unsigned 16-bit identifier and one or more mstrm label pointers.");
            return;
        }
        ulong floorId = ParseBoundedDecimal(tokens[0]);
        int index = 0;
        while (index < attributeIndex)
        {
            SdpAttribute earlier = attributes[index];
            if (SdpRtpValidationCore.NameEquals(earlier.Name, SdpAttributeNames.FloorId) && TryReadFloorId(earlier, floorId))
            {
                AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, attribute, mediaIndex, occurrenceIndex, "A BFCP floor identifier must be unique within its media description.");
                return;
            }
            index = index + 1;
        }
    }

    internal static void ValidateVersionValue(string value, SdpAttribute attribute, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<string> versions = SdpBfcpAttributeCodecCore.ReadTokens(value);
        if (versions.Count == 0)
        {
            AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, attribute, mediaIndex, occurrenceIndex, "The bfcpver attribute must contain one or more supported BFCP versions.");
            return;
        }
        int index = 0;
        while (index < versions.Count)
        {
            if (!IsBoundedDecimal(versions[index], 2UL) || ParseBoundedDecimal(versions[index]) == 0UL)
            {
                AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, attribute, mediaIndex, occurrenceIndex, "The bfcpver attribute may contain only the defined BFCP versions 1 and 2.");
                return;
            }
            ulong version = ParseBoundedDecimal(versions[index]);
            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (ParseBoundedDecimal(versions[earlierIndex]) == version)
                {
                    AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, attribute, mediaIndex, occurrenceIndex, "A BFCP version must not be repeated within one attribute.");
                    return;
                }
                earlierIndex = earlierIndex + 1;
            }
            index = index + 1;
        }
    }

    internal static void ValidateRequiredAttributes(SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (media.Port == 0)
        {
            return;
        }
        bool requireFloorControl = result.AppliedOptions.NegotiationRole != SdpNegotiationRole.Answerer;
        bool requireVersion = true;
        if (result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Answerer && result.AppliedOptions.OfferDescription != null)
        {
            SdpSessionDescription offer = result.AppliedOptions.OfferDescription;
            if (mediaIndex < offer.MediaDescriptions.Count)
            {
                requireFloorControl = ContainsNamedAttribute(offer.MediaDescriptions[mediaIndex].Attributes, SdpAttributeNames.FloorControl);
            }
        }
        if (requireFloorControl && !ContainsNamedAttribute(media.Attributes, SdpAttributeNames.FloorControl))
        {
            AddMissingAttributeIssue(result, limiter, media, mediaIndex, SdpAttributeNames.FloorControl, "An active BFCP offer, or an answer to an offer with floorctrl, requires a floorctrl attribute.");
        }
        if (requireVersion && !ContainsNamedAttribute(media.Attributes, SdpAttributeNames.BfcpVersion))
        {
            AddMissingAttributeIssue(result, limiter, media, mediaIndex, SdpAttributeNames.BfcpVersion, "An active BFCP media description requires a bfcpver attribute.");
        }
    }

    internal static void ValidateServerAttributes(SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (media.Port == 0)
        {
            return;
        }
        List<string> roles = ReadValidFloorRoles(media);
        bool defaultAnswerServer = result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Answerer && roles.Count == 0 && !ContainsNamedAttribute(media.Attributes, SdpAttributeNames.FloorControl);
        if (!defaultAnswerServer && !EndpointOffersServerRole(roles, result.AppliedOptions.NegotiationRole))
        {
            return;
        }
        if (!ContainsNamedAttribute(media.Attributes, SdpAttributeNames.ConferenceId))
        {
            AddMissingAttributeIssue(result, limiter, media, mediaIndex, SdpAttributeNames.ConferenceId, "A BFCP endpoint offering or selecting the server role requires a conference identifier.");
        }
        if (!ContainsNamedAttribute(media.Attributes, SdpAttributeNames.UserId))
        {
            AddMissingAttributeIssue(result, limiter, media, mediaIndex, SdpAttributeNames.UserId, "A BFCP endpoint offering or selecting the server role requires a user identifier.");
        }
        if (!ContainsNamedAttribute(media.Attributes, SdpAttributeNames.FloorId))
        {
            AddMissingAttributeIssue(result, limiter, media, mediaIndex, SdpAttributeNames.FloorId, "A BFCP endpoint offering or selecting the server role requires at least one floor identifier.");
        }
    }

    internal static bool EndpointOffersServerRole(List<string> roles, SdpNegotiationRole negotiationRole)
    {
        if (roles.Count == 0)
        {
            return false;
        }
        if (negotiationRole == SdpNegotiationRole.Answerer)
        {
            return roles.Count == 1 && string.Equals(roles[0], "s-only", StringComparison.OrdinalIgnoreCase);
        }
        return ContainsRole(roles, "s-only") || ContainsRole(roles, "c-s");
    }

    internal static void ValidateFloorReferences(SdpSessionDescription description, SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.FloorId) && attribute.Kind == SdpAttributeKind.Value)
            {
                List<string> tokens = SdpBfcpAttributeCodecCore.ReadTokens(SdpBfcpAttributeCodecCore.TrimHorizontalWhitespace(attribute.Value));
                if (HasValidFloorIdSyntax(tokens) && IsBoundedDecimal(tokens[0], 65535UL))
                {
                    int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, index);
                    ValidateFloorPointerList(description, tokens, attribute, mediaIndex, occurrenceIndex, result, limiter);
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateFloorPointerList(SdpSessionDescription description, List<string> tokens, SdpAttribute attribute, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int pointerIndex = 1;
        while (pointerIndex < tokens.Count)
        {
            string pointer = tokens[pointerIndex];
            if (pointerIndex == 1)
            {
                pointer = ReadFirstFloorPointer(pointer);
            }
            int referenceCount = CountMediaLabels(description, pointer);
            if (referenceCount == 0)
            {
                AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, attribute, mediaIndex, occurrenceIndex, "A floorid mstrm pointer must reference an existing media-level label attribute.");
            }
            else if (referenceCount > 1)
            {
                AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, attribute, mediaIndex, occurrenceIndex, "A floorid mstrm pointer must resolve to exactly one media description.");
            }
            pointerIndex = pointerIndex + 1;
        }
    }

    internal static int CountMediaLabels(SdpSessionDescription description, string pointer)
    {
        int count = 0;
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            List<SdpAttribute> attributes = description.MediaDescriptions[mediaIndex].Attributes;
            int attributeIndex = 0;
            while (attributeIndex < attributes.Count)
            {
                SdpAttribute attribute = attributes[attributeIndex];
                if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.Label) && attribute.Kind == SdpAttributeKind.Value)
                {
                    string value = SdpBfcpAttributeCodecCore.TrimHorizontalWhitespace(attribute.Value);
                    if (SdpCommonAttributeCodecCore.IsTokenValue(value) && value == pointer)
                    {
                        count = count + 1;
                    }
                }
                attributeIndex = attributeIndex + 1;
            }
            mediaIndex = mediaIndex + 1;
        }
        return count;
    }

    internal static void ValidateOfferAnswer(SdpSessionDescription description, SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (media.Port != 0)
        {
            ValidateFloorRoleShape(media, mediaIndex, result, limiter);
            ValidateStandaloneSetupShape(description, media, mediaIndex, result, limiter);
        }
        if (result.AppliedOptions.NegotiationRole != SdpNegotiationRole.Answerer || result.AppliedOptions.OfferDescription == null)
        {
            return;
        }
        SdpSessionDescription offer = result.AppliedOptions.OfferDescription;
        if (mediaIndex >= offer.MediaDescriptions.Count)
        {
            AddMediaIssue(result, limiter, media, mediaIndex, "A BFCP answer media description has no corresponding offered media description.");
            return;
        }
        SdpMediaDescription offeredMedia = offer.MediaDescriptions[mediaIndex];
        if (!string.Equals(media.Protocol, offeredMedia.Protocol, StringComparison.OrdinalIgnoreCase))
        {
            AddMediaIssue(result, limiter, media, mediaIndex, "A BFCP answer must retain the transport protocol from the corresponding offer.");
        }
        if (media.Port == 0)
        {
            return;
        }
        ValidateAnswerVersions(offeredMedia, media, mediaIndex, result, limiter);
        ValidateAnswerFloorRoles(offeredMedia, media, mediaIndex, result, limiter);
        ValidateAnswerSetupRoles(offer, offeredMedia, description, media, mediaIndex, result, limiter);
        ValidateAnswerConnectionReuse(offer, offeredMedia, description, media, mediaIndex, result, limiter);
    }

    internal static void ValidateFloorRoleShape(SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpAttribute? attribute = ReadFirstNamedAttribute(media.Attributes, SdpAttributeNames.FloorControl);
        if (attribute == null || attribute.Kind != SdpAttributeKind.Value)
        {
            return;
        }
        List<string> roles = SdpBfcpAttributeCodecCore.ReadTokens(SdpBfcpAttributeCodecCore.TrimHorizontalWhitespace(attribute.Value));
        if (!SdpBfcpAttributeCodecCore.AreFloorControlRoles(roles))
        {
            return;
        }
        SdpNegotiationRole role = result.AppliedOptions.NegotiationRole;
        if ((role == SdpNegotiationRole.Offerer || role == SdpNegotiationRole.Unknown) && !IsValidOfferRoleSet(roles))
        {
            AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, attribute, mediaIndex, 0, "A BFCP offer must contain c-only, s-only, both roles, or the accepted legacy c-s role.");
        }
        else if (role == SdpNegotiationRole.Answerer && !IsValidAnswerRoleSet(roles))
        {
            AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, attribute, mediaIndex, 0, "A BFCP answer must select exactly one c-only or s-only role.");
        }
    }

    internal static void ValidateAnswerVersions(SdpMediaDescription offer, SdpMediaDescription answer, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<string> offeredVersions = ReadEffectiveVersions(offer);
        List<string> answeredVersions = ReadExplicitVersions(answer);
        if (answeredVersions.Count == 0)
        {
            return;
        }
        int index = 0;
        while (index < answeredVersions.Count)
        {
            if (!ContainsExactValue(offeredVersions, answeredVersions[index]))
            {
                SdpAttribute? attribute = ReadFirstNamedAttribute(answer.Attributes, SdpAttributeNames.BfcpVersion);
                AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, attribute, mediaIndex, 0, "A BFCP answer may contain only versions offered by the corresponding media description.");
                return;
            }
            index = index + 1;
        }
    }

    internal static void ValidateAnswerFloorRoles(SdpMediaDescription offer, SdpMediaDescription answer, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<string> offeredRoles = ReadValidFloorRoles(offer);
        List<string> answeredRoles = ReadValidFloorRoles(answer);
        if (answeredRoles.Count == 0)
        {
            return;
        }
        if (offeredRoles.Count == 0)
        {
            offeredRoles.Add("c-only");
        }
        if (!IsValidAnswerRoleSet(answeredRoles))
        {
            return;
        }
        string answerRole = answeredRoles[0];
        bool compatible = string.Equals(answerRole, "s-only", StringComparison.OrdinalIgnoreCase) && (ContainsRole(offeredRoles, "c-only") || ContainsRole(offeredRoles, "c-s"))
            || string.Equals(answerRole, "c-only", StringComparison.OrdinalIgnoreCase) && (ContainsRole(offeredRoles, "s-only") || ContainsRole(offeredRoles, "c-s"));
        if (!compatible)
        {
            SdpAttribute? attribute = ReadFirstNamedAttribute(answer.Attributes, SdpAttributeNames.FloorControl);
            AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, attribute, mediaIndex, 0, "The BFCP answer floor-control role is not compatible with the corresponding offer.");
        }
    }

    internal static void ValidateStandaloneSetupShape(SdpSessionDescription description, SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (result.AppliedOptions.Profile != SdpValidationProfile.Bfcp || !RequiresDtlsSetup(media.Protocol))
        {
            return;
        }
        SdpAttribute? attribute = ReadEffectiveNamedAttribute(description.Attributes, media.Attributes, SdpAttributeNames.Setup);
        if (attribute == null)
        {
            AddMissingAttributeIssue(result, limiter, media, mediaIndex, SdpAttributeNames.Setup, "A BFCP-over-DTLS media description requires an effective setup attribute.");
            return;
        }
        SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
        if (decoded.SetupAttribute == null)
        {
            return;
        }
        if (result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Offerer && decoded.SetupAttribute.Role != SdpDtlsSetupRole.ActPass)
        {
            AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, attribute, mediaIndex, 0, "A BFCP-over-DTLS offer must use the actpass setup role.");
        }
        else if (result.AppliedOptions.NegotiationRole == SdpNegotiationRole.Answerer && decoded.SetupAttribute.Role != SdpDtlsSetupRole.Active && decoded.SetupAttribute.Role != SdpDtlsSetupRole.Passive)
        {
            AddAttributeIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, attribute, mediaIndex, 0, "A BFCP-over-DTLS answer must use an active or passive setup role.");
        }
    }

    internal static void ValidateAnswerSetupRoles(SdpSessionDescription offer, SdpMediaDescription offeredMedia, SdpSessionDescription answer, SdpMediaDescription answeredMedia, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!UsesSetupAttribute(offeredMedia.Protocol) && !UsesSetupAttribute(answeredMedia.Protocol))
        {
            return;
        }
        if (result.AppliedOptions.Profile == SdpValidationProfile.CombinedStrict && (RequiresDtlsSetup(offeredMedia.Protocol) || RequiresDtlsSetup(answeredMedia.Protocol)))
        {
            return;
        }
        SdpDtlsSetupRole offeredRole = ReadEffectiveSetupRole(offer.Attributes, offeredMedia.Attributes, SdpDtlsSetupRole.Active);
        SdpDtlsSetupRole answeredRole = ReadEffectiveSetupRole(answer.Attributes, answeredMedia.Attributes, SdpDtlsSetupRole.Passive);
        if (offeredRole == SdpDtlsSetupRole.Unknown || answeredRole == SdpDtlsSetupRole.Unknown)
        {
            return;
        }
        bool compatible = offeredRole == SdpDtlsSetupRole.Active && (answeredRole == SdpDtlsSetupRole.Passive || answeredRole == SdpDtlsSetupRole.HoldConnection)
            || offeredRole == SdpDtlsSetupRole.Passive && (answeredRole == SdpDtlsSetupRole.Active || answeredRole == SdpDtlsSetupRole.HoldConnection)
            || offeredRole == SdpDtlsSetupRole.ActPass && (answeredRole == SdpDtlsSetupRole.Active || answeredRole == SdpDtlsSetupRole.Passive || answeredRole == SdpDtlsSetupRole.HoldConnection)
            || offeredRole == SdpDtlsSetupRole.HoldConnection && answeredRole == SdpDtlsSetupRole.HoldConnection;
        if (!compatible)
        {
            SdpAttribute? attribute = ReadEffectiveNamedAttribute(answer.Attributes, answeredMedia.Attributes, SdpAttributeNames.Setup);
            SdpSourceLocation location = answeredMedia.Location;
            if (attribute != null)
            {
                location = attribute.Location;
            }
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, SdpValidationSeverity.Error, SdpScope.Media, SdpAttributeNames.Setup, mediaIndex, attribute == null ? -1 : 0, "The BFCP answer setup role is not compatible with the corresponding offer.", location);
        }
    }

    internal static void ValidateAnswerConnectionReuse(SdpSessionDescription offer, SdpMediaDescription offeredMedia, SdpSessionDescription answer, SdpMediaDescription answeredMedia, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!UsesConnectionAttribute(offeredMedia.Protocol) && !UsesConnectionAttribute(answeredMedia.Protocol))
        {
            return;
        }
        string offeredConnection = ReadEffectiveConnectionRole(offer.Attributes, offeredMedia.Attributes, "new");
        string answeredConnection = ReadEffectiveConnectionRole(answer.Attributes, answeredMedia.Attributes, "new");
        if (offeredConnection.Length == 0 || answeredConnection.Length == 0)
        {
            return;
        }
        if (string.Equals(offeredConnection, "new", StringComparison.OrdinalIgnoreCase) && !string.Equals(answeredConnection, "new", StringComparison.OrdinalIgnoreCase))
        {
            SdpAttribute? attribute = ReadEffectiveNamedAttribute(answer.Attributes, answeredMedia.Attributes, SdpAttributeNames.Connection);
            SdpSourceLocation location = answeredMedia.Location;
            if (attribute != null)
            {
                location = attribute.Location;
            }
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, SdpValidationSeverity.Error, SdpScope.Media, SdpAttributeNames.Connection, mediaIndex, attribute == null ? -1 : 0, "A BFCP answer to connection:new must also select connection:new.", location);
        }
    }

    internal static void ValidateConnectionAttributeList(List<SdpAttribute> attributes, SdpScope scope, int mediaIndex, bool setupApplicable, bool connectionApplicable, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            bool setup = SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.Setup);
            bool connection = SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.Connection);
            if (setup || connection)
            {
                int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(attributes, index);
                if (occurrenceIndex > 0)
                {
                    SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, SdpValidationSeverity.Error, scope, attribute.Name, mediaIndex, occurrenceIndex, "The BFCP setup or connection attribute may appear at most once in this scope.", attribute.Location);
                }
                bool applicable = setup && setupApplicable || connection && connectionApplicable;
                if (!applicable)
                {
                    SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpValidationSeverity.Error, scope, attribute.Name, mediaIndex, occurrenceIndex, setup ? "The setup attribute is not applicable to this BFCP transport." : "The connection attribute is applicable only to TCP-based BFCP transports.", attribute.Location);
                }
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success || decoded.SetupAttribute != null && decoded.SetupAttribute.Role == SdpDtlsSetupRole.Unknown || decoded.ConnectionAttribute != null && !IsKnownConnectionRole(decoded.ConnectionAttribute.ConnectionRole))
                {
                    SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, SdpValidationSeverity.Error, scope, attribute.Name, mediaIndex, occurrenceIndex, "The BFCP setup or connection attribute must use a registered role value.", attribute.Location);
                }
            }
            index = index + 1;
        }
    }

    internal static bool HasValidFloorIdSyntax(List<string> tokens)
    {
        if (tokens.Count < 2 || !SdpBfcpAttributeCodecCore.IsDecimal(tokens[0]))
        {
            return false;
        }
        string firstPointer = ReadFirstFloorPointer(tokens[1]);
        if (firstPointer.Length == 0 || !SdpCommonAttributeCodecCore.IsTokenValue(firstPointer))
        {
            return false;
        }
        int index = 2;
        while (index < tokens.Count)
        {
            if (!SdpCommonAttributeCodecCore.IsTokenValue(tokens[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static string ReadFirstFloorPointer(string token)
    {
        if (token.StartsWith("mstrm:", StringComparison.OrdinalIgnoreCase) && token.Length > 6)
        {
            return token.Substring(6);
        }
        if (token.StartsWith("m-stream:", StringComparison.OrdinalIgnoreCase) && token.Length > 9)
        {
            return token.Substring(9);
        }
        return string.Empty;
    }

    internal static bool TryReadFloorId(SdpAttribute attribute, ulong expected)
    {
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return false;
        }
        List<string> tokens = SdpBfcpAttributeCodecCore.ReadTokens(SdpBfcpAttributeCodecCore.TrimHorizontalWhitespace(attribute.Value));
        return HasValidFloorIdSyntax(tokens) && IsBoundedDecimal(tokens[0], 65535UL) && ParseBoundedDecimal(tokens[0]) == expected;
    }

    internal static bool IsBoundedDecimal(string value, ulong maximum)
    {
        if (!SdpBfcpAttributeCodecCore.IsDecimal(value))
        {
            return false;
        }
        SdpNumberParseResult parsed = SdpNumberParser.ParseCharacterUnsignedDecimal(value.AsSpan());
        return parsed.Success && parsed.UnsignedValue <= maximum;
    }

    internal static ulong ParseBoundedDecimal(string value)
    {
        SdpNumberParseResult parsed = SdpNumberParser.ParseCharacterUnsignedDecimal(value.AsSpan());
        return parsed.UnsignedValue;
    }

    internal static List<string> ReadValidFloorRoles(SdpMediaDescription media)
    {
        List<string> result = new List<string>();
        SdpAttribute? attribute = ReadFirstNamedAttribute(media.Attributes, SdpAttributeNames.FloorControl);
        if (attribute == null || attribute.Kind != SdpAttributeKind.Value)
        {
            return result;
        }
        List<string> roles = SdpBfcpAttributeCodecCore.ReadTokens(SdpBfcpAttributeCodecCore.TrimHorizontalWhitespace(attribute.Value));
        if (!SdpBfcpAttributeCodecCore.AreFloorControlRoles(roles))
        {
            return result;
        }
        int index = 0;
        while (index < roles.Count)
        {
            if (ContainsRole(result, roles[index]))
            {
                return new List<string>();
            }
            result.Add(roles[index]);
            index = index + 1;
        }
        return result;
    }

    internal static List<string> ReadEffectiveVersions(SdpMediaDescription media)
    {
        List<string> result = ReadExplicitVersions(media);
        if (result.Count == 0)
        {
            result.Add(SdpBfcpAttributeCodecCore.IsReliableBfcpTransport(media.Protocol) ? "1" : "2");
        }
        return result;
    }

    internal static List<string> ReadExplicitVersions(SdpMediaDescription media)
    {
        List<string> result = new List<string>();
        SdpAttribute? attribute = ReadFirstNamedAttribute(media.Attributes, SdpAttributeNames.BfcpVersion);
        if (attribute == null || attribute.Kind != SdpAttributeKind.Value)
        {
            return result;
        }
        List<string> values = SdpBfcpAttributeCodecCore.ReadTokens(SdpBfcpAttributeCodecCore.TrimHorizontalWhitespace(attribute.Value));
        int index = 0;
        while (index < values.Count)
        {
            if (!IsBoundedDecimal(values[index], 2UL) || ParseBoundedDecimal(values[index]) == 0UL)
            {
                return new List<string>();
            }
            result.Add(ParseBoundedDecimal(values[index]) == 1UL ? "1" : "2");
            index = index + 1;
        }
        return result;
    }

    internal static SdpDtlsSetupRole ReadEffectiveSetupRole(List<SdpAttribute> sessionAttributes, List<SdpAttribute> mediaAttributes, SdpDtlsSetupRole defaultRole)
    {
        SdpAttribute? attribute = ReadEffectiveNamedAttribute(sessionAttributes, mediaAttributes, SdpAttributeNames.Setup);
        if (attribute == null)
        {
            return defaultRole;
        }
        SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
        if (decoded.SetupAttribute == null)
        {
            return SdpDtlsSetupRole.Unknown;
        }
        return decoded.SetupAttribute.Role;
    }

    internal static string ReadEffectiveConnectionRole(List<SdpAttribute> sessionAttributes, List<SdpAttribute> mediaAttributes, string defaultRole)
    {
        SdpAttribute? attribute = ReadEffectiveNamedAttribute(sessionAttributes, mediaAttributes, SdpAttributeNames.Connection);
        if (attribute == null)
        {
            return defaultRole;
        }
        SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
        if (decoded.ConnectionAttribute == null || !IsKnownConnectionRole(decoded.ConnectionAttribute.ConnectionRole))
        {
            return string.Empty;
        }
        return decoded.ConnectionAttribute.ConnectionRole;
    }

    internal static SdpAttribute? ReadEffectiveNamedAttribute(List<SdpAttribute> sessionAttributes, List<SdpAttribute> mediaAttributes, string name)
    {
        if (ContainsNamedAttribute(mediaAttributes, name))
        {
            return ReadFirstNamedAttribute(mediaAttributes, name);
        }
        return ReadFirstNamedAttribute(sessionAttributes, name);
    }

    internal static SdpAttribute? ReadFirstNamedAttribute(List<SdpAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (SdpRtpValidationCore.NameEquals(attributes[index].Name, name))
            {
                return attributes[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static bool ContainsNamedAttribute(List<SdpAttribute> attributes, string name)
    {
        return ReadFirstNamedAttribute(attributes, name) != null;
    }

    internal static bool ContainsBfcpAttribute(List<SdpAttribute> attributes)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (IsBfcpAttributeName(attributes[index].Name))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool ContainsRole(List<string> roles, string expected)
    {
        int index = 0;
        while (index < roles.Count)
        {
            if (string.Equals(roles[index], expected, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool ContainsExactValue(List<string> values, string expected)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == expected)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool IsValidOfferRoleSet(List<string> roles)
    {
        if (roles.Count == 1)
        {
            return ContainsRole(roles, "c-only") || ContainsRole(roles, "s-only") || ContainsRole(roles, "c-s");
        }
        return roles.Count == 2 && ContainsRole(roles, "c-only") && ContainsRole(roles, "s-only");
    }

    internal static bool IsValidAnswerRoleSet(List<string> roles)
    {
        return roles.Count == 1 && (ContainsRole(roles, "c-only") || ContainsRole(roles, "s-only"));
    }

    internal static bool IsKnownConnectionRole(string role)
    {
        return string.Equals(role, "new", StringComparison.OrdinalIgnoreCase) || string.Equals(role, "existing", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsBfcpAttributeName(string name)
    {
        return SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.FloorControl)
            || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.ConferenceId)
            || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.UserId)
            || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.FloorId)
            || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.BfcpVersion);
    }

    internal static bool IsSingletonBfcpAttributeName(string name)
    {
        return !SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.FloorId);
    }

    internal static bool IsCurrentBfcpTransport(string protocol)
    {
        return SdpBfcpAttributeCodecCore.IsReliableBfcpTransport(protocol) || SdpBfcpAttributeCodecCore.IsUnreliableBfcpTransport(protocol);
    }

    internal static bool IsBfcpProtocolShape(string protocol)
    {
        return string.Equals(protocol, "BFCP", StringComparison.OrdinalIgnoreCase) || protocol.EndsWith("/BFCP", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool UsesSetupAttribute(string protocol)
    {
        return IsCurrentBfcpTransport(protocol) && !string.Equals(protocol, SdpTransportProtocols.UdpBfcp, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool UsesConnectionAttribute(string protocol)
    {
        return SdpBfcpAttributeCodecCore.IsReliableBfcpTransport(protocol);
    }

    internal static bool RequiresDtlsSetup(string protocol)
    {
        return string.Equals(protocol, SdpTransportProtocols.TcpDtlsBfcp, StringComparison.OrdinalIgnoreCase) || string.Equals(protocol, SdpTransportProtocols.UdpTlsBfcp, StringComparison.OrdinalIgnoreCase);
    }

    internal static void AddMissingAttributeIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, SdpMediaDescription media, int mediaIndex, string attributeName, string detail)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, SdpValidationSeverity.Error, SdpScope.Media, attributeName, mediaIndex, -1, detail, media.Location);
    }

    internal static void AddAttributeIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, string code, SdpAttribute? attribute, int mediaIndex, int occurrenceIndex, string detail)
    {
        if (attribute == null)
        {
            return;
        }
        SdpValidationCore.AddValidationIssue(result, limiter, code, SdpValidationSeverity.Error, SdpScope.Media, attribute.Name, mediaIndex, occurrenceIndex, detail, attribute.Location);
    }

    internal static void AddMediaIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, SdpMediaDescription media, int mediaIndex, string detail)
    {
        SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationBfcp, SdpValidationSeverity.Error, SdpScope.Media, 'm', mediaIndex, 0, detail, media.Location);
    }
}
