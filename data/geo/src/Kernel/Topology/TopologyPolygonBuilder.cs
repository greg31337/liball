using System.Collections.Generic;

namespace geo;

internal class PolygonShellAssignment
{
    internal TopologyRing Shell = new TopologyRing();
    internal List<TopologyRing> Holes = new List<TopologyRing>();
    internal int NestingDepth;
}

internal static class TopologyPolygonBuilder
{
    internal static Polygon[] BuildPolygons(TopologyRing[] minimalRings)
    {
        if (minimalRings == null)
        {
            throw new InvalidGeometryException("Minimal-ring array is null.");
        }
        int[] depths = new int[minimalRings.Length];
        List<PolygonShellAssignment> assignments =
            new List<PolygonShellAssignment>();
        int index = 0;
        while (index < minimalRings.Length)
        {
            ValidatePolygonRing(minimalRings[index]);
            index++;
        }
        index = 0;
        while (index < minimalRings.Length)
        {
            TopologyRing ring = minimalRings[index];
            depths[index] = CountContainingRings(ring, minimalRings);
            ring.IsHole = depths[index] % 2 == 1;
            if (!ring.IsHole)
            {
                PolygonShellAssignment assignment = new PolygonShellAssignment();
                assignment.Shell = ring;
                assignment.NestingDepth = depths[index];
                assignments.Add(assignment);
            }
            index++;
        }
        index = 0;
        while (index < minimalRings.Length)
        {
            if (minimalRings[index].IsHole)
            {
                PolygonShellAssignment? shell = FindContainingShell(
                    minimalRings[index], depths[index], assignments);
                if (shell == null)
                {
                    throw new TopologyException(
                        "A polygon hole is not contained by a shell ring.");
                }
                shell.Holes.Add(minimalRings[index]);
            }
            index++;
        }
        Polygon[] polygons = new Polygon[assignments.Count];
        index = 0;
        while (index < assignments.Count)
        {
            polygons[index] = CreatePolygon(assignments[index]);
            index++;
        }
        GeometryNormalization.SortPolygonValues(polygons);
        return polygons;
    }

    internal static int CountContainingRings(
        TopologyRing target, TopologyRing[] rings)
    {
        int count = 0;
        int index = 0;
        while (index < rings.Length)
        {
            TopologyRing candidate = rings[index];
            if (candidate != target && candidate.AbsoluteArea > target.AbsoluteArea &&
                ContainsRing(candidate, target)) count++;
            index++;
        }
        return count;
    }

    internal static PolygonShellAssignment? FindContainingShell(
        TopologyRing hole, int holeDepth,
        List<PolygonShellAssignment> assignments)
    {
        PolygonShellAssignment? best = null;
        int index = 0;
        while (index < assignments.Count)
        {
            PolygonShellAssignment candidate = assignments[index];
            if (candidate.NestingDepth == holeDepth - 1 &&
                candidate.Shell.AbsoluteArea > hole.AbsoluteArea &&
                ContainsRing(candidate.Shell, hole) &&
                (best == null || candidate.Shell.AbsoluteArea < best.Shell.AbsoluteArea))
            {
                best = candidate;
            }
            index++;
        }
        return best;
    }

    internal static bool ContainsRing(
        TopologyRing container, TopologyRing target)
    {
        int index = 0;
        while (index + 1 < target.Coordinates.Length)
        {
            TopologicalLocation location = RingPointLocation.LocatePointByRayCrossing(
                target.Coordinates[index], container.Coordinates);
            if (location == TopologicalLocation.Interior) return true;
            if (location == TopologicalLocation.Exterior) return false;
            index++;
        }
        return false;
    }

    internal static Polygon CreatePolygon(PolygonShellAssignment assignment)
    {
        Polygon polygon = new Polygon();
        polygon.Shell = CreateOrientedLinearRing(
            assignment.Shell.Coordinates, RingOrientation.CounterClockwise);
        polygon.Holes = new LinearRing[assignment.Holes.Count];
        int index = 0;
        while (index < assignment.Holes.Count)
        {
            polygon.Holes[index] = CreateOrientedLinearRing(
                assignment.Holes[index].Coordinates, RingOrientation.Clockwise);
            index++;
        }
        GeometryNormalization.SortLinearRingValues(polygon.Holes);
        return polygon;
    }

    internal static LinearRing CreateOrientedLinearRing(
        Coordinate[] coordinates, RingOrientation requiredOrientation)
    {
        RingOrientation orientation = RingMath.GetRingOrientation(coordinates);
        if (orientation == RingOrientation.Undefined)
        {
            throw new TopologyException("A polygon ring has zero signed area.");
        }
        LinearRing ring = new LinearRing();
        ring.Coordinates = new Coordinate[coordinates.Length];
        int index = 0;
        while (index < coordinates.Length)
        {
            int sourceIndex = orientation == requiredOrientation ? index :
                coordinates.Length - 1 - index;
            ring.Coordinates[index] = RobustLineIntersection.CopyCoordinate(
                coordinates[sourceIndex]);
            index++;
        }
        ring.Coordinates = GeometryNormalization.RotateRingToCanonicalStart(
            ring.Coordinates);
        return ring;
    }

    internal static void ValidatePolygonRing(TopologyRing ring)
    {
        if (ring == null)
        {
            throw new InvalidGeometryException("Minimal polygon ring is null.");
        }
        TopologyRingBuilder.ValidateClosedRing(ring.Coordinates);
        double area = RingMath.GetSignedArea(ring.Coordinates);
        ring.AbsoluteArea = System.Math.Abs(area);
        if (ring.AbsoluteArea <= 0.0)
        {
            throw new TopologyException("Minimal polygon ring has zero area.");
        }
    }
}
