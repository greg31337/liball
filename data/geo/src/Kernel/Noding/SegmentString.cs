namespace geo;

internal class SegmentString
{
    internal Coordinate[] Coordinates;
    internal object? SourceMetadata;
    internal int SourceIndex;
    internal int SourceStartSegment;
    internal int SourceEndSegment;
    internal SegmentNodeList Nodes;

    internal SegmentString(
        Coordinate[] coordinates, object? sourceMetadata, int sourceIndex)
    {
        Coordinates = SegmentNoding.CopyAndCleanCoordinates(coordinates);
        SourceMetadata = sourceMetadata;
        SourceIndex = sourceIndex;
        SourceEndSegment = Coordinates.Length - 2;
        Nodes = new SegmentNodeList(this);
    }
}
