using System.Collections.Generic;

namespace geo;

internal class CoverageCleaningResult
{
    internal Geometry Geometry = new Geometry();
    internal CoverageValidationResult InputDiagnostic =
        new CoverageValidationResult();
    internal CoverageValidationResult OutputDiagnostic =
        new CoverageValidationResult();
}

internal static class CoverageCleaner
{
    internal static CoverageCleaningResult CleanCoverage(
        Geometry geometry, RepairParameters parameters)
    {
        GeometryRepairEngine.ValidateRepairParameters(parameters);
        double cleaningDistance = GetCleaningDistance(parameters);
        CoverageValidationResult inputDiagnostic =
            CoverageValidator.ValidateCoverage(
                geometry, cleaningDistance);

        RepairParameters structureParameters =
            GeometryRepairEngine.CopyRepairParameters(parameters);
        structureParameters.Method = RepairMethod.Structure;
        structureParameters.KeepCollapsedComponents = false;
        Geometry cleaned = GeometryRepairEngine.CleanCoordinates(
            geometry, structureParameters);
        GeometryRepairComponents components =
            GeometryRepairEngine.CollectRepairComponents(cleaned);
        if (components.Lines.Count != 0 || components.Points.Count != 0)
        {
            throw new InvalidGeometryException(
                "Coverage cleaning requires polygonal geometry.");
        }

        List<Geometry> repairedPolygons = RepairPolygons(
            components.Polygons, structureParameters);
        List<Geometry> ordered =
            GeometryRepairEngine.OrderCoverageComponents(
                repairedPolygons, parameters.CoverageMergeStrategy);
        List<Geometry> accepted = ResolveOwnership(
            ordered, structureParameters, cleaningDistance);
        Geometry reconstructed = ReconstructCoverage(
            accepted, geometry);
        CoverageValidationResult outputDiagnostic =
            CoverageValidator.ValidateCoverage(
                reconstructed, cleaningDistance);

        CoverageCleaningResult result = new CoverageCleaningResult();
        result.Geometry = reconstructed;
        result.InputDiagnostic = inputDiagnostic;
        result.OutputDiagnostic = outputDiagnostic;
        return result;
    }

    internal static double GetCleaningDistance(
        RepairParameters parameters)
    {
        if (parameters.MaximumGapWidth > parameters.SnapTolerance)
        {
            return parameters.MaximumGapWidth;
        }
        return parameters.SnapTolerance;
    }

    internal static List<Geometry> RepairPolygons(
        List<Geometry> polygons, RepairParameters parameters)
    {
        List<Geometry> repaired = new List<Geometry>();
        int index = 0;
        while (index < polygons.Count)
        {
            Geometry polygon = GeometryRepairEngine.MakeValid(
                polygons[index], parameters);
            GeometryRepairEngine.AddNonEmptyLeaves(polygon, repaired);
            index++;
        }
        return repaired;
    }

    internal static List<Geometry> ResolveOwnership(
        List<Geometry> ordered, RepairParameters parameters,
        double cleaningDistance)
    {
        List<Geometry> accepted = new List<Geometry>();
        Geometry? occupied = null;
        int index = 0;
        while (index < ordered.Count)
        {
            Geometry candidate = ordered[index];
            if (occupied != null && cleaningDistance > 0.0)
            {
                candidate = GeometrySnapper.SnapGeometry(
                    candidate, occupied, cleaningDistance);
                candidate = GeometryRepairEngine.MakeValid(
                    candidate, parameters);
            }
            if (occupied != null)
            {
                candidate = OverlayEngine.GetDifference(
                    candidate, occupied);
            }
            if (!GeometryInspection.IsEmptyValue(candidate))
            {
                GeometryRepairEngine.AddNonEmptyLeaves(
                    candidate, accepted);
                if (occupied == null)
                {
                    occupied = GeometryCopy.CopyGeometry(candidate);
                }
                else
                {
                    occupied = OverlayEngine.GetUnion(
                        occupied, candidate);
                }
            }
            index++;
        }
        return accepted;
    }

    internal static Geometry ReconstructCoverage(
        List<Geometry> polygons, Geometry metadataSource)
    {
        if (polygons.Count == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.Polygon,
                GeometryNormalization.CreateNormalizationOptions(
                    metadataSource));
        }

        SegmentString[] rings =
            CoverageEdgeIndex.ExtractBoundarySegmentStrings(polygons);
        SegmentString[] noded = IndexedNoder.NodeSelf(
            rings, false).First;
        Coordinate[][] unique = ExtractUniqueSegments(noded);
        Geometry linework = CoverageUnionBuilder.CreateBoundaryLinework(
            metadataSource, unique);
        PolygonizationResult polygonization =
            Polygonizer.GetPolygonization(linework);
        if (CoverageSimplifier.HasPolygonizationDiagnostics(
            polygonization))
        {
            throw new TopologyException(
                "Coverage cleaning could not reconstruct valid polygon faces.");
        }

        List<Geometry> selected = CoverageSimplifier.SelectCoveredFaces(
            polygonization.Polygons, polygons);
        return CoverageSimplifier.CreatePolygonResult(
            selected, metadataSource);
    }

    internal static Coordinate[][] ExtractUniqueSegments(
        SegmentString[] edges)
    {
        Dictionary<string, bool> seen = new Dictionary<string, bool>();
        List<Coordinate[]> unique = new List<Coordinate[]>();
        int edgeIndex = 0;
        while (edgeIndex < edges.Length)
        {
            Coordinate[] coordinates = edges[edgeIndex].Coordinates;
            int segmentIndex = 0;
            while (segmentIndex + 1 < coordinates.Length)
            {
                Coordinate start = coordinates[segmentIndex];
                Coordinate end = coordinates[segmentIndex + 1];
                string key = CoverageUnionBuilder.CreateUndirectedEdgeKey(
                    start, end);
                if (!seen.ContainsKey(key))
                {
                    seen.Add(key, true);
                    unique.Add(
                    [
                        CoordinateSequences.CopyCoordinateValue(start),
                        CoordinateSequences.CopyCoordinateValue(end)
                    ]);
                }
                segmentIndex++;
            }
            edgeIndex++;
        }
        return unique.ToArray();
    }
}
