using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpSemanticComparisonCore
{
    internal static SdpSessionDescription PrepareDescription(SdpSessionDescription source, SdpComparisonOptions options)
    {
        ValidateComparisonOptions(options);
        if (!options.CompareSemanticValues && !RequiresStructuralPreparation(options))
        {
            return source;
        }
        SdpSessionDescription result = SdpCloneCore.CopyDescription(source);
        RemoveIgnoredAttributes(result, options.IgnoredAttributeNames);
        if (options.CompareSemanticValues)
        {
            NormalizeSemanticGraph(result);
            MaterializeEffectiveMedia(result, options.IgnoreInsignificantOrdering);
        }
        if (options.IgnoreOriginSessionVersion && result.Origin != null)
        {
            result.Origin.SessionVersion = string.Empty;
            ClearOriginVersionFormatting(result);
        }
        if (options.IgnoreRawFormatting || options.CompareSemanticValues || options.IgnoreInsignificantOrdering)
        {
            ClearRawFormatting(result);
        }
        if (options.IgnoreInsignificantOrdering)
        {
            SortInsignificantCollections(result);
        }
        return result;
    }

    internal static void ValidateComparisonOptions(SdpComparisonOptions options)
    {
        if (options.IgnoredAttributeNames == null)
        {
            throw new ArgumentException("The ignored-attribute collection must be initialized.", "options");
        }
        int index = 0;
        while (index < options.IgnoredAttributeNames.Count)
        {
            string name = options.IgnoredAttributeNames[index];
            if (name == null || !SdpCommonAttributeCodecCore.IsTokenValue(name))
            {
                throw new ArgumentException("Ignored attribute names must use nonempty token syntax.", "options");
            }
            index = index + 1;
        }
    }

    internal static bool RequiresStructuralPreparation(SdpComparisonOptions options)
    {
        return options.IgnoreOriginSessionVersion || options.IgnoreInsignificantOrdering || options.IgnoreRawFormatting || options.IgnoredAttributeNames.Count > 0;
    }

    internal static void NormalizeSemanticGraph(SdpSessionDescription description)
    {
        if (description.Origin != null)
        {
            SdpNormalizationCore.NormalizeOrigin(description.Origin);
            if (description.Origin.AddressKind == SdpAddressKind.HostName)
            {
                description.Origin.UnicastAddress = description.Origin.UnicastAddress.ToLowerInvariant();
            }
        }
        if (description.Connection != null)
        {
            SdpNormalizationCore.NormalizeConnection(description.Connection);
            NormalizeConnectionHostCase(description.Connection);
        }
        SdpNormalizationCore.NormalizeBandwidthList(description.Bandwidths);
        SdpNormalizationCore.NormalizeTimingList(description.Timings);
        SdpNormalizationCore.NormalizeZoneList(description.TimeZoneAdjustments);
        if (description.EncryptionKey != null)
        {
            SdpNormalizationCore.NormalizeEncryptionKey(description.EncryptionKey);
        }
        NormalizeSemanticAttributes(description.Attributes);
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            if (media == null)
            {
                throw new ArgumentException("Every media-description entry must be initialized.", "description");
            }
            media.MediaType = SdpNormalizationCore.ReadCanonicalRegistryToken("sdp-parameters-1", media.MediaType, true);
            media.MediaKind = SdpMediaLineParser.ReadMediaKind(media.MediaType);
            media.Protocol = SdpNormalizationCore.ReadCanonicalRegistryToken("sdp-parameters-2", media.Protocol, true);
            SdpNormalizationCore.NormalizeFormatList(media);
            int connectionIndex = 0;
            while (connectionIndex < media.Connections.Count)
            {
                SdpNormalizationCore.NormalizeConnection(media.Connections[connectionIndex]);
                NormalizeConnectionHostCase(media.Connections[connectionIndex]);
                connectionIndex = connectionIndex + 1;
            }
            SdpNormalizationCore.NormalizeBandwidthList(media.Bandwidths);
            if (media.EncryptionKey != null)
            {
                SdpNormalizationCore.NormalizeEncryptionKey(media.EncryptionKey);
            }
            NormalizeSemanticAttributes(media.Attributes);
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void NormalizeConnectionHostCase(SdpConnectionData connection)
    {
        if (connection.AddressKind == SdpAddressKind.HostName)
        {
            connection.Address = connection.Address.ToLowerInvariant();
        }
    }

    internal static void NormalizeSemanticAttributes(List<SdpAttribute> attributes)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpNormalizationCore.NormalizeAttribute(attributes[index]);
            NormalizeRtpMapEncodingCase(attributes[index]);
            index = index + 1;
        }
    }

    internal static void NormalizeRtpMapEncodingCase(SdpAttribute attribute)
    {
        if (!string.Equals(attribute.Name, SdpAttributeNames.RtpMap, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
        if (!decoded.Success || decoded.RtpMapAttribute == null)
        {
            return;
        }
        decoded.RtpMapAttribute.EncodingName = decoded.RtpMapAttribute.EncodingName.ToLowerInvariant();
        SdpAttributeEncodeResult encoded = SdpAttributeEncoderCore.EncodeAttribute(decoded.RtpMapAttribute);
        if (encoded.Success && encoded.Attribute != null)
        {
            CopyEncodedAttribute(attribute, encoded.Attribute);
        }
    }

    internal static void MaterializeEffectiveMedia(SdpSessionDescription description, bool ignoreOrdering)
    {
        if (description.MediaDescriptions.Count == 0)
        {
            return;
        }
        List<SdpEffectiveMedia> effectiveMedia = new List<SdpEffectiveMedia>();
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            effectiveMedia.Add(SdpQueryServiceCore.ResolveEffectiveMedia(description, mediaIndex));
            mediaIndex = mediaIndex + 1;
        }
        description.Connection = null;
        description.Bandwidths.Clear();
        RemoveEffectiveAttributes(description.Attributes);
        mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            SdpEffectiveMedia effective = effectiveMedia[mediaIndex];
            media.Port = effective.Port;
            media.HasPortCount = effective.HasPortCount;
            media.PortCount = effective.PortCount;
            media.Connections.Clear();
            if (effective.Connection != null)
            {
                media.Connections.Add(effective.Connection);
            }
            media.Bandwidths.Clear();
            int bandwidthIndex = 0;
            while (bandwidthIndex < effective.Bandwidths.Count)
            {
                media.Bandwidths.Add(effective.Bandwidths[bandwidthIndex]);
                bandwidthIndex = bandwidthIndex + 1;
            }
            RemoveEffectiveAttributes(media.Attributes);
            AddEffectiveDirection(media.Attributes, effective.Direction);
            AddEffectiveLanguages(media.Attributes, SdpAttributeNames.Language, effective.Languages);
            AddEffectiveLanguages(media.Attributes, SdpAttributeNames.SessionDescriptionLanguage, effective.SessionDescriptionLanguages);
            AddEffectiveIce(media.Attributes, effective.Ice, ignoreOrdering);
            AddEffectiveDtls(media.Attributes, effective.Dtls);
            AddEncodedAttributes(media.Attributes, effective.HeaderExtensions);
            AddEncodedSourceFilters(media.Attributes, effective.SourceFilters);
            AddEncodedClockReferences(media.Attributes, effective.ClockReferences);
            AddEncodedMediaClocks(media.Attributes, effective.MediaClocks);
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void AddEffectiveDirection(List<SdpAttribute> attributes, SdpDirection direction)
    {
        string name = ReadDirectionName(direction);
        if (name.Length > 0)
        {
            attributes.Add(CreatePropertyAttribute(name));
        }
    }

    internal static string ReadDirectionName(SdpDirection direction)
    {
        if (direction == SdpDirection.SendReceive)
        {
            return SdpAttributeNames.SendReceive;
        }
        if (direction == SdpDirection.SendOnly)
        {
            return SdpAttributeNames.SendOnly;
        }
        if (direction == SdpDirection.ReceiveOnly)
        {
            return SdpAttributeNames.ReceiveOnly;
        }
        if (direction == SdpDirection.Inactive)
        {
            return SdpAttributeNames.Inactive;
        }
        return string.Empty;
    }

    internal static void AddEffectiveLanguages(List<SdpAttribute> attributes, string name, List<string> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            attributes.Add(CreateValueAttribute(name, values[index].ToLowerInvariant()));
            index = index + 1;
        }
    }

    internal static void AddEffectiveIce(List<SdpAttribute> attributes, SdpIceDescription? ice, bool ignoreOrdering)
    {
        if (ice == null)
        {
            return;
        }
        if (ice.IceLite)
        {
            attributes.Add(CreatePropertyAttribute(SdpAttributeNames.IceLite));
        }
        if (ice.IceMismatch)
        {
            attributes.Add(CreatePropertyAttribute(SdpAttributeNames.IceMismatch));
        }
        if (ice.Credentials != null)
        {
            if (ice.Credentials.UsernameFragment.Length > 0)
            {
                SdpIceUsernameFragmentAttribute username = new SdpIceUsernameFragmentAttribute();
                username.UsernameFragment = ice.Credentials.UsernameFragment;
                AddEncodedAttribute(attributes, username);
            }
            if (ice.Credentials.Password.Length > 0)
            {
                SdpIcePasswordAttribute password = new SdpIcePasswordAttribute();
                password.Password = ice.Credentials.Password;
                AddEncodedAttribute(attributes, password);
            }
        }
        if (ice.Options.Count > 0)
        {
            SdpIceOptionsAttribute options = new SdpIceOptionsAttribute();
            int optionIndex = 0;
            while (optionIndex < ice.Options.Count)
            {
                options.Options.Add(ice.Options[optionIndex].ToLowerInvariant());
                optionIndex = optionIndex + 1;
            }
            if (ignoreOrdering)
            {
                SortStringValues(options.Options);
            }
            AddEncodedAttribute(attributes, options);
        }
        int candidateIndex = 0;
        while (candidateIndex < ice.Candidates.Count)
        {
            AddEncodedAttribute(attributes, ice.Candidates[candidateIndex]);
            candidateIndex = candidateIndex + 1;
        }
        if (ice.RemoteCandidates.Count > 0)
        {
            SdpRemoteCandidatesAttribute remote = new SdpRemoteCandidatesAttribute();
            int remoteIndex = 0;
            while (remoteIndex < ice.RemoteCandidates.Count)
            {
                remote.Candidates.Add(ice.RemoteCandidates[remoteIndex]);
                remoteIndex = remoteIndex + 1;
            }
            AddEncodedAttribute(attributes, remote);
        }
        if (ice.EndOfCandidates)
        {
            attributes.Add(CreatePropertyAttribute(SdpAttributeNames.EndOfCandidates));
        }
    }

    internal static void AddEffectiveDtls(List<SdpAttribute> attributes, SdpDtlsDescription? dtls)
    {
        if (dtls == null)
        {
            return;
        }
        int fingerprintIndex = 0;
        while (fingerprintIndex < dtls.Fingerprints.Count)
        {
            AddEncodedAttribute(attributes, dtls.Fingerprints[fingerprintIndex]);
            fingerprintIndex = fingerprintIndex + 1;
        }
        if (dtls.SetupRole != SdpDtlsSetupRole.Unknown || dtls.SetupRoleToken.Length > 0)
        {
            SdpSetupAttribute setup = new SdpSetupAttribute();
            setup.Role = dtls.SetupRole;
            setup.RoleToken = dtls.SetupRoleToken;
            AddEncodedAttribute(attributes, setup);
        }
        if (dtls.ConnectionRole.Length > 0)
        {
            SdpConnectionAttribute connection = new SdpConnectionAttribute();
            connection.ConnectionRole = dtls.ConnectionRole;
            AddEncodedAttribute(attributes, connection);
        }
        if (dtls.TlsId.Length > 0)
        {
            SdpTlsIdAttribute tlsId = new SdpTlsIdAttribute();
            tlsId.Id = dtls.TlsId;
            AddEncodedAttribute(attributes, tlsId);
        }
        int cryptoIndex = 0;
        while (cryptoIndex < dtls.CryptoAttributes.Count)
        {
            AddEncodedAttribute(attributes, dtls.CryptoAttributes[cryptoIndex]);
            cryptoIndex = cryptoIndex + 1;
        }
    }

    internal static void AddEncodedAttributes(List<SdpAttribute> attributes, List<SdpExtMapAttribute> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            AddEncodedAttribute(attributes, values[index]);
            index = index + 1;
        }
    }

    internal static void AddEncodedSourceFilters(List<SdpAttribute> attributes, List<SdpSourceFilterAttribute> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            AddEncodedAttribute(attributes, values[index]);
            index = index + 1;
        }
    }

    internal static void AddEncodedClockReferences(List<SdpAttribute> attributes, List<SdpClockReferenceAttribute> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            AddEncodedAttribute(attributes, values[index]);
            index = index + 1;
        }
    }

    internal static void AddEncodedMediaClocks(List<SdpAttribute> attributes, List<SdpMediaClockAttribute> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            AddEncodedAttribute(attributes, values[index]);
            index = index + 1;
        }
    }

    internal static void AddEncodedAttribute(List<SdpAttribute> attributes, object value)
    {
        SdpAttributeEncodeResult encoded = SdpAttributeEncoderCore.EncodeAttribute(value);
        if (!encoded.Success || encoded.Attribute == null)
        {
            throw new ArgumentException("An effective attribute cannot be represented for semantic comparison.", "description");
        }
        encoded.Attribute.RawText = string.Empty;
        encoded.Attribute.Location = new SdpSourceLocation();
        attributes.Add(encoded.Attribute);
    }

    internal static SdpAttribute CreatePropertyAttribute(string name)
    {
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Property;
        attribute.Name = name;
        attribute.RawText = string.Empty;
        return attribute;
    }

    internal static SdpAttribute CreateValueAttribute(string name, string value)
    {
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = name;
        attribute.Value = value;
        attribute.RawText = string.Empty;
        return attribute;
    }

    internal static void CopyEncodedAttribute(SdpAttribute destination, SdpAttribute source)
    {
        destination.Kind = source.Kind;
        destination.Name = source.Name;
        destination.Value = source.Value;
        destination.RawText = source.RawText;
    }

    internal static void RemoveIgnoredAttributes(SdpSessionDescription description, List<string> ignoredNames)
    {
        RemoveNamedAttributes(description.Attributes, ignoredNames);
        RemoveIgnoredRawAttributeLines(description.RawLines, ignoredNames);
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            RemoveNamedAttributes(description.MediaDescriptions[mediaIndex].Attributes, ignoredNames);
            RemoveIgnoredRawAttributeLines(description.MediaDescriptions[mediaIndex].RawLines, ignoredNames);
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void RemoveIgnoredRawAttributeLines(List<SdpRawLine> lines, List<string> ignoredNames)
    {
        int index = lines.Count - 1;
        while (index >= 0)
        {
            SdpRawLine line = lines[index];
            if (line.FieldName == 'a')
            {
                string value = line.Value;
                int separator = value.IndexOf(':');
                string name = separator < 0 ? value : value.Substring(0, separator);
                int nameIndex = 0;
                bool remove = false;
                while (nameIndex < ignoredNames.Count && !remove)
                {
                    remove = string.Equals(name, ignoredNames[nameIndex], StringComparison.OrdinalIgnoreCase);
                    nameIndex = nameIndex + 1;
                }
                if (remove)
                {
                    lines.RemoveAt(index);
                }
            }
            index = index - 1;
        }
    }

    internal static void ClearOriginVersionFormatting(SdpSessionDescription description)
    {
        if (description.Origin != null)
        {
            description.Origin.RawText = string.Empty;
        }
        int index = 0;
        while (index < description.RawLines.Count)
        {
            if (description.RawLines[index].FieldName == 'o')
            {
                description.RawLines[index].Text = string.Empty;
                description.RawLines[index].Value = string.Empty;
            }
            index = index + 1;
        }
    }

    internal static void RemoveNamedAttributes(List<SdpAttribute> attributes, List<string> names)
    {
        int index = attributes.Count - 1;
        while (index >= 0)
        {
            int nameIndex = 0;
            bool remove = false;
            while (nameIndex < names.Count && !remove)
            {
                remove = string.Equals(attributes[index].Name, names[nameIndex], StringComparison.OrdinalIgnoreCase);
                nameIndex = nameIndex + 1;
            }
            if (remove)
            {
                attributes.RemoveAt(index);
            }
            index = index - 1;
        }
    }

    internal static void RemoveEffectiveAttributes(List<SdpAttribute> attributes)
    {
        int index = attributes.Count - 1;
        while (index >= 0)
        {
            if (IsEffectiveAttributeName(attributes[index].Name))
            {
                attributes.RemoveAt(index);
            }
            index = index - 1;
        }
    }

    internal static bool IsEffectiveAttributeName(string name)
    {
        SdpDirection direction = SdpCommonAttributeCodecCore.ReadDirection(name);
        return direction != SdpDirection.Unknown
            || string.Equals(name, SdpAttributeNames.Language, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, SdpAttributeNames.SessionDescriptionLanguage, StringComparison.OrdinalIgnoreCase)
            || SdpIceValidationCore.IsIceAttributeName(name)
            || string.Equals(name, SdpAttributeNames.Fingerprint, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, SdpAttributeNames.Setup, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, SdpAttributeNames.Connection, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, SdpAttributeNames.TlsId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, SdpAttributeNames.Crypto, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, SdpAttributeNames.ExtMap, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, SdpAttributeNames.SourceFilter, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, SdpAttributeNames.TimestampReferenceClock, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, SdpAttributeNames.MediaClock, StringComparison.OrdinalIgnoreCase);
    }

    internal static void ClearRawFormatting(SdpSessionDescription description)
    {
        if (description.Origin != null)
        {
            description.Origin.RawText = string.Empty;
            description.Origin.Location = new SdpSourceLocation();
        }
        if (description.Connection != null)
        {
            ClearConnectionFormatting(description.Connection);
        }
        ClearBandwidthFormatting(description.Bandwidths);
        ClearTimingFormatting(description.Timings);
        ClearZoneFormatting(description.TimeZoneAdjustments);
        if (description.EncryptionKey != null)
        {
            ClearEncryptionFormatting(description.EncryptionKey);
        }
        ClearAttributeFormatting(description.Attributes);
        description.RawLines.Clear();
        description.LineEnding = SdpLineEnding.Unknown;
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            int connectionIndex = 0;
            while (connectionIndex < media.Connections.Count)
            {
                ClearConnectionFormatting(media.Connections[connectionIndex]);
                connectionIndex = connectionIndex + 1;
            }
            ClearBandwidthFormatting(media.Bandwidths);
            if (media.EncryptionKey != null)
            {
                ClearEncryptionFormatting(media.EncryptionKey);
            }
            ClearAttributeFormatting(media.Attributes);
            media.RawLines.Clear();
            media.Location = new SdpSourceLocation();
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void ClearConnectionFormatting(SdpConnectionData connection)
    {
        connection.RawAddress = string.Empty;
        connection.RawText = string.Empty;
        connection.Location = new SdpSourceLocation();
    }

    internal static void ClearBandwidthFormatting(List<SdpBandwidth> bandwidths)
    {
        int index = 0;
        while (index < bandwidths.Count)
        {
            bandwidths[index].RawValue = string.Empty;
            bandwidths[index].RawText = string.Empty;
            bandwidths[index].Location = new SdpSourceLocation();
            index = index + 1;
        }
    }

    internal static void ClearTimingFormatting(List<SdpTiming> timings)
    {
        int timingIndex = 0;
        while (timingIndex < timings.Count)
        {
            SdpTiming timing = timings[timingIndex];
            timing.RawText = string.Empty;
            timing.Location = new SdpSourceLocation();
            int repeatIndex = 0;
            while (repeatIndex < timing.RepeatTimes.Count)
            {
                SdpRepeatTime repeat = timing.RepeatTimes[repeatIndex];
                repeat.RawText = string.Empty;
                repeat.Location = new SdpSourceLocation();
                repeat.RepeatInterval.RawValue = string.Empty;
                repeat.ActiveDuration.RawValue = string.Empty;
                int offsetIndex = 0;
                while (offsetIndex < repeat.Offsets.Count)
                {
                    repeat.Offsets[offsetIndex].RawValue = string.Empty;
                    offsetIndex = offsetIndex + 1;
                }
                repeatIndex = repeatIndex + 1;
            }
            timingIndex = timingIndex + 1;
        }
    }

    internal static void ClearZoneFormatting(List<SdpTimeZoneAdjustment> zones)
    {
        int index = 0;
        while (index < zones.Count)
        {
            zones[index].Offset.RawValue = string.Empty;
            zones[index].Location = new SdpSourceLocation();
            index = index + 1;
        }
    }

    internal static void ClearEncryptionFormatting(SdpEncryptionKey key)
    {
        key.RawText = string.Empty;
        key.Location = new SdpSourceLocation();
    }

    internal static void ClearAttributeFormatting(List<SdpAttribute> attributes)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            attributes[index].RawText = string.Empty;
            attributes[index].Location = new SdpSourceLocation();
            index = index + 1;
        }
    }

    internal static void SortInsignificantCollections(SdpSessionDescription description)
    {
        SortAttributeOrder(description.Attributes);
        SortBandwidthOrder(description.Bandwidths);
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SortAttributeOrder(description.MediaDescriptions[mediaIndex].Attributes);
            SortBandwidthOrder(description.MediaDescriptions[mediaIndex].Bandwidths);
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void SortAttributeOrder(List<SdpAttribute> attributes)
    {
        int index = 1;
        while (index < attributes.Count)
        {
            SdpAttribute current = attributes[index];
            int destination = index;
            while (destination > 0 && string.Compare(current.Name, attributes[destination - 1].Name, StringComparison.OrdinalIgnoreCase) < 0)
            {
                attributes[destination] = attributes[destination - 1];
                destination = destination - 1;
            }
            attributes[destination] = current;
            index = index + 1;
        }
    }

    internal static void SortBandwidthOrder(List<SdpBandwidth> bandwidths)
    {
        int index = 1;
        while (index < bandwidths.Count)
        {
            SdpBandwidth current = bandwidths[index];
            int destination = index;
            while (destination > 0 && string.Compare(current.Type, bandwidths[destination - 1].Type, StringComparison.OrdinalIgnoreCase) < 0)
            {
                bandwidths[destination] = bandwidths[destination - 1];
                destination = destination - 1;
            }
            bandwidths[destination] = current;
            index = index + 1;
        }
    }

    internal static void SortStringValues(List<string> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            string current = values[index];
            int destination = index;
            while (destination > 0 && string.Compare(current, values[destination - 1], StringComparison.OrdinalIgnoreCase) < 0)
            {
                values[destination] = values[destination - 1];
                destination = destination - 1;
            }
            values[destination] = current;
            index = index + 1;
        }
    }
}
