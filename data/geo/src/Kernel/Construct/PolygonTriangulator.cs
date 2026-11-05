using System;
using System.Collections.Generic;

namespace geo;

internal class PolygonTriangulationData
{
    internal List<Coordinate> Coordinates = new List<Coordinate>();
    internal List<TriangulationTriangle> Triangles =
        new List<TriangulationTriangle>();
    internal List<TriangulationEdge> Constraints =
        new List<TriangulationEdge>();
}

internal static class PolygonTriangulator
{
    internal static Geometry GetPolygonTriangulation(Geometry polygonal)
    {
        return TriangulatePolygonal(polygonal, false);
    }

    internal static Geometry TriangulatePolygonal(
        Geometry polygonal, bool improveDelaunay)
    {
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            polygonal,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        PolygonTriangulationData combined =
            new PolygonTriangulationData();
        int index = 0;
        while (index < leaves.Length)
        {
            Geometry leaf = leaves[index];
            if (leaf.GeometryType != GeometryType.Polygon ||
                leaf.Polygon == null)
            {
                bool emptyContainer =
                    (leaf.GeometryType == GeometryType.MultiPolygon ||
                        leaf.GeometryType == GeometryType.GeometryCollection) &&
                    GeometryInspection.IsEmptyValue(leaf);
                if (!emptyContainer)
                {
                    throw new InvalidGeometryException(
                        "Polygon triangulation requires polygonal geometry.");
                }
            }
            else
            {
                GeometryInspection.ExtractValidatedCoordinates(leaf);
                if (!GeometryInspection.IsEmptyValue(leaf))
                {
                    AddPolygonTriangulation(
                        leaf.Polygon, combined, improveDelaunay);
                }
            }
            index++;
        }
        DelaunayBuilder.SortTriangles(combined.Triangles);
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(polygonal);
        return DelaunayBuilder.CreateTinValue(
            combined.Coordinates.ToArray(), combined.Triangles, options);
    }

    internal static void AddPolygonTriangulation(
        Polygon polygon, PolygonTriangulationData combined,
        bool improveDelaunay)
    {
        PolygonTriangulationData local =
            CreatePolygonData(polygon);
        List<int> path = CreateRingPath(
            polygon.Shell, true, local);
        int holeIndex = 0;
        while (holeIndex < polygon.Holes.Length)
        {
            List<int> hole = CreateRingPath(
                polygon.Holes[holeIndex], false, local);
            path = BridgeHole(path, hole, polygon, local.Coordinates);
            holeIndex++;
        }
        EarClip(path, polygon, local);
        if (improveDelaunay)
        {
            ImproveConstrainedDelaunay(local);
        }

        int offset = combined.Coordinates.Count;
        int coordinateIndex = 0;
        while (coordinateIndex < local.Coordinates.Count)
        {
            combined.Coordinates.Add(local.Coordinates[coordinateIndex]);
            coordinateIndex++;
        }
        int triangleIndex = 0;
        while (triangleIndex < local.Triangles.Count)
        {
            TriangulationTriangle source = local.Triangles[triangleIndex];
            TriangulationTriangle triangle = new TriangulationTriangle();
            triangle.First = source.First + offset;
            triangle.Second = source.Second + offset;
            triangle.Third = source.Third + offset;
            combined.Triangles.Add(triangle);
            triangleIndex++;
        }
    }

