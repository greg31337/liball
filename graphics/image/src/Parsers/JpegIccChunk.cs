namespace image;

internal class JpegIccChunk
{
    internal int SequenceNumber;
    internal int SequenceCount;
    internal byte[] Data;
    internal long PayloadOffset;
    internal long SegmentOffset;

    internal JpegIccChunk()
    {
        SequenceNumber = 0;
        SequenceCount = 0;
        Data = new byte[0];
        PayloadOffset = -1;
        SegmentOffset = -1;
    }
}
