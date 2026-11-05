using System;
using System.Collections.Generic;

namespace geo;

internal static class OffsetSegmentGenerator
{
    internal const int LeftSide = 1;
    internal const int RightSide = -1;

    internal static Coordinate[] CreateOffsetSegment(
        Coordinate start, Coordinate end, int side,
        double distance, PrecisionModel precision)
    {
        ValidateSideAndDistance(side, distance);
        ValidatePrecisionModel(precision);
        Coordinate[] raw = CreateRawOffsetSegment(
            start, end, side, distance);
        List<Coordinate> coordinates = new List<Coordinate>();
        AddFinalCoordinate(coordinates, raw[0], precision);
        AddFinalCoordinate(coordinates, raw[1], precision);
        return coordinates.ToArray();
    }

    internal static Coordinate[] CreateJoin(
        Coordinate previous, Coordinate vertex, Coordinate next,
        int side, double distance, BufferParameters parameters)
    {
        ValidateSideAndDistance(side, distance);
        ValidateBufferParameters(parameters);
        Coordinate[] incoming = CreateRawOffsetSegment(
            previous, vertex, side, distance);
        Coordinate[] outgoing = CreateRawOffsetSegment(
            vertex, next, side, distance);
        Coordinate incomingEnd = incoming[1];
        Coordinate outgoingStart = outgoing[0];
        int orientation = RobustOrientation.GetOrientationIndex(
            previous, vertex, next);
        List<Coordinate> coordinates = new List<Coordinate>();

        if (orientation == 0)
        {
            Coordinate incomingDirection =
                CreateUnitDirection(previous, vertex);
            Coordinate outgoingDirection =
                CreateUnitDirection(vertex, next);
            double dot = incomingDirection.X * outgoingDirection.X +
                incomingDirection.Y * outgoingDirection.Y;
            if (dot > 0.0)
            {
                AddFinalCoordinate(
                    coordinates, incomingEnd, parameters.Precision);
                return coordinates.ToArray();
            }
            if (parameters.JoinStyle == BufferJoinStyle.Round)
            {
                AddFilletCoordinates(
                    coordinates, vertex, incomingEnd, outgoingStart,
                    -side, distance, parameters);
            }
            else
            {
                AddBevelCoordinates(
                    coordinates, incomingEnd, outgoingStart,
                    parameters.Precision);
            }
            return coordinates.ToArray();
        }

        bool outsideTurn = orientation * side < 0;
        if (!outsideTurn)
        {
            Coordinate? intersection =
                RobustLineIntersection.GetInfiniteLineIntersection(
                    incoming[0], incoming[1], outgoing[0], outgoing[1]);
            if (intersection == null)
            {
                AddBevelCoordinates(
                    coordinates, incomingEnd, outgoingStart,
                    parameters.Precision);
            }
            else
            {
                intersection.Z = vertex.Z;
                intersection.M = vertex.M;
                AddFinalCoordinate(
                    coordinates, intersection, parameters.Precision);
            }
            return coordinates.ToArray();
        }

        switch (parameters.JoinStyle)
        {
            case BufferJoinStyle.Round:
                AddFilletCoordinates(
                    coordinates, vertex, incomingEnd, outgoingStart,
                    orientation, distance, parameters);
                break;
            case BufferJoinStyle.Mitre:
                AddMitreCoordinates(
                    coordinates, vertex, incoming, outgoing,
                    distance, parameters);
                break;
            case BufferJoinStyle.Bevel:
                AddBevelCoordinates(
                    coordinates, incomingEnd, outgoingStart,
                    parameters.Precision);
                break;
        }
        return coordinates.ToArray();
    }

    internal static Coordinate[] CreateEndCap(
        Coordinate adjacent, Coordinate endpoint,
        double distance, BufferParameters parameters)
    {
        ValidateSideAndDistance(LeftSide, distance);
        ValidateBufferParameters(parameters);
        Coordinate direction = CreateUnitDirection(adjacent, endpoint);
        Coordinate left = CreateOffsetPoint(
            endpoint, direction, LeftSide, distance);
        Coordinate right = CreateOffsetPoint(
            endpoint, direction, RightSide, distance);
        List<Coordinate> coordinates = new List<Coordinate>();
        switch (parameters.CapStyle)
        {
            case BufferCapStyle.Round:
                AddFilletCoordinates(
                    coordinates, endpoint, left, right,
                    -1, distance, parameters);
                break;
            case BufferCapStyle.Flat:
                AddBevelCoordinates(
                    coordinates, left, right, parameters.Precision);
                break;
            case BufferCapStyle.Square:
                AddFinalCoordinate(
                    coordinates, left, parameters.Precision);
                AddFinalCoordinate(
                    coordinates,
                    CreateProjectedCoordinate(
                        left, direction.X, direction.Y, distance),
                    parameters.Precision);
                AddFinalCoordinate(
                    coordinates,
                    CreateProjectedCoordinate(
                        right, direction.X, direction.Y, distance),
                    parameters.Precision);
                AddFinalCoordinate(
                    coordinates, right, parameters.Precision);
                break;
        }
        return coordinates.ToArray();
    }

