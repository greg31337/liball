using System;

namespace geo;

internal class HotPixel
{
    internal Coordinate Center;
    internal double HalfSize;
    internal double MinimumX;
    internal double MinimumY;
    internal double MaximumX;
    internal double MaximumY;

    internal HotPixel(Coordinate center, double gridSize)
    {
        VectorMath.ValidateCoordinateValue(center, "Hot-pixel center");
        if (!double.IsFinite(gridSize) || gridSize <= 0.0)
        {
            throw new PrecisionException("Hot-pixel grid size must be finite and positive.");
        }
        Center = RobustLineIntersection.CopyCoordinate(center);
        HalfSize = gridSize * 0.5;
        MinimumX = Center.X - HalfSize;
        MinimumY = Center.Y - HalfSize;
        MaximumX = Center.X + HalfSize;
        MaximumY = Center.Y + HalfSize;
        if (!double.IsFinite(MinimumX) || !double.IsFinite(MinimumY) ||
            !double.IsFinite(MaximumX) || !double.IsFinite(MaximumY))
        {
            throw new PrecisionException("Hot-pixel bounds are outside the finite range.");
        }
    }

    internal bool IntersectsSegment(Coordinate start, Coordinate end)
    {
        VectorMath.ValidateCoordinateValue(start, "Hot-pixel segment start");
        VectorMath.ValidateCoordinateValue(end, "Hot-pixel segment end");
        if (Math.Min(start.X, end.X) > MaximumX ||
            Math.Max(start.X, end.X) < MinimumX ||
            Math.Min(start.Y, end.Y) > MaximumY ||
            Math.Max(start.Y, end.Y) < MinimumY) return false;
        if (ContainsCoordinate(start) || ContainsCoordinate(end)) return true;
        Coordinate lowerLeft = CreateCorner(MinimumX, MinimumY);
        Coordinate lowerRight = CreateCorner(MaximumX, MinimumY);
        Coordinate upperRight = CreateCorner(MaximumX, MaximumY);
        Coordinate upperLeft = CreateCorner(MinimumX, MaximumY);
        if (SegmentTouchesBoundary(start, end, lowerLeft, lowerRight)) return true;
        if (SegmentTouchesBoundary(start, end, lowerRight, upperRight)) return true;
        if (SegmentTouchesBoundary(start, end, upperRight, upperLeft)) return true;
        return SegmentTouchesBoundary(start, end, upperLeft, lowerLeft);
    }

    internal bool ContainsCoordinate(Coordinate coordinate)
    {
        return coordinate.X >= MinimumX && coordinate.X <= MaximumX &&
            coordinate.Y >= MinimumY && coordinate.Y <= MaximumY;
    }

    internal static bool SegmentTouchesBoundary(
        Coordinate start, Coordinate end,
        Coordinate boundaryStart, Coordinate boundaryEnd)
    {
        SegmentIntersectionResult intersection =
            RobustLineIntersection.GetSegmentIntersection(
                start, end, boundaryStart, boundaryEnd);
        return intersection.Type != SegmentIntersectionType.None;
    }

    internal static Coordinate CreateCorner(double x, double y)
    {
        Coordinate coordinate = new Coordinate();
        coordinate.X = x;
        coordinate.Y = y;
        return coordinate;
    }
}
