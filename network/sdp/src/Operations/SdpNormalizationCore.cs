using System;
using System.Collections.Generic;
using System.Globalization;

namespace sdp;

internal static class SdpNormalizationCore
{
    internal static void NormalizeDescription(SdpSessionDescription description)
    {
        if (description.Origin != null)
        {
            NormalizeOrigin(description.Origin);
        }
        if (description.Connection != null)
        {
            NormalizeConnection(description.Connection);
        }
        NormalizeBandwidthList(description.Bandwidths);
        NormalizeTimingList(description.Timings);
        NormalizeZoneList(description.TimeZoneAdjustments);
        if (description.EncryptionKey != null)
        {
            NormalizeEncryptionKey(description.EncryptionKey);
        }
        NormalizeAttributeList(description.Attributes);
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            if (media == null)
            {
                throw new ArgumentException("Every media-description entry must be initialized.", "description");
            }
            NormalizeMedia(media);
            mediaIndex = mediaIndex + 1;
        }
        description.RawLines.Clear();
        description.LineEnding = SdpLineEnding.CrLf;
    }

    internal static void NormalizeOrigin(SdpOrigin origin)
    {
        origin.SessionId = NormalizeUnsignedDecimal(origin.SessionId);
        origin.SessionVersion = NormalizeUnsignedDecimal(origin.SessionVersion);
        origin.NetworkType = ReadCanonicalRegistryToken("sdp-parameters-4", origin.NetworkType, true);
        origin.AddressType = ReadCanonicalRegistryToken("sdp-parameters-5", origin.AddressType, true);
        origin.AddressKind = SdpSessionLineParser.ReadOriginAddressKind(origin.AddressType, origin.UnicastAddress);
        origin.RawText = string.Empty;
    }

    internal static void NormalizeConnection(SdpConnectionData connection)
    {
        connection.NetworkType = ReadCanonicalRegistryToken("sdp-parameters-4", connection.NetworkType, true);
        connection.AddressType = ReadCanonicalRegistryToken("sdp-parameters-5", connection.AddressType, true);
        connection.AddressKind = SdpRtcpAttributeCodecCore.ReadAddressKind(connection.AddressType, connection.Address);
        connection.RawAddress = string.Empty;
        connection.RawText = string.Empty;
    }

    internal static void NormalizeBandwidthList(List<SdpBandwidth> bandwidths)
    {
        if (bandwidths == null)
        {
            throw new ArgumentException("Every bandwidth collection must be initialized.", "description");
        }
        int index = 0;
        while (index < bandwidths.Count)
        {
            SdpBandwidth bandwidth = bandwidths[index];
            if (bandwidth == null)
            {
                throw new ArgumentException("Every bandwidth entry must be initialized.", "description");
            }
            bandwidth.Type = ReadCanonicalRegistryToken("sdp-parameters-3", bandwidth.Type, true);
            bandwidth.RawValue = string.Empty;
            bandwidth.RawText = string.Empty;
            index = index + 1;
        }
    }

    internal static void NormalizeTimingList(List<SdpTiming> timings)
    {
        if (timings == null)
        {
            throw new ArgumentException("Every timing collection must be initialized.", "description");
        }
        int timingIndex = 0;
        while (timingIndex < timings.Count)
        {
            SdpTiming timing = timings[timingIndex];
            if (timing == null || timing.RepeatTimes == null)
            {
                throw new ArgumentException("Every timing period and recurrence collection must be initialized.", "description");
            }
            timing.StartTime = NormalizeUnsignedDecimal(timing.StartTime);
            timing.StopTime = NormalizeUnsignedDecimal(timing.StopTime);
            timing.RawText = string.Empty;
            int repeatIndex = 0;
            while (repeatIndex < timing.RepeatTimes.Count)
            {
                SdpRepeatTime repeat = timing.RepeatTimes[repeatIndex];
                if (repeat == null || repeat.RepeatInterval == null || repeat.ActiveDuration == null || repeat.Offsets == null)
                {
                    throw new ArgumentException("Every recurrence value and offset collection must be initialized.", "description");
                }
                NormalizeTypedTimeValue(repeat.RepeatInterval);
                NormalizeTypedTimeValue(repeat.ActiveDuration);
                int offsetIndex = 0;
                while (offsetIndex < repeat.Offsets.Count)
                {
                    SdpTypedTime offset = repeat.Offsets[offsetIndex];
                    if (offset == null)
                    {
                        throw new ArgumentException("Every recurrence offset must be initialized.", "description");
                    }
                    NormalizeTypedTimeValue(offset);
                    offsetIndex = offsetIndex + 1;
                }
                repeat.RawText = string.Empty;
                repeatIndex = repeatIndex + 1;
            }
            timingIndex = timingIndex + 1;
        }
    }

    internal static void NormalizeZoneList(List<SdpTimeZoneAdjustment> adjustments)
    {
        if (adjustments == null)
        {
            throw new ArgumentException("Every time-zone adjustment collection must be initialized.", "description");
        }
        int index = 0;
        while (index < adjustments.Count)
        {
            SdpTimeZoneAdjustment adjustment = adjustments[index];
            if (adjustment == null || adjustment.Offset == null)
            {
                throw new ArgumentException("Every time-zone adjustment and offset must be initialized.", "description");
            }
            adjustment.AdjustmentTime = NormalizeUnsignedDecimal(adjustment.AdjustmentTime);
            NormalizeTypedTimeValue(adjustment.Offset);
            index = index + 1;
        }
    }

    internal static void NormalizeTypedTimeValue(SdpTypedTime value)
    {
        SdpTypedTimeNormalizationResult normalized = SdpTimingServiceCore.NormalizeTypedTime(value);
        if (!normalized.Success)
        {
            throw new ArgumentException("A typed-time value cannot be normalized to signed 64-bit seconds.", "description");
        }
        value.Value = normalized.Seconds;
        value.Unit = string.Empty;
        value.RawValue = normalized.Seconds.ToString(CultureInfo.InvariantCulture);
    }

    internal static void NormalizeEncryptionKey(SdpEncryptionKey key)
    {
        key.Method = ReadCanonicalRegistryToken("sdp-parameters-6", key.Method, true);
        key.RawText = string.Empty;
    }

    internal static void NormalizeAttributeList(List<SdpAttribute> attributes)
    {
        if (attributes == null)
        {
            throw new ArgumentException("Every attribute collection must be initialized.", "description");
        }
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (attribute == null)
            {
                throw new ArgumentException("Every attribute entry must be initialized.", "description");
            }
            NormalizeAttribute(attribute);
            index = index + 1;
        }
        RemoveDuplicateAttributes(attributes);
        SortAttributes(attributes);
    }

    internal static void NormalizeAttribute(SdpAttribute attribute)
    {
        if (attribute.Name == null || attribute.Value == null)
        {
            throw new ArgumentException("Every attribute name and value must be initialized.", "description");
        }
        SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
        if (decoded.Recognized)
        {
            if (!decoded.Success || decoded.TypedAttribute == null)
            {
                throw new ArgumentException("A recognized attribute contains invalid state that cannot be normalized.", "description");
            }
            SdpAttributeEncodeResult encoded = SdpAttributeEncoderCore.EncodeAttribute(decoded.TypedAttribute);
            if (!encoded.Success || encoded.Attribute == null)
            {
                throw new ArgumentException("A recognized attribute cannot be represented in canonical form.", "description");
            }
            attribute.Kind = encoded.Attribute.Kind;
            attribute.Name = encoded.Attribute.Name;
            attribute.Value = encoded.Attribute.Value;
            attribute.RawText = encoded.Attribute.RawText;
            return;
        }
        SdpAttributeRegistration registration = SdpRegistryLookup.FindAttributeRegistration(attribute.Name);
        if (registration.Found)
        {
            attribute.Name = registration.Name;
        }
        if (attribute.Kind == SdpAttributeKind.Property)
        {
            attribute.RawText = attribute.Name;
        }
        else if (attribute.Kind == SdpAttributeKind.Value)
        {
            attribute.RawText = attribute.Name + ":" + attribute.Value;
        }
        else
        {
            throw new ArgumentException("An attribute has an unknown property/value kind.", "description");
        }
    }

    internal static void RemoveDuplicateAttributes(List<SdpAttribute> attributes)
    {
        int index = attributes.Count - 1;
        while (index >= 0)
        {
            if (HasEarlierDuplicateAttribute(attributes, index))
            {
                attributes.RemoveAt(index);
            }
            index = index - 1;
        }
    }

    internal static bool HasEarlierDuplicateAttribute(List<SdpAttribute> attributes, int targetIndex)
    {
        SdpAttribute target = attributes[targetIndex];
        int index = 0;
        while (index < targetIndex)
        {
            SdpAttribute candidate = attributes[index];
            if (candidate.Kind == target.Kind && string.Equals(candidate.Name, target.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(candidate.Value, target.Value, StringComparison.Ordinal))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static void SortAttributes(List<SdpAttribute> attributes)
    {
        int index = 1;
        while (index < attributes.Count)
        {
            SdpAttribute current = attributes[index];
            int destination = index;
            while (destination > 0 && CompareAttributeNames(current, attributes[destination - 1]) < 0)
            {
                attributes[destination] = attributes[destination - 1];
                destination = destination - 1;
            }
            attributes[destination] = current;
            index = index + 1;
        }
    }

    internal static int CompareAttributeNames(SdpAttribute left, SdpAttribute right)
    {
        return string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
    }

    internal static void NormalizeMedia(SdpMediaDescription media)
    {
        if (media.Formats == null || media.Connections == null || media.Bandwidths == null || media.Attributes == null || media.RawLines == null)
        {
            throw new ArgumentException("Every media-description collection must be initialized.", "description");
        }
        media.MediaType = ReadCanonicalRegistryToken("sdp-parameters-1", media.MediaType, true);
        media.MediaKind = SdpMediaLineParser.ReadMediaKind(media.MediaType);
        media.Protocol = ReadCanonicalRegistryToken("sdp-parameters-2", media.Protocol, true);
        NormalizeFormatList(media);
        int connectionIndex = 0;
        while (connectionIndex < media.Connections.Count)
        {
            SdpConnectionData connection = media.Connections[connectionIndex];
            if (connection == null)
            {
                throw new ArgumentException("Every media connection must be initialized.", "description");
            }
            NormalizeConnection(connection);
            connectionIndex = connectionIndex + 1;
        }
        NormalizeBandwidthList(media.Bandwidths);
        if (media.EncryptionKey != null)
        {
            NormalizeEncryptionKey(media.EncryptionKey);
        }
        NormalizeAttributeList(media.Attributes);
        media.RawLines.Clear();
    }

    internal static void NormalizeFormatList(SdpMediaDescription media)
    {
        int index = 0;
        while (index < media.Formats.Count)
        {
            string format = media.Formats[index];
            if (format == null)
            {
                throw new ArgumentException("Every media format must be initialized.", "description");
            }
            if (SdpRtpValidationCore.IsRtpProtocol(media.Protocol))
            {
                SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(format.AsSpan());
                if (payload.Success)
                {
                    media.Formats[index] = payload.Int32Value.ToString(CultureInfo.InvariantCulture);
                }
            }
            index = index + 1;
        }
        index = media.Formats.Count - 1;
        while (index >= 0)
        {
            if (HasEarlierFormat(media.Formats, index))
            {
                media.Formats.RemoveAt(index);
            }
            index = index - 1;
        }
    }

    internal static bool HasEarlierFormat(List<string> formats, int targetIndex)
    {
        int index = 0;
        while (index < targetIndex)
        {
            if (string.Equals(formats[index], formats[targetIndex], StringComparison.Ordinal))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static string NormalizeUnsignedDecimal(string value)
    {
        if (value == null)
        {
            throw new ArgumentException("A required unsigned decimal value is uninitialized.", "description");
        }
        if (value.Length == 0)
        {
            throw new ArgumentException("A required unsigned decimal value cannot be normalized.", "description");
        }
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                throw new ArgumentException("A required unsigned decimal value cannot be normalized.", "description");
            }
            index = index + 1;
        }
        index = 0;
        while (index < value.Length - 1 && value[index] == '0')
        {
            index = index + 1;
        }
        if (index == 0)
        {
            return value;
        }
        return value.Substring(index);
    }

    internal static string ReadCanonicalRegistryToken(string registryId, string token, bool ignoreCase)
    {
        if (token == null)
        {
            throw new ArgumentException("A required token is uninitialized.", "description");
        }
        SdpRegisteredToken registration = SdpRegistryLookup.FindSdpNameRegistration(registryId, token, ignoreCase, false);
        if (registration.Found)
        {
            return registration.Token;
        }
        return token;
    }
}
