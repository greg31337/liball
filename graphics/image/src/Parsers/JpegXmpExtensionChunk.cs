namespace image;

internal class JpegXmpExtensionChunk
{
    internal string GuidText;
    internal long FullLength;
    internal long ChunkOffset;
    internal byte[] Data;
    internal long PayloadOffset;
    internal long SegmentOffset;

    internal JpegXmpExtensionChunk()
    {
        GuidText = "";
        FullLength = 0;
        ChunkOffset = 0;
        Data = new byte[0];
        PayloadOffset = -1;
        SegmentOffset = -1;
    }
}
