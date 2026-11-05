using System.Collections.Generic;

namespace geo;

internal static class CollapsedEdgeCleaner
{
    internal static SegmentString[] CleanEdges(SegmentString[] segmentStrings)
    {
        if (segmentStrings == null)
        {
            throw new InvalidGeometryException("Collapsed-edge input array is null.");
        }
        List<SegmentString> cleaned = new List<SegmentString>();
        int index = 0;
        while (index < segmentStrings.Length)
        {
            SegmentString source = segmentStrings[index];
            if (source == null)
            {
                throw new InvalidGeometryException("Collapsed-edge input is null.");
            }
            Coordinate[] coordinates = RemoveCollapsedCoordinates(source.Coordinates);
            if (coordinates.Length >= 2)
            {
                SegmentString result = new SegmentString(
                    coordinates, source.SourceMetadata, source.SourceIndex);
                result.SourceStartSegment = source.SourceStartSegment;
                result.SourceEndSegment = source.SourceEndSegment;
                cleaned.Add(result);
            }
            index++;
        }
        return cleaned.ToArray();
    }

    internal static Coordinate[] RemoveCollapsedCoordinates(Coordinate[] coordinates)
    {
        if (coordinates == null)
        {
            throw new InvalidGeometryException("Collapsed-edge coordinate array is null.");
        }
        List<Coordinate> cleaned = new List<Coordinate>();
        int index = 0;
        while (index < coordinates.Length)
        {
            Coordinate coordinate = coordinates[index];
            VectorMath.ValidateCoordinateValue(coordinate, "Collapsed-edge coordinate");
            if (cleaned.Count == 0 || !RobustLineIntersection.CoordinatesEqualPlanar(
                cleaned[cleaned.Count - 1], coordinate))
            {
                cleaned.Add(RobustLineIntersection.CopyCoordinate(coordinate));
            }
            index++;
        }
        return cleaned.ToArray();
    }
}
