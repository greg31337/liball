using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpRidValidationCore
{
    internal static void ValidateRidDescription(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        ValidateSessionRidAttributes(description, result, limiter);
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            ValidateMediaRidAttributes(description.MediaDescriptions[mediaIndex], mediaIndex, result, limiter);
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void ValidateSessionRidAttributes(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < description.Attributes.Count)
        {
            SdpAttribute attribute = description.Attributes[index];
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.Rid))
            {
                int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(description.Attributes, index);
                AddRidIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpScope.Session, attribute, -1, occurrenceIndex, "The rid attribute is permitted only at media scope.");
                ValidateDecodedRid(attribute, SdpScope.Session, -1, occurrenceIndex, result, limiter);
            }
            index = index + 1;
        }
    }

    internal static void ValidateMediaRidAttributes(SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<SdpRidAttribute> rids = new List<SdpRidAttribute>();
        bool rtpProtocol = SdpRtpValidationCore.IsRtpProtocol(media.Protocol);
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (SdpRtpValidationCore.NameEquals(attribute.Name, SdpAttributeNames.Rid))
            {
                int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, index);
                if (!rtpProtocol)
                {
                    AddRidIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpScope.Media, attribute, mediaIndex, occurrenceIndex, "The rid attribute requires an RTP transport protocol.");
                }
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success || decoded.RidAttribute == null)
                {
                    AddDecodeIssue(decoded, attribute, SdpScope.Media, mediaIndex, occurrenceIndex, result, limiter);
                }
                else if (rtpProtocol)
                {
                    rids.Add(decoded.RidAttribute);
                }
            }
            index = index + 1;
        }
        if (rtpProtocol)
        {
            ValidateRids(media, mediaIndex, rids, result, limiter);
        }
    }

    internal static void ValidateDecodedRid(SdpAttribute attribute, SdpScope scope, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
        if (!decoded.Success)
        {
            AddDecodeIssue(decoded, attribute, scope, mediaIndex, occurrenceIndex, result, limiter);
        }
    }

    internal static void ValidateRids(SdpMediaDescription media, int mediaIndex, List<SdpRidAttribute> rids, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<string> identifiers = new List<string>();
        int index = 0;
        while (index < rids.Count)
        {
            SdpRidAttribute rid = rids[index];
            if (identifiers.Contains(rid.Id))
            {
                AddProjectionIssue(media, mediaIndex, rid, SdpDiagnosticCodes.ValidationAttributeCardinality, "A rid identifier must be unique within its media section.", result, limiter);
            }
            else
            {
                identifiers.Add(rid.Id);
            }
            ValidateRidPayloads(media, mediaIndex, rid, result, limiter);
            ValidateRidRestrictions(media, mediaIndex, rid, result, limiter);
            ValidateRidDependencies(media, mediaIndex, rids, rid, result, limiter);
            index = index + 1;
        }
        ValidateDependencyCycles(media, mediaIndex, rids, result, limiter);
    }

    internal static void ValidateRidPayloads(SdpMediaDescription media, int mediaIndex, SdpRidAttribute rid, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<int> payloads = new List<int>();
        int index = 0;
        while (index < rid.PayloadTypes.Count)
        {
            int payload = rid.PayloadTypes[index];
            if (payloads.Contains(payload))
            {
                AddProjectionIssue(media, mediaIndex, rid, SdpDiagnosticCodes.ValidationPayloadType, "A rid payload type must not be repeated in one payload restriction list.", result, limiter);
            }
            else
            {
                payloads.Add(payload);
            }
            if (!SdpRtpValidationCore.ContainsMediaPayload(media, payload))
            {
                AddProjectionIssue(media, mediaIndex, rid, SdpDiagnosticCodes.ValidationDanglingPayloadReference, "Every rid payload type must appear in the media format list.", result, limiter);
            }
            index = index + 1;
        }
    }

    internal static void ValidateRidRestrictions(SdpMediaDescription media, int mediaIndex, SdpRidAttribute rid, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<string> names = new List<string>();
        int index = 0;
        while (index < rid.Restrictions.Count)
        {
            SdpRidRestriction restriction = rid.Restrictions[index];
            if (names.Contains(restriction.Name))
            {
                AddProjectionIssue(media, mediaIndex, rid, SdpDiagnosticCodes.ValidationAttributeCardinality, "A rid restriction name must not be repeated within one rid attribute.", result, limiter);
            }
            else
            {
                names.Add(restriction.Name);
            }
            if (IsKnownRestrictionIgnoringCase(restriction.Name) && !IsKnownRestriction(restriction.Name))
            {
                AddProjectionIssue(media, mediaIndex, rid, SdpDiagnosticCodes.ValidationRid, "Registered rid restriction names use case-sensitive lowercase spelling.", result, limiter);
            }
            else if (IsIntegerRestriction(restriction.Name) && restriction.HasValue)
            {
                ValidateIntegerRestriction(media, mediaIndex, rid, restriction, result, limiter);
            }
            else if (restriction.Name == "max-bpp" && restriction.HasValue)
            {
                ValidateBitsPerPixelRestriction(media, mediaIndex, rid, restriction, result, limiter);
            }
            index = index + 1;
        }
    }

    internal static void ValidateIntegerRestriction(SdpMediaDescription media, int mediaIndex, SdpRidAttribute rid, SdpRidRestriction restriction, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpNumberParseResult number = SdpNumberParser.ParseCharacterUnsignedDecimal(restriction.Value.AsSpan());
        if (!number.Success)
        {
            string code = number.DiagnosticCode.Length > 0 ? number.DiagnosticCode : SdpDiagnosticCodes.ParseInvalidNumber;
            AddProjectionIssue(media, mediaIndex, rid, code, "A numeric rid restriction value must contain unsigned decimal digits within the supported range.", result, limiter);
        }
    }

    internal static void ValidateBitsPerPixelRestriction(SdpMediaDescription media, int mediaIndex, SdpRidAttribute rid, SdpRidRestriction restriction, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        string value = restriction.Value;
        int decimalIndex = value.IndexOf('.');
        bool valid = decimalIndex > 0 && decimalIndex == value.LastIndexOf('.') && decimalIndex + 1 < value.Length;
        if (valid)
        {
            string wholeText = value.Substring(0, decimalIndex);
            string fractionText = value.Substring(decimalIndex + 1);
            SdpNumberParseResult whole = SdpNumberParser.ParseCharacterUnsignedDecimal(wholeText.AsSpan());
            SdpNumberParseResult fraction = SdpNumberParser.ParseCharacterUnsignedDecimal(fractionText.AsSpan());
            valid = fractionText.Length <= 4 && whole.Success && fraction.Success;
            if (valid)
            {
                valid = (whole.UnsignedValue > 0 || fraction.UnsignedValue > 0) && (whole.UnsignedValue < 48 || whole.UnsignedValue == 48 && fraction.UnsignedValue == 0);
            }
        }
        if (!valid)
        {
            AddProjectionIssue(media, mediaIndex, rid, SdpDiagnosticCodes.ValidationRid, "The max-bpp restriction must be a decimal value from 0.0001 through 48.0 with at most four fractional digits.", result, limiter);
        }
    }

    internal static void ValidateRidDependencies(SdpMediaDescription media, int mediaIndex, List<SdpRidAttribute> rids, SdpRidAttribute rid, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int restrictionIndex = 0;
        while (restrictionIndex < rid.Restrictions.Count)
        {
            SdpRidRestriction restriction = rid.Restrictions[restrictionIndex];
            if (restriction.Name == "depend")
            {
                if (!restriction.HasValue || restriction.Value.Length == 0)
                {
                    AddProjectionIssue(media, mediaIndex, rid, SdpDiagnosticCodes.ValidationRid, "The depend restriction requires a comma-separated rid identifier list.", result, limiter);
                }
                else
                {
                    ValidateDependencyList(media, mediaIndex, rids, rid, restriction.Value, result, limiter);
                }
            }
            restrictionIndex = restrictionIndex + 1;
        }
    }

    internal static void ValidateDependencyList(SdpMediaDescription media, int mediaIndex, List<SdpRidAttribute> rids, SdpRidAttribute rid, string value, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        string[] identifiers = value.Split(',');
        List<string> dependencies = new List<string>();
        int index = 0;
        while (index < identifiers.Length)
        {
            string identifier = identifiers[index];
            if (!SdpRidAttributeCodecCore.IsRidIdentifier(identifier))
            {
                AddProjectionIssue(media, mediaIndex, rid, SdpDiagnosticCodes.ValidationRid, "Every depend restriction entry must be a valid rid identifier.", result, limiter);
            }
            else if (dependencies.Contains(identifier))
            {
                AddProjectionIssue(media, mediaIndex, rid, SdpDiagnosticCodes.ValidationRid, "A depend restriction must not repeat a rid identifier.", result, limiter);
            }
            else
            {
                dependencies.Add(identifier);
                if (CountRidIdentifiers(rids, identifier) != 1)
                {
                    AddProjectionIssue(media, mediaIndex, rid, SdpDiagnosticCodes.ValidationRid, "Every depend restriction entry must unambiguously identify one rid in the same media section.", result, limiter);
                }
            }
            index = index + 1;
        }
    }

    internal static int CountRidIdentifiers(List<SdpRidAttribute> rids, string identifier)
    {
        int count = 0;
        int index = 0;
        while (index < rids.Count)
        {
            if (rids[index].Id == identifier)
            {
                count = count + 1;
            }
            index = index + 1;
        }
        return count;
    }

    internal static void ValidateDependencyCycles(SdpMediaDescription media, int mediaIndex, List<SdpRidAttribute> rids, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<int> states = new List<int>();
        int index = 0;
        while (index < rids.Count)
        {
            states.Add(0);
            index = index + 1;
        }
        index = 0;
        while (index < rids.Count)
        {
            if (states[index] == 0 && VisitDependencyGraph(rids, index, states))
            {
                AddProjectionIssue(media, mediaIndex, rids[index], SdpDiagnosticCodes.ValidationRid, "Rid dependency relationships must not contain a cycle.", result, limiter);
                return;
            }
            index = index + 1;
        }
    }

    internal static bool VisitDependencyGraph(List<SdpRidAttribute> rids, int ridIndex, List<int> states)
    {
        if (states[ridIndex] == 1)
        {
            return true;
        }
        if (states[ridIndex] == 2)
        {
            return false;
        }
        states[ridIndex] = 1;
        SdpRidAttribute rid = rids[ridIndex];
        int restrictionIndex = 0;
        while (restrictionIndex < rid.Restrictions.Count)
        {
            SdpRidRestriction restriction = rid.Restrictions[restrictionIndex];
            if (restriction.Name == "depend" && restriction.HasValue)
            {
                string[] identifiers = restriction.Value.Split(',');
                int identifierIndex = 0;
                while (identifierIndex < identifiers.Length)
                {
                    int dependencyIndex = FindRidIndex(rids, identifiers[identifierIndex]);
                    if (dependencyIndex >= 0 && VisitDependencyGraph(rids, dependencyIndex, states))
                    {
                        return true;
                    }
                    identifierIndex = identifierIndex + 1;
                }
            }
            restrictionIndex = restrictionIndex + 1;
        }
        states[ridIndex] = 2;
        return false;
    }

    internal static int FindRidIndex(List<SdpRidAttribute> rids, string identifier)
    {
        int index = 0;
        while (index < rids.Count)
        {
            if (rids[index].Id == identifier)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static bool IsIntegerRestriction(string name)
    {
        return name == "max-width" || name == "max-height" || name == "max-fps" || name == "max-fs" || name == "max-br" || name == "max-pps";
    }

    internal static bool IsKnownRestriction(string name)
    {
        return IsIntegerRestriction(name) || name == "max-bpp" || name == "depend";
    }

    internal static bool IsKnownRestrictionIgnoringCase(string name)
    {
        return string.Equals(name, "max-width", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "max-height", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "max-fps", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "max-fs", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "max-br", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "max-pps", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "max-bpp", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "depend", StringComparison.OrdinalIgnoreCase);
    }

    internal static void AddProjectionIssue(SdpMediaDescription media, int mediaIndex, SdpRidAttribute rid, string code, string detail, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpAttribute? attribute = rid.SourceAttribute;
        if (attribute != null)
        {
            int attributeIndex = media.Attributes.IndexOf(attribute);
            int occurrenceIndex = 0;
            if (attributeIndex >= 0)
            {
                occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, attributeIndex);
            }
            AddRidIssue(result, limiter, code, SdpScope.Media, attribute, mediaIndex, occurrenceIndex, detail);
        }
    }

    internal static void AddDecodeIssue(SdpAttributeDecodeResult decoded, SdpAttribute attribute, SdpScope scope, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        string code = SdpDiagnosticCodes.ValidationAttribute;
        string detail = "The rid attribute does not satisfy its registered syntax.";
        if (decoded.Diagnostics.Count > 0)
        {
            code = decoded.Diagnostics[0].Code;
            detail = decoded.Diagnostics[0].Detail;
        }
        AddRidIssue(result, limiter, code, scope, attribute, mediaIndex, occurrenceIndex, detail);
    }

    internal static void AddRidIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, string code, SdpScope scope, SdpAttribute attribute, int mediaIndex, int occurrenceIndex, string detail)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, code, SdpValidationSeverity.Error, scope, attribute.Name, mediaIndex, occurrenceIndex, detail, attribute.Location);
    }
}
