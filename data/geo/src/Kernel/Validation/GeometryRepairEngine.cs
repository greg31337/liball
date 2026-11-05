using System;
using System.Collections.Generic;

namespace geo;

internal class GeometryRepairComponents
{
    internal List<Geometry> Points = new List<Geometry>();
    internal List<Geometry> Lines = new List<Geometry>();
    internal List<Geometry> Polygons = new List<Geometry>();
}

internal static class GeometryRepairEngine
{
    internal static Geometry MakeValid(
        Geometry geometry, RepairParameters parameters)
    {
        ValidateRepairParameters(parameters);
        Geometry cleaned = CleanCoordinates(geometry, parameters);
        ValidationResult structure = StructuralValidator.ValidateGeometry(
            cleaned);
        if (!structure.IsValid)
        {
            throw new InvalidGeometryException(
                "Coordinate cleanup could not produce usable geometry: " +
                structure.Message);
        }
        if (!CollectionRelationGraphBuilder.SupportsCoreGeometry(cleaned))
        {
            throw new InvalidGeometryException(
                "Geometry repair supports core geometry types only.");
        }

        GeometryRepairComponents components =
            CollectRepairComponents(cleaned);
        Geometry area;
        switch (parameters.Method)
        {
            case RepairMethod.Linework:
                area = BuildLineworkArea(components.Polygons, cleaned);
                break;
            case RepairMethod.Structure:
                area = BuildStructureArea(components.Polygons, cleaned);
                break;
            default:
                throw new InvalidGeometryException(
                    "Repair method is not supported.");
        }

        List<Geometry> results = new List<Geometry>();
        AddNonEmptyLeaves(area, results);
        int index = 0;
        while (index < components.Lines.Count)
        {
            AddNonEmptyLeaves(components.Lines[index], results);
            index++;
        }
        index = 0;
        while (index < components.Points.Count)
        {
            AddNonEmptyLeaves(components.Points[index], results);
            index++;
        }
        if (parameters.KeepCollapsedComponents)
        {
            CollectCollapsedComponents(
                geometry, parameters, results);
        }
        Geometry repaired = CreateNarrowestResult(
            results, geometry, GetNominalEmptyType(geometry));
        ValidationResult validation =
            GeometryValidityChecker.ValidateGeometry(repaired);
        if (!validation.IsValid)
        {
            throw new TopologyException(
                "Geometry repair did not produce valid output: " +
                validation.Message);
        }
        return repaired;
    }

