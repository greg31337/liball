namespace avif;

internal enum ParsedDataReferenceKind
{
    Unknown = 0,
    Url = 1,
    Urn = 2
}

internal class ParsedDataReference
{
    int index;
    ParsedDataReferenceKind kind;
    string boxType;
    bool selfContained;
    string name;
    string location;

    internal ParsedDataReference(
        int index,
        ParsedDataReferenceKind kind,
        string boxType,
        bool selfContained,
        string name,
        string location)
    {
        this.index = index;
        this.kind = kind;
        this.boxType = boxType;
        this.selfContained = selfContained;
        this.name = name;
        this.location = location;
    }

    internal int GetIndex()
    {
        return index;
    }

    internal ParsedDataReferenceKind GetKind()
    {
        return kind;
    }

    internal string GetBoxType()
    {
        return boxType;
    }

    internal bool IsSelfContained()
    {
        return selfContained;
    }

    internal string GetName()
    {
        return name;
    }

    internal string GetLocation()
    {
        return location;
    }
}