    internal static Coordinate[] CreateCircle(
        Coordinate center, double distance, BufferParameters parameters)
    {
        ValidateSideAndDistance(LeftSide, distance);
        ValidateBufferParameters(parameters);
        VectorMath.ValidateCoordinateValue(center, "Circle center");
        List<Coordinate> raw = new List<Coordinate>();
        int segmentCount = parameters.QuadrantSegments * 4;
        int index = 0;
        while (index < segmentCount)
        {
            double angle = -2.0 * Math.PI * index / segmentCount;
            raw.Add(CreateRadialCoordinate(center, angle, distance));
            index++;
        }
        raw.Add(RobustLineIntersection.CopyCoordinate(raw[0]));
        return FinalizeCoordinates(raw, parameters.Precision);
    }

    internal static Coordinate[] CreateSquare(
        Coordinate center, double distance, BufferParameters parameters)
    {
        ValidateSideAndDistance(LeftSide, distance);
        ValidateBufferParameters(parameters);
        VectorMath.ValidateCoordinateValue(center, "Square center");
        List<Coordinate> raw = new List<Coordinate>();
        raw.Add(CreatePlanarCoordinateWithOrdinates(
            center.X + distance, center.Y + distance, center));
        raw.Add(CreatePlanarCoordinateWithOrdinates(
            center.X + distance, center.Y - distance, center));
        raw.Add(CreatePlanarCoordinateWithOrdinates(
            center.X - distance, center.Y - distance, center));
        raw.Add(CreatePlanarCoordinateWithOrdinates(
            center.X - distance, center.Y + distance, center));
        raw.Add(RobustLineIntersection.CopyCoordinate(raw[0]));
        return FinalizeCoordinates(raw, parameters.Precision);
    }

    internal static Coordinate[] CreateRawOffsetSegment(
        Coordinate start, Coordinate end, int side, double distance)
    {
        Coordinate direction = CreateUnitDirection(start, end);
        return
        [
            CreateOffsetPoint(start, direction, side, distance),
            CreateOffsetPoint(end, direction, side, distance)
        ];
    }

    internal static Coordinate CreateUnitDirection(
        Coordinate start, Coordinate end)
    {
        VectorMath.ValidateCoordinateValue(start, "Segment start");
        VectorMath.ValidateCoordinateValue(end, "Segment end");
        double deltaX = end.X - start.X;
        double deltaY = end.Y - start.Y;
        if (!double.IsFinite(deltaX) || !double.IsFinite(deltaY))
        {
            double scale = Math.Abs(start.X);
            double candidate = Math.Abs(start.Y);
            if (candidate > scale) scale = candidate;
            candidate = Math.Abs(end.X);
            if (candidate > scale) scale = candidate;
            candidate = Math.Abs(end.Y);
            if (candidate > scale) scale = candidate;
            deltaX = end.X / scale - start.X / scale;
            deltaY = end.Y / scale - start.Y / scale;
        }
        double absoluteX = Math.Abs(deltaX);
        double absoluteY = Math.Abs(deltaY);
        double directionScale = absoluteX > absoluteY ?
            absoluteX : absoluteY;
        if (directionScale == 0.0)
        {
            throw new InvalidGeometryException(
                "An offset segment must have distinct endpoints.");
        }
        double scaledX = deltaX / directionScale;
        double scaledY = deltaY / directionScale;
        double length = Math.Sqrt(
            scaledX * scaledX + scaledY * scaledY);
        Coordinate direction = new Coordinate();
        direction.X = scaledX / length;
        direction.Y = scaledY / length;
        return direction;
    }

    internal static Coordinate CreateOffsetPoint(
        Coordinate source, Coordinate direction,
        int side, double distance)
    {
        double normalX = -direction.Y * side;
        double normalY = direction.X * side;
        return CreateProjectedCoordinate(
            source, normalX, normalY, distance);
    }

