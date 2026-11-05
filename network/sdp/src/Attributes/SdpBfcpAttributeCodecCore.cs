using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpBfcpAttributeCodecCore
{
    internal static SdpBfcpDescription? ResolveMediaBfcp(SdpSessionDescription description, int mediaIndex)
    {
        SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
        bool reliableTransport = IsReliableBfcpTransport(media.Protocol);
        if (!reliableTransport && !IsUnreliableBfcpTransport(media.Protocol))
        {
            return null;
        }

        SdpBfcpDescription projection = new SdpBfcpDescription();
        projection.FloorControl = ReadFloorControl(media.Attributes);
        projection.ConferenceId = ReadDecimalAttribute(media.Attributes, SdpAttributeNames.ConferenceId);
        projection.UserId = ReadDecimalAttribute(media.Attributes, SdpAttributeNames.UserId);
        ReadFloorIds(media.Attributes, projection.FloorIds);
        ReadVersions(media.Attributes, projection.Versions);
        if (projection.Versions.Count == 0)
        {
            projection.Versions.Add(reliableTransport ? "1" : "2");
        }

        SdpDtlsDescription connection = SdpDtlsServiceCore.ResolveMediaDtls(description, mediaIndex);
        projection.SetupRole = connection.SetupRole;
        projection.SetupRoleToken = connection.SetupRoleToken;
        projection.ConnectionRole = connection.ConnectionRole;
        return projection;
    }

    internal static bool IsReliableBfcpTransport(string protocol)
    {
        return string.Equals(protocol, SdpTransportProtocols.TcpBfcp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocol, SdpTransportProtocols.TcpDtlsBfcp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocol, SdpTransportProtocols.TcpTlsBfcp, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsUnreliableBfcpTransport(string protocol)
    {
        return string.Equals(protocol, SdpTransportProtocols.UdpBfcp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(protocol, SdpTransportProtocols.UdpTlsBfcp, StringComparison.OrdinalIgnoreCase);
    }

    internal static string ReadFloorControl(List<SdpAttribute> attributes)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.FloorControl, StringComparison.OrdinalIgnoreCase) && attribute.Kind == SdpAttributeKind.Value)
            {
                string value = TrimHorizontalWhitespace(attribute.Value);
                List<string> roles = ReadTokens(value);
                if (roles.Count > 0 && AreFloorControlRoles(roles))
                {
                    return value;
                }
            }
            index = index + 1;
        }
        return string.Empty;
    }

    internal static string ReadDecimalAttribute(List<SdpAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase) && attribute.Kind == SdpAttributeKind.Value)
            {
                string value = TrimHorizontalWhitespace(attribute.Value);
                if (IsDecimal(value))
                {
                    return value;
                }
            }
            index = index + 1;
        }
        return string.Empty;
    }

    internal static void ReadFloorIds(List<SdpAttribute> attributes, List<string> floorIds)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.FloorId, StringComparison.OrdinalIgnoreCase) && attribute.Kind == SdpAttributeKind.Value)
            {
                string value = TrimHorizontalWhitespace(attribute.Value);
                if (IsFloorId(value))
                {
                    floorIds.Add(value);
                }
            }
            index = index + 1;
        }
    }

    internal static void ReadVersions(List<SdpAttribute> attributes, List<string> versions)
    {
        int index = 0;
        while (index < attributes.Count && versions.Count == 0)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, SdpAttributeNames.BfcpVersion, StringComparison.OrdinalIgnoreCase) && attribute.Kind == SdpAttributeKind.Value)
            {
                List<string> values = ReadTokens(TrimHorizontalWhitespace(attribute.Value));
                if (values.Count > 0 && AreDecimalValues(values))
                {
                    int valueIndex = 0;
                    while (valueIndex < values.Count)
                    {
                        versions.Add(values[valueIndex]);
                        valueIndex = valueIndex + 1;
                    }
                }
            }
            index = index + 1;
        }
    }

    internal static bool AreFloorControlRoles(List<string> roles)
    {
        int index = 0;
        while (index < roles.Count)
        {
            string role = roles[index];
            bool known = string.Equals(role, "c-only", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "s-only", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "c-s", StringComparison.OrdinalIgnoreCase);
            if (!known)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool AreDecimalValues(List<string> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (!IsDecimal(values[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsFloorId(string value)
    {
        List<string> values = ReadTokens(value);
        if (values.Count < 2 || !IsDecimal(values[0]))
        {
            return false;
        }
        string pointer = values[1];
        int prefixLength = 0;
        if (pointer.StartsWith("mstrm:", StringComparison.OrdinalIgnoreCase))
        {
            prefixLength = 6;
        }
        else if (pointer.StartsWith("m-stream:", StringComparison.OrdinalIgnoreCase))
        {
            prefixLength = 9;
        }
        if (prefixLength == 0 || pointer.Length == prefixLength || !SdpCommonAttributeCodecCore.IsTokenValue(pointer.Substring(prefixLength)))
        {
            return false;
        }
        int index = 2;
        while (index < values.Count)
        {
            if (!SdpCommonAttributeCodecCore.IsTokenValue(values[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsDecimal(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }
        int index = 0;
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

    internal static List<string> ReadTokens(string value)
    {
        List<string> result = new List<string>();
        int offset = 0;
        while (offset < value.Length)
        {
            while (offset < value.Length && IsHorizontalWhitespace(value[offset]))
            {
                offset = offset + 1;
            }
            int start = offset;
            while (offset < value.Length && !IsHorizontalWhitespace(value[offset]))
            {
                offset = offset + 1;
            }
            if (offset > start)
            {
                result.Add(value.Substring(start, offset - start));
            }
        }
        return result;
    }

    internal static string TrimHorizontalWhitespace(string value)
    {
        int start = 0;
        while (start < value.Length && IsHorizontalWhitespace(value[start]))
        {
            start = start + 1;
        }
        int end = value.Length;
        while (end > start && IsHorizontalWhitespace(value[end - 1]))
        {
            end = end - 1;
        }
        return value.Substring(start, end - start);
    }

    internal static bool IsHorizontalWhitespace(char character)
    {
        return character == ' ' || character == '\t';
    }
}
