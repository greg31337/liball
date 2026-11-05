namespace image;

internal class JpegMarkerSegment
{
    internal int Marker;
    internal string Name;
    internal long MarkerOffset;
    internal long SegmentLength;
    internal long DeclaredLength;
    internal long PayloadOffset;
    internal long PayloadLength;
    internal bool HasLength;
    internal bool IsStandalone;
    internal bool IsStartOfScan;
    internal bool IsEndOfImage;
    internal bool IsRestartMarker;
    internal bool IsTruncated;

    internal JpegMarkerSegment()
    {
        Marker = 0;
        Name = "";
        MarkerOffset = -1;
        SegmentLength = 0;
        DeclaredLength = 0;
        PayloadOffset = -1;
        PayloadLength = 0;
        HasLength = false;
        IsStandalone = false;
        IsStartOfScan = false;
        IsEndOfImage = false;
        IsRestartMarker = false;
        IsTruncated = false;
    }
}