    internal static Coordinate CreateProjectedCoordinate(
        Coordinate source, double directionX,
        double directionY, double distance)
    {
        double x = Math.FusedMultiplyAdd(
            distance, directionX, source.X);
        double y = Math.FusedMultiplyAdd(
            distance, directionY, source.Y);
        if (!double.IsFinite(x) || !double.IsFinite(y))
        {
            throw new InvalidGeometryException(
                "An offset coordinate is outside the finite double range.");
        }
        return CreatePlanarCoordinateWithOrdinates(x, y, source);
    }

    internal static Coordinate CreateRadialCoordinate(
        Coordinate center, double angle, double distance)
    {
        double directionX = SnapCardinalDirection(Math.Cos(angle));
        double directionY = SnapCardinalDirection(Math.Sin(angle));
        return CreateProjectedCoordinate(
            center, directionX, directionY, distance);
    }

    internal static double SnapCardinalDirection(double direction)
    {
        double tolerance = 0.000000000000001;
        if (Math.Abs(direction) < tolerance) return 0.0;
        if (Math.Abs(direction - 1.0) < tolerance) return 1.0;
        if (Math.Abs(direction + 1.0) < tolerance) return -1.0;
        return direction;
    }

    internal static Coordinate CreatePlanarCoordinate(double x, double y)
    {
        Coordinate coordinate = new Coordinate();
        coordinate.X = x;
        coordinate.Y = y;
        return coordinate;
    }

    internal static Coordinate CreatePlanarCoordinateWithOrdinates(
        double x, double y, Coordinate ordinateSource)
    {
        Coordinate coordinate = CreatePlanarCoordinate(x, y);
        coordinate.Z = ordinateSource.Z;
        coordinate.M = ordinateSource.M;
        return coordinate;
    }

    internal static void AddFilletCoordinates(
        List<Coordinate> coordinates, Coordinate center,
        Coordinate start, Coordinate end, int direction,
        double distance, BufferParameters parameters)
    {
        double startAngle = Math.Atan2(
            start.Y - center.Y, start.X - center.X);
        double endAngle = Math.Atan2(
            end.Y - center.Y, end.X - center.X);
        if (direction > 0)
        {
            while (endAngle <= startAngle) endAngle += 2.0 * Math.PI;
        }
        else
        {
            while (endAngle >= startAngle) endAngle -= 2.0 * Math.PI;
        }
        double sweep = endAngle - startAngle;
        double quantum = Math.PI /
            (2.0 * parameters.QuadrantSegments);
        int segmentCount = (int)Math.Ceiling(Math.Abs(sweep) / quantum);
        if (segmentCount < 1) segmentCount = 1;

        AddFinalCoordinate(coordinates, start, parameters.Precision);
        int index = 1;
        while (index < segmentCount)
        {
            double angle = startAngle + sweep * index / segmentCount;
            AddFinalCoordinate(
                coordinates,
                CreateRadialCoordinate(center, angle, distance),
                parameters.Precision);
            index++;
        }
        AddFinalCoordinate(coordinates, end, parameters.Precision);
    }

    internal static void AddMitreCoordinates(
        List<Coordinate> coordinates, Coordinate vertex,
        Coordinate[] incoming, Coordinate[] outgoing,
        double distance, BufferParameters parameters)
    {
        Coordinate incomingEnd = incoming[1];
        Coordinate outgoingStart = outgoing[0];
        Coordinate? intersection =
            RobustLineIntersection.GetInfiniteLineIntersection(
                incoming[0], incoming[1], outgoing[0], outgoing[1]);
        if (intersection == null ||
            GetDistanceRatio(vertex, intersection, distance) >
                parameters.MitreLimit)
        {
            AddBevelCoordinates(
                coordinates, incomingEnd, outgoingStart,
                parameters.Precision);
            return;
        }
        intersection.Z = vertex.Z;
        intersection.M = vertex.M;
        AddFinalCoordinate(
            coordinates, intersection, parameters.Precision);
    }

    internal static void AddBevelCoordinates(
        List<Coordinate> coordinates, Coordinate start,
        Coordinate end, PrecisionModel precision)
    {
        AddFinalCoordinate(coordinates, start, precision);
        AddFinalCoordinate(coordinates, end, precision);
    }

