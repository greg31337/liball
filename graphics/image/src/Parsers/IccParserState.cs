namespace image;

internal class IccParserState
{
    internal ImageMetadataDocument Document;
    internal MetadataProfile Profile;
    internal byte[] Data;
    internal int ProfileLength;
    internal string PathPrefix;
    internal string BlockType;
    internal int TagCount;
    internal int MaxTagCount;
    internal bool TagLimitReported;

    internal IccParserState(ImageMetadataDocument document, MetadataProfile profile)
    {
        Document = document;
        Profile = profile;
        Data = new byte[0];
        ProfileLength = 0;
        PathPrefix = "ICC";
        BlockType = "ICC";
        TagCount = 0;
        MaxTagCount = 0;
        TagLimitReported = false;
    }
}
