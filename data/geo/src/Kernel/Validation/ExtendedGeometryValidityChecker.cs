using System;
using System.Collections.Generic;

namespace geo;

internal class MultipartValidityRing
{
    internal PolygonValidityRing Ring = new PolygonValidityRing();
    internal int ComponentIndex;
}

internal class SurfaceEdgeUse
{
    internal int Count;
    internal Coordinate Coordinate = new Coordinate();
    internal Coordinate Start = new Coordinate();
    internal Coordinate End = new Coordinate();
    internal string Path = "";
}

internal static class ExtendedGeometryValidityChecker
{
    internal static ValidationResult ValidateMultiPolygon(
        MultiPolygon multiPolygon, string path)
    {
        string[] paths = new string[multiPolygon.Polygons.Length];
        int index = 0;
        while (index < paths.Length)
        {
            paths[index] = path + "/Polygons[" + index + "]";
            index++;
        }
        return ValidatePolygonComponents(
            multiPolygon.Polygons, paths);
    }

    internal static ValidationResult ValidatePolygonComponents(
        Polygon[] polygons, string[] paths)
    {
        int[] groups = new int[polygons.Length];
        int index = 0;
        while (index < groups.Length)
        {
            groups[index] = index;
            index++;
        }
        return ValidateGroupedPolygonComponents(
            polygons, paths, groups);
    }

    internal static ValidationResult ValidateGroupedPolygonComponents(
        Polygon[] polygons, string[] paths, int[] groups)
    {
        int index = 0;
        while (index < polygons.Length)
        {
            ValidationResult component =
                GeometryValidityChecker.ValidatePolygon(
                    polygons[index], paths[index]);
            if (!component.IsValid) return component;
            index++;
        }
        ValidationResult? intersectionFailure =
            CheckMultipartIntersections(polygons, paths, groups);
        if (intersectionFailure != null) return intersectionFailure;
        ValidationResult? containmentFailure =
            CheckMultipartContainment(polygons, paths, groups);
        return containmentFailure == null ?
            new ValidationResult() : containmentFailure;
    }

    internal static ValidationResult? CheckMultipartIntersections(
        Polygon[] polygons, string[] paths, int[] groups)
    {
        List<SegmentString> strings = new List<SegmentString>();
        int ringIndex = 0;
        int polygonIndex = 0;
        while (polygonIndex < polygons.Length)
        {
            Polygon polygon = polygons[polygonIndex];
            AddMultipartRing(
                polygon.Shell.Coordinates,
                paths[polygonIndex] + "/Shell",
                groups[polygonIndex], ringIndex, strings);
            ringIndex++;
            int holeIndex = 0;
            while (holeIndex < polygon.Holes.Length)
            {
                AddMultipartRing(
                    polygon.Holes[holeIndex].Coordinates,
                    paths[polygonIndex] + "/Holes[" + holeIndex + "]",
                    groups[polygonIndex], ringIndex, strings);
                ringIndex++;
                holeIndex++;
            }
            polygonIndex++;
        }
        if (strings.Count < 2) return null;
        SweepLineIndex sweep = new SweepLineIndex(strings.ToArray());
        SegmentPairCandidate[] candidates =
            sweep.GetCandidatePairs(false);
        int index = 0;
        while (index < candidates.Length)
        {
            SegmentPairCandidate candidate = candidates[index];
            MultipartValidityRing first = GetMultipartRing(candidate.First);
            MultipartValidityRing second = GetMultipartRing(candidate.Second);
            if (first.ComponentIndex != second.ComponentIndex)
            {
                SegmentIntersectionResult intersection =
                    GeometryValidityChecker.GetCandidateIntersection(
                        candidate);
                if (MultipartIntersectionIsInvalid(
                    intersection, first.Ring, second.Ring))
                {
                    return GeometryValidityChecker.CreateTopologyFailure(
                        ValidationError.OverlappingComponents,
                        "Polygon components have overlapping boundaries or interiors.",
                        intersection.FirstIntersection,
                        second.Ring.Path,
                        candidate.SecondSegmentIndex);
                }
            }
            index++;
        }
        return null;
    }

    internal static void AddMultipartRing(
        Coordinate[] coordinates, string path,
        int componentIndex, int ringIndex,
        List<SegmentString> strings)
    {
        PolygonValidityRing ring = GeometryValidityChecker.CreateRing(
            coordinates, path, ringIndex);
        if (ring.Coordinates.Length < 2) return;
        MultipartValidityRing metadata = new MultipartValidityRing();
        metadata.Ring = ring;
        metadata.ComponentIndex = componentIndex;
        strings.Add(SegmentNoding.CreateSegmentString(
            ring.Coordinates, metadata, ringIndex));
    }

