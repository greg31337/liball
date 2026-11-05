using System.Collections.Generic;

namespace image;

internal class BmffBox
{
    internal int Index;
    internal int SiblingIndex;
    internal string Type;
    internal byte[] TypeBytes;
    internal long Offset;
    internal ulong Size;
    internal int HeaderSize;
    internal long DataOffset;
    internal ulong DataSize;
    internal int Depth;
    internal bool UsesLargeSize;
    internal bool ExtendsToEnd;
    internal bool TypeValid;
    internal bool DataLimited;
    internal byte[] UserType;
    internal BmffBrandInfo? BrandInfo;
    internal List<BmffBox> Children;

    internal BmffBox()
    {
        Index = 0;
        SiblingIndex = 0;
        Type = "";
        TypeBytes = new byte[0];
        Offset = -1;
        Size = 0;
        HeaderSize = 0;
        DataOffset = -1;
        DataSize = 0;
        Depth = 0;
        UsesLargeSize = false;
        ExtendsToEnd = false;
        TypeValid = false;
        DataLimited = false;
        UserType = new byte[0];
        BrandInfo = null;
        Children = new List<BmffBox>();
    }
}

internal class BmffTraversalState
{
    internal int BoxCount;
    internal long FileEnd;
    internal bool CountLimited;

    internal BmffTraversalState()
    {
        BoxCount = 0;
        FileEnd = 0;
        CountLimited = false;
    }
}
