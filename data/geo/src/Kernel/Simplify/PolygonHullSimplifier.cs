using System;
using System.Collections.Generic;

namespace geo;

internal class PolygonHullRingState
{
    internal List<Coordinate> Coordinates = new List<Coordinate>();
    internal bool CounterClockwise;
    internal bool RemoveConvex;
    internal int TargetRemovals;
    internal int Removed;
}

internal class PolygonHullCandidate
{
    internal PolygonHullRingState Ring = new PolygonHullRingState();
    internal int VertexIndex;
    internal double Area;
}

internal static class PolygonHullSimplifier
{
    internal static Geometry SimplifyGeometry(
        Geometry geometry, SimplifyParameters parameters,
        bool outer)
    {
        if (parameters == null)
        {
            throw new InvalidGeometryException(
                "Simplification parameters are null.");
        }
        if (parameters.Algorithm != SimplificationAlgorithm.PolygonHull)
        {
            throw new InvalidGeometryException(
                "Polygon hull simplification requires its matching algorithm selection.");
        }
        return SimplifyPolygonHull(
            geometry, parameters.Tolerance, outer);
    }

    internal static Geometry SimplifyPolygonHull(
        Geometry geometry, double vertexRatio, bool outer)
    {
        ValidateHullRatio(vertexRatio);
        GeometryInspection.EnsureGeometryGraph(geometry);
        if (geometry.GeometryType == GeometryType.Polygon)
        {
            if (geometry.Polygon == null)
            {
                throw new InvalidGeometryException(
                    "Polygon hull input payload is null.");
            }
            return SimplifySinglePolygon(
                geometry.Polygon, geometry, vertexRatio, outer);
        }
        if (geometry.GeometryType == GeometryType.MultiPolygon)
        {
            return SimplifyMultiPolygonHull(
                geometry, vertexRatio, outer);
        }
        throw new InvalidGeometryException(
            "Polygon hull simplification requires Polygon or MultiPolygon input.");
    }

    internal static void ValidateHullRatio(double vertexRatio)
    {
        if (!double.IsFinite(vertexRatio) ||
            vertexRatio < 0.0 || vertexRatio > 1.0)
        {
            throw new InvalidGeometryException(
                "Polygon hull vertex ratio must be finite and between zero and one.");
        }
    }

    internal static Geometry SimplifySinglePolygon(
        Polygon polygon, Geometry metadataSource,
        double vertexRatio, bool outer)
    {
        if (polygon.Shell.Coordinates.Length == 0 ||
            vertexRatio == 0.0)
        {
            return GeometryCopy.CopyGeometry(metadataSource);
        }
        List<PolygonHullRingState> rings =
            CreateRingStates(polygon, vertexRatio, outer);
        int ringIndex = 0;
        while (ringIndex < rings.Count)
        {
            SimplifyHullRing(
                rings[ringIndex], rings, metadataSource, outer);
            ringIndex++;
        }
        Geometry result = CreateStateGeometry(rings, metadataSource);
        bool contains = outer ?
            GeometryPredicateAlgorithms.Covers(result, metadataSource) :
            GeometryPredicateAlgorithms.Covers(metadataSource, result);
        if (!contains) return GeometryCopy.CopyGeometry(metadataSource);
        return GeometryNormalization.NormalizeGeometryValue(result);
    }

