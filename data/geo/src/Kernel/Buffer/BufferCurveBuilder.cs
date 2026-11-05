using System;
using System.Collections.Generic;

namespace geo;

internal static class BufferCurveBuilder
{
    internal static Geometry CreatePointBufferSurface(
        Point point, double distance, BufferParameters parameters,
        GeometryFactoryOptions options)
    {
        if (point == null || point.Coordinate == null)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.Polygon, options);
        }
        Coordinate[] curve;
        if (parameters.CapStyle == BufferCapStyle.Flat)
        {
            curve = [];
        }
        else if (parameters.CapStyle == BufferCapStyle.Square)
        {
            curve = OffsetSegmentGenerator.CreateSquare(
                point.Coordinate, distance, parameters);
        }
        else
        {
            curve = OffsetSegmentGenerator.CreateCircle(
                point.Coordinate, distance, parameters);
        }
        return CreatePolygonSurface(curve, [], options);
    }

    internal static Geometry CreateLineBufferSurface(
        LineString line, double distance, BufferParameters parameters,
        GeometryFactoryOptions options)
    {
        if (line == null || line.Coordinates == null)
        {
            throw new InvalidGeometryException(
                "Buffer line or coordinate sequence is null.");
        }
        Coordinate[] coordinates = CleanCoordinates(line.Coordinates);
        if (coordinates.Length == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.Polygon, options);
        }
        if (coordinates.Length == 1)
        {
            Point point = new Point();
            point.Coordinate = coordinates[0];
            return CreatePointBufferSurface(
                point, Math.Abs(distance), parameters, options);
        }
        bool closed = coordinates.Length > 3 &&
            RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[0], coordinates[coordinates.Length - 1]);
        if (closed)
        {
            if (parameters.SingleSided)
            {
                return CreateSingleSidedRingSurface(
                    coordinates, distance, parameters, options);
            }
            return CreateRingBandSurface(
                coordinates, Math.Abs(distance), parameters, options);
        }

        Coordinate[] curve;
        if (parameters.SingleSided)
        {
            curve = CreateSingleSidedLineCurve(
                coordinates, distance, parameters);
        }
        else
        {
            curve = CreateTwoSidedLineCurve(
                coordinates, distance, parameters);
        }
        return CreatePolygonSurface(curve, [], options);
    }

    internal static Geometry CreateRingBandSurface(
        Coordinate[] ring, double distance, BufferParameters parameters,
        GeometryFactoryOptions options)
    {
        Coordinate[] cleaned = CleanCoordinates(ring);
        if (cleaned.Length < 4 ||
            !RobustLineIntersection.CoordinatesEqualPlanar(
                cleaned[0], cleaned[cleaned.Length - 1]))
        {
            throw new InvalidGeometryException(
                "A buffer ring must be closed and contain at least three vertices.");
        }
        int sourceOrientation = RingMath.GetExactSignedAreaSign(cleaned);
        if (sourceOrientation == 0)
        {
            LineString line = new LineString();
            line.Coordinates = cleaned;
            BufferParameters copy = CopyParameters(parameters, false);
            return CreateLineBufferSurface(
                line, distance, copy, options);
        }
        int enclosedSide = sourceOrientation > 0 ?
            OffsetSegmentGenerator.LeftSide :
            OffsetSegmentGenerator.RightSide;
        Coordinate[] outer = CreateRingOffsetCurve(
            cleaned, -enclosedSide, distance, parameters);
        Coordinate[] inner = CreateRingOffsetCurve(
            cleaned, enclosedSide, distance, parameters);
        if (outer.Length < 4)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.Polygon, options);
        }
        LinearRing[] holes = [];
        if (inner.Length >= 4 &&
            RingMath.GetExactSignedAreaSign(inner) == sourceOrientation)
        {
            holes = [CreateRingPayload(inner)];
        }
        return CreatePolygonSurface(outer, holes, options);
    }

    internal static Coordinate[] CreateAreaOffsetRing(
        Coordinate[] ring, bool hole, double distance,
        BufferParameters parameters)
    {
        Coordinate[] cleaned = CleanCoordinates(ring);
        if (cleaned.Length < 4 ||
            !RobustLineIntersection.CoordinatesEqualPlanar(
                cleaned[0], cleaned[cleaned.Length - 1]))
        {
            throw new InvalidGeometryException(
                "A polygon buffer ring must be closed and non-empty.");
        }
        int sourceOrientation = RingMath.GetExactSignedAreaSign(cleaned);
        if (sourceOrientation == 0) return [];
        int enclosedSide = sourceOrientation > 0 ?
            OffsetSegmentGenerator.LeftSide :
            OffsetSegmentGenerator.RightSide;
        bool positive = distance > 0.0;
        int side;
        if (hole)
        {
            side = positive ? enclosedSide : -enclosedSide;
        }
        else
        {
            side = positive ? -enclosedSide : enclosedSide;
        }
        if (side == enclosedSide &&
            IsRingDefinitelyEroded(cleaned, Math.Abs(distance)))
        {
            return [];
        }
        Coordinate[] offset = CreateRingOffsetCurve(
            cleaned, side, Math.Abs(distance), parameters);
        if (offset.Length < 4 ||
            RingMath.GetExactSignedAreaSign(offset) != sourceOrientation)
        {
            return [];
        }
        return offset;
    }

    internal static bool IsRingDefinitelyEroded(
        Coordinate[] ring, double distance)
    {
        double minimumX = ring[0].X;
        double maximumX = ring[0].X;
        double minimumY = ring[0].Y;
        double maximumY = ring[0].Y;
        int index = 1;
        while (index < ring.Length)
        {
            if (ring[index].X < minimumX) minimumX = ring[index].X;
            if (ring[index].X > maximumX) maximumX = ring[index].X;
            if (ring[index].Y < minimumY) minimumY = ring[index].Y;
            if (ring[index].Y > maximumY) maximumY = ring[index].Y;
            index++;
        }
        double width = maximumX - minimumX;
        double height = maximumY - minimumY;
        double minimumSpan = width < height ? width : height;
        return distance >= minimumSpan / 2.0;
    }

    internal static double GetPlanarDistance(
        Coordinate first, Coordinate second)
    {
        double deltaX = Math.Abs(second.X - first.X);
        double deltaY = Math.Abs(second.Y - first.Y);
        double scale = deltaX > deltaY ? deltaX : deltaY;
        if (scale == 0.0) return 0.0;
        double scaledX = deltaX / scale;
        double scaledY = deltaY / scale;
        return scale * Math.Sqrt(
            scaledX * scaledX + scaledY * scaledY);
    }

    internal static Geometry CreateSingleSidedRingSurface(
        Coordinate[] ring, double distance, BufferParameters parameters,
        GeometryFactoryOptions options)
    {
        Coordinate[] cleaned = CleanCoordinates(ring);
        int side = distance >= 0.0 ?
            OffsetSegmentGenerator.LeftSide :
            OffsetSegmentGenerator.RightSide;
        Coordinate[] offset = CreateRingOffsetCurve(
            cleaned, side, Math.Abs(distance), parameters);
        if (offset.Length < 4)
        {
            return CreatePolygonSurface(cleaned, [], options);
        }
        double sourceArea = Math.Abs(RingMath.GetSignedArea(cleaned));
        double offsetArea = Math.Abs(RingMath.GetSignedArea(offset));
        Coordinate[] shell = sourceArea >= offsetArea ? cleaned : offset;
        Coordinate[] hole = sourceArea >= offsetArea ? offset : cleaned;
        LinearRing[] holes = [];
        if (RingMath.GetExactSignedAreaSign(hole) ==
            RingMath.GetExactSignedAreaSign(shell))
        {
            holes = [CreateRingPayload(hole)];
        }
        return CreatePolygonSurface(shell, holes, options);
    }

    internal static Coordinate[] CreateTwoSidedLineCurve(
        Coordinate[] coordinates, double distance,
        BufferParameters parameters)
    {
        List<Coordinate> curve = new List<Coordinate>();
        Coordinate[] forward = CreateSideCurve(
            coordinates, OffsetSegmentGenerator.LeftSide,
            distance, parameters);
        AppendCoordinates(curve, forward);
        Coordinate[] endCap = OffsetSegmentGenerator.CreateEndCap(
            coordinates[coordinates.Length - 2],
            coordinates[coordinates.Length - 1],
            distance, parameters);
        AppendCoordinates(curve, endCap);
        Coordinate[] reversed = ReverseCoordinates(coordinates);
        Coordinate[] backward = CreateSideCurve(
            reversed, OffsetSegmentGenerator.LeftSide,
            distance, parameters);
        AppendCoordinates(curve, backward);
        Coordinate[] startCap = OffsetSegmentGenerator.CreateEndCap(
            coordinates[1], coordinates[0], distance, parameters);
        AppendCoordinates(curve, startCap);
        CloseCoordinates(curve);
        return curve.ToArray();
    }

    internal static Coordinate[] CreateSingleSidedLineCurve(
        Coordinate[] coordinates, double distance,
        BufferParameters parameters)
    {
        double magnitude = Math.Abs(distance);
        int side = distance >= 0.0 ?
            OffsetSegmentGenerator.LeftSide :
            OffsetSegmentGenerator.RightSide;
        Coordinate[] offset = CreateSideCurve(
            coordinates, side, magnitude, parameters);
        List<Coordinate> curve = new List<Coordinate>();
        if (side == OffsetSegmentGenerator.LeftSide)
        {
            AppendCoordinates(curve, offset);
            AppendCoordinates(curve, ReverseCoordinates(coordinates));
        }
        else
        {
            AppendCoordinates(curve, coordinates);
            AppendCoordinates(curve, ReverseCoordinates(offset));
        }
        CloseCoordinates(curve);
        return curve.ToArray();
    }

    internal static Coordinate[] CreateSideCurve(
        Coordinate[] coordinates, int side, double distance,
        BufferParameters parameters)
    {
        List<Coordinate> curve = new List<Coordinate>();
        Coordinate[] first = OffsetSegmentGenerator.CreateOffsetSegment(
            coordinates[0], coordinates[1], side,
            distance, parameters.Precision);
        if (first.Length != 0)
        {
            AppendCoordinate(curve, first[0]);
        }
        int index = 1;
        while (index + 1 < coordinates.Length)
        {
            Coordinate[] join = OffsetSegmentGenerator.CreateJoin(
                coordinates[index - 1], coordinates[index],
                coordinates[index + 1], side, distance, parameters);
            AppendCoordinates(curve, join);
            index++;
        }
        Coordinate[] last = OffsetSegmentGenerator.CreateOffsetSegment(
            coordinates[coordinates.Length - 2],
            coordinates[coordinates.Length - 1], side,
            distance, parameters.Precision);
        if (last.Length != 0)
        {
            AppendCoordinate(curve, last[last.Length - 1]);
        }
        return curve.ToArray();
    }

    internal static Coordinate[] CreateRingOffsetCurve(
        Coordinate[] ring, int side, double distance,
        BufferParameters parameters)
    {
        int vertexCount = ring.Length - 1;
        List<Coordinate> curve = new List<Coordinate>();
        int index = 0;
        while (index < vertexCount)
        {
            int previousIndex = index == 0 ? vertexCount - 1 : index - 1;
            int nextIndex = index + 1 == vertexCount ? 0 : index + 1;
            Coordinate[] join = OffsetSegmentGenerator.CreateJoin(
                ring[previousIndex], ring[index], ring[nextIndex],
                side, distance, parameters);
            AppendCoordinates(curve, join);
            index++;
        }
        CloseCoordinates(curve);
        if (curve.Count < 4)
        {
            return [];
        }
        return curve.ToArray();
    }

    internal static Geometry CreatePolygonSurface(
        Coordinate[] shellCoordinates, LinearRing[] holes,
        GeometryFactoryOptions options)
    {
        if (shellCoordinates.Length < 4)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.Polygon, options);
        }
        LinearRing shell = CreateRingPayload(shellCoordinates);
        return GeometryBuilder.CreatePolygonValue(shell, holes, options);
    }

    internal static LinearRing CreateRingPayload(Coordinate[] coordinates)
    {
        LinearRing ring = new LinearRing();
        ring.Coordinates = CoordinateSequences.CopyCoordinateSequence(
            coordinates);
        return ring;
    }

    internal static Coordinate[] CleanCoordinates(Coordinate[] coordinates)
    {
        if (coordinates == null)
        {
            throw new InvalidGeometryException(
                "Buffer coordinate sequence is null.");
        }
        List<Coordinate> cleaned = new List<Coordinate>();
        int index = 0;
        while (index < coordinates.Length)
        {
            VectorMath.ValidateCoordinateValue(
                coordinates[index], "Buffer coordinate");
            AppendCoordinate(cleaned, coordinates[index]);
            index++;
        }
        return cleaned.ToArray();
    }

    internal static Coordinate[] ReverseCoordinates(Coordinate[] coordinates)
    {
        Coordinate[] reversed = new Coordinate[coordinates.Length];
        int index = 0;
        while (index < coordinates.Length)
        {
            reversed[index] = RobustLineIntersection.CopyCoordinate(
                coordinates[coordinates.Length - 1 - index]);
            index++;
        }
        return reversed;
    }

    internal static void AppendCoordinates(
        List<Coordinate> target, Coordinate[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            AppendCoordinate(target, source[index]);
            index++;
        }
    }

    internal static void AppendCoordinate(
        List<Coordinate> target, Coordinate coordinate)
    {
        if (target.Count != 0 &&
            RobustLineIntersection.CoordinatesEqualPlanar(
                target[target.Count - 1], coordinate))
        {
            return;
        }
        target.Add(RobustLineIntersection.CopyCoordinate(coordinate));
    }

    internal static void CloseCoordinates(List<Coordinate> coordinates)
    {
        if (coordinates.Count == 0) return;
        if (!RobustLineIntersection.CoordinatesEqualPlanar(
            coordinates[0], coordinates[coordinates.Count - 1]))
        {
            coordinates.Add(RobustLineIntersection.CopyCoordinate(
                coordinates[0]));
        }
    }

    internal static BufferParameters CopyParameters(
        BufferParameters source, bool singleSided)
    {
        BufferParameters copy = new BufferParameters();
        copy.CapStyle = source.CapStyle;
        copy.JoinStyle = source.JoinStyle;
        copy.QuadrantSegments = source.QuadrantSegments;
        copy.MitreLimit = source.MitreLimit;
        copy.SingleSided = singleSided;
        copy.Precision = source.Precision;
        return copy;
    }
}
