using System.Collections.Generic;

namespace image;

internal class Jpeg2000Box
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
    internal bool UsesExtendedLength;
    internal bool ExtendsToEnd;
    internal bool TypeValid;
    internal bool DataLimited;
    internal byte[] Uuid;
    internal MetadataDirectory Directory;
    internal List<Jpeg2000Box> Children;

    internal Jpeg2000Box()
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
        UsesExtendedLength = false;
        ExtendsToEnd = false;
        TypeValid = false;
        DataLimited = false;
        Uuid = new byte[0];
        Directory = new MetadataDirectory();
        Children = new List<Jpeg2000Box>();
    }
}

internal class Jpeg2000TraversalState
{
    internal int BoxCount;
    internal long FileEnd;
    internal bool CountLimited;

    internal Jpeg2000TraversalState()
    {
        BoxCount = 0;
        FileEnd = 0;
        CountLimited = false;
    }
}
