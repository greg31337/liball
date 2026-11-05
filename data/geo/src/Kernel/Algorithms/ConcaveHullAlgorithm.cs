using System;
using System.Collections.Generic;

namespace geo;

internal class ConcaveHullInsertion
{
    internal int EdgeIndex;
    internal Coordinate Coordinate = new Coordinate();
    internal double EdgeLength;
    internal double Cost;
}

internal static class ConcaveHullAlgorithm
{
    internal static Geometry GetConcaveHullByRatio(
        Geometry geometry, double targetRatio, bool allowHoles)
    {
        if (!double.IsFinite(targetRatio) ||
            targetRatio < 0.0 || targetRatio > 1.0)
        {
            throw new InvalidGeometryException(
                "Concave hull target ratio must be finite and between zero and one.");
        }
        Coordinate[] source =
            GeometryInspection.ExtractValidatedCoordinates(geometry);
        Coordinate[] sorted =
            ConvexHullAlgorithm.SortAndRemovePlanarDuplicates(source);
        if (sorted.Length < 3)
        {
            return ConvexHullAlgorithm.GetConvexHull(geometry);
        }
        double minimum = GetMinimumLocalSpacing(sorted);
        double maximum = GetCoordinateSpan(sorted);
        double threshold;
        if (targetRatio == 0.0)
        {
            threshold = minimum;
        }
        else if (targetRatio == 1.0)
        {
            threshold = maximum;
        }
        else
        {
            threshold = minimum * (1.0 - targetRatio) +
                maximum * targetRatio;
        }
        return CreateConcaveHull(
            geometry, sorted, threshold, allowHoles);
    }

    internal static Geometry GetConcaveHullByMaximumEdgeLength(
        Geometry geometry, double maximumEdgeLength, bool allowHoles)
    {
        if (!double.IsFinite(maximumEdgeLength) ||
            maximumEdgeLength <= 0.0)
        {
            throw new InvalidGeometryException(
                "Concave hull maximum edge length must be finite and positive.");
        }
        Coordinate[] source =
            GeometryInspection.ExtractValidatedCoordinates(geometry);
        Coordinate[] sorted =
            ConvexHullAlgorithm.SortAndRemovePlanarDuplicates(source);
        if (sorted.Length < 3)
        {
            return ConvexHullAlgorithm.GetConvexHull(geometry);
        }
        return CreateConcaveHull(
            geometry, sorted, maximumEdgeLength, allowHoles);
    }

    internal static Geometry CreateConcaveHull(
        Geometry geometry, Coordinate[] sorted,
        double maximumEdgeLength, bool allowHoles)
    {
        Coordinate[] initial =
            ConvexHullAlgorithm.BuildMonotoneHull(sorted);
        if (initial.Length < 3)
        {
            return ConvexHullAlgorithm.GetConvexHull(geometry);
        }
        List<Coordinate> shell = new List<Coordinate>();
        int index = 0;
        while (index < initial.Length)
        {
            shell.Add(initial[index]);
            index++;
        }

        while (true)
        {
            ConcaveHullInsertion? insertion = FindBestInsertion(
                shell, sorted, maximumEdgeLength);
            if (insertion == null) break;
            shell.Insert(
                insertion.EdgeIndex + 1, insertion.Coordinate);
        }

        LinearRing shellRing = CreateShellRing(shell);
        LinearRing[] holes = [];
        if (allowHoles)
        {
            LinearRing? hole = CreateOptionalHole(shell, sorted);
            if (hole != null) holes = [hole];
        }
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(geometry);
        return GeometryBuilder.CreatePolygonValue(
            shellRing, holes, options);
    }