    internal static bool MultipartIntersectionIsInvalid(
        SegmentIntersectionResult intersection,
        PolygonValidityRing first, PolygonValidityRing second)
    {
        if (intersection.Type == SegmentIntersectionType.None) return false;
        if (intersection.Type == SegmentIntersectionType.ProperPoint ||
            intersection.Type == SegmentIntersectionType.CollinearOverlap)
        {
            return true;
        }
        Coordinate? witness = intersection.FirstIntersection;
        if (witness != null &&
            GeometryValidityChecker.RingsCrossAtWitness(
                witness, first, second)) return true;
        witness = intersection.SecondIntersection;
        return witness != null &&
            GeometryValidityChecker.RingsCrossAtWitness(
                witness, first, second);
    }

    internal static MultipartValidityRing GetMultipartRing(
        SegmentString segmentString)
    {
        object? metadata = segmentString.SourceMetadata;
        if (metadata == null)
        {
            throw new InvalidGeometryException(
                "Multipart validity segment metadata is null.");
        }
        return (MultipartValidityRing)metadata;
    }

    internal static ValidationResult? CheckMultipartContainment(
        Polygon[] polygons, string[] paths, int[] groups)
    {
        int first = 0;
        while (first < polygons.Length)
        {
            int second = first + 1;
            while (second < polygons.Length)
            {
                if (groups[first] == groups[second])
                {
                    second++;
                    continue;
                }
                Coordinate? firstWitness = FindShellWitness(
                    polygons[first], polygons[second]);
                if (firstWitness != null &&
                    LocatePointInPolygon(
                        firstWitness, polygons[second]) ==
                    TopologicalLocation.Interior)
                {
                    return StructuralValidator.CreateFailureResult(
                        ValidationError.OverlappingComponents,
                        "A polygon component lies inside another component.",
                        firstWitness, paths[first]);
                }
                Coordinate? secondWitness = FindShellWitness(
                    polygons[second], polygons[first]);
                if (secondWitness != null &&
                    LocatePointInPolygon(
                        secondWitness, polygons[first]) ==
                    TopologicalLocation.Interior)
                {
                    return StructuralValidator.CreateFailureResult(
                        ValidationError.OverlappingComponents,
                        "A polygon component lies inside another component.",
                        secondWitness, paths[second]);
                }
                second++;
            }
            first++;
        }
        return null;
    }

