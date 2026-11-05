using System;

namespace geo;

internal class MinimumRectangleState
{
    internal Coordinate Origin = new Coordinate();
    internal Coordinate Direction = new Coordinate();
    internal double MinimumAlong;
    internal double MaximumAlong;
    internal double MinimumAcross;
    internal double MaximumAcross;
    internal double ProjectionScale = 1.0;
    internal double Area = double.PositiveInfinity;
    internal bool HasValue;
}

internal class MinimumBoundingCircleState
{
    internal Coordinate? Center;
    internal double Radius;
    internal Coordinate[] Witnesses = [];
}

internal static class MinimumShapes
{
    internal static Geometry GetMinimumRotatedRectangle(Geometry geometry)
    {
        Coordinate[] coordinates = GetUniqueCoordinates(geometry);
        if (coordinates.Length < 3)
        {
            return ConvexHullAlgorithm.GetConvexHull(geometry);
        }
        Coordinate[] hull =
            ConvexHullAlgorithm.BuildMonotoneHull(coordinates);
        if (hull.Length < 3)
        {
            return ConvexHullAlgorithm.GetConvexHull(geometry);
        }

        MinimumRectangleState state = new MinimumRectangleState();
        int edgeIndex = 0;
        while (edgeIndex < hull.Length)
        {
            Coordinate origin = hull[edgeIndex];
            Coordinate end = hull[(edgeIndex + 1) % hull.Length];
            Coordinate direction =
                OffsetSegmentGenerator.CreateUnitDirection(origin, end);
            EvaluateRectangleDirection(
                hull, origin, direction, state);
            edgeIndex++;
        }
        return CreateRectangleGeometry(geometry, hull, state);
    }

    internal static DistanceResult GetMinimumWidth(Geometry geometry)
    {
        Coordinate[] coordinates = GetUniqueCoordinates(geometry);
        if (coordinates.Length == 0) return new DistanceResult();
        if (coordinates.Length < 3)
        {
            return CreateZeroDistanceResult(coordinates[0]);
        }
        Coordinate[] hull =
            ConvexHullAlgorithm.BuildMonotoneHull(coordinates);
        if (hull.Length < 3)
        {
            return CreateZeroDistanceResult(hull[0]);
        }

        DistanceResult best = new DistanceResult();
        int edgeIndex = 0;
        while (edgeIndex < hull.Length)
        {
            Coordinate start = hull[edgeIndex];
            Coordinate end = hull[(edgeIndex + 1) % hull.Length];
            int farthestIndex = 0;
            double maximumDistance = -1.0;
            int pointIndex = 0;
            while (pointIndex < hull.Length)
            {
                double distance = GetPerpendicularDistance(
                    hull[pointIndex], start, end);
                if (distance > maximumDistance)
                {
                    maximumDistance = distance;
                    farthestIndex = pointIndex;
                }
                pointIndex++;
            }
            if (!best.HasResult || maximumDistance < best.Distance)
            {
                Coordinate farthest = hull[farthestIndex];
                double factor = VectorMath.GetProjectionFactor(
                    farthest, start, end);
                Coordinate foot =
                    RobustLineIntersection.InterpolateLineCoordinate(
                        start, end, factor);
                best.HasResult = true;
                best.Distance = maximumDistance;
                best.FirstCoordinate = foot;
                best.SecondCoordinate =
                    RobustLineIntersection.CopyCoordinate(farthest);
            }
            edgeIndex++;
        }
        return best;
    }

    internal static DistanceResult GetMinimumDiameter(Geometry geometry)
    {
        return GetMinimumWidth(geometry);
    }

    internal static DistanceResult GetMaximumDiameter(Geometry geometry)
    {
        Coordinate[] coordinates = GetUniqueCoordinates(geometry);
        if (coordinates.Length == 0) return new DistanceResult();
        if (coordinates.Length == 1)
        {
            return CreateZeroDistanceResult(coordinates[0]);
        }
        Coordinate[] hull =
            ConvexHullAlgorithm.BuildMonotoneHull(coordinates);
        Coordinate first = hull[0];
        Coordinate second = hull[1];
        ExactDyadicValue bestSquared =
            SegmentDistanceAlgorithms.CreateSquaredSegmentLength(
                first, second);
        int firstIndex = 0;
        while (firstIndex < hull.Length)
        {
            int secondIndex = firstIndex + 1;
            while (secondIndex < hull.Length)
            {
                ExactDyadicValue candidateSquared =
                    SegmentDistanceAlgorithms.CreateSquaredSegmentLength(
                        hull[firstIndex], hull[secondIndex]);
                if (ExactDyadicArithmetic.CompareAbsoluteValues(
                    candidateSquared, bestSquared) > 0)
                {
                    first = hull[firstIndex];
                    second = hull[secondIndex];
                    bestSquared = candidateSquared;
                }
                secondIndex++;
            }
            firstIndex++;
        }
        DistanceResult result = new DistanceResult();
        result.HasResult = true;
        result.Distance = SegmentDistanceAlgorithms.GetPlanarDistance(
            first, second);
        result.FirstCoordinate =
            RobustLineIntersection.CopyCoordinate(first);
        result.SecondCoordinate =
            RobustLineIntersection.CopyCoordinate(second);
        return result;
    }