    internal static ConcaveHullInsertion? FindBestInsertion(
        List<Coordinate> shell, Coordinate[] coordinates,
        double maximumEdgeLength)
    {
        ConcaveHullInsertion? best = null;
        int edgeIndex = 0;
        while (edgeIndex < shell.Count)
        {
            Coordinate start = shell[edgeIndex];
            Coordinate end = shell[(edgeIndex + 1) % shell.Count];
            double edgeLength =
                SegmentDistanceAlgorithms.GetPlanarDistance(start, end);
            if (edgeLength > maximumEdgeLength)
            {
                int coordinateIndex = 0;
                while (coordinateIndex < coordinates.Length)
                {
                    Coordinate candidate = coordinates[coordinateIndex];
                    if (!ContainsPlanarCoordinate(shell, candidate) &&
                        RobustOrientation.GetOrientationIndex(
                            start, end, candidate) > 0 &&
                        IsValidInsertion(
                            shell, coordinates, edgeIndex, candidate))
                    {
                        double cost = GetInsertionCost(
                            start, end, candidate);
                        if (IsBetterInsertion(
                            best, edgeIndex, candidate,
                            edgeLength, cost))
                        {
                            ConcaveHullInsertion value =
                                new ConcaveHullInsertion();
                            value.EdgeIndex = edgeIndex;
                            value.Coordinate = candidate;
                            value.EdgeLength = edgeLength;
                            value.Cost = cost;
                            best = value;
                        }
                    }
                    coordinateIndex++;
                }
            }
            edgeIndex++;
        }
        return best;
    }

    internal static bool IsBetterInsertion(
        ConcaveHullInsertion? best, int edgeIndex,
        Coordinate candidate, double edgeLength, double cost)
    {
        if (best == null) return true;
        if (edgeLength > best.EdgeLength) return true;
        if (edgeLength < best.EdgeLength) return false;
        if (cost < best.Cost) return true;
        if (cost > best.Cost) return false;
        int coordinateOrder =
            ConvexHullAlgorithm.CompareHullCoordinates(
                candidate, best.Coordinate);
        if (coordinateOrder < 0) return true;
        if (coordinateOrder > 0) return false;
        return edgeIndex < best.EdgeIndex;
    }

    internal static double GetInsertionCost(
        Coordinate start, Coordinate end, Coordinate candidate)
    {
        double first = SegmentDistanceAlgorithms.GetPlanarDistance(
            start, candidate);
        double second = SegmentDistanceAlgorithms.GetPlanarDistance(
            candidate, end);
        double original = SegmentDistanceAlgorithms.GetPlanarDistance(
            start, end);
        double cost = first + second - original;
        if (double.IsNaN(cost)) return double.PositiveInfinity;
        return cost;
    }