    internal static Geometry CleanCoordinates(
        Geometry geometry, RepairParameters parameters)
    {
        ValidateRepairParameters(parameters);
        GeometryCycleGuard.EnsureAcyclic(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        ValidateRepairMetadata(geometry);
        return CleanGeometryNode(geometry, parameters);
    }

    internal static Geometry RepairRings(
        Geometry geometry, RepairParameters parameters)
    {
        ValidateRepairParameters(parameters);
        Geometry cleaned = CleanCoordinates(geometry, parameters);
        if (!CollectionRelationGraphBuilder.SupportsCoreGeometry(cleaned))
        {
            throw new InvalidGeometryException(
                "Ring repair supports core geometry types only.");
        }
        GeometryRepairComponents components =
            CollectRepairComponents(cleaned);
        Geometry repaired = BuildLineworkArea(
            components.Polygons, cleaned);
        if (!parameters.KeepCollapsedComponents) return repaired;
        List<Geometry> results = new List<Geometry>();
        AddNonEmptyLeaves(repaired, results);
        CollectCollapsedComponents(geometry, parameters, results);
        return CreateNarrowestResult(
            results, geometry, GeometryType.Polygon);
    }

    internal static Geometry RemoveCollapsedComponents(
        Geometry geometry)
    {
        RepairParameters parameters = new RepairParameters();
        parameters.Method = RepairMethod.Linework;
        parameters.KeepCollapsedComponents = false;
        return MakeValid(geometry, parameters);
    }

    internal static Geometry CleanCoverage(
        Geometry geometry, RepairParameters parameters)
    {
        CoverageCleaningResult result =
            CoverageCleaner.CleanCoverage(geometry, parameters);
        if (!result.OutputDiagnostic.IsValid)
        {
            throw new TopologyException(
                "Coverage cleaning did not produce a valid coverage: " +
                result.OutputDiagnostic.Message);
        }
        return result.Geometry;
    }

    internal static void ValidateRepairParameters(
        RepairParameters parameters)
    {
        if (parameters == null)
        {
            throw new InvalidGeometryException(
                "Repair parameters are null.");
        }
        switch (parameters.Method)
        {
            case RepairMethod.Linework:
            case RepairMethod.Structure:
                break;
            default:
                throw new InvalidGeometryException(
                    "Repair method is not supported.");
        }
        switch (parameters.CoverageMergeStrategy)
        {
            case CoverageMergeStrategy.KeepFirst:
            case CoverageMergeStrategy.KeepLast:
            case CoverageMergeStrategy.LargestArea:
                break;
            default:
                throw new InvalidGeometryException(
                    "Coverage merge strategy is not supported.");
        }
        if (!double.IsFinite(parameters.SnapTolerance) ||
            parameters.SnapTolerance < 0.0 ||
            !double.IsFinite(parameters.MaximumGapWidth) ||
            parameters.MaximumGapWidth < 0.0)
        {
            throw new InvalidGeometryException(
                "Repair tolerances must be finite and non-negative.");
        }
    }

    internal static RepairParameters CopyRepairParameters(
        RepairParameters source)
    {
        RepairParameters copy = new RepairParameters();
        copy.Method = source.Method;
        copy.CoverageMergeStrategy = source.CoverageMergeStrategy;
        copy.KeepCollapsedComponents = source.KeepCollapsedComponents;
        copy.RemoveNonFiniteCoordinates =
            source.RemoveNonFiniteCoordinates;
        copy.RemoveRepeatedCoordinates =
            source.RemoveRepeatedCoordinates;
        copy.SnapTolerance = source.SnapTolerance;
        copy.MaximumGapWidth = source.MaximumGapWidth;
        return copy;
    }

    internal static void ValidateRepairMetadata(Geometry geometry)
    {
        switch (geometry.CoordinateLayout)
        {
            case CoordinateLayout.XY:
            case CoordinateLayout.XYZ:
            case CoordinateLayout.XYM:
            case CoordinateLayout.XYZM:
                break;
            default:
                throw new InvalidGeometryException(
                    "Geometry coordinate layout is not supported.");
        }
        if (geometry.SpatialReferenceId <
            GeometryConstants.UnknownSpatialReferenceId)
        {
            throw new InvalidGeometryException(
                "Geometry spatial reference identifier is invalid.");
        }
    }

    internal static Geometry CleanGeometryNode(
        Geometry source, RepairParameters parameters)
    {
        object payload = GeometryDispatch.GetSelectedPayload(source);
        Geometry result = CreateGeometryShell(source);
        switch (source.GeometryType)
        {
            case GeometryType.Point:
                result.Point = CleanPointPayload(
                    (Point)payload, source.CoordinateLayout, parameters);
                break;
            case GeometryType.LineString:
                LineString line = new LineString();
                line.Coordinates = CleanLineCoordinates(
                    ((LineString)payload).Coordinates,
                    source.CoordinateLayout, parameters);
                result.LineString = line;
                break;
            case GeometryType.LinearRing:
                LinearRing ring = new LinearRing();
                ring.Coordinates = CleanRingCoordinates(
                    ((LinearRing)payload).Coordinates,
                    source.CoordinateLayout, parameters);
                result.LinearRing = ring;
                break;
            case GeometryType.Polygon:
                result.Polygon = CleanPolygonPayload(
                    (Polygon)payload, source.CoordinateLayout, parameters);
                break;
            case GeometryType.MultiPoint:
                result.MultiPoint = CleanMultiPointPayload(
                    (MultiPoint)payload, source.CoordinateLayout, parameters);
                break;
            case GeometryType.MultiLineString:
                result.MultiLineString = CleanMultiLinePayload(
                    (MultiLineString)payload,
                    source.CoordinateLayout, parameters);
                break;
            case GeometryType.MultiPolygon:
                result.MultiPolygon = CleanMultiPolygonPayload(
                    (MultiPolygon)payload,
                    source.CoordinateLayout, parameters);
                break;
            case GeometryType.GeometryCollection:
                result.GeometryCollection = CleanCollectionPayload(
                    (GeometryCollection)payload, parameters);
                break;
            default:
                throw new InvalidGeometryException(
                    "Coordinate cleanup supports core geometry types only.");
        }
        return result;
    }

    internal static Geometry CreateGeometryShell(Geometry source)
    {
        Geometry geometry = new Geometry();
        geometry.Point = null;
        geometry.GeometryType = source.GeometryType;
        geometry.CoordinateLayout = source.CoordinateLayout;
        geometry.SpatialReferenceId = source.SpatialReferenceId;
        return geometry;
    }

    internal static Point CleanPointPayload(
        Point source, CoordinateLayout layout,
        RepairParameters parameters)
    {
        Point point = new Point();
        if (source.Coordinate == null) return point;
        if (CoordinateCanBeRetained(
            source.Coordinate, layout, parameters))
        {
            point.Coordinate = CoordinateSequences.CopyCoordinateValue(
                source.Coordinate);
        }
        return point;
    }

    internal static MultiPoint CleanMultiPointPayload(
        MultiPoint source, CoordinateLayout layout,
        RepairParameters parameters)
    {
        if (source.Points == null)
        {
            throw new InvalidGeometryException(
                "MultiPoint component array is null.");
        }
        MultiPoint multiPoint = new MultiPoint();
        multiPoint.Points = new Point[source.Points.Length];
        int index = 0;
        while (index < source.Points.Length)
        {
            multiPoint.Points[index] = CleanPointPayload(
                source.Points[index], layout, parameters);
            index++;
        }
        return multiPoint;
    }

    internal static MultiLineString CleanMultiLinePayload(
        MultiLineString source, CoordinateLayout layout,
        RepairParameters parameters)
    {
        if (source.LineStrings == null)
        {
            throw new InvalidGeometryException(
                "MultiLineString component array is null.");
        }
        MultiLineString multiLine = new MultiLineString();
        multiLine.LineStrings = new LineString[source.LineStrings.Length];
        int index = 0;
        while (index < source.LineStrings.Length)
        {
            LineString line = new LineString();
            line.Coordinates = CleanLineCoordinates(
                source.LineStrings[index].Coordinates,
                layout, parameters);
            multiLine.LineStrings[index] = line;
            index++;
        }
        return multiLine;
    }

    internal static MultiPolygon CleanMultiPolygonPayload(
        MultiPolygon source, CoordinateLayout layout,
        RepairParameters parameters)
    {
        if (source.Polygons == null)
        {
            throw new InvalidGeometryException(
                "MultiPolygon component array is null.");
        }
        MultiPolygon multiPolygon = new MultiPolygon();
        multiPolygon.Polygons = new Polygon[source.Polygons.Length];
        int index = 0;
        while (index < source.Polygons.Length)
        {
            multiPolygon.Polygons[index] = CleanPolygonPayload(
                source.Polygons[index], layout, parameters);
            index++;
        }
        return multiPolygon;
    }

    internal static GeometryCollection CleanCollectionPayload(
        GeometryCollection source, RepairParameters parameters)
    {
        if (source.Geometries == null)
        {
            throw new InvalidGeometryException(
                "GeometryCollection component array is null.");
        }
        GeometryCollection collection = new GeometryCollection();
        collection.Geometries = new Geometry[source.Geometries.Length];
        int index = 0;
        while (index < source.Geometries.Length)
        {
            collection.Geometries[index] = CleanGeometryNode(
                source.Geometries[index], parameters);
            index++;
        }
        return collection;
    }

    internal static Polygon CleanPolygonPayload(
        Polygon source, CoordinateLayout layout,
        RepairParameters parameters)
    {
        if (source.Shell == null || source.Holes == null)
        {
            throw new InvalidGeometryException(
                "Polygon shell or hole array is null.");
        }
        Polygon polygon = new Polygon();
        polygon.Shell.Coordinates = CleanRingCoordinates(
            source.Shell.Coordinates, layout, parameters);
        if (polygon.Shell.Coordinates.Length == 0)
        {
            polygon.Holes = [];
            return polygon;
        }
        List<LinearRing> holes = new List<LinearRing>();
        int index = 0;
        while (index < source.Holes.Length)
        {
            LinearRing hole = new LinearRing();
            hole.Coordinates = CleanRingCoordinates(
                source.Holes[index].Coordinates, layout, parameters);
            if (hole.Coordinates.Length != 0) holes.Add(hole);
            index++;
        }
        polygon.Holes = holes.ToArray();
        return polygon;
    }

    internal static Coordinate[] CleanLineCoordinates(
        Coordinate[] source, CoordinateLayout layout,
        RepairParameters parameters)
    {
        Coordinate[] coordinates = CleanCoordinateArray(
            source, layout, parameters);
        if (coordinates.Length < 2) return [];
        return coordinates;
    }

    internal static Coordinate[] CleanRingCoordinates(
        Coordinate[] source, CoordinateLayout layout,
        RepairParameters parameters)
    {
        Coordinate[] coordinates = CleanCoordinateArray(
            source, layout, parameters);
        if (coordinates.Length == 0) return [];
        Coordinate first = coordinates[0];
        Coordinate last = coordinates[coordinates.Length - 1];
        if (!RobustLineIntersection.CoordinatesEqualPlanar(first, last))
        {
            List<Coordinate> closed = new List<Coordinate>(coordinates);
            closed.Add(CoordinateSequences.CopyCoordinateValue(first));
            coordinates = closed.ToArray();
        }
        if (coordinates.Length < 4 ||
            !GeometryInspection.HasThreeDistinctPlanarCoordinates(
                coordinates))
        {
            return [];
        }
        return coordinates;
    }

    internal static Coordinate[] CleanCoordinateArray(
        Coordinate[] source, CoordinateLayout layout,
        RepairParameters parameters)
    {
        if (source == null)
        {
            throw new InvalidGeometryException(
                "Coordinate sequence is null.");
        }
        List<Coordinate> coordinates = new List<Coordinate>();
        int index = 0;
        while (index < source.Length)
        {
            Coordinate coordinate = source[index];
            if (coordinate == null)
            {
                if (!parameters.RemoveNonFiniteCoordinates)
                {
                    throw new InvalidGeometryException(
                        "Coordinate is null and cleanup is disabled.");
                }
            }
            else if (CoordinateCanBeRetained(
                coordinate, layout, parameters))
            {
                bool repeated = false;
                if (parameters.RemoveRepeatedCoordinates &&
                    coordinates.Count != 0)
                {
                    repeated = RobustLineIntersection.
                        CoordinatesEqualPlanar(
                            coordinates[coordinates.Count - 1],
                            coordinate);
                }
                if (!repeated)
                {
                    coordinates.Add(
                        CoordinateSequences.CopyCoordinateValue(coordinate));
                }
            }
            index++;
        }
        return coordinates.ToArray();
    }

    internal static bool CoordinateCanBeRetained(
        Coordinate coordinate, CoordinateLayout layout,
        RepairParameters parameters)
    {
        bool finite = double.IsFinite(coordinate.X) &&
            double.IsFinite(coordinate.Y);
        if (layout == CoordinateLayout.XYZ ||
            layout == CoordinateLayout.XYZM)
        {
            finite = finite && double.IsFinite(coordinate.Z);
        }
        if (layout == CoordinateLayout.XYM ||
            layout == CoordinateLayout.XYZM)
        {
            finite = finite && double.IsFinite(coordinate.M);
        }
        if (finite) return true;
        if (parameters.RemoveNonFiniteCoordinates) return false;
        throw new InvalidGeometryException(
            "A coordinate contains a non-finite ordinate and cleanup is disabled.");
    }

    internal static GeometryRepairComponents CollectRepairComponents(
        Geometry geometry)
    {
        GeometryRepairComponents components =
            new GeometryRepairComponents();
        CollectRepairGeometryNode(geometry, components);
        return components;
    }

    internal static void CollectRepairGeometryNode(
        Geometry geometry, GeometryRepairComponents components)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(geometry);
        int index = 0;
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                if (!GeometryInspection.IsEmptyValue(geometry))
                {
                    components.Points.Add(GeometryCopy.CopyGeometry(geometry));
                }
                return;
            case GeometryType.LineString:
            case GeometryType.LinearRing:
                if (!GeometryInspection.IsEmptyValue(geometry))
                {
                    Coordinate[] lineCoordinates = geometry.GeometryType ==
                        GeometryType.LineString ?
                        ((LineString)payload).Coordinates :
                        ((LinearRing)payload).Coordinates;
                    components.Lines.Add(
                        GeometryBuilder.CreateLineStringValue(
                            lineCoordinates, options));
                }
                return;
            case GeometryType.Polygon:
                if (!GeometryInspection.IsEmptyValue(geometry))
                {
                    components.Polygons.Add(
                        GeometryCopy.CopyGeometry(geometry));
                }
                return;
            case GeometryType.MultiPoint:
                MultiPoint multiPoint = (MultiPoint)payload;
                while (index < multiPoint.Points.Length)
                {
                    if (multiPoint.Points[index].Coordinate != null)
                    {
                        components.Points.Add(
                            GeometryBuilder.CreatePointValue(
                                multiPoint.Points[index].Coordinate,
                                options));
                    }
                    index++;
                }
                return;
            case GeometryType.MultiLineString:
                MultiLineString multiLine = (MultiLineString)payload;
                while (index < multiLine.LineStrings.Length)
                {
                    if (multiLine.LineStrings[index].Coordinates.Length != 0)
                    {
                        components.Lines.Add(
                            GeometryBuilder.CreateLineStringValue(
                                multiLine.LineStrings[index].Coordinates,
                                options));
                    }
                    index++;
                }
                return;
            case GeometryType.MultiPolygon:
                MultiPolygon multiPolygon = (MultiPolygon)payload;
                while (index < multiPolygon.Polygons.Length)
                {
                    if (multiPolygon.Polygons[index].Shell.Coordinates.Length !=
                        0)
                    {
                        components.Polygons.Add(
                            GeometryBuilder.CreatePolygonValue(
                                multiPolygon.Polygons[index].Shell,
                                multiPolygon.Polygons[index].Holes,
                                options));
                    }
                    index++;
                }
                return;
            case GeometryType.GeometryCollection:
                GeometryCollection collection = (GeometryCollection)payload;
                while (index < collection.Geometries.Length)
                {
                    CollectRepairGeometryNode(
                        collection.Geometries[index], components);
                    index++;
                }
                return;
            default:
                throw new InvalidGeometryException(
                    "Geometry repair supports core geometry types only.");
        }
    }

    internal static Geometry BuildLineworkArea(
        List<Geometry> polygons, Geometry metadataSource)
    {
        List<Coordinate[]> rings = new List<Coordinate[]>();
        int index = 0;
        while (index < polygons.Count)
        {
            Polygon polygon = (Polygon)GeometryDispatch.GetSelectedPayload(
                polygons[index]);
            rings.Add(polygon.Shell.Coordinates);
            int holeIndex = 0;
            while (holeIndex < polygon.Holes.Length)
            {
                rings.Add(polygon.Holes[holeIndex].Coordinates);
                holeIndex++;
            }
            index++;
        }
        return BuildAreaFromRingSequences(rings, metadataSource);
    }

    internal static Geometry BuildStructureArea(
        List<Geometry> polygons, Geometry metadataSource)
    {
        List<Geometry> repairedAreas = new List<Geometry>();
        int index = 0;
        while (index < polygons.Count)
        {
            Polygon polygon = (Polygon)GeometryDispatch.GetSelectedPayload(
                polygons[index]);
            Geometry shellArea = BuildAreaFromSingleRing(
                polygon.Shell.Coordinates, metadataSource);
            List<Geometry> intersectingHoles = new List<Geometry>();
            int holeIndex = 0;
            while (holeIndex < polygon.Holes.Length)
            {
                Geometry holeArea = BuildAreaFromSingleRing(
                    polygon.Holes[holeIndex].Coordinates,
                    metadataSource);
                if (!GeometryInspection.IsEmptyValue(holeArea))
                {
                    if (!GeometryInspection.IsEmptyValue(shellArea) &&
                        GeometryPredicateAlgorithms.Intersects(
                            shellArea, holeArea))
                    {
                        intersectingHoles.Add(holeArea);
                    }
                    else
                    {
                        repairedAreas.Add(holeArea);
                    }
                }
                holeIndex++;
            }
            if (!GeometryInspection.IsEmptyValue(shellArea))
            {
                Geometry holes = UnionAreaValues(
                    intersectingHoles, metadataSource);
                if (!GeometryInspection.IsEmptyValue(holes))
                {
                    shellArea = OverlayEngine.GetDifference(
                        shellArea, holes);
                }
                if (!GeometryInspection.IsEmptyValue(shellArea))
                {
                    repairedAreas.Add(shellArea);
                }
            }
            index++;
        }
        return UnionAreaValues(repairedAreas, metadataSource);
    }

    internal static Geometry BuildAreaFromSingleRing(
        Coordinate[] coordinates, Geometry metadataSource)
    {
        List<Coordinate[]> rings = new List<Coordinate[]>();
        rings.Add(coordinates);
        return BuildAreaFromRingSequences(rings, metadataSource);
    }

    internal static Geometry BuildAreaFromRingSequences(
        List<Coordinate[]> rings, Geometry metadataSource)
    {
        List<SegmentString> edges = new List<SegmentString>();
        int index = 0;
        while (index < rings.Count)
        {
            if (GeometrySplitter.HasUsableSegment(rings[index]) &&
                !RingCoordinatesAreCollinear(rings[index]))
            {
                edges.Add(SegmentNoding.CreateSegmentString(
                    rings[index], rings[index], index));
            }
            index++;
        }
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(metadataSource);
        if (edges.Count == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.Polygon, options);
        }
        SegmentString[] noded = IndexedNoder.NodeSelf(
            edges.ToArray(), false).First;
        List<LineString> lines = new List<LineString>();
        index = 0;
        while (index < noded.Length)
        {
            if (noded[index].Coordinates.Length >= 2)
            {
                LineString line = new LineString();
                line.Coordinates = CoordinateSequences.CopyCoordinateSequence(
                    noded[index].Coordinates);
                lines.Add(line);
            }
            index++;
        }
        if (lines.Count == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.Polygon, options);
        }
        Geometry linework = GeometryBuilder.CreateMultiLineStringValue(
            lines.ToArray(), options);
        return AreaBuilder.BuildGeometry(linework);
    }

    internal static Geometry UnionAreaValues(
        List<Geometry> areas, Geometry metadataSource)
    {
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(metadataSource);
        if (areas.Count == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.Polygon, options);
        }
        Geometry result = GeometryCopy.CopyGeometry(areas[0]);
        int index = 1;
        while (index < areas.Count)
        {
            result = OverlayEngine.GetUnion(result, areas[index]);
            index++;
        }
        return result;
    }

    internal static void CollectCollapsedComponents(
        Geometry geometry, RepairParameters parameters,
        List<Geometry> results)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(geometry);
        int index = 0;
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return;
            case GeometryType.LineString:
                AddCollapsedLineValue(
                    ((LineString)payload).Coordinates,
                    geometry.CoordinateLayout, parameters,
                    options, results);
                return;
            case GeometryType.LinearRing:
                AddCollapsedRingValue(
                    ((LinearRing)payload).Coordinates,
                    geometry.CoordinateLayout, parameters,
                    options, results);
                return;
            case GeometryType.Polygon:
                CollectCollapsedPolygonValues(
                    (Polygon)payload, geometry.CoordinateLayout,
                    parameters, options, results);
                return;
            case GeometryType.MultiLineString:
                MultiLineString multiLine = (MultiLineString)payload;
                while (index < multiLine.LineStrings.Length)
                {
                    AddCollapsedLineValue(
                        multiLine.LineStrings[index].Coordinates,
                        geometry.CoordinateLayout, parameters,
                        options, results);
                    index++;
                }
                return;
            case GeometryType.MultiPolygon:
                MultiPolygon multiPolygon = (MultiPolygon)payload;
                while (index < multiPolygon.Polygons.Length)
                {
                    CollectCollapsedPolygonValues(
                        multiPolygon.Polygons[index],
                        geometry.CoordinateLayout, parameters,
                        options, results);
                    index++;
                }
                return;
            case GeometryType.GeometryCollection:
                GeometryCollection collection = (GeometryCollection)payload;
                while (index < collection.Geometries.Length)
                {
                    CollectCollapsedComponents(
                        collection.Geometries[index], parameters, results);
                    index++;
                }
                return;
            default:
                throw new InvalidGeometryException(
                    "Collapsed-component collection supports core geometry types only.");
        }
    }

    internal static void CollectCollapsedPolygonValues(
        Polygon polygon, CoordinateLayout layout,
        RepairParameters parameters, GeometryFactoryOptions options,
        List<Geometry> results)
    {
        AddCollapsedRingValue(
            polygon.Shell.Coordinates, layout,
            parameters, options, results);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            AddCollapsedRingValue(
                polygon.Holes[index].Coordinates, layout,
                parameters, options, results);
            index++;
        }
    }

    internal static void AddCollapsedLineValue(
        Coordinate[] source, CoordinateLayout layout,
        RepairParameters parameters, GeometryFactoryOptions options,
        List<Geometry> results)
    {
        Coordinate[] cleaned = CleanCoordinateArray(
            source, layout, parameters);
        if (cleaned.Length == 1)
        {
            results.Add(GeometryBuilder.CreatePointValue(
                cleaned[0], options));
        }
    }

    internal static void AddCollapsedRingValue(
        Coordinate[] source, CoordinateLayout layout,
        RepairParameters parameters, GeometryFactoryOptions options,
        List<Geometry> results)
    {
        Coordinate[] cleaned = CleanCoordinateArray(
            source, layout, parameters);
        if (cleaned.Length > 1 &&
            RobustLineIntersection.CoordinatesEqualPlanar(
                cleaned[0], cleaned[cleaned.Length - 1]))
        {
            Coordinate[] open = new Coordinate[cleaned.Length - 1];
            int index = 0;
            while (index < open.Length)
            {
                open[index] = CoordinateSequences.CopyCoordinateValue(
                    cleaned[index]);
                index++;
            }
            cleaned = open;
        }
        bool collapsed = cleaned.Length < 3 ||
            RingCoordinatesAreCollinear(cleaned);
        if (!collapsed) return;
        if (cleaned.Length == 1)
        {
            results.Add(GeometryBuilder.CreatePointValue(
                cleaned[0], options));
        }
        else if (cleaned.Length >= 2)
        {
            results.Add(GeometryBuilder.CreateLineStringValue(
                cleaned, options));
        }
    }

    internal static bool RingCoordinatesAreCollinear(
        Coordinate[] coordinates)
    {
        if (coordinates.Length < 3) return true;
        int secondIndex = 1;
        while (secondIndex < coordinates.Length &&
            RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[0], coordinates[secondIndex]))
        {
            secondIndex++;
        }
        if (secondIndex == coordinates.Length) return true;
        int index = secondIndex + 1;
        while (index < coordinates.Length)
        {
            if (RobustOrientation.GetOrientationIndex(
                coordinates[0], coordinates[secondIndex],
                coordinates[index]) != 0)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static void AddNonEmptyLeaves(
        Geometry geometry, List<Geometry> output)
    {
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        int index = 0;
        while (index < leaves.Length)
        {
            if (!GeometryInspection.IsEmptyValue(leaves[index]))
            {
                output.Add(GeometryCopy.CopyGeometry(leaves[index]));
            }
            index++;
        }
    }

    internal static Geometry CreateNarrowestResult(
        List<Geometry> components, Geometry metadataSource,
        GeometryType emptyType)
    {
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(metadataSource);
        if (components.Count == 0)
        {
            return GeometryBuilder.CreateEmptyValue(emptyType, options);
        }
        return FactoryConstructions.CreateNarrowestCollectionValue(
            components.ToArray(), options);
    }

    internal static GeometryType GetNominalEmptyType(Geometry geometry)
    {
        TopologicalDimension dimension =
            OverlayEngine.GetNominalDimension(geometry);
        switch (dimension)
        {
            case TopologicalDimension.Point:
                return GeometryType.Point;
            case TopologicalDimension.Curve:
                return GeometryType.LineString;
            case TopologicalDimension.Surface:
                return GeometryType.Polygon;
            default:
                return GeometryType.GeometryCollection;
        }
    }

    internal static List<Geometry> OrderCoverageComponents(
        List<Geometry> polygons, CoverageMergeStrategy strategy)
    {
        List<Geometry> ordered = new List<Geometry>();
        bool[] used = new bool[polygons.Count];
        int outputIndex = 0;
        while (outputIndex < polygons.Count)
        {
            int selected = -1;
            int index = 0;
            while (index < polygons.Count)
            {
                if (!used[index] && CoverageCandidateIsPreferred(
                    polygons, strategy, index, selected))
                {
                    selected = index;
                }
                index++;
            }
            used[selected] = true;
            ordered.Add(GeometryCopy.CopyGeometry(polygons[selected]));
            outputIndex++;
        }
        return ordered;
    }

    internal static bool CoverageCandidateIsPreferred(
        List<Geometry> polygons, CoverageMergeStrategy strategy,
        int candidate, int selected)
    {
        if (selected < 0) return true;
        switch (strategy)
        {
            case CoverageMergeStrategy.KeepFirst:
                return candidate < selected;
            case CoverageMergeStrategy.KeepLast:
                return candidate > selected;
            case CoverageMergeStrategy.LargestArea:
                double candidateArea =
                    GeometryMeasureAlgorithms.GetProjectedArea(
                        polygons[candidate]);
                double selectedArea =
                    GeometryMeasureAlgorithms.GetProjectedArea(
                        polygons[selected]);
                return candidateArea > selectedArea ||
                    (candidateArea == selectedArea && candidate < selected);
            default:
                throw new InvalidGeometryException(
                    "Coverage merge strategy is not supported.");
        }
    }
}