    internal static CircleResult GetMinimumBoundingCircle(
        Geometry geometry)
    {
        Coordinate[] coordinates = GetUniqueCoordinates(geometry);
        MinimumBoundingCircleState state =
            new MinimumBoundingCircleState();
        int pointIndex = 0;
        while (pointIndex < coordinates.Length)
        {
            Coordinate point = coordinates[pointIndex];
            if (!CircleContainsPoint(state, point))
            {
                SetPointCircle(state, point);
                int secondIndex = 0;
                while (secondIndex < pointIndex)
                {
                    Coordinate second = coordinates[secondIndex];
                    if (!CircleContainsPoint(state, second))
                    {
                        SetDiameterCircle(state, point, second);
                        int thirdIndex = 0;
                        while (thirdIndex < secondIndex)
                        {
                            Coordinate third = coordinates[thirdIndex];
                            if (!CircleContainsPoint(state, third))
                            {
                                SetCircleThroughThreePoints(
                                    state, point, second, third);
                            }
                            thirdIndex++;
                        }
                    }
                    secondIndex++;
                }
            }
            pointIndex++;
        }
        return CreateCircleResult(geometry, state);
    }

    internal static Coordinate[] GetUniqueCoordinates(Geometry geometry)
    {
        Coordinate[] source =
            GeometryInspection.ExtractValidatedCoordinates(geometry);
        return ConvexHullAlgorithm.SortAndRemovePlanarDuplicates(source);
    }

    internal static void EvaluateRectangleDirection(
        Coordinate[] hull, Coordinate origin, Coordinate direction,
        MinimumRectangleState state)
    {
        double scale = GetMaximumAbsoluteCoordinateArray(hull);
        double originX = origin.X / scale;
        double originY = origin.Y / scale;
        double minimumAlong = 0.0;
        double maximumAlong = 0.0;
        double minimumAcross = 0.0;
        double maximumAcross = 0.0;
        int index = 0;
        while (index < hull.Length)
        {
            double deltaX = hull[index].X / scale - originX;
            double deltaY = hull[index].Y / scale - originY;
            double along = deltaX * direction.X +
                deltaY * direction.Y;
            double across = -deltaX * direction.Y +
                deltaY * direction.X;
            if (index == 0 || along < minimumAlong) minimumAlong = along;
            if (index == 0 || along > maximumAlong) maximumAlong = along;
            if (index == 0 || across < minimumAcross) minimumAcross = across;
            if (index == 0 || across > maximumAcross) maximumAcross = across;
            index++;
        }
        double area = (maximumAlong - minimumAlong) *
            (maximumAcross - minimumAcross);
        if (!state.HasValue || area < state.Area)
        {
            state.HasValue = true;
            state.Origin = origin;
            state.Direction = direction;
            state.MinimumAlong = minimumAlong;
            state.MaximumAlong = maximumAlong;
            state.MinimumAcross = minimumAcross;
            state.MaximumAcross = maximumAcross;
            state.ProjectionScale = scale;
            state.Area = area;
        }
    }

    internal static Geometry CreateRectangleGeometry(
        Geometry source, Coordinate[] hull,
        MinimumRectangleState state)
    {
        Coordinate ordinateSource = hull[0];
        Geometry rectangle = BuildRectangleGeometry(
            source, ordinateSource, state, 0.0);
        if (RectangleCoversCoordinates(rectangle, hull)) return rectangle;
        double padding = GetRectangleProjectionPadding(state);
        return BuildRectangleGeometry(
            source, ordinateSource, state, padding);
    }

