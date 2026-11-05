using System.Collections.Generic;

namespace geo;

internal static class CoverageSimplifier
{
    internal static Geometry SimplifyCoverage(
        Geometry geometry, SimplifyParameters parameters)
    {
        VisvalingamWhyatt.ValidateVisvalingamParameters(parameters);
        CoverageValidationResult validation =
            CoverageValidator.ValidateCoverage(geometry, 0.0);
        if (!validation.IsValid)
        {
            throw new InvalidGeometryException(
                "Coverage simplification requires a valid noded coverage: " +
                validation.Message);
        }
        GeometryRepairComponents sourceComponents =
            GeometryRepairEngine.CollectRepairComponents(geometry);
        if (sourceComponents.Polygons.Count == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.Polygon,
                GeometryNormalization.CreateNormalizationOptions(geometry));
        }

        Coordinate[][] uniqueSegments = ExtractUniqueBoundarySegments(
            sourceComponents.Polygons);
        Geometry uniqueLinework =
            CoverageUnionBuilder.CreateBoundaryLinework(
                geometry, uniqueSegments);
        Geometry mergedLinework = LineMerger.MergeGeometry(
            uniqueLinework, false);
        Geometry simplifiedLinework = SimplifyMaximalEdges(
            geometry, mergedLinework, parameters.Tolerance);
        if (!LineworkIsFullyNoded(simplifiedLinework))
        {
            return CreatePolygonResult(
                sourceComponents.Polygons, geometry);
        }

        PolygonizationResult polygonization =
            Polygonizer.GetPolygonization(simplifiedLinework);
        if (HasPolygonizationDiagnostics(polygonization))
        {
            return CreatePolygonResult(
                sourceComponents.Polygons, geometry);
        }
        List<Geometry> selected = SelectCoveredFaces(
            polygonization.Polygons, sourceComponents.Polygons);
        Geometry result = CreatePolygonResult(selected, geometry);
        if (!HasMatchingTopology(sourceComponents.Polygons, selected))
        {
            return CreatePolygonResult(
                sourceComponents.Polygons, geometry);
        }
        CoverageValidationResult resultValidation =
            CoverageValidator.ValidateCoverage(result, 0.0);
        if (!resultValidation.IsValid)
        {
            return CreatePolygonResult(
                sourceComponents.Polygons, geometry);
        }
        return result;
    }

    internal static Coordinate[][] ExtractUniqueBoundarySegments(
        List<Geometry> polygons)
    {
        SegmentString[] rings =
            CoverageEdgeIndex.ExtractBoundarySegmentStrings(polygons);
        Dictionary<string, bool> seen =
            new Dictionary<string, bool>();
        List<Coordinate[]> unique = new List<Coordinate[]>();
        int ringIndex = 0;
        while (ringIndex < rings.Length)
        {
            Coordinate[] coordinates = rings[ringIndex].Coordinates;
            int segmentIndex = 0;
            while (segmentIndex + 1 < coordinates.Length)
            {
                string key = CoverageUnionBuilder.CreateUndirectedEdgeKey(
                    coordinates[segmentIndex],
                    coordinates[segmentIndex + 1]);
                if (!seen.ContainsKey(key))
                {
                    seen.Add(key, true);
                    unique.Add(
                    [
                        CoordinateSequences.CopyCoordinateValue(
                            coordinates[segmentIndex]),
                        CoordinateSequences.CopyCoordinateValue(
                            coordinates[segmentIndex + 1])
                    ]);
                }
                segmentIndex++;
            }
            ringIndex++;
        }
        return unique.ToArray();
    }

    internal static Geometry SimplifyMaximalEdges(
        Geometry source, Geometry mergedLinework, double tolerance)
    {
        List<Coordinate[]> merged = new List<Coordinate[]>();
        Polygonizer.CollectLinework(mergedLinework, merged);
        List<Coordinate[]> simplified = new List<Coordinate[]>();
        int index = 0;
        while (index < merged.Count)
        {
            Coordinate[] coordinates = merged[index];
            bool closed = coordinates.Length > 1 &&
                RobustLineIntersection.CoordinatesEqualPlanar(
                    coordinates[0],
                    coordinates[coordinates.Length - 1]);
            simplified.Add(VisvalingamWhyatt.SimplifyCoordinates(
                coordinates, tolerance, closed, true));
            index++;
        }
        return CoverageUnionBuilder.CreateBoundaryLinework(
            source, simplified.ToArray());
    }

    internal static bool LineworkIsFullyNoded(Geometry linework)
    {
        List<Coordinate[]> lines = new List<Coordinate[]>();
        Polygonizer.CollectLinework(linework, lines);
        SegmentString[] segments = new SegmentString[lines.Count];
        int index = 0;
        while (index < lines.Count)
        {
            segments[index] = SegmentNoding.CreateSegmentString(
                lines[index], linework, index);
            index++;
        }
        if (segments.Length == 0) return true;
        NodingResult validation = IndexedNoder.NodeSelf(
            segments, true);
        return validation.IsValid;
    }

    internal static bool HasPolygonizationDiagnostics(
        PolygonizationResult polygonization)
    {
        return !GeometryInspection.IsEmptyValue(
            polygonization.Dangles) ||
            !GeometryInspection.IsEmptyValue(
                polygonization.CutEdges) ||
            !GeometryInspection.IsEmptyValue(
                polygonization.InvalidRings);
    }

    internal static List<Geometry> SelectCoveredFaces(
        Geometry faces, List<Geometry> sourcePolygons)
    {
        GeometryRepairComponents faceComponents =
            GeometryRepairEngine.CollectRepairComponents(faces);
        List<Geometry> selected = new List<Geometry>();
        int faceIndex = 0;
        while (faceIndex < faceComponents.Polygons.Count)
        {
            Geometry face = faceComponents.Polygons[faceIndex];
            Coordinate? point =
                RepresentativePointAlgorithm.GetPointOnSurface(face);
            if (point != null &&
                IsCoveredBySource(point, sourcePolygons))
            {
                selected.Add(face);
            }
            faceIndex++;
        }
        return selected;
    }

    internal static bool IsCoveredBySource(
        Coordinate coordinate, List<Geometry> sourcePolygons)
    {
        int polygonIndex = 0;
        while (polygonIndex < sourcePolygons.Count)
        {
            Polygon polygon = (Polygon)
                GeometryDispatch.GetSelectedPayload(
                    sourcePolygons[polygonIndex]);
            if (GeometryPointLocation.LocatePointInPolygon(
                coordinate, polygon) != TopologicalLocation.Exterior)
            {
                return true;
            }
            polygonIndex++;
        }
        return false;
    }

    internal static bool HasMatchingTopology(
        List<Geometry> source, List<Geometry> result)
    {
        if (source.Count != result.Count) return false;
        return CountHoles(source) == CountHoles(result);
    }

    internal static int CountHoles(List<Geometry> polygons)
    {
        int count = 0;
        int index = 0;
        while (index < polygons.Count)
        {
            Polygon polygon = (Polygon)
                GeometryDispatch.GetSelectedPayload(polygons[index]);
            count += polygon.Holes.Length;
            index++;
        }
        return count;
    }

    internal static Geometry CreatePolygonResult(
        List<Geometry> polygons, Geometry source)
    {
        Polygon[] payloads = new Polygon[polygons.Count];
        int index = 0;
        while (index < polygons.Count)
        {
            payloads[index] = (Polygon)
                GeometryDispatch.GetSelectedPayload(polygons[index]);
            index++;
        }
        return OverlayResultExtractor.CreateResult(
            payloads, [], [], source, GeometryType.Polygon);
    }
}