    internal static double GetDistanceRatio(
        Coordinate first, Coordinate second, double distance)
    {
        double deltaX = Math.Abs(second.X - first.X);
        double deltaY = Math.Abs(second.Y - first.Y);
        double scale = deltaX > deltaY ? deltaX : deltaY;
        if (scale == 0.0) return 0.0;
        if (!double.IsFinite(scale)) return double.PositiveInfinity;
        double scaledX = deltaX / scale;
        double scaledY = deltaY / scale;
        double length = scale * Math.Sqrt(
            scaledX * scaledX + scaledY * scaledY);
        return length / distance;
    }

    internal static Coordinate[] FinalizeCoordinates(
        List<Coordinate> raw, PrecisionModel precision)
    {
        List<Coordinate> result = new List<Coordinate>();
        int index = 0;
        while (index < raw.Count)
        {
            AddFinalCoordinate(result, raw[index], precision);
            index++;
        }
        return result.ToArray();
    }

    internal static void AddFinalCoordinate(
        List<Coordinate> coordinates, Coordinate raw,
        PrecisionModel precision)
    {
        Coordinate coordinate = ApplyPrecision(raw, precision);
        if (coordinates.Count != 0 &&
            RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[coordinates.Count - 1], coordinate))
        {
            return;
        }
        coordinates.Add(coordinate);
    }

    internal static Coordinate ApplyPrecision(
        Coordinate raw, PrecisionModel precision)
    {
        VectorMath.ValidateCoordinateValue(raw, "Offset coordinate");
        Coordinate result = CreatePlanarCoordinateWithOrdinates(
            raw.X, raw.Y, raw);
        switch (precision.Type)
        {
            case PrecisionModelType.Floating:
                break;
            case PrecisionModelType.FloatingSingle:
                result.X = (double)(float)result.X;
                result.Y = (double)(float)result.Y;
                if (!double.IsFinite(result.X) || !double.IsFinite(result.Y))
                {
                    throw new PrecisionException(
                        "Single precision produced a non-finite offset coordinate.");
                }
                break;
            case PrecisionModelType.Fixed:
                result.X = SnapRoundingNoder.RoundOrdinate(
                    result.X, precision.OffsetX, precision.Scale);
                result.Y = SnapRoundingNoder.RoundOrdinate(
                    result.Y, precision.OffsetY, precision.Scale);
                break;
        }
        return result;
    }

    internal static void ValidateSideAndDistance(
        int side, double distance)
    {
        if (side != LeftSide && side != RightSide)
        {
            throw new InvalidGeometryException(
                "An offset side must be left or right.");
        }
        if (!double.IsFinite(distance) || distance <= 0.0)
        {
            throw new InvalidGeometryException(
                "An offset distance must be finite and positive.");
        }
    }

    internal static void ValidateBufferParameters(BufferParameters parameters)
    {
        if (parameters == null)
        {
            throw new InvalidGeometryException(
                "Buffer parameters are null.");
        }
        switch (parameters.CapStyle)
        {
            case BufferCapStyle.Round:
            case BufferCapStyle.Flat:
            case BufferCapStyle.Square:
                break;
            default:
                throw new InvalidGeometryException(
                    "Buffer cap style is not supported.");
        }
        switch (parameters.JoinStyle)
        {
            case BufferJoinStyle.Round:
            case BufferJoinStyle.Mitre:
            case BufferJoinStyle.Bevel:
                break;
            default:
                throw new InvalidGeometryException(
                    "Buffer join style is not supported.");
        }
        if (parameters.QuadrantSegments < 1 ||
            parameters.QuadrantSegments >
                (GeometryConstants.DefaultMaximumCoordinates - 1) / 4)
        {
            throw new InvalidGeometryException(
                "Buffer quadrant segments are outside the supported range.");
        }
        if (!double.IsFinite(parameters.MitreLimit) ||
            parameters.MitreLimit <= 0.0)
        {
            throw new InvalidGeometryException(
                "Buffer mitre limit must be finite and positive.");
        }
        ValidatePrecisionModel(parameters.Precision);
    }

    internal static void ValidatePrecisionModel(PrecisionModel precision)
    {
        if (precision == null)
        {
            throw new PrecisionException("Buffer precision model is null.");
        }
        GeometryBuilder.ValidatePrecisionModelType(precision.Type);
        if (precision.Type == PrecisionModelType.Fixed)
        {
            SnapRoundingNoder.ValidateFixedPrecision(precision);
        }
    }
}
