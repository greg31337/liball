using System.Collections.Generic;

namespace image;

internal class XmpParserState
{
    internal ImageMetadataDocument Document;
    internal List<MetadataDirectory> NamespaceDirectories;
    internal long Offset;
    internal long Length;
    internal string PathPrefix;
    internal string BlockType;
    internal int TagCount;
    internal int MaxTagCount;
    internal int MaxNestingDepth;
    internal bool TagLimitReported;
    internal bool DepthLimitReported;

    internal XmpParserState(ImageMetadataDocument document)
    {
        Document = document;
        NamespaceDirectories = new List<MetadataDirectory>();
        Offset = -1;
        Length = 0;
        PathPrefix = "XMP";
        BlockType = "XMP";
        TagCount = 0;
        MaxTagCount = 0;
        MaxNestingDepth = 0;
        TagLimitReported = false;
        DepthLimitReported = false;
    }
}
