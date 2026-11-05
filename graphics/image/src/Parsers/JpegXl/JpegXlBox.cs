using System.Collections.Generic;

namespace image;

internal class JpegXlBox
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
    internal bool PartialIndexValid;
    internal uint PartialIndex;
    internal bool PartialIsLast;
    internal long CodestreamOffset;
    internal ulong CodestreamSize;
    internal string MajorBrand;
    internal uint MinorVersion;
    internal List<string> CompatibleBrands;
    internal MetadataDirectory Directory;
    internal List<JpegXlBox> Children;

    internal JpegXlBox()
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
        PartialIndexValid = false;
        PartialIndex = 0;
        PartialIsLast = false;
        CodestreamOffset = -1;
        CodestreamSize = 0;
        MajorBrand = "";
        MinorVersion = 0;
        CompatibleBrands = new List<string>();
        Directory = new MetadataDirectory();
        Children = new List<JpegXlBox>();
    }
}

internal class JpegXlTraversalState
{
    internal int BoxCount;
    internal long FileEnd;
    internal bool CountLimited;

    internal JpegXlTraversalState()
    {
        BoxCount = 0;
        FileEnd = 0;
        CountLimited = false;
    }
}