    internal static Coordinate? FindShellWitness(
        Polygon test, Polygon target)
    {
        Coordinate[] coordinates = test.Shell.Coordinates;
        int count = coordinates.Length;
        if (count > 1 &&
            RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[0], coordinates[count - 1])) count--;
        int index = 0;
        while (index < count)
        {
            if (LocatePointOnPolygonBoundary(
                coordinates[index], target) !=
                TopologicalLocation.Boundary)
            {
                return coordinates[index];
            }
            index++;
        }
        return null;
    }

    internal static TopologicalLocation LocatePointInPolygon(
        Coordinate point, Polygon polygon)
    {
        TopologicalLocation shell =
            RingPointLocation.LocatePointByRayCrossing(
                point, polygon.Shell.Coordinates);
        if (shell != TopologicalLocation.Interior) return shell;
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            TopologicalLocation hole =
                RingPointLocation.LocatePointByRayCrossing(
                    point, polygon.Holes[index].Coordinates);
            if (hole == TopologicalLocation.Boundary) return hole;
            if (hole == TopologicalLocation.Interior)
            {
                return TopologicalLocation.Exterior;
            }
            index++;
        }
        return TopologicalLocation.Interior;
    }

    internal static TopologicalLocation LocatePointOnPolygonBoundary(
        Coordinate point, Polygon polygon)
    {
        TopologicalLocation shell =
            RingPointLocation.LocatePointByRayCrossing(
                point, polygon.Shell.Coordinates);
        if (shell == TopologicalLocation.Boundary) return shell;
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            TopologicalLocation hole =
                RingPointLocation.LocatePointByRayCrossing(
                    point, polygon.Holes[index].Coordinates);
            if (hole == TopologicalLocation.Boundary) return hole;
            index++;
        }
        return TopologicalLocation.Exterior;
    }

    internal static ValidationResult ValidateCollection(
        GeometryCollection collection, string path)
    {
        int index = 0;
        while (index < collection.Geometries.Length)
        {
            ValidationResult component =
                GeometryValidityChecker.ValidateGeometryPayload(
                    collection.Geometries[index],
                    path + "/Geometries[" + index + "]");
            if (!component.IsValid) return component;
            index++;
        }
        return new ValidationResult();
    }

    internal static ValidationResult ValidateTriangle(
        Triangle triangle, string path)
    {
        Coordinate[] coordinates =
            DouglasPeucker.RemoveAdjacentDuplicates(
                triangle.Shell.Coordinates, true);
        if (coordinates.Length == 0) return new ValidationResult();
        if (coordinates.Length != 4)
        {
            return StructuralValidator.CreateFailureResult(
                ValidationError.InvalidSurface,
                "Triangle must contain exactly three distinct vertices.",
                coordinates[0], path + "/Shell");
        }
        if (RobustOrientation.GetOrientationIndex(
            coordinates[0], coordinates[1], coordinates[2]) == 0)
        {
            return StructuralValidator.CreateFailureResult(
                ValidationError.InvalidSurface,
                "Triangle vertices must not be collinear.",
                coordinates[1], path + "/Shell");
        }
        return GeometryValidityChecker.ValidateLinearRing(
            triangle.Shell, path + "/Shell");
    }

    internal static ValidationResult ValidateTin(Tin tin, string path)
    {
        Polygon[] polygons = new Polygon[tin.Triangles.Length];
        string[] paths = new string[tin.Triangles.Length];
        int index = 0;
        while (index < tin.Triangles.Length)
        {
            string trianglePath =
                path + "/Triangles[" + index + "]";
            ValidationResult triangleResult = ValidateTriangle(
                tin.Triangles[index], trianglePath);
            if (!triangleResult.IsValid) return triangleResult;
            Polygon polygon = new Polygon();
            polygon.Shell = tin.Triangles[index].Shell;
            polygons[index] = polygon;
            paths[index] = trianglePath;
            index++;
        }
        ValidationResult? incidence =
            SurfaceTopologyValidator.ValidateManifoldEdges(
                polygons, paths);
        if (incidence != null) return incidence;
        ValidationResult? topology = ValidateTinTopology(polygons, paths);
        return topology == null ? new ValidationResult() : topology;
    }

    internal static ValidationResult? ValidateTinTopology(
        Polygon[] triangles, string[] paths)
    {
        int first = 0;
        while (first < triangles.Length)
        {
            Coordinate[] firstCoordinates =
                DouglasPeucker.RemoveAdjacentDuplicates(
                    triangles[first].Shell.Coordinates, true);
            int second = first + 1;
            while (second < triangles.Length)
            {
                Coordinate[] secondCoordinates =
                    DouglasPeucker.RemoveAdjacentDuplicates(
                        triangles[second].Shell.Coordinates, true);
                ValidationResult? pair = ValidateTrianglePair(
                    firstCoordinates, secondCoordinates,
                    paths[second]);
                if (pair != null) return pair;
                second++;
            }
            first++;
        }
        return null;
    }

    internal static ValidationResult? ValidateTrianglePair(
        Coordinate[] first, Coordinate[] second, string path)
    {
        int sharedEdges = 0;
        Coordinate? sharedStart = null;
        Coordinate? sharedEnd = null;
        int firstIndex = 0;
        while (firstIndex + 1 < first.Length)
        {
            int secondIndex = 0;
            while (secondIndex + 1 < second.Length)
            {
                SegmentIntersectionResult intersection =
                    RobustLineIntersection.GetSegmentIntersection(
                        first[firstIndex], first[firstIndex + 1],
                        second[secondIndex], second[secondIndex + 1]);
                if (intersection.Type ==
                    SegmentIntersectionType.CollinearOverlap)
                {
                    if (!SegmentsAreEqual(
                        first[firstIndex], first[firstIndex + 1],
                        second[secondIndex], second[secondIndex + 1]))
                    {
                        return CreateInvalidSurfaceFailure(
                            intersection.FirstIntersection, path,
                            "TIN patches partially overlap along an edge.");
                    }
                    sharedEdges++;
                    sharedStart = first[firstIndex];
                    sharedEnd = first[firstIndex + 1];
                }
                else if (intersection.Type ==
                    SegmentIntersectionType.ProperPoint)
                {
                    return CreateInvalidSurfaceFailure(
                        intersection.FirstIntersection, path,
                        "TIN patch boundaries cross.");
                }
                else if (intersection.Type !=
                    SegmentIntersectionType.None &&
                    !IntersectionIsNodeOfBoth(
                        intersection.FirstIntersection,
                        first[firstIndex], first[firstIndex + 1],
                        second[secondIndex], second[secondIndex + 1]))
                {
                    return CreateInvalidSurfaceFailure(
                        intersection.FirstIntersection, path,
                        "TIN patches contain a non-noded intersection.");
                }
                secondIndex++;
            }
            firstIndex++;
        }
        if (sharedEdges > 1)
        {
            return CreateInvalidSurfaceFailure(
                sharedStart, path,
                "TIN patches share more than one edge.");
        }
        if (sharedEdges == 1 && sharedStart != null && sharedEnd != null &&
            TrianglesLieOnSameEdgeSide(
                first, second, sharedStart, sharedEnd))
        {
            return CreateInvalidSurfaceFailure(
                sharedStart, path,
                "TIN patches overlap on the same side of a shared edge.");
        }
        Coordinate? firstWitness = FindTriangleInteriorVertex(first, second);
        if (firstWitness != null)
        {
            return CreateInvalidSurfaceFailure(
                firstWitness, path,
                "TIN patch interiors overlap.");
        }
        Coordinate? secondWitness = FindTriangleInteriorVertex(second, first);
        if (secondWitness != null)
        {
            return CreateInvalidSurfaceFailure(
                secondWitness, path,
                "TIN patch interiors overlap.");
        }
        return null;
    }

    internal static bool SegmentsAreEqual(
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        return SurfaceCoordinatesEqual(
            firstStart, secondStart) &&
            SurfaceCoordinatesEqual(
                firstEnd, secondEnd) ||
            SurfaceCoordinatesEqual(
                firstStart, secondEnd) &&
            SurfaceCoordinatesEqual(
                firstEnd, secondStart);
    }

    internal static bool SurfaceCoordinatesEqual(
        Coordinate first, Coordinate second)
    {
        if (!RobustLineIntersection.CoordinatesEqualPlanar(
            first, second)) return false;
        if (double.IsNaN(first.Z) && double.IsNaN(second.Z)) return true;
        return first.Z == second.Z;
    }

    internal static bool IntersectionIsNodeOfBoth(
        Coordinate? witness,
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        if (witness == null) return true;
        bool firstNode = SurfaceCoordinatesEqual(
            witness, firstStart) ||
            SurfaceCoordinatesEqual(
                witness, firstEnd);
        bool secondNode = SurfaceCoordinatesEqual(
            witness, secondStart) ||
            SurfaceCoordinatesEqual(
                witness, secondEnd);
        return firstNode && secondNode;
    }

    internal static bool TrianglesLieOnSameEdgeSide(
        Coordinate[] first, Coordinate[] second,
        Coordinate edgeStart, Coordinate edgeEnd)
    {
        Coordinate? firstOther = FindCoordinateOffEdge(
            first, edgeStart, edgeEnd);
        Coordinate? secondOther = FindCoordinateOffEdge(
            second, edgeStart, edgeEnd);
        if (firstOther == null || secondOther == null) return true;
        int firstSide = RobustOrientation.GetOrientationIndex(
            edgeStart, edgeEnd, firstOther);
        int secondSide = RobustOrientation.GetOrientationIndex(
            edgeStart, edgeEnd, secondOther);
        return firstSide == secondSide;
    }

    internal static Coordinate? FindCoordinateOffEdge(
        Coordinate[] triangle,
        Coordinate edgeStart, Coordinate edgeEnd)
    {
        int index = 0;
        while (index + 1 < triangle.Length)
        {
            if (!RobustLineIntersection.CoordinatesEqualPlanar(
                    triangle[index], edgeStart) &&
                !RobustLineIntersection.CoordinatesEqualPlanar(
                    triangle[index], edgeEnd))
            {
                return triangle[index];
            }
            index++;
        }
        return null;
    }

    internal static Coordinate? FindTriangleInteriorVertex(
        Coordinate[] test, Coordinate[] target)
    {
        int index = 0;
        while (index + 1 < test.Length)
        {
            if (RingPointLocation.LocatePointByRayCrossing(
                test[index], target) == TopologicalLocation.Interior)
            {
                return test[index];
            }
            index++;
        }
        return null;
    }

    internal static ValidationResult CreateInvalidSurfaceFailure(
        Coordinate? coordinate, string path, string message)
    {
        return StructuralValidator.CreateFailureResult(
            ValidationError.InvalidSurface,
            message, coordinate, path);
    }

    internal static ValidationResult ValidatePolyhedralSurface(
        PolyhedralSurface surface, string path)
    {
        string[] paths = new string[surface.Patches.Length];
        int index = 0;
        while (index < surface.Patches.Length)
        {
            paths[index] = path + "/Patches[" + index + "]";
            ValidationResult patch =
                GeometryValidityChecker.ValidatePolygon(
                    surface.Patches[index], paths[index]);
            if (!patch.IsValid) return patch;
            index++;
        }
        ValidationResult? incidence =
            SurfaceTopologyValidator.ValidateManifoldEdges(
                surface.Patches, paths);
        return incidence == null ? new ValidationResult() : incidence;
    }

    internal static ValidationResult ValidateCompoundCurve(
        CompoundCurve compound, string path)
    {
        Coordinate? previousEnd = null;
        int index = 0;
        while (index < compound.Curves.Length)
        {
            Geometry curve = compound.Curves[index];
            string curvePath = path + "/Curves[" + index + "]";
            ValidationResult component =
                GeometryValidityChecker.ValidateGeometryPayload(
                    curve, curvePath);
            if (!component.IsValid) return component;
            Coordinate[] coordinates =
                CoordinateSequences.GetDirectCoordinates(curve);
            if (coordinates.Length != 0)
            {
                if (previousEnd != null &&
                    !RobustLineIntersection.CoordinatesEqualPlanar(
                        previousEnd, coordinates[0]))
                {
                    return StructuralValidator.CreateFailureResult(
                        ValidationError.CurveDiscontinuity,
                        "CompoundCurve components are not continuous.",
                        coordinates[0], curvePath);
                }
                previousEnd = coordinates[coordinates.Length - 1];
            }
            index++;
        }
        return new ValidationResult();
    }

    internal static bool IsCompoundCurveClosed(CompoundCurve compound)
    {
        Coordinate? firstStart = null;
        Coordinate? previousEnd = null;
        int index = 0;
        while (index < compound.Curves.Length)
        {
            Coordinate[] coordinates = CoordinateSequences.
                GetDirectCoordinates(compound.Curves[index]);
            if (coordinates.Length != 0)
            {
                if (previousEnd != null &&
                    !RobustLineIntersection.CoordinatesEqualPlanar(
                        previousEnd, coordinates[0])) return false;
                if (firstStart == null) firstStart = coordinates[0];
                previousEnd = coordinates[coordinates.Length - 1];
            }
            index++;
        }
        if (firstStart == null || previousEnd == null) return false;
        return RobustLineIntersection.CoordinatesEqualPlanar(
            firstStart, previousEnd);
    }

    internal static ValidationResult ValidateCurvePolygon(
        CurvePolygon curvePolygon, string path)
    {
        if (curvePolygon.Shell == null) return new ValidationResult();
        ValidationResult shell = ValidateCurveRing(
            curvePolygon.Shell, path + "/Shell");
        if (!shell.IsValid) return shell;
        int index = 0;
        while (index < curvePolygon.Holes.Length)
        {
            ValidationResult hole = ValidateCurveRing(
                curvePolygon.Holes[index],
                path + "/Holes[" + index + "]");
            if (!hole.IsValid) return hole;
            index++;
        }
        Polygon polygon = LinearizeCurvePolygon(curvePolygon);
        return GeometryValidityChecker.ValidatePolygon(polygon, path);
    }

    internal static ValidationResult ValidateCurveRing(
        Geometry curve, string path)
    {
        if (curve.GeometryType == GeometryType.CompoundCurve)
        {
            return ValidateCompoundCurve(
                (CompoundCurve)
                    GeometryDispatch.GetSelectedPayload(curve),
                path);
        }
        if (curve.GeometryType == GeometryType.LinearRing)
        {
            return GeometryValidityChecker.ValidateLinearRing(
                (LinearRing)GeometryDispatch.GetSelectedPayload(curve),
                path);
        }
        return new ValidationResult();
    }

    internal static Polygon LinearizeCurvePolygon(
        CurvePolygon curvePolygon)
    {
        CurveLinearizationParameters parameters =
            new CurveLinearizationParameters();
        Polygon polygon = new Polygon();
        if (curvePolygon.Shell == null) return polygon;
        polygon.Shell = CreateLinearizedRing(
            curvePolygon.Shell, parameters);
        LinearRing[] holes =
            new LinearRing[curvePolygon.Holes.Length];
        int index = 0;
        while (index < holes.Length)
        {
            holes[index] = CreateLinearizedRing(
                curvePolygon.Holes[index], parameters);
            index++;
        }
        polygon.Holes = holes;
        return polygon;
    }

    internal static LinearRing CreateLinearizedRing(
        Geometry curve, CurveLinearizationParameters parameters)
    {
        LinearRing ring = new LinearRing();
        ring.Coordinates = CurveLinearizer.LinearizeCurveGeometry(
            curve, parameters);
        return ring;
    }

    internal static ValidationResult ValidateMultiCurve(
        MultiCurve multiCurve, string path)
    {
        int index = 0;
        while (index < multiCurve.Curves.Length)
        {
            ValidationResult component =
                GeometryValidityChecker.ValidateGeometryPayload(
                    multiCurve.Curves[index],
                    path + "/Curves[" + index + "]");
            if (!component.IsValid) return component;
            index++;
        }
        return new ValidationResult();
    }

    internal static ValidationResult ValidateMultiSurface(
        MultiSurface multiSurface, string path)
    {
        List<Polygon> polygons = new List<Polygon>();
        List<string> paths = new List<string>();
        List<int> groups = new List<int>();
        int index = 0;
        while (index < multiSurface.Surfaces.Length)
        {
            Geometry surface = multiSurface.Surfaces[index];
            string surfacePath = path + "/Surfaces[" + index + "]";
            ValidationResult component =
                GeometryValidityChecker.ValidateGeometryPayload(
                    surface, surfacePath);
            if (!component.IsValid) return component;
            AddSurfacePolygons(
                surface, surfacePath, index,
                polygons, paths, groups);
            index++;
        }
        return ValidateGroupedPolygonComponents(
            polygons.ToArray(), paths.ToArray(), groups.ToArray());
    }

    internal static void AddSurfacePolygons(
        Geometry surface, string path, int group,
        List<Polygon> polygons, List<string> paths,
        List<int> groups)
    {
        object payload = GeometryDispatch.GetSelectedPayload(surface);
        if (surface.GeometryType == GeometryType.Polygon ||
            surface.GeometryType == GeometryType.Triangle ||
            surface.GeometryType == GeometryType.CurvePolygon)
        {
            polygons.Add(ConvertSurfaceToPolygon(surface));
            paths.Add(path);
            groups.Add(group);
            return;
        }
        if (surface.GeometryType == GeometryType.Tin)
        {
            Tin tin = (Tin)payload;
            int index = 0;
            while (index < tin.Triangles.Length)
            {
                Polygon polygon = new Polygon();
                polygon.Shell = tin.Triangles[index].Shell;
                polygons.Add(polygon);
                paths.Add(path + "/Triangles[" + index + "]");
                groups.Add(group);
                index++;
            }
            return;
        }
        if (surface.GeometryType == GeometryType.PolyhedralSurface)
        {
            PolyhedralSurface polyhedral =
                (PolyhedralSurface)payload;
            int index = 0;
            while (index < polyhedral.Patches.Length)
            {
                polygons.Add(polyhedral.Patches[index]);
                paths.Add(path + "/Patches[" + index + "]");
                groups.Add(group);
                index++;
            }
            return;
        }
        throw new InvalidGeometryException(
            "MultiSurface contains an unsupported surface.");
    }

    internal static Polygon ConvertSurfaceToPolygon(Geometry surface)
    {
        object payload = GeometryDispatch.GetSelectedPayload(surface);
        switch (surface.GeometryType)
        {
            case GeometryType.Polygon:
                return (Polygon)payload;
            case GeometryType.Triangle:
                Triangle triangle = (Triangle)payload;
                Polygon polygon = new Polygon();
                polygon.Shell = triangle.Shell;
                return polygon;
            case GeometryType.CurvePolygon:
                return LinearizeCurvePolygon((CurvePolygon)payload);
            default:
                throw new InvalidGeometryException(
                    "MultiSurface contains an unsupported surface.");
        }
    }
}