    internal static Geometry SimplifyMultiPolygonHull(
        Geometry geometry, double vertexRatio, bool outer)
    {
        if (geometry.MultiPolygon == null)
        {
            throw new InvalidGeometryException(
                "MultiPolygon hull input payload is null.");
        }
        MultiPolygon source = geometry.MultiPolygon;
        Polygon[] polygons = new Polygon[source.Polygons.Length];
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(geometry);
        int index = 0;
        while (index < polygons.Length)
        {
            Geometry component = GeometryBuilder.CreatePolygonValue(
                source.Polygons[index].Shell,
                source.Polygons[index].Holes, options);
            Geometry simplified = SimplifySinglePolygon(
                source.Polygons[index], component,
                vertexRatio, outer);
            if (simplified.Polygon == null)
            {
                throw new InvalidGeometryException(
                    "Polygon hull component result is not polygonal.");
            }
            polygons[index] = simplified.Polygon;
            index++;
        }
        Geometry result = GeometryBuilder.CreateMultiPolygonValue(
            polygons, options);
        bool contains = outer ?
            GeometryPredicateAlgorithms.Covers(result, geometry) :
            GeometryPredicateAlgorithms.Covers(geometry, result);
        if (!contains || !MultiPolygonComponentsAreDisjoint(
            polygons, options))
        {
            return GeometryCopy.CopyGeometry(geometry);
        }
        return GeometryNormalization.NormalizeGeometryValue(result);
    }

