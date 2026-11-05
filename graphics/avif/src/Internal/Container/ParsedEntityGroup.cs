using System.Collections.Generic;

namespace avif;

internal enum ParsedEntityGroupKind
{
    Unknown = 0,
    Alternate = 1,
    Stereo = 2
}

internal class ParsedEntityGroup
{
    ParsedEntityGroupKind kind;
    string groupingType;
    uint typeValue;
    int version;
    int flags;
    uint groupId;
    List<uint> entityIds;
    List<ParsedItem?> entityItems;
    byte[] rawData;

    internal ParsedEntityGroup(
        ParsedEntityGroupKind kind,
        string groupingType,
        uint typeValue,
        int version,
        int flags,
        uint groupId,
        List<uint> entityIds,
        List<ParsedItem?> entityItems,
        byte[] rawData)
    {
        this.kind = kind;
        this.groupingType = groupingType;
        this.typeValue = typeValue;
        this.version = version;
        this.flags = flags;
        this.groupId = groupId;
        this.entityIds = new List<uint>(entityIds);
        this.entityItems = new List<ParsedItem?>(entityItems);
        this.rawData = (byte[])rawData.Clone();
    }

    internal ParsedEntityGroupKind GetKind()
    {
        return kind;
    }

    internal string GetGroupingType()
    {
        return groupingType;
    }

    internal uint GetTypeValue()
    {
        return typeValue;
    }

    internal int GetVersion()
    {
        return version;
    }

    internal int GetFlags()
    {
        return flags;
    }

    internal uint GetGroupId()
    {
        return groupId;
    }

    internal int GetEntityCount()
    {
        return entityIds.Count;
    }

    internal uint GetEntityId(int index)
    {
        if (index < 0 || index >= entityIds.Count)
        {
            return 0;
        }

        return entityIds[index];
    }

    internal ParsedItem? GetEntityItem(int index)
    {
        if (index < 0 || index >= entityItems.Count)
        {
            return null;
        }

        return entityItems[index];
    }

    internal bool HasPreservedData()
    {
        return rawData.Length != 0;
    }

    internal byte[] GetRawData()
    {
        return (byte[])rawData.Clone();
    }

    internal ParsedItem? SelectFirstAvailableItem()
    {
        if (kind != ParsedEntityGroupKind.Alternate)
        {
            return null;
        }

        int index = 0;
        while (index < entityItems.Count)
        {
            if (entityItems[index] != null)
            {
                return entityItems[index];
            }

            index++;
        }

        return null;
    }

    internal ParsedItem? GetLeftItem()
    {
        if (kind != ParsedEntityGroupKind.Stereo || entityItems.Count != 2)
        {
            return null;
        }

        return entityItems[0];
    }

    internal ParsedItem? GetRightItem()
    {
        if (kind != ParsedEntityGroupKind.Stereo || entityItems.Count != 2)
        {
            return null;
        }

        return entityItems[1];
    }

    internal void ResolveItems(List<ParsedItem?> resolvedItems)
    {
        entityItems = new List<ParsedItem?>(resolvedItems);
    }
}
