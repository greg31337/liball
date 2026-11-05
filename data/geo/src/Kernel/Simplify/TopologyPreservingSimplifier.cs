using System;
using System.Collections.Generic;

namespace geo;

internal class TopologyTaggedSegment
{
    internal Coordinate Start;
    internal Coordinate End;
    internal TopologyTaggedLine Parent;
    internal int SourceSegment;
    internal Envelope Bounds;
    internal bool Active = true;

    internal TopologyTaggedSegment(
        Coordinate start, Coordinate end,
        TopologyTaggedLine parent, int sourceSegment)
    {
        Start = start;
        End = end;
        Parent = parent;
        SourceSegment = sourceSegment;
        Bounds = TopologySegmentIndex.CreateSegmentBounds(start, end);
    }
}

internal class TopologyTaggedLine
{
    internal Coordinate[] Coordinates;
    internal TopologyTaggedSegment[] SourceSegments;
    internal List<TopologyTaggedSegment> ResultSegments =
        new List<TopologyTaggedSegment>();
    internal int MinimumSize;
    internal int SourceIndex;

    internal TopologyTaggedLine(
        Coordinate[] coordinates, int minimumSize, int sourceIndex)
    {
        Coordinate[] cleaned = DouglasPeucker.RemoveAdjacentDuplicates(
            coordinates, minimumSize == 4);
        if (minimumSize == 2 && cleaned.Length == 1)
        {
            Coordinates = [
                RobustLineIntersection.CopyCoordinate(cleaned[0]),
                RobustLineIntersection.CopyCoordinate(cleaned[0])
            ];
        }
        else
        {
            Coordinates =
                CoordinateSequences.CopyCoordinateSequence(cleaned);
        }
        MinimumSize = minimumSize;
        SourceIndex = sourceIndex;
        int segmentCount = Coordinates.Length == 0 ?
            0 : Coordinates.Length - 1;
        SourceSegments = new TopologyTaggedSegment[segmentCount];
        int index = 0;
        while (index < segmentCount)
        {
            SourceSegments[index] = new TopologyTaggedSegment(
                Coordinates[index], Coordinates[index + 1], this, index);
            index++;
        }
    }

    internal Coordinate[] CreateResultCoordinates()
    {
        if (Coordinates.Length == 0) return [];
        if (ResultSegments.Count == 0)
        {
            return CoordinateSequences.CopyCoordinateSequence(Coordinates);
        }
        Coordinate[] result =
            new Coordinate[ResultSegments.Count + 1];
        int index = 0;
        while (index < ResultSegments.Count)
        {
            result[index] = RobustLineIntersection.CopyCoordinate(
                ResultSegments[index].Start);
            index++;
        }
        result[result.Length - 1] =
            RobustLineIntersection.CopyCoordinate(
                ResultSegments[ResultSegments.Count - 1].End);
        return result;
    }
}

internal class TopologySegmentIndex
{
    internal SortedDictionary<double, List<TopologyTaggedSegment>> Buckets =
        new SortedDictionary<double, List<TopologyTaggedSegment>>();

    internal void AddSegment(TopologyTaggedSegment segment)
    {
        double key = segment.Bounds.MinX;
        if (!Buckets.ContainsKey(key))
        {
            Buckets.Add(key, new List<TopologyTaggedSegment>());
        }
        Buckets[key].Add(segment);
    }

    internal void RemoveSegment(TopologyTaggedSegment segment)
    {
        segment.Active = false;
    }

    internal TopologyTaggedSegment[] QuerySegments(Envelope bounds)
    {
        List<TopologyTaggedSegment> matches =
            new List<TopologyTaggedSegment>();
        foreach (KeyValuePair<double, List<TopologyTaggedSegment>>
            bucket in Buckets)
        {
            if (bucket.Key > bounds.MaxX) break;
            int index = 0;
            while (index < bucket.Value.Count)
            {
                TopologyTaggedSegment segment = bucket.Value[index];
                if (segment.Active &&
                    segment.Bounds.MaxX >= bounds.MinX &&
                    segment.Bounds.MinY <= bounds.MaxY &&
                    segment.Bounds.MaxY >= bounds.MinY)
                {
                    matches.Add(segment);
                }
                index++;
            }
        }
        return matches.ToArray();
    }

