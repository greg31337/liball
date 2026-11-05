using System;
using System.Collections.Generic;

namespace geo;

internal static class OffsetCurveBuilder
{
    internal static Geometry GetOffsetCurve(
        Geometry geometry, double distance, BufferParameters parameters)
    {
        if (!double.IsFinite(distance))
        {
            throw new InvalidGeometryException(
                "Offset curve distance must be finite.");
        }
        OffsetSegmentGenerator.ValidateBufferParameters(parameters);
        GeometryInspection.EnsureGeometryGraph(geometry);
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        ValidateLinealLeaves(leaves);

        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(geometry);
        List<LineString> sections = new List<LineString>();
        int index = 0;
        while (index < leaves.Length)
        {
            Geometry leaf = leaves[index];
            if (!GeometryInspection.IsEmptyValue(leaf))
            {
                Coordinate[] coordinates =
                    RelationGraphBuilder.GetCurveCoordinates(leaf);
                Coordinate[] cleaned =
                    BufferCurveBuilder.CleanCoordinates(coordinates);
                if (distance == 0.0)
                {
                    AddZeroDistanceSection(cleaned, sections);
                }
                else
                {
                    AddOffsetSections(
                        cleaned, Math.Abs(distance),
                        distance > 0.0 ?
                            OffsetSegmentGenerator.LeftSide :
                            OffsetSegmentGenerator.RightSide,
                        parameters, options, sections);
                }
            }
            index++;
        }
        return CreateResult(sections, options);
    }

    internal static void ValidateLinealLeaves(Geometry[] leaves)
    {
        int index = 0;
        while (index < leaves.Length)
        {
            Geometry leaf = leaves[index];
            if (leaf.GeometryType != GeometryType.LineString &&
                leaf.GeometryType != GeometryType.LinearRing)
            {
                bool emptyContainer =
                    (leaf.GeometryType == GeometryType.MultiLineString ||
                        leaf.GeometryType == GeometryType.GeometryCollection) &&
                    GeometryInspection.IsEmptyValue(leaf);
                if (!emptyContainer)
                {
                    throw new InvalidGeometryException(
                        "Offset curve construction requires lineal geometry.");
                }
            }
            index++;
        }
    }

    internal static void AddZeroDistanceSection(
        Coordinate[] coordinates, List<LineString> sections)
    {
        if (coordinates.Length < 2) return;
        LineString line = new LineString();
        line.Coordinates = CoordinateSequences.CopyCoordinateSequence(
            coordinates);
        sections.Add(line);
    }

    internal static void AddOffsetSections(
        Coordinate[] coordinates, double distance, int side,
        BufferParameters parameters, GeometryFactoryOptions options,
        List<LineString> sections)
    {
        if (coordinates.Length < 2) return;
        bool closed = coordinates.Length > 3 &&
            RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[0], coordinates[coordinates.Length - 1]);
        if (closed)
        {
            int orientation = RingMath.GetExactSignedAreaSign(coordinates);
            int enclosedSide = orientation > 0 ?
                OffsetSegmentGenerator.LeftSide :
                OffsetSegmentGenerator.RightSide;
            if (orientation != 0 && side == enclosedSide &&
                BufferCurveBuilder.IsRingDefinitelyEroded(
                    coordinates, distance))
            {
                return;
            }
        }

        Coordinate[] raw = closed ?
            BufferCurveBuilder.CreateRingOffsetCurve(
                coordinates, side, distance, parameters) :
            BufferCurveBuilder.CreateSideCurve(
                coordinates, side, distance, parameters);
        if (raw.Length < 2) return;

        LineString sourcePayload = new LineString();
        sourcePayload.Coordinates = coordinates;
        Geometry source = GeometryBuilder.CreateLineStringValue(
            sourcePayload.Coordinates, options);
        BufferParameters fullParameters =
            BufferCurveBuilder.CopyParameters(parameters, false);
        Geometry buffer = BufferBuilder.GetBuffer(
            source, distance, fullParameters);
        if (GeometryInspection.IsEmptyValue(buffer)) return;
        Geometry boundary = GeometryBoundary.CreateBoundaryValue(
            buffer, BoundaryNodeRule.Mod2);
        SegmentString[] boundaryEdges = GeometrySplitter.CreateSplitterEdges(
            new RelationGeometryParts(boundary));
        if (boundaryEdges.Length == 0) return;