    internal static bool IsValidInsertion(
        List<Coordinate> shell, Coordinate[] allCoordinates,
        int edgeIndex, Coordinate candidate)
    {
        List<Coordinate> proposed = new List<Coordinate>();
        int index = 0;
        while (index < shell.Count)
        {
            proposed.Add(shell[index]);
            if (index == edgeIndex) proposed.Add(candidate);
            index++;
        }
        if (!IsSimpleRing(proposed)) return false;
        Coordinate[] ring = CreateClosedCoordinates(proposed);
        index = 0;
        while (index < allCoordinates.Length)
        {
            if (RingPointLocation.LocatePointByWinding(
                allCoordinates[index], ring) ==
                    TopologicalLocation.Exterior)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool IsSimpleRing(List<Coordinate> ring)
    {
        int edgeCount = ring.Count;
        int firstIndex = 0;
        while (firstIndex < edgeCount)
        {
            Coordinate firstStart = ring[firstIndex];
            Coordinate firstEnd = ring[(firstIndex + 1) % edgeCount];
            int secondIndex = firstIndex + 1;
            while (secondIndex < edgeCount)
            {
                bool adjacent = secondIndex == firstIndex + 1 ||
                    firstIndex == 0 && secondIndex == edgeCount - 1;
                if (!adjacent)
                {
                    Coordinate secondStart = ring[secondIndex];
                    Coordinate secondEnd =
                        ring[(secondIndex + 1) % edgeCount];
                    SegmentIntersectionResult intersection =
                        RobustLineIntersection.GetSegmentIntersection(
                            firstStart, firstEnd,
                            secondStart, secondEnd);
                    if (intersection.Type != SegmentIntersectionType.None)
                    {
                        return false;
                    }
                }
                secondIndex++;
            }
            firstIndex++;
        }
        return true;
    }

    internal static LinearRing? CreateOptionalHole(
        List<Coordinate> shell, Coordinate[] allCoordinates)
    {
        List<Coordinate> unused = new List<Coordinate>();
        int index = 0;
        while (index < allCoordinates.Length)
        {
            if (!ContainsPlanarCoordinate(shell, allCoordinates[index]))
            {
                unused.Add(allCoordinates[index]);
            }
            index++;
        }
        if (unused.Count < 3) return null;
        Coordinate[] sortedUnused =
            ConvexHullAlgorithm.SortAndRemovePlanarDuplicates(
                unused.ToArray());
        Coordinate[] holeHull =
            ConvexHullAlgorithm.BuildMonotoneHull(sortedUnused);
        if (holeHull.Length < 3 ||
            holeHull.Length != sortedUnused.Length)
        {
            return null;
        }
        Coordinate[] shellRing = CreateClosedCoordinates(shell);
        index = 0;
        while (index < holeHull.Length)
        {
            if (RingPointLocation.LocatePointByWinding(
                holeHull[index], shellRing) !=
                    TopologicalLocation.Interior)
            {
                return null;
            }
            index++;
        }
        if (!RingSegmentsAreInsideShell(holeHull, shellRing))
        {
            return null;
        }
        LinearRing hole = new LinearRing();
        hole.Coordinates = CreateClockwiseCoordinates(holeHull);
        return hole;
    }

    internal static bool RingSegmentsAreInsideShell(
        Coordinate[] hole, Coordinate[] shell)
    {
        int index = 0;
        while (index < hole.Length)
        {
            Coordinate midpoint =
                RobustLineIntersection.InterpolateLineCoordinate(
                    hole[index], hole[(index + 1) % hole.Length], 0.5);
            if (RingPointLocation.LocatePointByWinding(
                midpoint, shell) != TopologicalLocation.Interior)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool ContainsPlanarCoordinate(
        List<Coordinate> coordinates, Coordinate candidate)
    {
        int index = 0;
        while (index < coordinates.Count)
        {
            if (RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[index], candidate))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static double GetMinimumLocalSpacing(Coordinate[] sorted)
    {
        double minimum = double.PositiveInfinity;
        int index = 0;
        while (index + 1 < sorted.Length)
        {
            double distance = SegmentDistanceAlgorithms.GetPlanarDistance(
                sorted[index], sorted[index + 1]);
            if (distance < minimum) minimum = distance;
            index++;
        }
        return minimum;
    }

    internal static double GetCoordinateSpan(Coordinate[] sorted)
    {
        double minimumX = sorted[0].X;
        double maximumX = sorted[0].X;
        double minimumY = sorted[0].Y;
        double maximumY = sorted[0].Y;
        int index = 1;
        while (index < sorted.Length)
        {
            if (sorted[index].X < minimumX) minimumX = sorted[index].X;
            if (sorted[index].X > maximumX) maximumX = sorted[index].X;
            if (sorted[index].Y < minimumY) minimumY = sorted[index].Y;
            if (sorted[index].Y > maximumY) maximumY = sorted[index].Y;
            index++;
        }
        Coordinate minimum = new Coordinate();
        minimum.X = minimumX;
        minimum.Y = minimumY;
        Coordinate maximum = new Coordinate();
        maximum.X = maximumX;
        maximum.Y = maximumY;
        return SegmentDistanceAlgorithms.GetPlanarDistance(
            minimum, maximum);
    }

    internal static LinearRing CreateShellRing(List<Coordinate> shell)
    {
        LinearRing ring = new LinearRing();
        ring.Coordinates = CreateClosedCoordinates(shell);
        return ring;
    }

    internal static Coordinate[] CreateClosedCoordinates(
        List<Coordinate> coordinates)
    {
        Coordinate[] ring = new Coordinate[coordinates.Count + 1];
        int index = 0;
        while (index < coordinates.Count)
        {
            ring[index] = RobustLineIntersection.CopyCoordinate(
                coordinates[index]);
            index++;
        }
        ring[coordinates.Count] =
            RobustLineIntersection.CopyCoordinate(coordinates[0]);
        return ring;
    }

    internal static Coordinate[] CreateClockwiseCoordinates(
        Coordinate[] counterClockwise)
    {
        Coordinate[] ring = new Coordinate[counterClockwise.Length + 1];
        ring[0] = RobustLineIntersection.CopyCoordinate(
            counterClockwise[0]);
        int index = 1;
        while (index < counterClockwise.Length)
        {
            ring[index] = RobustLineIntersection.CopyCoordinate(
                counterClockwise[counterClockwise.Length - index]);
            index++;
        }
        ring[counterClockwise.Length] =
            RobustLineIntersection.CopyCoordinate(counterClockwise[0]);
        return ring;
    }
}
