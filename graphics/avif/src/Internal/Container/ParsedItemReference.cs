using System.Collections.Generic;

namespace avif;

internal enum ParsedItemReferenceKind
{
    Unknown = 0,
    Auxiliary = 1,
    Thumbnail = 2,
    DerivedImage = 3,
    Premultiplied = 4,
    ContentDescription = 5
}

internal class ParsedItemReference
{
    ParsedItemReferenceKind kind;
    string type;
    uint typeValue;
    uint fromItemId;
    List<uint> toItemIds;

    internal ParsedItemReference(
        ParsedItemReferenceKind kind,
        string type,
        uint typeValue,
        uint fromItemId,
        List<uint> toItemIds)
    {
        this.kind = kind;
        this.type = type;
        this.typeValue = typeValue;
        this.fromItemId = fromItemId;
        this.toItemIds = new List<uint>(toItemIds);
    }

    internal ParsedItemReferenceKind GetKind()
    {
        return kind;
    }

    internal string GetBoxType()
    {
        return type;
    }

    internal uint GetTypeValue()
    {
        return typeValue;
    }

    internal uint GetFromItemId()
    {
        return fromItemId;
    }

    internal int GetTargetCount()
    {
        return toItemIds.Count;
    }

    internal uint GetTargetItemId(int index)
    {
        if (index < 0 || index >= toItemIds.Count)
        {
            return 0;
        }

        return toItemIds[index];
    }
}
