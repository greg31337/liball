using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace sdp;

internal static class SdpRegistryLookup
{
    internal static FrozenSet<string> CreateSdpNameSet(string registryId, StringComparer comparer)
    {
        HashSet<string> result = new HashSet<string>(comparer);
        string[] lines = SdpRegistrySnapshot.SdpRecords.Split('\n');
        string prefix = registryId + "\t";
        int index = 0;
        while (index < lines.Length)
        {
            string line = lines[index];
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                string name = ReadFieldValue(line, "name");
                if (name.Length > 0)
                {
                    result.Add(name);
                }
            }
            index = index + 1;
        }
        return FrozenSet.ToFrozenSet(result, comparer);
    }

    internal static FrozenSet<string> CreateRtpEncodingSet()
    {
        HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] lines = SdpRegistrySnapshot.RtpRecords.Split('\n');
        string prefix = "rtp-parameters-2\t";
        int index = 0;
        while (index < lines.Length)
        {
            string line = lines[index];
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                string mediaType = ReadFieldValue(line, "type");
                string encodingName = ReadFieldValue(line, "subtype");
                if (mediaType.Length > 0 && encodingName.Length > 0)
                {
                    result.Add(mediaType + "\t" + encodingName);
                }
            }
            index = index + 1;
        }
        return FrozenSet.ToFrozenSet(result, StringComparer.OrdinalIgnoreCase);
    }

    internal static FrozenSet<string> CreateHeaderExtensionSet()
    {
        HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
        AddRtpNamesForRegistry(result, "rtp-parameters-10");
        AddRtpNamesForRegistry(result, "sdes-compact-header-extensions");
        return FrozenSet.ToFrozenSet(result, StringComparer.Ordinal);
    }

    internal static FrozenSet<int> CreateAssignedPayloadTypeSet()
    {
        HashSet<int> result = new HashSet<int>();
        string[] lines = SdpRegistrySnapshot.RtpRecords.Split('\n');
        string prefix = "rtp-parameters-1\t";
        int index = 0;
        while (index < lines.Length)
        {
            string line = lines[index];
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                string name = ReadFieldValue(line, "name");
                string value = ReadFieldValue(line, "value");
                int payloadType = ParseDecimal(value);
                if (payloadType >= 0 && name != "Reserved" && name != "Unassigned" && name != "dynamic")
                {
                    result.Add(payloadType);
                }
            }
            index = index + 1;
        }
        return FrozenSet.ToFrozenSet(result, EqualityComparer<int>.Default);
    }

    internal static string JoinRtpEncodingKey(string mediaType, string encodingName)
    {
        return mediaType + "\t" + encodingName;
    }

    internal static SdpRegisteredToken FindSdpNameRegistration(string registryId, string token, bool ignoreCase, bool obsolete)
    {
        string[] lines = SdpRegistrySnapshot.SdpRecords.Split('\n');
        string prefix = registryId + "\t";
        int index = 0;
        while (index < lines.Length)
        {
            string line = lines[index];
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                string name = ReadFieldValue(line, "name");
                if (TokenEquals(name, token, ignoreCase))
                {
                    return CreateTokenRegistration(registryId, name, line, obsolete);
                }
            }
            index = index + 1;
        }
        return CreateUnknownTokenRegistration(registryId, token);
    }

    internal static SdpAttributeRegistration FindAttributeRegistration(string name)
    {
        SdpAttributeRegistration result = new SdpAttributeRegistration();
        result.RegistryId = "sdp-att-field";
        result.Name = name;
        string[] lines = SdpRegistrySnapshot.SdpRecords.Split('\n');
        string prefix = "sdp-att-field\t";
        int index = 0;
        while (index < lines.Length)
        {
            string line = lines[index];
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                string registeredName = ReadFieldValue(line, "name");
                if (TokenEquals(registeredName, name, true))
                {
                    result.Found = true;
                    result.Name = registeredName;
                    result.ScopeText = ReadFieldValue(line, "usage");
                    AddScopes(result.Scopes, result.ScopeText);
                    result.MuxCategoryToken = ReadFieldValue(line, "mux");
                    result.MuxCategory = ReadMuxCategory(result.MuxCategoryToken);
                    bool obsolete = registeredName == "cat" || registeredName == "keywds";
                    result.Status = ReadStatus(line, obsolete);
                    AddReferenceValues(result.References, line);
                    return result;
                }
            }
            index = index + 1;
        }
        return result;
    }

    internal static SdpRegisteredToken FindPayloadTypeRegistration(int payloadType)
    {
        string query = payloadType.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string[] lines = SdpRegistrySnapshot.RtpRecords.Split('\n');
        string prefix = "rtp-parameters-1\t";
        int index = 0;
        while (index < lines.Length)
        {
            string line = lines[index];
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                string value = ReadFieldValue(line, "value");
                if (PayloadValueContains(value, payloadType))
                {
                    return CreateTokenRegistration("rtp-parameters-1", value, line, false);
                }
            }
            index = index + 1;
        }
        return CreateUnknownTokenRegistration("rtp-parameters-1", query);
    }

    internal static SdpRegisteredToken FindRtpEncodingRegistration(string mediaType, string encodingName)
    {
        string[] lines = SdpRegistrySnapshot.RtpRecords.Split('\n');
        string prefix = "rtp-parameters-2\t";
        int index = 0;
        while (index < lines.Length)
        {
            string line = lines[index];
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                string registeredMediaType = ReadFieldValue(line, "type");
                string registeredEncodingName = ReadFieldValue(line, "subtype");
                if (TokenEquals(registeredMediaType, mediaType, true) && TokenEquals(registeredEncodingName, encodingName, true))
                {
                    SdpRegisteredToken result = CreateTokenRegistration("rtp-parameters-2", registeredEncodingName, line, false);
                    result.Description = registeredMediaType;
                    return result;
                }
            }
            index = index + 1;
        }
        return CreateUnknownTokenRegistration("rtp-parameters-2", encodingName);
    }

    internal static SdpRegisteredToken FindHeaderExtensionRegistration(string uri)
    {
        string[] lines = SdpRegistrySnapshot.RtpRecords.Split('\n');
        int index = 0;
        while (index < lines.Length)
        {
            string line = lines[index];
            bool headerRegistry = line.StartsWith("rtp-parameters-10\t", StringComparison.Ordinal);
            bool sdesRegistry = line.StartsWith("sdes-compact-header-extensions\t", StringComparison.Ordinal);
            if (headerRegistry || sdesRegistry)
            {
                string name = ReadFieldValue(line, "name");
                if (name == uri)
                {
                    string registryId = headerRegistry ? "rtp-parameters-10" : "sdes-compact-header-extensions";
                    return CreateTokenRegistration(registryId, name, line, false);
                }
            }
            index = index + 1;
        }
        return CreateUnknownTokenRegistration("rtp-header-extensions", uri);
    }

    internal static string ReadFieldValue(string line, string fieldName)
    {
        string marker = "\t" + fieldName + "=";
        int start = line.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }
        start = start + marker.Length;
        int end = line.IndexOf('\t', start);
        if (end < 0)
        {
            end = line.Length;
        }
        return DecodeSnapshotValue(line.Substring(start, end - start));
    }

    internal static int ParseDecimal(string value)
    {
        if (value.Length == 0)
        {
            return -1;
        }
        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return -1;
            }
            result = result * 10 + character - '0';
            index = index + 1;
        }
        return result;
    }

    internal static void AddRtpNamesForRegistry(HashSet<string> result, string registryId)
    {
        string[] lines = SdpRegistrySnapshot.RtpRecords.Split('\n');
        string prefix = registryId + "\t";
        int index = 0;
        while (index < lines.Length)
        {
            string line = lines[index];
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                string name = ReadFieldValue(line, "name");
                if (name.Length > 0)
                {
                    result.Add(name);
                }
            }
            index = index + 1;
        }
    }

    internal static bool TokenEquals(string registeredToken, string query, bool ignoreCase)
    {
        if (ignoreCase)
        {
            return string.Equals(registeredToken, query, StringComparison.OrdinalIgnoreCase);
        }
        return string.Equals(registeredToken, query, StringComparison.Ordinal);
    }

    internal static SdpRegisteredToken CreateTokenRegistration(string registryId, string token, string line, bool obsolete)
    {
        SdpRegisteredToken result = new SdpRegisteredToken();
        result.Found = true;
        result.RegistryId = registryId;
        result.Token = token;
        result.Name = ReadFieldValue(line, "name");
        if (result.Name.Length == 0)
        {
            result.Name = ReadFieldValue(line, "subtype");
        }
        result.Description = ReadFieldValue(line, "description");
        if (result.Description.Length == 0)
        {
            result.Description = ReadFieldValue(line, "long_name");
        }
        result.MuxCategoryToken = ReadFieldValue(line, "mux");
        result.MuxCategory = ReadMuxCategory(result.MuxCategoryToken);
        result.Status = ReadStatus(line, obsolete);
        AddReferenceValues(result.References, line);
        return result;
    }

    internal static SdpRegisteredToken CreateUnknownTokenRegistration(string registryId, string token)
    {
        SdpRegisteredToken result = new SdpRegisteredToken();
        result.RegistryId = registryId;
        result.Token = token;
        result.Name = token;
        return result;
    }

    internal static string ReadStatus(string line, bool obsolete)
    {
        if (obsolete)
        {
            return "Obsolete";
        }
        string explicitStatus = ReadFieldValue(line, "status");
        if (explicitStatus.Length > 0)
        {
            return explicitStatus;
        }
        string name = ReadFieldValue(line, "name");
        string description = ReadFieldValue(line, "description");
        if (name == "Reserved" || description.IndexOf("Reserved", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Reserved";
        }
        if (name == "Unassigned" || description.IndexOf("Unassigned", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Unassigned";
        }
        if (name == "dynamic")
        {
            return "Dynamic";
        }
        if (description.IndexOf("Deprecated", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Deprecated";
        }
        return "Registered";
    }

    internal static void AddReferenceValues(List<string> references, string line)
    {
        string[] fields = line.Split('\t');
        int index = 1;
        while (index < fields.Length)
        {
            string field = fields[index];
            if (field.StartsWith("xref=", StringComparison.Ordinal))
            {
                references.Add(DecodeSnapshotValue(field.Substring(5)));
            }
            index = index + 1;
        }
    }

    internal static void AddScopes(List<SdpScope> scopes, string scopeText)
    {
        if (scopeText.IndexOf("session", StringComparison.Ordinal) >= 0)
        {
            scopes.Add(SdpScope.Session);
        }
        if (scopeText.IndexOf("media", StringComparison.Ordinal) >= 0)
        {
            scopes.Add(SdpScope.Media);
        }
        if (scopeText.IndexOf("source", StringComparison.Ordinal) >= 0)
        {
            scopes.Add(SdpScope.Source);
        }
    }

    internal static SdpMuxCategory ReadMuxCategory(string token)
    {
        switch (token)
        {
            case "NORMAL":
                return SdpMuxCategory.Normal;
            case "CAUTION":
                return SdpMuxCategory.Caution;
            case "IDENTICAL":
                return SdpMuxCategory.Identical;
            case "SUM":
                return SdpMuxCategory.Sum;
            case "TRANSPORT":
                return SdpMuxCategory.Transport;
            case "IDENTICAL-PER-PT":
                return SdpMuxCategory.IdenticalPerPayloadType;
            case "SPECIAL":
                return SdpMuxCategory.Special;
            default:
                return SdpMuxCategory.Unknown;
        }
    }

    internal static bool PayloadValueContains(string registeredValue, int payloadType)
    {
        int separator = registeredValue.IndexOf('-');
        if (separator < 0)
        {
            return ParseDecimal(registeredValue) == payloadType;
        }
        int minimum = ParseDecimal(registeredValue.Substring(0, separator));
        int maximum = ParseDecimal(registeredValue.Substring(separator + 1));
        return minimum >= 0 && maximum >= minimum && payloadType >= minimum && payloadType <= maximum;
    }

    internal static string DecodeSnapshotValue(string value)
    {
        return value.Replace("%09", "\t").Replace("%0D", "\r").Replace("%0A", "\n").Replace("%3D", "=").Replace("%25", "%");
    }
}
