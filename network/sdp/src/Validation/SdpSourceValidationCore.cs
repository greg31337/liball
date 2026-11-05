using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpSourceValidationCore
{
    internal static void ValidateSourceDescription(SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<SdpSourceAttribute> sources = new List<SdpSourceAttribute>();
        List<SdpSourceGroupAttribute> groups = new List<SdpSourceGroupAttribute>();
        ReadSourceAttributes(media, mediaIndex, sources, groups, result, limiter);
        ValidateSourceAttributes(media, mediaIndex, sources, result, limiter);
        ValidateSourceGroups(media, mediaIndex, sources, groups, result, limiter);
    }

    internal static bool IsSourceAttribute(string name)
    {
        return SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.SynchronizationSource)
            || SdpRtpValidationCore.NameEquals(name, SdpAttributeNames.SynchronizationSourceGroup);
    }

    internal static void ReadSourceAttributes(SdpMediaDescription media, int mediaIndex, List<SdpSourceAttribute> sources, List<SdpSourceGroupAttribute> groups, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            if (IsSourceAttribute(attribute.Name))
            {
                int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, index);
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success)
                {
                    SdpRtpValidationCore.AddDecodeIssue(decoded, attribute, mediaIndex, occurrenceIndex, result, limiter);
                }
                else if (decoded.SynchronizationSourceAttribute != null)
                {
                    sources.Add(decoded.SynchronizationSourceAttribute);
                }
                else if (decoded.SourceGroupAttribute != null)
                {
                    groups.Add(decoded.SourceGroupAttribute);
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateSourceAttributes(SdpMediaDescription media, int mediaIndex, List<SdpSourceAttribute> sources, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<uint> sourceIdentifiers = new List<uint>();
        int index = 0;
        while (index < sources.Count)
        {
            SdpSourceAttribute source = sources[index];
            if (!sourceIdentifiers.Contains(source.SynchronizationSource))
            {
                sourceIdentifiers.Add(source.SynchronizationSource);
                if (!HasNamedSourceAttribute(sources, source.SynchronizationSource, "cname"))
                {
                    AddSourceIssue(media, mediaIndex, source.SourceAttribute, SdpDiagnosticCodes.ValidationSource, "Every synchronization source with source attributes must have a cname source attribute.", result, limiter);
                }
            }
            ValidateSingletonSourceAttribute(media, mediaIndex, sources, index, "cname", result, limiter);
            ValidateSingletonSourceAttribute(media, mediaIndex, sources, index, "previous-ssrc", result, limiter);
            ValidateSingletonSourceAttribute(media, mediaIndex, sources, index, SdpAttributeNames.MediaStreamId, result, limiter);
            ValidatePreviousSourceIdentifiers(media, mediaIndex, source, result, limiter);
            index = index + 1;
        }
    }

    internal static void ValidatePreviousSourceIdentifiers(SdpMediaDescription media, int mediaIndex, SdpSourceAttribute source, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!SdpRtpValidationCore.NameEquals(source.Name, "previous-ssrc"))
        {
            return;
        }
        if (!source.HasValue)
        {
            AddSourceIssue(media, mediaIndex, source.SourceAttribute, SdpDiagnosticCodes.ValidationAttribute, "The previous-ssrc source attribute requires at least one unsigned source identifier.", result, limiter);
            return;
        }
        string[] tokens = SdpSourceAttributeCodecCore.ReadSourceTokens(source.Value);
        if (tokens.Length == 0)
        {
            AddSourceIssue(media, mediaIndex, source.SourceAttribute, SdpDiagnosticCodes.ValidationAttribute, "The previous-ssrc source attribute requires at least one unsigned source identifier.", result, limiter);
            return;
        }
        List<uint> identifiers = new List<uint>();
        int index = 0;
        while (index < tokens.Length)
        {
            SdpNumberParseResult identifier = SdpNumberParser.ParseCharacterSsrc(tokens[index].AsSpan());
            if (!SdpCommonAttributeCodecCore.IsZeroBasedIntegerValue(tokens[index]) || !identifier.Success)
            {
                string code = identifier.DiagnosticCode.Length > 0 ? identifier.DiagnosticCode : SdpDiagnosticCodes.ParseInvalidNumber;
                AddSourceIssue(media, mediaIndex, source.SourceAttribute, code, "Every previous-ssrc value must be a decimal unsigned 32-bit integer.", result, limiter);
                return;
            }
            if (identifiers.Contains(identifier.UInt32Value))
            {
                AddSourceIssue(media, mediaIndex, source.SourceAttribute, SdpDiagnosticCodes.ValidationSource, "A previous source identifier must not be repeated within one previous-ssrc attribute.", result, limiter);
                return;
            }
            identifiers.Add(identifier.UInt32Value);
            index = index + 1;
        }
    }

    internal static void ValidateSingletonSourceAttribute(SdpMediaDescription media, int mediaIndex, List<SdpSourceAttribute> sources, int sourceIndex, string name, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpSourceAttribute current = sources[sourceIndex];
        if (!SdpRtpValidationCore.NameEquals(current.Name, name))
        {
            return;
        }
        int index = 0;
        while (index < sourceIndex)
        {
            SdpSourceAttribute previous = sources[index];
            if (previous.SynchronizationSource == current.SynchronizationSource && SdpRtpValidationCore.NameEquals(previous.Name, current.Name))
            {
                bool sameValue = previous.HasValue == current.HasValue && previous.Value == current.Value;
                string code = sameValue ? SdpDiagnosticCodes.ValidationAttributeCardinality : SdpDiagnosticCodes.ValidationSource;
                string detail = sameValue
                    ? "A singleton source attribute must not be repeated for the same synchronization source."
                    : "A singleton source attribute has conflicting values for the same synchronization source.";
                AddSourceIssue(media, mediaIndex, current.SourceAttribute, code, detail, result, limiter);
                return;
            }
            index = index + 1;
        }
    }

    internal static void ValidateSourceGroups(SdpMediaDescription media, int mediaIndex, List<SdpSourceAttribute> sources, List<SdpSourceGroupAttribute> groups, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < groups.Count)
        {
            SdpSourceGroupAttribute group = groups[index];
            ValidateGroupCardinality(media, mediaIndex, group, result, limiter);
            ValidateGroupMembers(media, mediaIndex, sources, group, result, limiter);
            ValidateDuplicateGroup(media, mediaIndex, groups, index, result, limiter);
            ValidateGroupSourceConsistency(media, mediaIndex, sources, group, "cname", result, limiter);
            ValidateGroupSourceConsistency(media, mediaIndex, sources, group, SdpAttributeNames.MediaStreamId, result, limiter);
            index = index + 1;
        }
    }

    internal static void ValidateGroupCardinality(SdpMediaDescription media, int mediaIndex, SdpSourceGroupAttribute group, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (IsRegisteredSemantic(group.Semantic) && CountDistinctSources(group.SynchronizationSources) < 2)
        {
            AddSourceIssue(media, mediaIndex, group.SourceAttribute, SdpDiagnosticCodes.ValidationAttributeCardinality, "A registered source-group semantic must relate at least two distinct synchronization sources.", result, limiter);
        }
    }

    internal static void ValidateGroupMembers(SdpMediaDescription media, int mediaIndex, List<SdpSourceAttribute> sources, SdpSourceGroupAttribute group, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<uint> members = new List<uint>();
        int index = 0;
        while (index < group.SynchronizationSources.Count)
        {
            uint member = group.SynchronizationSources[index];
            if (members.Contains(member))
            {
                AddSourceIssue(media, mediaIndex, group.SourceAttribute, SdpDiagnosticCodes.ValidationSource, "A synchronization source must not be repeated within one source group.", result, limiter);
            }
            else
            {
                members.Add(member);
            }
            if (!HasSourceIdentifier(sources, member))
            {
                AddSourceIssue(media, mediaIndex, group.SourceAttribute, SdpDiagnosticCodes.ValidationSource, "Every source-group member must be defined by an ssrc attribute in the same media section.", result, limiter);
            }
            index = index + 1;
        }
    }

    internal static void ValidateDuplicateGroup(SdpMediaDescription media, int mediaIndex, List<SdpSourceGroupAttribute> groups, int groupIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpSourceGroupAttribute current = groups[groupIndex];
        int index = 0;
        while (index < groupIndex)
        {
            SdpSourceGroupAttribute previous = groups[index];
            if (SdpRtpValidationCore.NameEquals(previous.Semantic, current.Semantic) && SourceListsEqual(previous.SynchronizationSources, current.SynchronizationSources))
            {
                AddSourceIssue(media, mediaIndex, current.SourceAttribute, SdpDiagnosticCodes.ValidationAttributeCardinality, "An identical source group must not be declared more than once.", result, limiter);
                return;
            }
            index = index + 1;
        }
    }

    internal static void ValidateGroupSourceConsistency(SdpMediaDescription media, int mediaIndex, List<SdpSourceAttribute> sources, SdpSourceGroupAttribute group, string name, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool found = false;
        string expected = string.Empty;
        int memberIndex = 0;
        while (memberIndex < group.SynchronizationSources.Count)
        {
            SdpSourceAttribute? source = FindNamedSourceAttribute(sources, group.SynchronizationSources[memberIndex], name);
            if (source != null)
            {
                if (!found)
                {
                    found = true;
                    expected = source.Value;
                }
                else if (source.Value != expected)
                {
                    AddSourceIssue(media, mediaIndex, group.SourceAttribute, SdpDiagnosticCodes.ValidationSource, "Grouped synchronization sources must use consistent cname and msid associations.", result, limiter);
                    return;
                }
            }
            memberIndex = memberIndex + 1;
        }
    }

    internal static bool HasNamedSourceAttribute(List<SdpSourceAttribute> sources, uint synchronizationSource, string name)
    {
        return FindNamedSourceAttribute(sources, synchronizationSource, name) != null;
    }

    internal static SdpSourceAttribute? FindNamedSourceAttribute(List<SdpSourceAttribute> sources, uint synchronizationSource, string name)
    {
        int index = 0;
        while (index < sources.Count)
        {
            SdpSourceAttribute source = sources[index];
            if (source.SynchronizationSource == synchronizationSource && SdpRtpValidationCore.NameEquals(source.Name, name))
            {
                return source;
            }
            index = index + 1;
        }
        return null;
    }

    internal static bool HasSourceIdentifier(List<SdpSourceAttribute> sources, uint synchronizationSource)
    {
        int index = 0;
        while (index < sources.Count)
        {
            if (sources[index].SynchronizationSource == synchronizationSource)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool IsRegisteredSemantic(string semantic)
    {
        return semantic == SdpGroupSemantics.FlowIdentification
            || semantic == SdpGroupSemantics.ForwardErrorCorrection
            || semantic == SdpGroupSemantics.ForwardErrorCorrectionFramework
            || semantic == SdpGroupSemantics.Duplication
            || semantic == SdpGroupSemantics.Simulcast;
    }

    internal static int CountDistinctSources(List<uint> sources)
    {
        List<uint> distinct = new List<uint>();
        int index = 0;
        while (index < sources.Count)
        {
            if (!distinct.Contains(sources[index]))
            {
                distinct.Add(sources[index]);
            }
            index = index + 1;
        }
        return distinct.Count;
    }

    internal static bool SourceListsEqual(List<uint> first, List<uint> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }
        int index = 0;
        while (index < first.Count)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static void AddSourceIssue(SdpMediaDescription media, int mediaIndex, SdpAttribute? attribute, string code, string detail, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        string attributeName = SdpAttributeNames.SynchronizationSource;
        int occurrenceIndex = 0;
        SdpSourceLocation location = media.Location;
        if (attribute != null)
        {
            attributeName = attribute.Name;
            int attributeIndex = media.Attributes.IndexOf(attribute);
            if (attributeIndex >= 0)
            {
                occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(media.Attributes, attributeIndex);
            }
            location = attribute.Location;
        }
        SdpValidationCore.AddValidationIssue(result, limiter, code, SdpValidationSeverity.Error, SdpScope.Media, attributeName, mediaIndex, occurrenceIndex, detail, location);
    }
}