internal static class SurfaceTopologyValidator
{
    internal static bool IsTinClosed(Tin tin)
    {
        Polygon[] patches = new Polygon[tin.Triangles.Length];
        string[] paths = new string[tin.Triangles.Length];
        int index = 0;
        while (index < tin.Triangles.Length)
        {
            Polygon polygon = new Polygon();
            polygon.Shell = tin.Triangles[index].Shell;
            patches[index] = polygon;
            paths[index] = "Tin/Triangles[" + index + "]";
            index++;
        }
        return IsClosedSurface(patches, paths);
    }

    internal static bool IsPolyhedralClosed(
        PolyhedralSurface surface)
    {
        string[] paths = new string[surface.Patches.Length];
        int index = 0;
        while (index < paths.Length)
        {
            paths[index] = "PolyhedralSurface/Patches[" + index + "]";
            index++;
        }
        return IsClosedSurface(surface.Patches, paths);
    }

    internal static ValidationResult? ValidateManifoldEdges(
        Polygon[] patches, string[] paths)
    {
        Dictionary<string, SurfaceEdgeUse> edges =
            CreateSurfaceEdges(patches, paths);
        foreach (SurfaceEdgeUse edge in edges.Values)
        {
            if (edge.Count > 2)
            {
                return StructuralValidator.CreateFailureResult(
                    ValidationError.InvalidSurface,
                    "More than two surface patches share an edge.",
                    edge.Coordinate, edge.Path);
            }
        }
        return null;
    }