    internal static Envelope CreateSegmentBounds(
        Coordinate start, Coordinate end)
    {
        return EnvelopeAlgorithms.CreateBoundsEnvelope(
            start.X < end.X ? start.X : end.X,
            start.Y < end.Y ? start.Y : end.Y,
            start.X > end.X ? start.X : end.X,
            start.Y > end.Y ? start.Y : end.Y);
    }
}

internal class TopologySimplifySection
{
    internal int First;
    internal int Last;
    internal int Depth;

    internal TopologySimplifySection(
        int first, int last, int depth)
    {
        First = first;
        Last = last;
        Depth = depth;
    }
}

internal class TopologyFarthestPoint
{
    internal int Index;
    internal double Distance;
}

internal class TopologyTaggedLineCursor
{
    internal int Index;
}

internal static class TopologyPreservingSimplifier
{
    internal static Geometry SimplifyGeometry(
        Geometry geometry, SimplifyParameters parameters)
    {
        ValidateTopologyParameters(parameters);
        GeometryInspection.EnsureGeometryGraph(geometry);
        List<TopologyTaggedLine> lines =
            new List<TopologyTaggedLine>();
        CollectTaggedLines(geometry, lines);
        TopologySegmentIndex inputIndex = BuildInputIndex(lines);
        TopologySegmentIndex outputIndex = new TopologySegmentIndex();
        int lineIndex = 0;
        while (lineIndex < lines.Count)
        {
            SimplifyTaggedLine(
                lines[lineIndex], parameters.Tolerance,
                inputIndex, outputIndex);
            lineIndex++;
        }
        Geometry result = GeometryCopy.CopyGeometry(geometry);
        TopologyTaggedLineCursor cursor =
            new TopologyTaggedLineCursor();
        ReplaceTaggedLines(result, lines, cursor);
        GeometryInspection.EnsureGeometryGraph(result);
        return result;
    }

    internal static void ValidateTopologyParameters(
        SimplifyParameters parameters)
    {
        if (parameters == null)
        {
            throw new InvalidGeometryException(
                "Simplification parameters are null.");
        }
        if (parameters.Algorithm !=
            SimplificationAlgorithm.TopologyPreserving)
        {
            throw new InvalidGeometryException(
                "Topology-preserving simplification requires its matching algorithm selection.");
        }
        if (!double.IsFinite(parameters.Tolerance) ||
            parameters.Tolerance < 0.0)
        {
            throw new InvalidGeometryException(
                "Topology-preserving tolerance must be finite and non-negative.");
        }
    }

