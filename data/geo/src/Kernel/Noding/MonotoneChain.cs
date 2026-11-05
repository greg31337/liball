using System.Collections.Generic;

namespace geo;

internal class MonotoneChain
{
    internal SegmentString Source;
    internal int StartIndex;
    internal int EndIndex;
    internal int Id;
    internal Envelope Bounds;

    internal MonotoneChain(
        SegmentString source, int startIndex, int endIndex, int id)
    {
        if (source == null || startIndex < 0 || endIndex <= startIndex ||
            endIndex >= source.Coordinates.Length || id < 0)
        {
            throw new InvalidGeometryException("Monotone chain range or identifier is invalid.");
        }
        Source = source;
        StartIndex = startIndex;
        EndIndex = endIndex;
        Id = id;
        Bounds = CreateChainEnvelope(source, startIndex, endIndex);
    }

    internal static MonotoneChain[] CreateChains(
        SegmentString source, int firstIdentifier)
    {
        if (source == null || source.Coordinates == null || firstIdentifier < 0)
        {
            throw new InvalidGeometryException("Monotone chain source or identifier is invalid.");
        }
        List<MonotoneChain> chains = new List<MonotoneChain>();
        int start = 0;
        int identifier = firstIdentifier;
        while (start + 1 < source.Coordinates.Length)
        {
            int quadrant = GetSegmentQuadrant(source, start);
            int end = start + 1;
            while (end + 1 < source.Coordinates.Length &&
                GetSegmentQuadrant(source, end) == quadrant)
            {
                end++;
            }
            chains.Add(new MonotoneChain(source, start, end, identifier));
            identifier++;
            start = end;
        }
        return chains.ToArray();
    }

    internal static int GetSegmentQuadrant(SegmentString source, int segmentIndex)
    {
        Coordinate start = source.Coordinates[segmentIndex];
        Coordinate end = source.Coordinates[segmentIndex + 1];
        double directionX = end.X > start.X ? 1.0 : end.X < start.X ? -1.0 : 0.0;
        double directionY = end.Y > start.Y ? 1.0 : end.Y < start.Y ? -1.0 : 0.0;
        return VectorMath.GetQuadrant(directionX, directionY);
    }

    internal static Envelope CreateChainEnvelope(
        SegmentString source, int startIndex, int endIndex)
    {
        Coordinate first = source.Coordinates[startIndex];
        double minimumX = first.X;
        double minimumY = first.Y;
        double maximumX = first.X;
        double maximumY = first.Y;
        int index = startIndex + 1;
        while (index <= endIndex)
        {
            Coordinate coordinate = source.Coordinates[index];
            if (coordinate.X < minimumX) minimumX = coordinate.X;
            if (coordinate.Y < minimumY) minimumY = coordinate.Y;
            if (coordinate.X > maximumX) maximumX = coordinate.X;
            if (coordinate.Y > maximumY) maximumY = coordinate.Y;
            index++;
        }
        return EnvelopeAlgorithms.CreateBoundsEnvelope(
            minimumX, minimumY, maximumX, maximumY);
    }

    internal Envelope GetSegmentEnvelope(int segmentIndex)
    {
        if (segmentIndex < StartIndex || segmentIndex >= EndIndex)
        {
            throw new InvalidGeometryException("Segment index is outside the monotone chain.");
        }
        Coordinate start = Source.Coordinates[segmentIndex];
        Coordinate end = Source.Coordinates[segmentIndex + 1];
        return EnvelopeAlgorithms.CreateBoundsEnvelope(
            start.X < end.X ? start.X : end.X,
            start.Y < end.Y ? start.Y : end.Y,
            start.X > end.X ? start.X : end.X,
            start.Y > end.Y ? start.Y : end.Y);
    }
}