    internal static bool IsClosedSurface(
        Polygon[] patches, string[] paths)
    {
        Dictionary<string, SurfaceEdgeUse> edges =
            CreateSurfaceEdges(patches, paths);
        if (edges.Count == 0) return false;
        foreach (SurfaceEdgeUse edge in edges.Values)
        {
            if (edge.Count != 2) return false;
        }
        return true;
    }

    internal static Dictionary<string, SurfaceEdgeUse> CreateSurfaceEdges(
        Polygon[] patches, string[] paths)
    {
        Dictionary<string, SurfaceEdgeUse> edges =
            new Dictionary<string, SurfaceEdgeUse>();
        int index = 0;
        while (index < patches.Length)
        {
            AddSurfaceRingEdges(
                patches[index].Shell.Coordinates,
                paths[index] + "/Shell", edges);
            int holeIndex = 0;
            while (holeIndex < patches[index].Holes.Length)
            {
                AddSurfaceRingEdges(
                    patches[index].Holes[holeIndex].Coordinates,
                    paths[index] + "/Holes[" + holeIndex + "]",
                    edges);
                holeIndex++;
            }
            index++;
        }
        return edges;
    }

    internal static void AddSurfaceRingEdges(
        Coordinate[] source, string path,
        Dictionary<string, SurfaceEdgeUse> edges)
    {
        Coordinate[] coordinates =
            DouglasPeucker.RemoveAdjacentDuplicates(source, true);
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            string key = GetSurfaceEdgeIdentity(
                coordinates[index], coordinates[index + 1]);
            if (edges.ContainsKey(key))
            {
                edges[key].Count++;
                edges[key].Coordinate =
                    RobustLineIntersection.CopyCoordinate(
                        coordinates[index]);
                edges[key].Path = path;
            }
            else
            {
                SurfaceEdgeUse edge = new SurfaceEdgeUse();
                edge.Count = 1;
                edge.Coordinate = RobustLineIntersection.CopyCoordinate(
                    coordinates[index]);
                edge.Start = RobustLineIntersection.CopyCoordinate(
                    coordinates[index]);
                edge.End = RobustLineIntersection.CopyCoordinate(
                    coordinates[index + 1]);
                edge.Path = path;
                edges.Add(key, edge);
            }
            index++;
        }
    }

    internal static string GetSurfaceEdgeIdentity(
        Coordinate start, Coordinate end)
    {
        string first = GetSurfaceCoordinateIdentity(start);
        string second = GetSurfaceCoordinateIdentity(end);
        if (string.CompareOrdinal(first, second) <= 0)
        {
            return first + "|" + second;
        }
        return second + "|" + first;
    }

    internal static string GetSurfaceCoordinateIdentity(Coordinate coordinate)
    {
        return GeometryInspection.GetOrdinateIdentity(coordinate.X) + ":" +
            GeometryInspection.GetOrdinateIdentity(coordinate.Y) + ":" +
            GeometryInspection.GetOrdinateIdentity(coordinate.Z);
    }
}
