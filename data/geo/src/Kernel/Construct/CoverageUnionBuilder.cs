using System.Collections.Generic;

namespace geo;

internal class CoverageBoundaryEdge
{
    internal string Key;
    internal Coordinate Start;
    internal Coordinate End;
    internal bool Cancelled;

    internal CoverageBoundaryEdge(
        string key, Coordinate start, Coordinate end)
    {
        Key = key;
        Start = CoordinateSequences.CopyCoordinateValue(start);
        End = CoordinateSequences.CopyCoordinateValue(end);
    }
}

internal static class CoverageUnionBuilder
{
    internal static Geometry UnionCoverage(Geometry geometry)
    {
        CoverageValidationResult validation =
            CoverageValidator.ValidateCoverage(geometry, 0.0);
        if (!validation.IsValid)
        {
            throw new InvalidGeometryException(
                "Coverage union requires a valid noded coverage: " +
                validation.Message);
        }
        GeometryRepairComponents components =
            GeometryRepairEngine.CollectRepairComponents(geometry);
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(geometry);
        if (components.Polygons.Count == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.Polygon, options);
        }

        Coordinate[][] boundary = CancelSharedEdges(
            components.Polygons);
        Geometry linework = CreateBoundaryLinework(
            geometry, boundary);
        PolygonizationResult polygonization =
            Polygonizer.GetPolygonization(linework);
        if (!GeometryInspection.IsEmptyValue(polygonization.Dangles) ||
            !GeometryInspection.IsEmptyValue(polygonization.CutEdges) ||
            !GeometryInspection.IsEmptyValue(polygonization.InvalidRings))
        {
            throw new TopologyException(
                "Coverage boundary cancellation did not form valid rings.");
        }
        Polygon[] polygons = TopologyPolygonBuilder.BuildPolygons(
            polygonization.AreaRings);
        Geometry result = OverlayResultExtractor.CreateResult(
            polygons, [], [], geometry, GeometryType.Polygon);
        ValidationResult resultValidation =
            GeometryValidityChecker.ValidateGeometry(result);
        if (!resultValidation.IsValid)
        {
            throw new TopologyException(
                "Coverage union reconstruction is invalid: " +
                resultValidation.Message);
        }
        return result;
    }

    internal static Coordinate[][] CancelSharedEdges(
        List<Geometry> polygons)
    {
        SegmentString[] rings =
            CoverageEdgeIndex.ExtractBoundarySegmentStrings(polygons);
        Dictionary<string, CoverageBoundaryEdge> active =
            new Dictionary<string, CoverageBoundaryEdge>();
        List<CoverageBoundaryEdge> ordered =
            new List<CoverageBoundaryEdge>();
        int ringIndex = 0;
        while (ringIndex < rings.Length)
        {
            Coordinate[] coordinates = rings[ringIndex].Coordinates;
            int segmentIndex = 0;
            while (segmentIndex + 1 < coordinates.Length)
            {
                string key = CreateUndirectedEdgeKey(
                    coordinates[segmentIndex],
                    coordinates[segmentIndex + 1]);
                if (active.ContainsKey(key))
                {
                    CoverageBoundaryEdge existing = active[key];
                    existing.Cancelled = true;
                    active.Remove(key);
                }
                else
                {
                    CoverageBoundaryEdge edge =
                        new CoverageBoundaryEdge(
                            key, coordinates[segmentIndex],
                            coordinates[segmentIndex + 1]);
                    ordered.Add(edge);
                    active.Add(key, edge);
                }
                segmentIndex++;
            }
            ringIndex++;
        }

        List<Coordinate[]> boundary = new List<Coordinate[]>();
        int edgeIndex = 0;
        while (edgeIndex < ordered.Count)
        {
            CoverageBoundaryEdge edge = ordered[edgeIndex];
            if (!edge.Cancelled)
            {
                boundary.Add([edge.Start, edge.End]);
            }
            edgeIndex++;
        }
        return boundary.ToArray();
    }

    internal static string CreateUndirectedEdgeKey(
        Coordinate start, Coordinate end)
    {
        PlanarNodeKey startKey = new PlanarNodeKey(start);
        PlanarNodeKey endKey = new PlanarNodeKey(end);
        if (PlanarNodeKey.CompareKeys(startKey, endKey) <= 0)
        {
            return startKey.Identity + "|" + endKey.Identity;
        }
        return endKey.Identity + "|" + startKey.Identity;
    }

    internal static Geometry CreateBoundaryLinework(
        Geometry source, Coordinate[][] boundary)
    {
        LineString[] lines = new LineString[boundary.Length];
        int index = 0;
        while (index < boundary.Length)
        {
            LineString line = new LineString();
            line.Coordinates = boundary[index];
            lines[index] = line;
            index++;
        }
        return GeometryBuilder.CreateMultiLineStringValue(
            lines,
            GeometryNormalization.CreateNormalizationOptions(source));
    }
}
