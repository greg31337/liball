using System;
using System.Collections.Generic;
using System.Net;

namespace sdp;

internal static class SdpEffectiveValueCore
{
    internal static List<SdpSourceFilterAttribute> ResolveSourceFilters(SdpSessionDescription description, int mediaIndex)
    {
        SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
        List<SdpAttribute> attributes = description.Attributes;
        if (ContainsNamedAttribute(media.Attributes, SdpAttributeNames.SourceFilter))
        {
            attributes = media.Attributes;
        }
        SdpConnectionData? connection = SdpQueryServiceCore.ResolveEffectiveConnection(description, mediaIndex);
        List<SdpSourceFilterAttribute> result = new List<SdpSourceFilterAttribute>();
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (NamesEqual(attribute.Name, SdpAttributeNames.SourceFilter))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.SourceFilterAttribute != null && connection != null && FilterMatchesConnection(decoded.SourceFilterAttribute, connection))
                {
                    result.Add(decoded.SourceFilterAttribute);
                }
            }
            index = index + 1;
        }
        return result;
    }

    internal static List<SdpClockReferenceAttribute> ResolveClockReferences(SdpSessionDescription description, int mediaIndex)
    {
        List<SdpClockReferenceAttribute> result = new List<SdpClockReferenceAttribute>();
        SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
        if (!SdpRtpValidationCore.IsRtpProtocol(media.Protocol))
        {
            return result;
        }
        List<SdpAttribute> attributes = description.Attributes;
        if (ContainsNamedAttribute(media.Attributes, SdpAttributeNames.TimestampReferenceClock))
        {
            attributes = media.Attributes;
        }
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (NamesEqual(attribute.Name, SdpAttributeNames.TimestampReferenceClock))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.ClockReferenceAttribute != null)
                {
                    result.Add(decoded.ClockReferenceAttribute);
                }
            }
            index = index + 1;
        }
        if (result.Count == 0 && !ContainsReferenceSignaling(description))
        {
            SdpClockReferenceAttribute assumed = new SdpClockReferenceAttribute();
            assumed.SourceKind = SdpClockSourceKind.Local;
            assumed.SourceToken = "local";
            result.Add(assumed);
        }
        return result;
    }

    internal static List<SdpMediaClockAttribute> ResolveMediaClocks(SdpSessionDescription description, int mediaIndex)
    {
        List<SdpMediaClockAttribute> result = new List<SdpMediaClockAttribute>();
        SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
        if (!SdpRtpValidationCore.IsRtpProtocol(media.Protocol))
        {
            return result;
        }
        List<SdpAttribute> attributes = description.Attributes;
        if (ContainsNamedAttribute(media.Attributes, SdpAttributeNames.MediaClock))
        {
            attributes = media.Attributes;
        }
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (NamesEqual(attribute.Name, SdpAttributeNames.MediaClock))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.MediaClockAttribute != null)
                {
                    result.Add(decoded.MediaClockAttribute);
                }
            }
            index = index + 1;
        }
        bool explicitSignaling = ContainsNamedAttribute(attributes, SdpAttributeNames.MediaClock);
        if (result.Count == 0 && !explicitSignaling)
        {
            SdpMediaClockAttribute assumed = new SdpMediaClockAttribute();
            assumed.ClockName = "sender";
            result.Add(assumed);
        }
        return result;
    }

    internal static void ValidateEffectiveValues(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        ValidateAttributeScope(description.Attributes, SdpScope.Session, -1, true, result, limiter);
        ValidateSourceFilterScope(description.Attributes, SdpScope.Session, -1, description, null, result, limiter);
        ValidateTraceableClockScope(description.Attributes, SdpScope.Session, -1, result, limiter);
        bool hasReferenceSignaling = ContainsReferenceSignaling(description);
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            bool rtp = SdpRtpValidationCore.IsRtpProtocol(media.Protocol);
            ValidateAttributeScope(media.Attributes, SdpScope.Media, mediaIndex, rtp, result, limiter);
            ValidateSourceFilterScope(media.Attributes, SdpScope.Media, mediaIndex, description, media, result, limiter);
            ValidateTraceableClockScope(media.Attributes, SdpScope.Media, mediaIndex, result, limiter);
            if (rtp)
            {
                List<SdpClockReferenceAttribute> references = ResolveClockReferences(description, mediaIndex);
                if (hasReferenceSignaling && references.Count == 0)
                {
                    SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, SdpScope.Media, SdpAttributeNames.TimestampReferenceClock, mediaIndex, -1, "Timestamp reference-clock signaling must be defined effectively for every RTP media section when it is used anywhere in the description.", media.Location);
                }
                ValidateDirectMediaClocks(description, mediaIndex, references, result, limiter);
            }
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void ValidateAttributeScope(List<SdpAttribute> attributes, SdpScope scope, int mediaIndex, bool clockApplicable, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            bool clock = NamesEqual(attribute.Name, SdpAttributeNames.TimestampReferenceClock) || NamesEqual(attribute.Name, SdpAttributeNames.MediaClock);
            if (clock && !clockApplicable)
            {
                AddAttributeIssue(attribute, attributes, index, scope, mediaIndex, SdpDiagnosticCodes.ValidationAttributeScope, "Clock-source attributes require an applicable RTP media stream.", result, limiter);
            }
            else if (clock || NamesEqual(attribute.Name, SdpAttributeNames.SourceFilter))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (!decoded.Success)
                {
                    AddDecodeIssue(decoded, attribute, attributes, index, scope, mediaIndex, result, limiter);
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateSourceFilterScope(List<SdpAttribute> attributes, SdpScope scope, int mediaIndex, SdpSessionDescription description, SdpMediaDescription? media, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        List<SdpSourceFilterAttribute> filters = new List<SdpSourceFilterAttribute>();
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (NamesEqual(attribute.Name, SdpAttributeNames.SourceFilter))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.SourceFilterAttribute != null)
                {
                    SdpSourceFilterAttribute filter = decoded.SourceFilterAttribute;
                    if (!FilterMatchesApplicableConnection(filter, description, media))
                    {
                        AddAttributeIssue(attribute, attributes, index, scope, mediaIndex, SdpDiagnosticCodes.ValidationConnection, "The source-filter destination must match a connection address applicable at its scope.", result, limiter);
                    }
                    if (FilterCoverageAlreadyExists(filters, filter))
                    {
                        AddAttributeIssue(attribute, attributes, index, scope, mediaIndex, SdpDiagnosticCodes.ValidationAttributeCardinality, "At most one source-filter may cover the same destination at one scope.", result, limiter);
                    }
                    filters.Add(filter);
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateTraceableClockScope(List<SdpAttribute> attributes, SdpScope scope, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool traceableSeen = false;
        bool nonTraceableSeen = false;
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (NamesEqual(attribute.Name, SdpAttributeNames.TimestampReferenceClock))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.ClockReferenceAttribute != null)
                {
                    int traceability = ReadTraceability(decoded.ClockReferenceAttribute);
                    if (traceability > 0)
                    {
                        traceableSeen = true;
                    }
                    else if (traceability < 0)
                    {
                        nonTraceableSeen = true;
                    }
                    if (traceableSeen && nonTraceableSeen)
                    {
                        AddAttributeIssue(attribute, attributes, index, scope, mediaIndex, SdpDiagnosticCodes.ValidationAttribute, "Traceable and non-traceable timestamp reference clocks must not be mixed at one scope.", result, limiter);
                        return;
                    }
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateDirectMediaClocks(SdpSessionDescription description, int mediaIndex, List<SdpClockReferenceAttribute> references, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
        List<SdpAttribute> attributes = description.Attributes;
        SdpScope scope = SdpScope.Session;
        if (ContainsNamedAttribute(media.Attributes, SdpAttributeNames.MediaClock))
        {
            attributes = media.Attributes;
            scope = SdpScope.Media;
        }
        if (scope == SdpScope.Session && HasEarlierRtpMedia(description, mediaIndex))
        {
            return;
        }
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (NamesEqual(attribute.Name, SdpAttributeNames.MediaClock))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                if (decoded.Success && decoded.MediaClockAttribute != null && decoded.MediaClockAttribute.ClockName == "direct" && !ContainsSignaledReference(references))
                {
                    int issueMediaIndex = scope == SdpScope.Session ? -1 : mediaIndex;
                    AddAttributeIssue(attribute, attributes, index, scope, issueMediaIndex, SdpDiagnosticCodes.ValidationAttribute, "A direct-referenced media clock requires an effective timestamp reference clock.", result, limiter);
                }
            }
            index = index + 1;
        }
    }

    internal static bool ContainsReferenceSignaling(SdpSessionDescription description)
    {
        if (ContainsNamedAttribute(description.Attributes, SdpAttributeNames.TimestampReferenceClock))
        {
            return true;
        }
        int index = 0;
        while (index < description.MediaDescriptions.Count)
        {
            if (ContainsNamedAttribute(description.MediaDescriptions[index].Attributes, SdpAttributeNames.TimestampReferenceClock))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool ContainsSignaledReference(List<SdpClockReferenceAttribute> references)
    {
        int index = 0;
        while (index < references.Count)
        {
            if (references[index].SourceAttribute != null)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool HasEarlierRtpMedia(SdpSessionDescription description, int mediaIndex)
    {
        int index = 0;
        while (index < mediaIndex)
        {
            if (SdpRtpValidationCore.IsRtpProtocol(description.MediaDescriptions[index].Protocol))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool ContainsNamedAttribute(List<SdpAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (NamesEqual(attributes[index].Name, name))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool FilterMatchesApplicableConnection(SdpSourceFilterAttribute filter, SdpSessionDescription description, SdpMediaDescription? media)
    {
        if (media == null)
        {
            if (description.Connection != null && FilterMatchesConnection(filter, description.Connection))
            {
                return true;
            }
            int mediaIndex = 0;
            while (mediaIndex < description.MediaDescriptions.Count)
            {
                SdpMediaDescription current = description.MediaDescriptions[mediaIndex];
                int connectionIndex = 0;
                while (connectionIndex < current.Connections.Count)
                {
                    if (FilterMatchesConnection(filter, current.Connections[connectionIndex]))
                    {
                        return true;
                    }
                    connectionIndex = connectionIndex + 1;
                }
                mediaIndex = mediaIndex + 1;
            }
            return false;
        }
        if (media.Connections.Count == 0)
        {
            return description.Connection != null && FilterMatchesConnection(filter, description.Connection);
        }
        int index = 0;
        while (index < media.Connections.Count)
        {
            if (FilterMatchesConnection(filter, media.Connections[index]))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool FilterMatchesConnection(SdpSourceFilterAttribute filter, SdpConnectionData connection)
    {
        if (!NamesEqual(filter.NetworkType, connection.NetworkType))
        {
            return false;
        }
        if (filter.AddressType != "*" && !NamesEqual(filter.AddressType, connection.AddressType))
        {
            return false;
        }
        if (filter.DestinationAddress == "*")
        {
            return true;
        }
        if (string.Equals(filter.DestinationAddress, connection.Address, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (!connection.HasAddressCount || connection.AddressCount <= 1 || filter.AddressKind != connection.AddressKind)
        {
            return false;
        }
        return AddressFallsInConnectionRange(connection, filter.DestinationAddress);
    }

    internal static bool AddressFallsInConnectionRange(SdpConnectionData connection, string address)
    {
        bool ipv4 = connection.AddressKind == SdpAddressKind.Ipv4Multicast;
        if (ipv4 && (!SdpConnectionLineParser.IsValidIpv4Address(connection.Address) || !SdpConnectionLineParser.IsValidIpv4Address(address)))
        {
            return false;
        }
        if (!ipv4 && (!SdpConnectionLineParser.IsValidIpv6Address(connection.Address) || !SdpConnectionLineParser.IsValidIpv6Address(address)))
        {
            return false;
        }
        byte[] first = IPAddress.Parse(connection.Address).GetAddressBytes();
        byte[] candidate = IPAddress.Parse(address).GetAddressBytes();
        if (first.Length != candidate.Length)
        {
            return false;
        }
        byte[] last = new byte[first.Length];
        Array.Copy(first, last, first.Length);
        long carry = connection.AddressCount - 1;
        int index = last.Length - 1;
        while (index >= 0 && carry > 0)
        {
            long sum = last[index] + carry;
            last[index] = (byte)(sum & 255);
            carry = sum >> 8;
            index = index - 1;
        }
        if (carry > 0)
        {
            return false;
        }
        return CompareAddressBytes(candidate, first) >= 0 && CompareAddressBytes(candidate, last) <= 0;
    }

    internal static int CompareAddressBytes(byte[] left, byte[] right)
    {
        int index = 0;
        while (index < left.Length)
        {
            if (left[index] < right[index])
            {
                return -1;
            }
            if (left[index] > right[index])
            {
                return 1;
            }
            index = index + 1;
        }
        return 0;
    }

    internal static bool FilterCoverageAlreadyExists(List<SdpSourceFilterAttribute> filters, SdpSourceFilterAttribute candidate)
    {
        int index = 0;
        while (index < filters.Count)
        {
            SdpSourceFilterAttribute existing = filters[index];
            bool sameNetwork = NamesEqual(existing.NetworkType, candidate.NetworkType);
            bool addressTypesOverlap = existing.AddressType == "*" || candidate.AddressType == "*" || NamesEqual(existing.AddressType, candidate.AddressType);
            bool destinationsOverlap = existing.DestinationAddress == "*" || candidate.DestinationAddress == "*" || string.Equals(existing.DestinationAddress, candidate.DestinationAddress, StringComparison.OrdinalIgnoreCase);
            if (sameNetwork && addressTypesOverlap && destinationsOverlap)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static int ReadTraceability(SdpClockReferenceAttribute reference)
    {
        if (reference.SourceKind == SdpClockSourceKind.Gps || reference.SourceKind == SdpClockSourceKind.Galileo || reference.SourceKind == SdpClockSourceKind.Glonass)
        {
            return 1;
        }
        if (reference.SourceKind == SdpClockSourceKind.Ntp && reference.Value == "/traceable/" || reference.SourceKind == SdpClockSourceKind.Ptp && reference.Value.EndsWith(":traceable", StringComparison.Ordinal) || reference.SourceKind == SdpClockSourceKind.Private && reference.Value == "traceable")
        {
            return 1;
        }
        if (reference.SourceKind != SdpClockSourceKind.Unknown)
        {
            return -1;
        }
        return 0;
    }

    internal static void AddDecodeIssue(SdpAttributeDecodeResult decoded, SdpAttribute attribute, List<SdpAttribute> attributes, int index, SdpScope scope, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        string code = SdpDiagnosticCodes.ValidationAttribute;
        string detail = "The attribute value does not satisfy its registered syntax.";
        if (decoded.Diagnostics.Count > 0)
        {
            code = decoded.Diagnostics[0].Code;
            detail = decoded.Diagnostics[0].Detail;
        }
        AddAttributeIssue(attribute, attributes, index, scope, mediaIndex, code, detail, result, limiter);
    }

    internal static void AddAttributeIssue(SdpAttribute attribute, List<SdpAttribute> attributes, int index, SdpScope scope, int mediaIndex, string code, string detail, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int occurrenceIndex = SdpRtpValidationCore.ReadOccurrenceIndex(attributes, index);
        SdpValidationCore.AddValidationIssue(result, limiter, code, SdpValidationSeverity.Error, scope, attribute.Name, mediaIndex, occurrenceIndex, detail, attribute.Location);
    }

    internal static bool NamesEqual(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