    internal static Geometry BuildRectangleGeometry(
        Geometry source, Coordinate ordinateSource,
        MinimumRectangleState state, double padding)
    {
        Coordinate first = CreateRectangleCoordinate(
            state, state.MinimumAlong - padding,
            state.MinimumAcross - padding, ordinateSource);
        Coordinate second = CreateRectangleCoordinate(
            state, state.MaximumAlong + padding,
            state.MinimumAcross - padding, ordinateSource);
        Coordinate third = CreateRectangleCoordinate(
            state, state.MaximumAlong + padding,
            state.MaximumAcross + padding, ordinateSource);
        Coordinate fourth = CreateRectangleCoordinate(
            state, state.MinimumAlong - padding,
            state.MaximumAcross + padding, ordinateSource);
        LinearRing shell = new LinearRing();
        shell.Coordinates = [first, second, third, fourth,
            RobustLineIntersection.CopyCoordinate(first)];
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(source);
        Geometry rectangle = GeometryBuilder.CreatePolygonValue(
            shell, [], options);
        return GeometryNormalization.NormalizeGeometryValue(rectangle);
    }

    internal static bool RectangleCoversCoordinates(
        Geometry rectangle, Coordinate[] coordinates)
    {
        if (rectangle.Polygon == null) return false;
        int index = 0;
        while (index < coordinates.Length)
        {
            if (GeometryPointLocation.LocatePointInPolygon(
                coordinates[index], rectangle.Polygon) ==
                    TopologicalLocation.Exterior)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static double GetRectangleProjectionPadding(
        MinimumRectangleState state)
    {
        double magnitude = Math.Abs(state.MinimumAlong);
        double candidate = Math.Abs(state.MaximumAlong);
        if (candidate > magnitude) magnitude = candidate;
        candidate = Math.Abs(state.MinimumAcross);
        if (candidate > magnitude) magnitude = candidate;
        candidate = Math.Abs(state.MaximumAcross);
        if (candidate > magnitude) magnitude = candidate;
        if (magnitude < 1.0) magnitude = 1.0;
        return magnitude * 0.00000000000001;
    }

    internal static Coordinate CreateRectangleCoordinate(
        MinimumRectangleState state, double along, double across,
        Coordinate ordinateSource)
    {
        Coordinate coordinate = new Coordinate();
        coordinate.X = VectorMath.RequireFiniteResult(
            (state.Origin.X / state.ProjectionScale +
                state.Direction.X * along -
                state.Direction.Y * across) * state.ProjectionScale,
            "Minimum rectangle X ordinate");
        coordinate.Y = VectorMath.RequireFiniteResult(
            (state.Origin.Y / state.ProjectionScale +
                state.Direction.Y * along +
                state.Direction.X * across) * state.ProjectionScale,
            "Minimum rectangle Y ordinate");
        coordinate.Z = ordinateSource.Z;
        coordinate.M = ordinateSource.M;
        return coordinate;
    }

    internal static double GetPerpendicularDistance(
        Coordinate point, Coordinate start, Coordinate end)
    {
        Coordinate direction =
            OffsetSegmentGenerator.CreateUnitDirection(start, end);
        double scale = Math.Abs(point.X);
        double value = Math.Abs(point.Y);
        if (value > scale) scale = value;
        value = Math.Abs(start.X);
        if (value > scale) scale = value;
        value = Math.Abs(start.Y);
        if (value > scale) scale = value;
        if (scale == 0.0) return 0.0;
        double deltaX = point.X / scale - start.X / scale;
        double deltaY = point.Y / scale - start.Y / scale;
        return Math.Abs(-deltaX * direction.Y +
            deltaY * direction.X) * scale;
    }

    internal static DistanceResult CreateZeroDistanceResult(
        Coordinate coordinate)
    {
        DistanceResult result = new DistanceResult();
        result.HasResult = true;
        result.Distance = 0.0;
        result.FirstCoordinate =
            RobustLineIntersection.CopyCoordinate(coordinate);
        result.SecondCoordinate =
            RobustLineIntersection.CopyCoordinate(coordinate);
        return result;
    }

    internal static bool CircleContainsPoint(
        MinimumBoundingCircleState state, Coordinate point)
    {
        if (state.Center == null) return false;
        double distance = SegmentDistanceAlgorithms.GetPlanarDistance(
            state.Center, point);
        double nextRadius = Math.BitIncrement(state.Radius);
        double tolerance = Math.Abs(nextRadius - state.Radius) * 16.0;
        return distance <= state.Radius + tolerance;
    }

    internal static void SetPointCircle(
        MinimumBoundingCircleState state, Coordinate point)
    {
        state.Center = RobustLineIntersection.CopyCoordinate(point);
        state.Radius = 0.0;
        state.Witnesses =
            [RobustLineIntersection.CopyCoordinate(point)];
    }

    internal static void SetDiameterCircle(
        MinimumBoundingCircleState state,
        Coordinate first, Coordinate second)
    {
        state.Center = RobustLineIntersection.InterpolateLineCoordinate(
            first, second, 0.5);
        state.Radius = SegmentDistanceAlgorithms.GetPlanarDistance(
            state.Center, first);
        state.Witnesses =
        [
            RobustLineIntersection.CopyCoordinate(first),
            RobustLineIntersection.CopyCoordinate(second)
        ];
    }

    internal static void SetCircleThroughThreePoints(
        MinimumBoundingCircleState state,
        Coordinate first, Coordinate second, Coordinate third)
    {
        if (RobustOrientation.GetOrientationIndex(
            first, second, third) == 0)
        {
            SetFarthestPairCircle(state, first, second, third);
            return;
        }
        double scale = GetMaximumAbsoluteOrdinate(
            first, second, third);
        double firstX = first.X / scale;
        double firstY = first.Y / scale;
        double secondX = second.X / scale - firstX;
        double secondY = second.Y / scale - firstY;
        double thirdX = third.X / scale - firstX;
        double thirdY = third.Y / scale - firstY;
        double denominator = 2.0 *
            (secondX * thirdY - secondY * thirdX);
        double secondLength = secondX * secondX +
            secondY * secondY;
        double thirdLength = thirdX * thirdX +
            thirdY * thirdY;
        Coordinate center = new Coordinate();
        center.X = VectorMath.RequireFiniteResult(
            (firstX + (thirdY * secondLength -
                secondY * thirdLength) / denominator) * scale,
            "Minimum bounding circle center X ordinate");
        center.Y = VectorMath.RequireFiniteResult(
            (firstY + (secondX * thirdLength -
                thirdX * secondLength) / denominator) * scale,
            "Minimum bounding circle center Y ordinate");
        center.Z = first.Z;
        center.M = first.M;
        state.Center = center;
        state.Radius = SegmentDistanceAlgorithms.GetPlanarDistance(
            center, first);
        state.Witnesses =
        [
            RobustLineIntersection.CopyCoordinate(first),
            RobustLineIntersection.CopyCoordinate(second),
            RobustLineIntersection.CopyCoordinate(third)
        ];
    }

    internal static void SetFarthestPairCircle(
        MinimumBoundingCircleState state,
        Coordinate first, Coordinate second, Coordinate third)
    {
        Coordinate pairFirst = first;
        Coordinate pairSecond = second;
        ExactDyadicValue best =
            SegmentDistanceAlgorithms.CreateSquaredSegmentLength(
                first, second);
        ExactDyadicValue candidate =
            SegmentDistanceAlgorithms.CreateSquaredSegmentLength(
                first, third);
        if (ExactDyadicArithmetic.CompareAbsoluteValues(
            candidate, best) > 0)
        {
            pairSecond = third;
            best = candidate;
        }
        candidate = SegmentDistanceAlgorithms.CreateSquaredSegmentLength(
            second, third);
        if (ExactDyadicArithmetic.CompareAbsoluteValues(
            candidate, best) > 0)
        {
            pairFirst = second;
            pairSecond = third;
        }
        SetDiameterCircle(state, pairFirst, pairSecond);
    }

    internal static double GetMaximumAbsoluteOrdinate(
        Coordinate first, Coordinate second, Coordinate third)
    {
        double scale = Math.Abs(first.X);
        double value = Math.Abs(first.Y);
        if (value > scale) scale = value;
        value = Math.Abs(second.X);
        if (value > scale) scale = value;
        value = Math.Abs(second.Y);
        if (value > scale) scale = value;
        value = Math.Abs(third.X);
        if (value > scale) scale = value;
        value = Math.Abs(third.Y);
        if (value > scale) scale = value;
        return scale == 0.0 ? 1.0 : scale;
    }

    internal static double GetMaximumAbsoluteCoordinateArray(
        Coordinate[] coordinates)
    {
        double scale = 0.0;
        int index = 0;
        while (index < coordinates.Length)
        {
            double value = Math.Abs(coordinates[index].X);
            if (value > scale) scale = value;
            value = Math.Abs(coordinates[index].Y);
            if (value > scale) scale = value;
            index++;
        }
        return scale == 0.0 ? 1.0 : scale;
    }

    internal static CircleResult CreateCircleResult(
        Geometry source, MinimumBoundingCircleState state)
    {
        CircleResult result = new CircleResult();
        if (state.Center == null) return result;
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(source);
        result.Center =
            RobustLineIntersection.CopyCoordinate(state.Center);
        result.Radius = state.Radius;
        result.Witnesses = CoordinateSequences.CopyCoordinateSequence(
            state.Witnesses);
        if (state.Radius == 0.0)
        {
            result.Geometry = GeometryBuilder.CreatePointValue(
                state.Center, options);
        }
        else
        {
            result.Geometry = FactoryConstructions.CreateCircleValue(
                state.Center, state.Radius, 8, options);
        }
        return result;
    }
}