    internal static PolygonTriangulationData CreatePolygonData(
        Polygon polygon)
    {
        if (polygon == null || polygon.Shell == null ||
            polygon.Holes == null)
        {
            throw new InvalidGeometryException(
                "Polygon triangulation payload is incomplete.");
        }
        PolygonTriangulationData data =
            new PolygonTriangulationData();
        AddRingCoordinates(polygon.Shell, data);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            AddRingCoordinates(polygon.Holes[index], data);
            index++;
        }
        return data;
    }

    internal static void AddRingCoordinates(
        LinearRing ring, PolygonTriangulationData data)
    {
        if (ring == null || ring.Coordinates == null)
        {
            throw new InvalidGeometryException(
                "Polygon triangulation ring is null.");
        }
        int count = GetOpenRingCount(ring.Coordinates);
        if (count < 3)
        {
            throw new InvalidGeometryException(
                "Polygon triangulation ring has fewer than three vertices.");
        }
        int first = -1;
        int previous = -1;
        int index = 0;
        while (index < count)
        {
            int vertex = AddUniqueCoordinate(
                data.Coordinates, ring.Coordinates[index]);
            if (first < 0) first = vertex;
            if (previous >= 0)
            {
                AddConstraint(data.Constraints, previous, vertex);
            }
            previous = vertex;
            index++;
        }
        AddConstraint(data.Constraints, previous, first);
    }

    internal static int AddUniqueCoordinate(
        List<Coordinate> coordinates, Coordinate coordinate)
    {
        int index = 0;
        while (index < coordinates.Count)
        {
            if (RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[index], coordinate))
            {
                return index;
            }
            index++;
        }
        coordinates.Add(coordinate);
        return coordinates.Count - 1;
    }

    internal static void AddConstraint(
        List<TriangulationEdge> constraints, int first, int second)
    {
        if (first == second) return;
        int minimum = first < second ? first : second;
        int maximum = first < second ? second : first;
        int index = 0;
        while (index < constraints.Count)
        {
            if (constraints[index].First == minimum &&
                constraints[index].Second == maximum)
            {
                return;
            }
            index++;
        }
        TriangulationEdge edge = new TriangulationEdge();
        edge.First = minimum;
        edge.Second = maximum;
        constraints.Add(edge);
    }

    internal static List<int> CreateRingPath(
        LinearRing ring, bool counterClockwise,
        PolygonTriangulationData data)
    {
        int count = GetOpenRingCount(ring.Coordinates);
        bool isCounterClockwise =
            RingMath.GetRingOrientation(ring.Coordinates) ==
                RingOrientation.CounterClockwise;
        List<int> path = new List<int>();
        int index = 0;
        while (index < count)
        {
            int sourceIndex = isCounterClockwise == counterClockwise ?
                index : count - 1 - index;
            path.Add(FindCoordinateIndex(
                data.Coordinates, ring.Coordinates[sourceIndex]));
            index++;
        }
        return path;
    }

    internal static int GetOpenRingCount(Coordinate[] coordinates)
    {
        if (coordinates == null) return 0;
        int count = coordinates.Length;
        if (count > 1 &&
            RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[0], coordinates[count - 1]))
        {
            count--;
        }
        return count;
    }

    internal static int FindCoordinateIndex(
        List<Coordinate> coordinates, Coordinate coordinate)
    {
        int index = 0;
        while (index < coordinates.Count)
        {
            if (RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[index], coordinate))
            {
                return index;
            }
            index++;
        }
        throw new InvalidGeometryException(
            "Polygon triangulation vertex was not registered.");
    }

    internal static List<int> BridgeHole(
        List<int> shell, List<int> hole, Polygon polygon,
        List<Coordinate> coordinates)
    {
        int bestShell = -1;
        int bestHole = -1;
        double bestDistance = double.PositiveInfinity;
        int shellIndex = 0;
        while (shellIndex < shell.Count)
        {
            int holeIndex = 0;
            while (holeIndex < hole.Count)
            {
                Coordinate shellCoordinate = coordinates[shell[shellIndex]];
                Coordinate holeCoordinate = coordinates[hole[holeIndex]];
                double distance =
                    SegmentDistanceAlgorithms.GetPlanarDistance(
                        shellCoordinate, holeCoordinate);
                if (distance < bestDistance && IsVisibleBridge(
                    shellCoordinate, holeCoordinate,
                    shell, polygon, coordinates))
                {
                    bestDistance = distance;
                    bestShell = shellIndex;
                    bestHole = holeIndex;
                }
                holeIndex++;
            }
            shellIndex++;
        }
        if (bestShell < 0)
        {
            throw new TopologyException(
                "A polygon hole could not be connected to its shell.");
        }

        List<int> merged = new List<int>();
        int index = 0;
        while (index <= bestShell)
        {
            merged.Add(shell[index]);
            index++;
        }
        index = 0;
        while (index < hole.Count)
        {
            merged.Add(hole[(bestHole + index) % hole.Count]);
            index++;
        }
        merged.Add(hole[bestHole]);
        merged.Add(shell[bestShell]);
        index = bestShell + 1;
        while (index < shell.Count)
        {
            merged.Add(shell[index]);
            index++;
        }
        return merged;
    }

    internal static bool IsVisibleBridge(
        Coordinate first, Coordinate second,
        List<int> currentPath, Polygon polygon,
        List<Coordinate> coordinates)
    {
        if (RobustLineIntersection.CoordinatesEqualPlanar(first, second))
        {
            return false;
        }
        Coordinate midpoint =
            RobustLineIntersection.InterpolateLineCoordinate(
                first, second, 0.5);
        if (GeometryPointLocation.LocatePointInPolygon(
            midpoint, polygon) != TopologicalLocation.Interior)
        {
            return false;
        }
        if (BridgeCrossesPath(
            first, second, currentPath, coordinates))
        {
            return false;
        }
        if (BridgeCrossesRing(first, second, polygon.Shell)) return false;
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            if (BridgeCrossesRing(
                first, second, polygon.Holes[index])) return false;
            index++;
        }
        return true;
    }

    internal static bool BridgeCrossesPath(
        Coordinate first, Coordinate second,
        List<int> path, List<Coordinate> coordinates)
    {
        int index = 0;
        while (index < path.Count)
        {
            Coordinate edgeStart = coordinates[path[index]];
            Coordinate edgeEnd = coordinates[path[(index + 1) % path.Count]];
            if (BridgeCrossesSegment(
                first, second, edgeStart, edgeEnd)) return true;
            index++;
        }
        return false;
    }

    internal static bool BridgeCrossesRing(
        Coordinate first, Coordinate second, LinearRing ring)
    {
        int count = GetOpenRingCount(ring.Coordinates);
        int index = 0;
        while (index < count)
        {
            if (BridgeCrossesSegment(
                first, second, ring.Coordinates[index],
                ring.Coordinates[(index + 1) % count])) return true;
            index++;
        }
        return false;
    }

    internal static bool BridgeCrossesSegment(
        Coordinate first, Coordinate second,
        Coordinate edgeStart, Coordinate edgeEnd)
    {
        if (RobustLineIntersection.CoordinatesEqualPlanar(first, edgeStart) ||
            RobustLineIntersection.CoordinatesEqualPlanar(first, edgeEnd) ||
            RobustLineIntersection.CoordinatesEqualPlanar(second, edgeStart) ||
            RobustLineIntersection.CoordinatesEqualPlanar(second, edgeEnd))
        {
            return false;
        }
        SegmentIntersectionResult intersection =
            RobustLineIntersection.GetSegmentIntersection(
                first, second, edgeStart, edgeEnd);
        return intersection.Type != SegmentIntersectionType.None;
    }

    internal static void EarClip(
        List<int> sourcePath, Polygon polygon,
        PolygonTriangulationData data)
    {
        List<int> path = new List<int>();
        int sourceIndex = 0;
        while (sourceIndex < sourcePath.Count)
        {
            path.Add(sourcePath[sourceIndex]);
            sourceIndex++;
        }
        int work = 0;
        while (path.Count > 3)
        {
            int earIndex = FindEar(path, polygon, data.Coordinates);
            if (earIndex < 0)
            {
                throw new TopologyException(
                    "Polygon triangulation could not find a valid ear.");
            }
            int previous = (earIndex + path.Count - 1) % path.Count;
            int next = (earIndex + 1) % path.Count;
            data.Triangles.Add(DelaunayBuilder.CreateCounterClockwiseTriangle(
                path[previous], path[earIndex], path[next],
                data.Coordinates.ToArray()));
            path.RemoveAt(earIndex);
            work++;
            if (work > GeometryConstants.DefaultMaximumCoordinates)
            {
                throw new InvalidGeometryException(
                    "Polygon triangulation exceeded the work limit.");
            }
        }
        if (path.Count == 3 && IsValidTriangle(
            path[0], path[1], path[2], polygon, data.Coordinates))
        {
            data.Triangles.Add(DelaunayBuilder.CreateCounterClockwiseTriangle(
                path[0], path[1], path[2], data.Coordinates.ToArray()));
        }
    }

    internal static int FindEar(
        List<int> path, Polygon polygon,
        List<Coordinate> coordinates)
    {
        int best = -1;
        int index = 0;
        while (index < path.Count)
        {
            int previous = (index + path.Count - 1) % path.Count;
            int next = (index + 1) % path.Count;
            if (IsValidTriangle(
                path[previous], path[index], path[next],
                polygon, coordinates) &&
                !ContainsOtherVertex(
                    path, previous, index, next, coordinates))
            {
                if (best < 0 ||
                    ConvexHullAlgorithm.CompareHullCoordinates(
                        coordinates[path[index]],
                        coordinates[path[best]]) < 0)
                {
                    best = index;
                }
            }
            index++;
        }
        return best;
    }

    internal static bool IsValidTriangle(
        int firstIndex, int secondIndex, int thirdIndex,
        Polygon polygon, List<Coordinate> coordinates)
    {
        Coordinate first = coordinates[firstIndex];
        Coordinate second = coordinates[secondIndex];
        Coordinate third = coordinates[thirdIndex];
        if (firstIndex == secondIndex || secondIndex == thirdIndex ||
            thirdIndex == firstIndex) return false;
        if (RobustOrientation.GetOrientationIndex(
            first, second, third) <= 0) return false;
        Coordinate firstMidpoint =
            RobustLineIntersection.InterpolateLineCoordinate(
                first, second, 0.5);
        Coordinate centroid =
            RobustLineIntersection.InterpolateLineCoordinate(
                firstMidpoint, third, 1.0 / 3.0);
        return GeometryPointLocation.LocatePointInPolygon(
            centroid, polygon) != TopologicalLocation.Exterior;
    }

    internal static bool ContainsOtherVertex(
        List<int> path, int firstPathIndex,
        int secondPathIndex, int thirdPathIndex,
        List<Coordinate> coordinates)
    {
        Coordinate first = coordinates[path[firstPathIndex]];
        Coordinate second = coordinates[path[secondPathIndex]];
        Coordinate third = coordinates[path[thirdPathIndex]];
        int index = 0;
        while (index < path.Count)
        {
            if (index != firstPathIndex && index != secondPathIndex &&
                index != thirdPathIndex)
            {
                Coordinate candidate = coordinates[path[index]];
                bool sameVertex =
                    RobustLineIntersection.CoordinatesEqualPlanar(
                        candidate, first) ||
                    RobustLineIntersection.CoordinatesEqualPlanar(
                        candidate, second) ||
                    RobustLineIntersection.CoordinatesEqualPlanar(
                        candidate, third);
                if (!sameVertex && PointInCounterClockwiseTriangle(
                    candidate, first, second, third)) return true;
            }
            index++;
        }
        return false;
    }

    internal static bool PointInCounterClockwiseTriangle(
        Coordinate point, Coordinate first,
        Coordinate second, Coordinate third)
    {
        return RobustOrientation.GetOrientationIndex(
            first, second, point) >= 0 &&
            RobustOrientation.GetOrientationIndex(
                second, third, point) >= 0 &&
            RobustOrientation.GetOrientationIndex(
                third, first, point) >= 0;
    }

    internal static void ImproveConstrainedDelaunay(
        PolygonTriangulationData data)
    {
        int work = 0;
        bool changed = true;
        while (changed)
        {
            changed = false;
            int firstIndex = 0;
            while (firstIndex < data.Triangles.Count && !changed)
            {
                int secondIndex = firstIndex + 1;
                while (secondIndex < data.Triangles.Count && !changed)
                {
                    if (TryFlipPair(
                        data.Triangles[firstIndex],
                        data.Triangles[secondIndex], data))
                    {
                        changed = true;
                    }
                    secondIndex++;
                }
                firstIndex++;
            }
            work++;
            if (work > data.Triangles.Count * data.Triangles.Count + 1)
            {
                throw new TopologyException(
                    "Constrained Delaunay edge flipping did not converge.");
            }
        }
    }

    internal static bool TryFlipPair(
        TriangulationTriangle first, TriangulationTriangle second,
        PolygonTriangulationData data)
    {
        int sharedFirst = -1;
        int sharedSecond = -1;
        int firstOpposite = -1;
        int secondOpposite = -1;
        int[] firstVertices = [first.First, first.Second, first.Third];
        int[] secondVertices = [second.First, second.Second, second.Third];
        int index = 0;
        while (index < firstVertices.Length)
        {
            if (ContainsIndex(secondVertices, firstVertices[index]))
            {
                if (sharedFirst < 0) sharedFirst = firstVertices[index];
                else sharedSecond = firstVertices[index];
            }
            else firstOpposite = firstVertices[index];
            index++;
        }
        index = 0;
        while (index < secondVertices.Length)
        {
            if (!ContainsIndex(firstVertices, secondVertices[index]))
            {
                secondOpposite = secondVertices[index];
            }
            index++;
        }
        if (sharedSecond < 0 || firstOpposite < 0 || secondOpposite < 0 ||
            IsConstraint(data.Constraints, sharedFirst, sharedSecond))
        {
            return false;
        }
        Coordinate[] coordinates = data.Coordinates.ToArray();
        int firstSide = RobustOrientation.GetOrientationIndex(
            coordinates[sharedFirst], coordinates[sharedSecond],
            coordinates[firstOpposite]);
        int secondSide = RobustOrientation.GetOrientationIndex(
            coordinates[sharedFirst], coordinates[sharedSecond],
            coordinates[secondOpposite]);
        if (firstSide == 0 || secondSide == 0 || firstSide == secondSide)
        {
            return false;
        }
        TriangulationTriangle oriented =
            DelaunayBuilder.CreateCounterClockwiseTriangle(
                sharedFirst, sharedSecond, firstOpposite, coordinates);
        if (DelaunayBuilder.GetCircumcircleSign(
            coordinates[oriented.First], coordinates[oriented.Second],
            coordinates[oriented.Third],
            coordinates[secondOpposite]) <= 0)
        {
            return false;
        }
        TriangulationTriangle replacementFirst =
            DelaunayBuilder.CreateCounterClockwiseTriangle(
                firstOpposite, secondOpposite,
                sharedFirst, coordinates);
        TriangulationTriangle replacementSecond =
            DelaunayBuilder.CreateCounterClockwiseTriangle(
                secondOpposite, firstOpposite,
                sharedSecond, coordinates);
        first.First = replacementFirst.First;
        first.Second = replacementFirst.Second;
        first.Third = replacementFirst.Third;
        second.First = replacementSecond.First;
        second.Second = replacementSecond.Second;
        second.Third = replacementSecond.Third;
        return true;
    }

    internal static bool ContainsIndex(int[] values, int value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == value) return true;
            index++;
        }
        return false;
    }

    internal static bool IsConstraint(
        List<TriangulationEdge> constraints, int first, int second)
    {
        int minimum = first < second ? first : second;
        int maximum = first < second ? second : first;
        int index = 0;
        while (index < constraints.Count)
        {
            if (constraints[index].First == minimum &&
                constraints[index].Second == maximum) return true;
            index++;
        }
        return false;
    }
}