    internal static void CollectTaggedLines(
        Geometry geometry, List<TopologyTaggedLine> lines)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        int index = 0;
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return;
            case GeometryType.LineString:
                LineString line = (LineString)payload;
                lines.Add(new TopologyTaggedLine(
                    line.Coordinates, 2, lines.Count));
                return;
            case GeometryType.LinearRing:
                LinearRing ring = (LinearRing)payload;
                lines.Add(new TopologyTaggedLine(
                    ring.Coordinates, 4, lines.Count));
                return;
            case GeometryType.Polygon:
                CollectPolygonLines((Polygon)payload, lines);
                return;
            case GeometryType.MultiLineString:
                MultiLineString multiLine = (MultiLineString)payload;
                while (index < multiLine.LineStrings.Length)
                {
                    lines.Add(new TopologyTaggedLine(
                        multiLine.LineStrings[index].Coordinates,
                        2, lines.Count));
                    index++;
                }
                return;
            case GeometryType.MultiPolygon:
                MultiPolygon multiPolygon = (MultiPolygon)payload;
                while (index < multiPolygon.Polygons.Length)
                {
                    CollectPolygonLines(
                        multiPolygon.Polygons[index], lines);
                    index++;
                }
                return;
            case GeometryType.GeometryCollection:
                GeometryCollection collection =
                    (GeometryCollection)payload;
                while (index < collection.Geometries.Length)
                {
                    CollectTaggedLines(collection.Geometries[index], lines);
                    index++;
                }
                return;
            default:
                throw new InvalidGeometryException(
                    "Topology-preserving simplification supports core planar geometry.");
        }
    }

    internal static void CollectPolygonLines(
        Polygon polygon, List<TopologyTaggedLine> lines)
    {
        lines.Add(new TopologyTaggedLine(
            polygon.Shell.Coordinates, 4, lines.Count));
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            lines.Add(new TopologyTaggedLine(
                polygon.Holes[index].Coordinates, 4, lines.Count));
            index++;
        }
    }

    internal static TopologySegmentIndex BuildInputIndex(
        List<TopologyTaggedLine> lines)
    {
        TopologySegmentIndex index = new TopologySegmentIndex();
        int lineIndex = 0;
        while (lineIndex < lines.Count)
        {
            TopologyTaggedSegment[] segments =
                lines[lineIndex].SourceSegments;
            int segmentIndex = 0;
            while (segmentIndex < segments.Length)
            {
                index.AddSegment(segments[segmentIndex]);
                segmentIndex++;
            }
            lineIndex++;
        }
        return index;
    }

    internal static void SimplifyTaggedLine(
        TopologyTaggedLine line, double tolerance,
        TopologySegmentIndex inputIndex,
        TopologySegmentIndex outputIndex)
    {
        if (line.Coordinates.Length < 2) return;
        List<TopologySimplifySection> sections =
            new List<TopologySimplifySection>();
        sections.Add(new TopologySimplifySection(
            0, line.Coordinates.Length - 1, 0));
        while (sections.Count != 0)
        {
            TopologySimplifySection section =
                sections[sections.Count - 1];
            sections.RemoveAt(sections.Count - 1);
            if (section.Last == section.First + 1)
            {
                AddOriginalResult(
                    line, section.First, outputIndex);
                continue;
            }
            TopologyFarthestPoint farthest = FindFarthestPoint(
                line.Coordinates, section.First, section.Last);
            bool flatten = CanFlattenSection(
                line, section, farthest, tolerance,
                inputIndex, outputIndex);
            if (flatten)
            {
                TopologyTaggedSegment candidate = CreateCandidate(
                    line, section.First, section.Last);
                RemoveSection(line, section.First, section.Last,
                    inputIndex);
                outputIndex.AddSegment(candidate);
                line.ResultSegments.Add(candidate);
            }
            else
            {
                int depth = section.Depth + 1;
                sections.Add(new TopologySimplifySection(
                    farthest.Index, section.Last, depth));
                sections.Add(new TopologySimplifySection(
                    section.First, farthest.Index, depth));
            }
        }
    }

    internal static bool CanFlattenSection(
        TopologyTaggedLine line, TopologySimplifySection section,
        TopologyFarthestPoint farthest, double tolerance,
        TopologySegmentIndex inputIndex,
        TopologySegmentIndex outputIndex)
    {
        if (farthest.Distance > tolerance) return false;
        if (line.MinimumSize > 2 &&
            section.Depth + 2 < line.MinimumSize)
        {
            return false;
        }
        TopologyTaggedSegment candidate = CreateCandidate(
            line, section.First, section.Last);
        if (HasBadOutputIntersection(candidate, outputIndex))
        {
            return false;
        }
        return !HasBadInputIntersection(
            candidate, section.First, section.Last, inputIndex);
    }

    internal static TopologyFarthestPoint FindFarthestPoint(
        Coordinate[] coordinates, int first, int last)
    {
        TopologyFarthestPoint result = new TopologyFarthestPoint();
        result.Index = first + 1;
        result.Distance = -1.0;
        int index = first + 1;
        while (index < last)
        {
            DistanceResult distance =
                SegmentDistanceAlgorithms.GetNearestPointSegment(
                    coordinates[index], coordinates[first],
                    coordinates[last]);
            if (distance.Distance > result.Distance)
            {
                result.Index = index;
                result.Distance = distance.Distance;
            }
            index++;
        }
        return result;
    }

    internal static bool HasBadInputIntersection(
        TopologyTaggedSegment candidate, int first, int last,
        TopologySegmentIndex inputIndex)
    {
        TopologyTaggedSegment[] segments =
            inputIndex.QuerySegments(candidate.Bounds);
        int index = 0;
        while (index < segments.Length)
        {
            TopologyTaggedSegment segment = segments[index];
            bool replaced = segment.Parent == candidate.Parent &&
                segment.SourceSegment >= first &&
                segment.SourceSegment < last;
            if (!replaced && HasInteriorIntersection(candidate, segment))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool HasBadOutputIntersection(
        TopologyTaggedSegment candidate,
        TopologySegmentIndex outputIndex)
    {
        TopologyTaggedSegment[] segments =
            outputIndex.QuerySegments(candidate.Bounds);
        int index = 0;
        while (index < segments.Length)
        {
            if (HasInteriorIntersection(candidate, segments[index]))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool HasInteriorIntersection(
        TopologyTaggedSegment first, TopologyTaggedSegment second)
    {
        SegmentIntersectionResult intersection =
            RobustLineIntersection.GetSegmentIntersection(
                first.Start, first.End, second.Start, second.End);
        if (intersection.Type == SegmentIntersectionType.None) return false;
        if (intersection.Type == SegmentIntersectionType.ProperPoint ||
            intersection.Type == SegmentIntersectionType.CollinearOverlap)
        {
            return true;
        }
        if (WitnessIsInterior(
            intersection.FirstIntersection, first, second))
        {
            return true;
        }
        return WitnessIsInterior(
            intersection.SecondIntersection, first, second);
    }

    internal static bool WitnessIsInterior(
        Coordinate? witness, TopologyTaggedSegment first,
        TopologyTaggedSegment second)
    {
        if (witness == null) return false;
        bool firstEndpoint =
            RobustLineIntersection.CoordinatesEqualPlanar(
                witness, first.Start) ||
            RobustLineIntersection.CoordinatesEqualPlanar(
                witness, first.End);
        bool secondEndpoint =
            RobustLineIntersection.CoordinatesEqualPlanar(
                witness, second.Start) ||
            RobustLineIntersection.CoordinatesEqualPlanar(
                witness, second.End);
        return !firstEndpoint || !secondEndpoint;
    }

    internal static void AddOriginalResult(
        TopologyTaggedLine line, int segmentIndex,
        TopologySegmentIndex outputIndex)
    {
        TopologyTaggedSegment segment =
            line.SourceSegments[segmentIndex];
        outputIndex.AddSegment(segment);
        line.ResultSegments.Add(segment);
    }

    internal static TopologyTaggedSegment CreateCandidate(
        TopologyTaggedLine line, int first, int last)
    {
        return new TopologyTaggedSegment(
            line.Coordinates[first], line.Coordinates[last],
            line, first);
    }

    internal static void RemoveSection(
        TopologyTaggedLine line, int first, int last,
        TopologySegmentIndex inputIndex)
    {
        int index = first;
        while (index < last)
        {
            inputIndex.RemoveSegment(line.SourceSegments[index]);
            index++;
        }
    }

    internal static void ReplaceTaggedLines(
        Geometry geometry, List<TopologyTaggedLine> lines,
        TopologyTaggedLineCursor cursor)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        int index = 0;
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return;
            case GeometryType.LineString:
                ((LineString)payload).Coordinates =
                    TakeCoordinates(lines, cursor);
                return;
            case GeometryType.LinearRing:
                ((LinearRing)payload).Coordinates =
                    TakeCoordinates(lines, cursor);
                return;
            case GeometryType.Polygon:
                ReplacePolygonLines((Polygon)payload, lines, cursor);
                return;
            case GeometryType.MultiLineString:
                MultiLineString multiLine = (MultiLineString)payload;
                while (index < multiLine.LineStrings.Length)
                {
                    multiLine.LineStrings[index].Coordinates =
                        TakeCoordinates(lines, cursor);
                    index++;
                }
                return;
            case GeometryType.MultiPolygon:
                MultiPolygon multiPolygon = (MultiPolygon)payload;
                while (index < multiPolygon.Polygons.Length)
                {
                    ReplacePolygonLines(
                        multiPolygon.Polygons[index], lines, cursor);
                    index++;
                }
                return;
            case GeometryType.GeometryCollection:
                GeometryCollection collection =
                    (GeometryCollection)payload;
                while (index < collection.Geometries.Length)
                {
                    ReplaceTaggedLines(
                        collection.Geometries[index], lines, cursor);
                    index++;
                }
                return;
            default:
                throw new InvalidGeometryException(
                    "Topology-preserving result contains an unsupported geometry.");
        }
    }

    internal static void ReplacePolygonLines(
        Polygon polygon, List<TopologyTaggedLine> lines,
        TopologyTaggedLineCursor cursor)
    {
        polygon.Shell.Coordinates = TakeCoordinates(lines, cursor);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            polygon.Holes[index].Coordinates =
                TakeCoordinates(lines, cursor);
            index++;
        }
    }

    internal static Coordinate[] TakeCoordinates(
        List<TopologyTaggedLine> lines,
        TopologyTaggedLineCursor cursor)
    {
        if (cursor.Index >= lines.Count)
        {
            throw new InvalidGeometryException(
                "Topology-preserving result line mapping is incomplete.");
        }
        Coordinate[] coordinates =
            lines[cursor.Index].CreateResultCoordinates();
        cursor.Index++;
        return coordinates;
    }
}