        SegmentString rawString = SegmentNoding.CreateSegmentString(
            raw, sourcePayload, 0);
        SegmentString[] rawFragments =
            BufferPolygonizer.NodeUntilStable([rawString]);
        rawFragments = IndexedNoder.NodeMutual(
            rawFragments, boundaryEdges, false).First;
        AddBoundarySections(
            rawFragments, boundaryEdges,
            distance * 0.0001, sections);
    }

    internal static void AddBoundarySections(
        SegmentString[] fragments, SegmentString[] boundaryEdges,
        double matchTolerance, List<LineString> sections)
    {
        List<Coordinate> current = new List<Coordinate>();
        int index = 0;
        while (index < fragments.Length)
        {
            Coordinate[] coordinates = fragments[index].Coordinates;
            if (IsBoundaryFragment(
                coordinates, boundaryEdges, matchTolerance))
            {
                if (current.Count != 0 &&
                    !RobustLineIntersection.CoordinatesEqualPlanar(
                        current[current.Count - 1], coordinates[0]) &&
                    BufferCurveBuilder.GetPlanarDistance(
                        current[current.Count - 1], coordinates[0]) >
                            matchTolerance)
                {
                    FlushSection(
                        current, sections, matchTolerance);
                }
                AppendSectionCoordinates(
                    current, coordinates, matchTolerance);
            }
            else
            {
                FlushSection(current, sections, matchTolerance);
            }
            index++;
        }
        FlushSection(current, sections, matchTolerance);
    }

    internal static void AppendSectionCoordinates(
        List<Coordinate> target, Coordinate[] source,
        double matchTolerance)
    {
        int index = 0;
        while (index < source.Length)
        {
            if (target.Count == 0 ||
                BufferCurveBuilder.GetPlanarDistance(
                    target[target.Count - 1], source[index]) >
                        matchTolerance)
            {
                target.Add(RobustLineIntersection.CopyCoordinate(
                    source[index]));
            }
            index++;
        }
    }

    internal static bool IsBoundaryFragment(
        Coordinate[] coordinates, SegmentString[] boundaryEdges,
        double matchTolerance)
    {
        if (coordinates.Length < 2) return false;
        int segmentIndex = 0;
        while (segmentIndex + 1 < coordinates.Length)
        {
            Coordinate midpoint =
                RobustLineIntersection.InterpolateLineCoordinate(
                    coordinates[segmentIndex],
                    coordinates[segmentIndex + 1], 0.5);
            if (!IsPointOnEdges(
                midpoint, boundaryEdges, matchTolerance)) return false;
            segmentIndex++;
        }
        return true;
    }

    internal static bool IsPointOnEdges(
        Coordinate point, SegmentString[] edges, double matchTolerance)
    {
        int edgeIndex = 0;
        while (edgeIndex < edges.Length)
        {
            Coordinate[] coordinates = edges[edgeIndex].Coordinates;
            int segmentIndex = 0;
            while (segmentIndex + 1 < coordinates.Length)
            {
                DistanceResult distance =
                    SegmentDistanceAlgorithms.GetNearestPointSegment(
                        point, coordinates[segmentIndex],
                        coordinates[segmentIndex + 1]);
                if (distance.Distance <= matchTolerance)
                {
                    return true;
                }
                segmentIndex++;
            }
            edgeIndex++;
        }
        return false;
    }

    internal static void FlushSection(
        List<Coordinate> current, List<LineString> sections,
        double matchTolerance)
    {
        if (current.Count >= 2)
        {
            if (sections.Count != 0)
            {
                LineString previous = sections[sections.Count - 1];
                Coordinate previousEnd = previous.Coordinates[
                    previous.Coordinates.Length - 1];
                if (BufferCurveBuilder.GetPlanarDistance(
                    previousEnd, current[0]) <= matchTolerance)
                {
                    List<Coordinate> joined = new List<Coordinate>();
                    BufferCurveBuilder.AppendCoordinates(
                        joined, previous.Coordinates);
                    AppendSectionCoordinates(
                        joined, current.ToArray(), matchTolerance);
                    previous.Coordinates = joined.ToArray();
                    current.Clear();
                    return;
                }
            }
            LineString line = new LineString();
            line.Coordinates = current.ToArray();
            sections.Add(line);
        }
        current.Clear();
    }

    internal static Geometry CreateResult(
        List<LineString> sections, GeometryFactoryOptions options)
    {
        if (sections.Count == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.LineString, options);
        }
        if (sections.Count == 1)
        {
            return GeometryBuilder.CreateLineStringValue(
                sections[0].Coordinates, options);
        }
        return GeometryBuilder.CreateMultiLineStringValue(
            sections.ToArray(), options);
    }
}