    internal static List<PolygonHullRingState> CreateRingStates(
        Polygon polygon, double vertexRatio, bool outer)
    {
        List<PolygonHullRingState> rings =
            new List<PolygonHullRingState>();
        rings.Add(CreateRingState(
            polygon.Shell.Coordinates, vertexRatio,
            outer, false));
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            rings.Add(CreateRingState(
                polygon.Holes[index].Coordinates, vertexRatio,
                outer, true));
            index++;
        }
        return rings;
    }

    internal static PolygonHullRingState CreateRingState(
        Coordinate[] source, double vertexRatio,
        bool outer, bool hole)
    {
        Coordinate[] cleaned = DouglasPeucker.RemoveAdjacentDuplicates(
            source, true);
        int count = cleaned.Length;
        if (count > 1 &&
            RobustLineIntersection.CoordinatesEqualPlanar(
                cleaned[0], cleaned[count - 1]))
        {
            count--;
        }
        PolygonHullRingState state = new PolygonHullRingState();
        int index = 0;
        while (index < count)
        {
            state.Coordinates.Add(
                RobustLineIntersection.CopyCoordinate(cleaned[index]));
            index++;
        }
        RingOrientation orientation =
            RingMath.GetRingOrientation(source);
        state.CounterClockwise =
            orientation == RingOrientation.CounterClockwise;
        state.RemoveConvex = hole ? outer : !outer;
        state.TargetRemovals = (int)Math.Floor(
            (count - 3) * vertexRatio);
        return state;
    }

    internal static void SimplifyHullRing(
        PolygonHullRingState ring,
        List<PolygonHullRingState> rings,
        Geometry metadataSource, bool outer)
    {
        while (ring.Coordinates.Count > 3 &&
            ring.Removed < ring.TargetRemovals)
        {
            PolygonHullCandidate? best = FindHullCandidate(
                ring, rings, metadataSource, outer);
            if (best == null) break;
            ring.Coordinates.RemoveAt(best.VertexIndex);
            ring.Removed++;
        }
    }

    internal static PolygonHullCandidate? FindHullCandidate(
        PolygonHullRingState ring,
        List<PolygonHullRingState> rings,
        Geometry metadataSource, bool outer)
    {
        PolygonHullCandidate? best = null;
        int index = 0;
        while (index < ring.Coordinates.Count)
        {
            if (VertexCanBeRemoved(ring, index) &&
                RemovalPreservesHull(
                    ring, index, rings, metadataSource, outer))
            {
                int previous = index == 0 ?
                    ring.Coordinates.Count - 1 : index - 1;
                int next = index + 1 == ring.Coordinates.Count ?
                    0 : index + 1;
                double area = VisvalingamWhyatt.GetTriangleArea(
                    ring.Coordinates[previous],
                    ring.Coordinates[index], ring.Coordinates[next]);
                if (best == null || area < best.Area)
                {
                    PolygonHullCandidate candidate =
                        new PolygonHullCandidate();
                    candidate.Ring = ring;
                    candidate.VertexIndex = index;
                    candidate.Area = area;
                    best = candidate;
                }
            }
            index++;
        }
        return best;
    }

    internal static bool VertexCanBeRemoved(
        PolygonHullRingState ring, int vertexIndex)
    {
        int previous = vertexIndex == 0 ?
            ring.Coordinates.Count - 1 : vertexIndex - 1;
        int next = vertexIndex + 1 == ring.Coordinates.Count ?
            0 : vertexIndex + 1;
        int turn = RobustOrientation.GetOrientationIndex(
            ring.Coordinates[previous],
            ring.Coordinates[vertexIndex],
            ring.Coordinates[next]);
        if (turn == 0) return true;
        bool convex = ring.CounterClockwise ? turn > 0 : turn < 0;
        return convex == ring.RemoveConvex;
    }

    internal static bool RemovalPreservesHull(
        PolygonHullRingState ring, int vertexIndex,
        List<PolygonHullRingState> rings,
        Geometry metadataSource, bool outer)
    {
        Coordinate removed = ring.Coordinates[vertexIndex];
        ring.Coordinates.RemoveAt(vertexIndex);
        bool valid = StateRingsAreValid(rings);
        if (valid)
        {
            Geometry candidate = CreateStateGeometry(
                rings, metadataSource);
            valid = outer ?
                GeometryPredicateAlgorithms.Covers(
                    candidate, metadataSource) :
                GeometryPredicateAlgorithms.Covers(
                    metadataSource, candidate);
        }
        ring.Coordinates.Insert(vertexIndex, removed);
        return valid;
    }

    internal static bool StateRingsAreValid(
        List<PolygonHullRingState> rings)
    {
        LinearRing[] holes = new LinearRing[rings.Count - 1];
        int index = 1;
        while (index < rings.Count)
        {
            LinearRing hole = new LinearRing();
            hole.Coordinates = CreateClosedRing(
                rings[index].Coordinates);
            holes[index - 1] = hole;
            index++;
        }
        return VisvalingamWhyatt.PolygonRingsAreValid(
            CreateClosedRing(rings[0].Coordinates), holes);
    }

    internal static Geometry CreateStateGeometry(
        List<PolygonHullRingState> rings,
        Geometry metadataSource)
    {
        LinearRing shell = new LinearRing();
        shell.Coordinates = CreateClosedRing(rings[0].Coordinates);
        LinearRing[] holes = new LinearRing[rings.Count - 1];
        int index = 1;
        while (index < rings.Count)
        {
            LinearRing hole = new LinearRing();
            hole.Coordinates = CreateClosedRing(
                rings[index].Coordinates);
            holes[index - 1] = hole;
            index++;
        }
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(
                metadataSource);
        return GeometryBuilder.CreatePolygonValue(shell, holes, options);
    }

    internal static Coordinate[] CreateClosedRing(
        List<Coordinate> coordinates)
    {
        Coordinate[] result =
            new Coordinate[coordinates.Count + 1];
        int index = 0;
        while (index < coordinates.Count)
        {
            result[index] = RobustLineIntersection.CopyCoordinate(
                coordinates[index]);
            index++;
        }
        result[result.Length - 1] =
            RobustLineIntersection.CopyCoordinate(result[0]);
        return result;
    }

    internal static bool MultiPolygonComponentsAreDisjoint(
        Polygon[] polygons, GeometryFactoryOptions options)
    {
        int first = 0;
        while (first < polygons.Length)
        {
            Geometry firstGeometry = GeometryBuilder.CreatePolygonValue(
                polygons[first].Shell, polygons[first].Holes, options);
            int second = first + 1;
            while (second < polygons.Length)
            {
                Geometry secondGeometry =
                    GeometryBuilder.CreatePolygonValue(
                        polygons[second].Shell,
                        polygons[second].Holes, options);
                if (GeometryPredicateAlgorithms.Intersects(
                    firstGeometry, secondGeometry))
                {
                    return false;
                }
                second++;
            }
            first++;
        }
        return true;
    }
}
