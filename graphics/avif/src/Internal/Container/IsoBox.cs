using System.Collections.Generic;

namespace avif;

internal class IsoBox
{
    internal uint TypeValue;
    internal string Type;
    internal long Offset;
    internal long Size;
    internal int HeaderSize;
    internal long PayloadOffset;
    internal long PayloadSize;
    internal long EndOffset;
    internal bool UsesLargeSize;
    internal bool ExtendsToEnd;
    internal bool IsUuid;
    internal byte[] UserType;
    internal bool IsContainer;
    internal bool IsUnknown;
    internal bool HasFullBoxFields;
    internal int Version;
    internal int Flags;
    internal byte[] RawData;
    internal List<IsoBox> Children;

    internal IsoBox()
    {
        TypeValue = 0;
        Type = string.Empty;
        Offset = 0;
        Size = 0;
        HeaderSize = 0;
        PayloadOffset = 0;
        PayloadSize = 0;
        EndOffset = 0;
        UsesLargeSize = false;
        ExtendsToEnd = false;
        IsUuid = false;
        UserType = System.Array.Empty<byte>();
        IsContainer = false;
        IsUnknown = true;
        HasFullBoxFields = false;
        Version = 0;
        Flags = 0;
        RawData = System.Array.Empty<byte>();
        Children = new List<IsoBox>();
    }
}

internal class IsoBoxParseResult
{
    internal AvifResultCode Code;
    internal List<IsoBox> Boxes;
    internal ResourceGuard Resources;
    internal DiagnosticLog Diagnostics;

    internal IsoBoxParseResult(
        AvifResultCode code,
        List<IsoBox> boxes,
        ResourceGuard resources,
        DiagnosticLog diagnostics)
    {
        Code = code;
        Boxes = boxes;
        Resources = resources;
        Diagnostics = diagnostics;
    }
}

internal static class IsoBoxType
{
    internal const uint Uuid = 1970628964;

    internal static bool IsContainer(string type)
    {
        return type == "moov"
            || type == "trak"
            || type == "mdia"
            || type == "minf"
            || type == "stbl"
            || type == "edts"
            || type == "dinf"
            || type == "dref"
            || type == "mvex"
            || type == "tref"
            || type == "meta"
            || type == "iinf"
            || type == "iprp"
            || type == "ipco"
            || type == "iref"
            || type == "grpl"
            || type == "sinf"
            || type == "schi"
            || type == "meco"
            || type == "moof"
            || type == "traf"
            || type == "mfra"
            || type == "udta";
    }

    internal static bool IsFullBoxContainer(string type)
    {
        return type == "meta"
            || type == "iinf"
            || type == "iref"
            || type == "dref";
    }

    internal static bool IsValid(string? type)
    {
        if (type == null || type.Length != 4)
        {
            return false;
        }

        int index = 0;
        while (index < type.Length)
        {
            if (type[index] > 255)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static uint GetValue(string type)
    {
        if (!IsValid(type))
        {
            return 0;
        }

        return (uint)type[0] << 24
            | (uint)type[1] << 16
            | (uint)type[2] << 8
            | type[3];
    }
}
