using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpCoreValidationCore
{
    internal static void ValidateCoreDescription(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!HasInitializedSessionCollections(description, result, limiter))
        {
            return;
        }

        ValidateRequiredFields(description, result, limiter);
        ValidateOriginField(description, result, limiter);
        ValidateSessionTextFields(description, result, limiter);
        ValidateConnectionField(description.Connection, SdpScope.Session, -1, 0, result, limiter);
        ValidateBandwidthFields(description.Bandwidths, SdpScope.Session, -1, result, limiter);
        ValidateTimingFields(description, result, limiter);
        ValidateAttributeFields(description.Attributes, SdpScope.Session, -1, result, limiter);
        if (description.EncryptionKey != null)
        {
            AddObsoleteFieldIssue(result, limiter, SdpScope.Session, -1, description.EncryptionKey.Location);
        }

        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            if (media == null)
            {
                SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationMedia, SdpValidationSeverity.Fatal, SdpScope.Media, 'm', mediaIndex, 0, "Every media-description entry must be initialized.", new SdpSourceLocation());
            }
            else
            {
                ValidateMediaField(description, media, mediaIndex, result, limiter);
            }
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static bool HasInitializedSessionCollections(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (description.Emails == null || description.Phones == null || description.Bandwidths == null || description.Timings == null || description.TimeZoneAdjustments == null || description.Attributes == null || description.MediaDescriptions == null || description.RawLines == null)
        {
            SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ParseMissingRequiredField, SdpValidationSeverity.Fatal, SdpScope.Session, 'v', -1, 0, "Every session-description collection must be initialized.", new SdpSourceLocation());
            return false;
        }
        return true;
    }

    internal static void ValidateRequiredFields(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (!description.HasProtocolVersion)
        {
            AddMissingFieldIssue(result, limiter, 'v', "The required protocol-version field is absent.");
        }
        else if (description.ProtocolVersion != 0)
        {
            SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationProtocolVersion, SdpValidationSeverity.Error, SdpScope.Session, 'v', -1, 0, "The SDP protocol version must be zero.", new SdpSourceLocation());
        }
        if (description.Origin == null)
        {
            AddMissingFieldIssue(result, limiter, 'o', "The required origin field is absent.");
        }
        if (!description.HasSessionName)
        {
            AddMissingFieldIssue(result, limiter, 's', "The required session-name field is absent.");
        }
        if (description.Timings.Count == 0)
        {
            AddMissingFieldIssue(result, limiter, 't', "At least one timing field is required.");
        }
    }

    internal static void AddMissingFieldIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, char fieldName, string detail)
    {
        SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ParseMissingRequiredField, SdpValidationSeverity.Error, SdpScope.Session, fieldName, -1, 0, detail, new SdpSourceLocation());
    }

    internal static void ValidateOriginField(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpOrigin? origin = description.Origin;
        if (origin == null)
        {
            return;
        }
        if (origin.Username == null || !SdpCommonAttributeCodecCore.IsNonWhitespaceValue(origin.Username))
        {
            AddOriginIssue(result, limiter, origin, "The origin username must contain a non-whitespace value.");
        }
        if (origin.SessionId == null || !SdpNumberParser.ParseCharacterUnsignedDecimal(origin.SessionId.AsSpan()).Success)
        {
            AddOriginIssue(result, limiter, origin, "The origin session identifier must be an unsigned decimal integer within the supported 64-bit range.");
        }
        if (origin.SessionVersion == null || !SdpNumberParser.ParseCharacterUnsignedDecimal(origin.SessionVersion.AsSpan()).Success)
        {
            AddOriginIssue(result, limiter, origin, "The origin session version must be an unsigned decimal integer within the supported 64-bit range.");
        }
        if (origin.NetworkType == null || origin.AddressType == null || !SdpCommonAttributeCodecCore.IsTokenValue(origin.NetworkType) || !SdpCommonAttributeCodecCore.IsTokenValue(origin.AddressType))
        {
            AddOriginIssue(result, limiter, origin, "The origin network and address types must use token syntax.");
        }
        if (!IsValidOriginAddress(origin))
        {
            AddOriginIssue(result, limiter, origin, "The origin address must be a unicast address or host name consistent with its address type and address-kind projection.");
        }
    }

    internal static bool IsValidOriginAddress(SdpOrigin origin)
    {
        if (origin.AddressType == null || origin.UnicastAddress == null)
        {
            return false;
        }
        if (string.Equals(origin.AddressType, "IP4", StringComparison.OrdinalIgnoreCase))
        {
            if (SdpConnectionLineParser.IsValidIpv4Address(origin.UnicastAddress))
            {
                int firstOctet = SdpConnectionLineParser.ReadIpv4FirstOctet(origin.UnicastAddress);
                return firstOctet < 224 && origin.AddressKind == SdpAddressKind.Ipv4;
            }
            return SdpConnectionLineParser.IsValidFqdnAddress(origin.UnicastAddress) && !SdpConnectionLineParser.LooksLikeIpv4Literal(origin.UnicastAddress) && origin.AddressKind == SdpAddressKind.HostName;
        }
        if (string.Equals(origin.AddressType, "IP6", StringComparison.OrdinalIgnoreCase))
        {
            if (SdpConnectionLineParser.IsValidIpv6Address(origin.UnicastAddress))
            {
                return !SdpConnectionLineParser.IsIpv6MulticastAddress(origin.UnicastAddress) && origin.AddressKind == SdpAddressKind.Ipv6;
            }
            return SdpConnectionLineParser.IsValidFqdnAddress(origin.UnicastAddress) && origin.AddressKind == SdpAddressKind.HostName;
        }
        return SdpCommonAttributeCodecCore.IsNonWhitespaceValue(origin.UnicastAddress) && origin.AddressKind == SdpAddressKind.Unknown;
    }

    internal static void AddOriginIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, SdpOrigin origin, string detail)
    {
        SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationOrigin, SdpValidationSeverity.Error, SdpScope.Session, 'o', -1, 0, detail, origin.Location);
    }

    internal static void ValidateSessionTextFields(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (description.HasSessionName && (description.SessionName == null || !SdpCommonAttributeCodecCore.IsByteStringValue(description.SessionName)))
        {
            SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, SdpScope.Session, 's', -1, 0, "The session name contains a prohibited NUL or line-ending character.", new SdpSourceLocation());
        }
        ValidateOptionalTextField(description.Information, 'i', result, limiter);
        ValidateOptionalTextField(description.Uri, 'u', result, limiter);
        ValidateTextList(description.Emails, 'e', result, limiter);
        ValidateTextList(description.Phones, 'p', result, limiter);
    }

    internal static void ValidateOptionalTextField(string? value, char fieldName, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (value != null && !SdpCommonAttributeCodecCore.IsByteStringValue(value))
        {
            SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, SdpScope.Session, fieldName, -1, 0, "The field contains a prohibited NUL or line-ending character.", new SdpSourceLocation());
        }
    }

    internal static void ValidateTextList(List<string> values, char fieldName, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < values.Count)
        {
            string value = values[index];
            if (value == null || !SdpCommonAttributeCodecCore.IsByteStringValue(value))
            {
                SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, SdpScope.Session, fieldName, -1, index, "The field value must be initialized and contain no NUL or line-ending characters.", new SdpSourceLocation());
            }
            index = index + 1;
        }
    }

    internal static void ValidateConnectionField(SdpConnectionData? connection, SdpScope scope, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (connection == null)
        {
            return;
        }
        bool valid = connection.NetworkType != null && connection.AddressType != null && connection.Address != null && SdpCommonAttributeCodecCore.IsTokenValue(connection.NetworkType) && SdpCommonAttributeCodecCore.IsTokenValue(connection.AddressType);
        if (valid)
        {
            valid = IsValidConnectionAddress(connection);
        }
        if (!valid)
        {
            SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationConnection, SdpValidationSeverity.Error, scope, 'c', mediaIndex, occurrenceIndex, "Connection data must contain a valid address and suffix state consistent with its address type and address-kind projection.", connection.Location);
        }
    }

    internal static bool IsValidConnectionAddress(SdpConnectionData connection)
    {
        if (string.Equals(connection.AddressType, "IP4", StringComparison.OrdinalIgnoreCase))
        {
            return IsValidIpv4ConnectionAddress(connection);
        }
        if (string.Equals(connection.AddressType, "IP6", StringComparison.OrdinalIgnoreCase))
        {
            return IsValidIpv6ConnectionAddress(connection);
        }
        return SdpCommonAttributeCodecCore.IsNonWhitespaceValue(connection.Address) && connection.AddressKind == SdpAddressKind.Unknown && !connection.HasTimeToLive && connection.TimeToLive == 0 && !connection.HasAddressCount && connection.AddressCount == 1;
    }

    internal static bool IsValidIpv4ConnectionAddress(SdpConnectionData connection)
    {
        if (!SdpConnectionLineParser.IsValidIpv4Address(connection.Address))
        {
            return SdpConnectionLineParser.IsValidFqdnAddress(connection.Address) && !SdpConnectionLineParser.LooksLikeIpv4Literal(connection.Address) && connection.AddressKind == SdpAddressKind.HostName && !connection.HasTimeToLive && connection.TimeToLive == 0 && !connection.HasAddressCount && connection.AddressCount == 1;
        }
        int firstOctet = SdpConnectionLineParser.ReadIpv4FirstOctet(connection.Address);
        if (firstOctet >= 224 && firstOctet <= 239)
        {
            return connection.AddressKind == SdpAddressKind.Ipv4Multicast && connection.HasTimeToLive && connection.TimeToLive >= 0 && connection.TimeToLive <= 255 && (!connection.HasAddressCount && connection.AddressCount == 1 || connection.HasAddressCount && connection.AddressCount > 0);
        }
        return firstOctet < 224 && connection.AddressKind == SdpAddressKind.Ipv4 && !connection.HasTimeToLive && connection.TimeToLive == 0 && !connection.HasAddressCount && connection.AddressCount == 1;
    }

    internal static bool IsValidIpv6ConnectionAddress(SdpConnectionData connection)
    {
        if (!SdpConnectionLineParser.IsValidIpv6Address(connection.Address))
        {
            return SdpConnectionLineParser.IsValidFqdnAddress(connection.Address) && connection.AddressKind == SdpAddressKind.HostName && !connection.HasTimeToLive && connection.TimeToLive == 0 && !connection.HasAddressCount && connection.AddressCount == 1;
        }
        bool multicast = SdpConnectionLineParser.IsIpv6MulticastAddress(connection.Address);
        if (multicast)
        {
            return connection.AddressKind == SdpAddressKind.Ipv6Multicast && !connection.HasTimeToLive && connection.TimeToLive == 0 && (!connection.HasAddressCount && connection.AddressCount == 1 || connection.HasAddressCount && connection.AddressCount > 0);
        }
        return connection.AddressKind == SdpAddressKind.Ipv6 && !connection.HasTimeToLive && connection.TimeToLive == 0 && !connection.HasAddressCount && connection.AddressCount == 1;
    }

    internal static void ValidateBandwidthFields(List<SdpBandwidth> bandwidths, SdpScope scope, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int index = 0;
        while (index < bandwidths.Count)
        {
            SdpBandwidth bandwidth = bandwidths[index];
            if (bandwidth == null || bandwidth.Type == null || !SdpCommonAttributeCodecCore.IsTokenValue(bandwidth.Type) || bandwidth.Value < 0)
            {
                SdpSourceLocation location = bandwidth == null ? new SdpSourceLocation() : bandwidth.Location;
                SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationBandwidth, SdpValidationSeverity.Error, scope, 'b', mediaIndex, index, "A bandwidth entry must have a token type and a nonnegative value.", location);
            }
            index = index + 1;
        }
    }

    internal static void ValidateTimingFields(SdpSessionDescription description, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int timingIndex = 0;
        while (timingIndex < description.Timings.Count)
        {
            SdpTiming timing = description.Timings[timingIndex];
            if (timing == null || timing.RepeatTimes == null)
            {
                SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationTiming, SdpValidationSeverity.Fatal, SdpScope.Session, 't', -1, timingIndex, "Every timing period and repeat collection must be initialized.", new SdpSourceLocation());
            }
            else
            {
                ValidateTimingPeriod(timing, timingIndex, result, limiter);
            }
            timingIndex = timingIndex + 1;
        }
        int zoneIndex = 0;
        while (zoneIndex < description.TimeZoneAdjustments.Count)
        {
            SdpTimeZoneAdjustment adjustment = description.TimeZoneAdjustments[zoneIndex];
            bool valid = adjustment != null && IsValidRfcTime(adjustment.AdjustmentTime, false) && IsValidTypedTime(adjustment.Offset, true, false);
            if (!valid)
            {
                SdpSourceLocation location = adjustment == null ? new SdpSourceLocation() : adjustment.Location;
                SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationTiming, SdpValidationSeverity.Error, SdpScope.Session, 'z', -1, zoneIndex, "A time-zone adjustment must contain a nonzero RFC time and a representable signed typed-time offset.", location);
            }
            zoneIndex = zoneIndex + 1;
        }
    }

    internal static void ValidateTimingPeriod(SdpTiming timing, int timingIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        bool valid = IsValidRfcTime(timing.StartTime, true) && IsValidRfcTime(timing.StopTime, true);
        if (valid && timing.StopTime != "0")
        {
            valid = CompareRfcTimes(timing.StopTime, timing.StartTime) > 0;
        }
        if (!valid)
        {
            SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationTiming, SdpValidationSeverity.Error, SdpScope.Session, 't', -1, timingIndex, "A timing period must contain RFC time values and a bounded stop later than its start.", timing.Location);
        }
        int repeatIndex = 0;
        while (repeatIndex < timing.RepeatTimes.Count)
        {
            SdpRepeatTime repeat = timing.RepeatTimes[repeatIndex];
            bool repeatValid = repeat != null && repeat.Offsets != null && IsValidTypedTime(repeat.RepeatInterval, false, true) && IsValidTypedTime(repeat.ActiveDuration, false, true) && repeat.Offsets.Count > 0;
            if (repeatValid && repeat != null && repeat.Offsets != null)
            {
                int offsetIndex = 0;
                while (offsetIndex < repeat.Offsets.Count)
                {
                    if (!IsValidTypedTime(repeat.Offsets[offsetIndex], false, false))
                    {
                        repeatValid = false;
                    }
                    offsetIndex = offsetIndex + 1;
                }
            }
            if (!repeatValid)
            {
                SdpSourceLocation location = repeat == null ? timing.Location : repeat.Location;
                SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationTiming, SdpValidationSeverity.Error, SdpScope.Session, 'r', -1, repeatIndex, "A repeat field requires positive interval and duration values plus at least one nonnegative offset.", location);
            }
            repeatIndex = repeatIndex + 1;
        }
    }

    internal static bool IsValidRfcTime(string value, bool allowZero)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }
        if (value == "0")
        {
            return allowZero;
        }
        if (value.Length < 10 || value[0] < '1' || value[0] > '9')
        {
            return false;
        }
        int index = 1;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static int CompareRfcTimes(string first, string second)
    {
        if (first.Length < second.Length)
        {
            return -1;
        }
        if (first.Length > second.Length)
        {
            return 1;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] < second[index])
            {
                return -1;
            }
            if (first[index] > second[index])
            {
                return 1;
            }
            index = index + 1;
        }
        return 0;
    }

    internal static bool IsValidTypedTime(SdpTypedTime? value, bool allowNegative, bool requirePositive)
    {
        if (value == null || !allowNegative && value.Value < 0 || requirePositive && value.Value <= 0)
        {
            return false;
        }
        return SdpTimingServiceCore.NormalizeTypedTime(value).Success;
    }

    internal static void ValidateMediaField(SdpSessionDescription description, SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (media.Formats == null || media.Connections == null || media.Bandwidths == null || media.Attributes == null || media.RawLines == null)
        {
            SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationMedia, SdpValidationSeverity.Fatal, SdpScope.Media, 'm', mediaIndex, 0, "Every media-description collection must be initialized.", media.Location);
            return;
        }
        bool mediaSyntaxValid = media.MediaType != null && SdpCommonAttributeCodecCore.IsTokenValue(media.MediaType) && IsValidProtocolToken(media.Protocol) && media.Formats.Count > 0;
        int formatIndex = 0;
        while (formatIndex < media.Formats.Count)
        {
            string format = media.Formats[formatIndex];
            if (format == null || !SdpCommonAttributeCodecCore.IsTokenValue(format) || ContainsEarlierFormat(media.Formats, formatIndex))
            {
                mediaSyntaxValid = false;
            }
            formatIndex = formatIndex + 1;
        }
        if (!mediaSyntaxValid)
        {
            SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationMedia, SdpValidationSeverity.Error, SdpScope.Media, 'm', mediaIndex, 0, "A media description requires media and protocol tokens plus one or more unique format tokens.", media.Location);
        }
        if (!IsConsistentMediaKind(media))
        {
            SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationMedia, SdpValidationSeverity.Error, SdpScope.Media, 'm', mediaIndex, 0, "The media-kind projection is inconsistent with the media token.", media.Location);
        }
        if (media.Port < 0 || media.Port > 65535 || media.HasPortCount && (media.PortCount <= 0 || media.PortCount > 65536 - media.Port) || !media.HasPortCount && media.PortCount != 1)
        {
            SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationPort, SdpValidationSeverity.Error, SdpScope.Media, 'm', mediaIndex, 0, "The media port must be zero through 65535 and its optional port count must describe a positive range within that limit.", media.Location);
        }
        int connectionIndex = 0;
        while (connectionIndex < media.Connections.Count)
        {
            ValidateConnectionField(media.Connections[connectionIndex], SdpScope.Media, mediaIndex, connectionIndex, result, limiter);
            connectionIndex = connectionIndex + 1;
        }
        if (media.Port != 0 && description.Connection == null && media.Connections.Count == 0)
        {
            SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationMissingConnection, SdpValidationSeverity.Error, SdpScope.Media, 'c', mediaIndex, 0, "An active media description requires session-level or media-level connection data.", media.Location);
        }
        ValidateBandwidthFields(media.Bandwidths, SdpScope.Media, mediaIndex, result, limiter);
        ValidateAttributeFields(media.Attributes, SdpScope.Media, mediaIndex, result, limiter);
        ValidateOptionalMediaText(media, mediaIndex, result, limiter);
        if (media.EncryptionKey != null)
        {
            AddObsoleteFieldIssue(result, limiter, SdpScope.Media, mediaIndex, media.EncryptionKey.Location);
        }
    }

    internal static bool IsValidProtocolToken(string protocol)
    {
        if (protocol == null || protocol.Length == 0)
        {
            return false;
        }
        string[] parts = protocol.Split('/');
        int index = 0;
        while (index < parts.Length)
        {
            if (!SdpCommonAttributeCodecCore.IsTokenValue(parts[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool ContainsEarlierFormat(List<string> formats, int formatIndex)
    {
        int index = 0;
        while (index < formatIndex)
        {
            if (formats[index] == formats[formatIndex])
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool IsConsistentMediaKind(SdpMediaDescription media)
    {
        if (media.MediaType == null)
        {
            return false;
        }
        SdpMediaKind expected = SdpMediaLineParser.ReadMediaKind(media.MediaType);
        return media.MediaKind == SdpMediaKind.Unknown || expected == SdpMediaKind.Unknown || media.MediaKind == expected;
    }

    internal static void ValidateOptionalMediaText(SdpMediaDescription media, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        if (media.Information != null && !SdpCommonAttributeCodecCore.IsByteStringValue(media.Information))
        {
            SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, SdpScope.Media, 'i', mediaIndex, 0, "The media information contains a prohibited NUL or line-ending character.", media.Location);
        }
    }

    internal static void ValidateAttributeFields(List<SdpAttribute> attributes, SdpScope scope, int mediaIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        int directionCount = 0;
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            int occurrenceIndex = attribute == null ? index : ReadCoreOccurrenceIndex(attributes, index);
            if (attribute == null)
            {
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Fatal, scope, string.Empty, mediaIndex, occurrenceIndex, "Every attribute entry must be initialized.", new SdpSourceLocation());
            }
            else
            {
                ValidateAttributeEntry(attribute, scope, mediaIndex, occurrenceIndex, result, limiter);
                string canonicalName = SdpCommonAttributeCodecCore.ReadCanonicalName(attribute.Name);
                if (SdpCommonAttributeCodecCore.ReadDirection(canonicalName) != SdpDirection.Unknown)
                {
                    directionCount = directionCount + 1;
                    if (directionCount > 1)
                    {
                        AddCardinalityIssue(attribute, scope, mediaIndex, occurrenceIndex, "At most one direction attribute is permitted in an SDP scope.", result, limiter);
                    }
                }
                if (IsSingletonCommonAttribute(canonicalName) && HasEarlierNamedAttribute(attributes, index, canonicalName))
                {
                    AddCardinalityIssue(attribute, scope, mediaIndex, occurrenceIndex, "This SDP attribute may appear at most once in its scope.", result, limiter);
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateAttributeEntry(SdpAttribute attribute, SdpScope scope, int mediaIndex, int occurrenceIndex, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        string attributeName = attribute.Name == null ? string.Empty : attribute.Name;
        bool syntaxValid = attribute.Name != null && attribute.Value != null && SdpCommonAttributeCodecCore.IsTokenValue(attributeName) && (attribute.Kind == SdpAttributeKind.Property || attribute.Kind == SdpAttributeKind.Value);
        if (attribute.Value == null || attribute.Kind == SdpAttributeKind.Property && attribute.Value.Length != 0 || attribute.Kind == SdpAttributeKind.Value && !SdpCommonAttributeCodecCore.IsByteStringValue(attribute.Value))
        {
            syntaxValid = false;
        }
        if (!syntaxValid)
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, scope, attributeName, mediaIndex, occurrenceIndex, "An attribute requires a token name, a known property/value kind, and a value consistent with that kind.", attribute.Location);
        }
        SdpAttributeRegistration registration = attribute.Name == null ? new SdpAttributeRegistration() : SdpRegistryLookup.FindAttributeRegistration(attributeName);
        if (registration.Found && registration.Scopes.Count > 0 && !registration.Scopes.Contains(scope))
        {
            SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeScope, SdpValidationSeverity.Error, scope, attributeName, mediaIndex, occurrenceIndex, "The registered attribute is not permitted in this SDP scope.", attribute.Location);
        }
        string canonicalName = attribute.Name == null ? string.Empty : SdpCommonAttributeCodecCore.ReadCanonicalName(attributeName);
        if (canonicalName.Length > 0 && syntaxValid)
        {
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
            if (!decoded.Success)
            {
                string detail = decoded.Diagnostics.Count > 0 ? decoded.Diagnostics[0].Detail : "The common SDP attribute value is invalid.";
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttribute, SdpValidationSeverity.Error, scope, attributeName, mediaIndex, occurrenceIndex, detail, attribute.Location);
            }
            if (canonicalName == SdpAttributeNames.Category || canonicalName == SdpAttributeNames.Keywords)
            {
                SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationObsoleteSyntax, SdpValidationSeverity.Warning, scope, attributeName, mediaIndex, occurrenceIndex, "The attribute is obsolete in RFC 8866 and should be replaced where possible.", attribute.Location);
            }
        }
    }

    internal static int ReadCoreOccurrenceIndex(List<SdpAttribute> attributes, int targetIndex)
    {
        string name = attributes[targetIndex].Name == null ? string.Empty : attributes[targetIndex].Name;
        int occurrenceIndex = 0;
        int index = 0;
        while (index < targetIndex)
        {
            SdpAttribute attribute = attributes[index];
            if (attribute != null && attribute.Name != null && string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                occurrenceIndex = occurrenceIndex + 1;
            }
            index = index + 1;
        }
        return occurrenceIndex;
    }

    internal static bool HasEarlierNamedAttribute(List<SdpAttribute> attributes, int attributeIndex, string canonicalName)
    {
        int index = 0;
        while (index < attributeIndex)
        {
            SdpAttribute attribute = attributes[index];
            if (attribute != null && attribute.Name != null && SdpCommonAttributeCodecCore.ReadCanonicalName(attribute.Name) == canonicalName)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool IsSingletonCommonAttribute(string name)
    {
        return name == SdpAttributeNames.Orientation || name == SdpAttributeNames.ConferenceType || name == SdpAttributeNames.CharacterSet || name == SdpAttributeNames.SessionDescriptionLanguage || name == SdpAttributeNames.FrameRate || name == SdpAttributeNames.Quality || name == SdpAttributeNames.Tool || name == SdpAttributeNames.Category || name == SdpAttributeNames.Keywords || name == SdpAttributeNames.PacketTime || name == SdpAttributeNames.MaximumPacketTime;
    }

    internal static void AddCardinalityIssue(SdpAttribute attribute, SdpScope scope, int mediaIndex, int occurrenceIndex, string detail, SdpValidationResult result, SdpValidationIssueLimiter limiter)
    {
        SdpValidationCore.AddValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationAttributeCardinality, SdpValidationSeverity.Error, scope, attribute.Name, mediaIndex, occurrenceIndex, detail, attribute.Location);
    }

    internal static void AddObsoleteFieldIssue(SdpValidationResult result, SdpValidationIssueLimiter limiter, SdpScope scope, int mediaIndex, SdpSourceLocation location)
    {
        SdpValidationCore.AddFieldValidationIssue(result, limiter, SdpDiagnosticCodes.ValidationObsoleteSyntax, SdpValidationSeverity.Warning, scope, 'k', mediaIndex, 0, "The encryption-key field is obsolete in RFC 8866 and should not be used.", location);
    }
}
