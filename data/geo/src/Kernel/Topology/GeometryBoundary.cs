using System.Collections.Generic;

namespace geo;

internal class BoundaryEndpointNode
{
    internal Coordinate Coordinate = new Coordinate();
    internal int Valence;
}

internal static class GeometryBoundary
{
    internal static Geometry CreateBoundaryValue(Geometry geometry, BoundaryNodeRule boundaryNodeRule)
    {
        ValidateBoundaryNodeRule(boundaryNodeRule);
        GeometryInspection.EnsureGeometryGraph(geometry);
        GeometryFactoryOptions options = CreateBoundaryOptions(geometry);
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return GeometryBuilder.CreateEmptyValue(GeometryType.GeometryCollection, options);
            case GeometryType.LineString:
                return CreateLineBoundaryValue((LineString)payload, boundaryNodeRule, options);
            case GeometryType.LinearRing:
                LinearRing ring = (LinearRing)payload;
                LineString ringLine = new LineString();
                ringLine.Coordinates = ring.Coordinates;
                return CreateLineBoundaryValue(ringLine, boundaryNodeRule, options);
            case GeometryType.MultiLineString:
                return CreateMultiLineBoundaryValue((MultiLineString)payload, boundaryNodeRule, options);
            case GeometryType.Polygon:
                return CreatePolygonBoundaryValue((Polygon)payload, options);
            case GeometryType.MultiPolygon:
                return CreateMultiPolygonBoundaryValue((MultiPolygon)payload, options);
            case GeometryType.Triangle:
                ValidateSurfaceBoundaryStructure(geometry);
                return CreateTriangleBoundaryValue(
                    (Triangle)payload, options);
            case GeometryType.Tin:
                ValidateSurfaceBoundaryStructure(geometry);
                return CreateTinBoundaryValue((Tin)payload, options);
            case GeometryType.PolyhedralSurface:
                ValidateSurfaceBoundaryStructure(geometry);
                return CreatePolyhedralBoundaryValue(
                    (PolyhedralSurface)payload, options);
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
                return CreateCurveBoundaryValue(
                    geometry, boundaryNodeRule, options);
            case GeometryType.CurvePolygon:
                return CreateCurvePolygonBoundaryValue(
                    (CurvePolygon)payload, options);
            case GeometryType.MultiCurve:
                return CreateMultiCurveBoundaryValue(
                    (MultiCurve)payload, boundaryNodeRule, options);
            case GeometryType.MultiSurface:
                return CreateMultiSurfaceBoundaryValue(
                    (MultiSurface)payload, options);
            case GeometryType.GeometryCollection:
                if (GeometryInspection.IsEmptyValue(geometry))
                {
                    return GeometryBuilder.CreateEmptyValue(GeometryType.GeometryCollection, options);
                }
                throw new InvalidGeometryException("OGC boundary is not defined for a non-empty GeometryCollection.");
            default:
                throw new InvalidGeometryException("Boundary construction currently supports core linear geometry types.");
        }
    }

    internal static Geometry CreateLineBoundaryValue(LineString lineString, BoundaryNodeRule boundaryNodeRule, GeometryFactoryOptions options)
    {
        List<BoundaryEndpointNode> nodes = new List<BoundaryEndpointNode>();
        Dictionary<string, int> indexes = new Dictionary<string, int>();
        AddLineEndpoints(lineString, nodes, indexes, options.CoordinateLayout);
        return CreateEndpointBoundary(nodes, boundaryNodeRule, options);
    }

    internal static Geometry CreateMultiLineBoundaryValue(MultiLineString multiLineString, BoundaryNodeRule boundaryNodeRule, GeometryFactoryOptions options)
    {
        if (multiLineString == null || multiLineString.LineStrings == null)
        {
            throw new InvalidGeometryException("MultiLineString payload or component array is null.");
        }

        List<BoundaryEndpointNode> nodes = new List<BoundaryEndpointNode>();
        Dictionary<string, int> indexes = new Dictionary<string, int>();
        int index = 0;
        while (index < multiLineString.LineStrings.Length)
        {
            AddLineEndpoints(multiLineString.LineStrings[index], nodes, indexes, options.CoordinateLayout);
            index++;
        }

        return CreateEndpointBoundary(nodes, boundaryNodeRule, options);
    }

    internal static Geometry CreateCurveBoundaryValue(
        Geometry curve, BoundaryNodeRule boundaryNodeRule,
        GeometryFactoryOptions options)
    {
        List<BoundaryEndpointNode> nodes =
            new List<BoundaryEndpointNode>();
        Dictionary<string, int> indexes =
            new Dictionary<string, int>();
        AddCurveEndpoints(curve, nodes, indexes);
        return CreateEndpointBoundary(
            nodes, boundaryNodeRule, options);
    }

    internal static Geometry CreateMultiCurveBoundaryValue(
        MultiCurve multiCurve, BoundaryNodeRule boundaryNodeRule,
        GeometryFactoryOptions options)
    {
        if (multiCurve == null || multiCurve.Curves == null)
        {
            throw new InvalidGeometryException(
                "MultiCurve payload or component array is null.");
        }
        List<BoundaryEndpointNode> nodes =
            new List<BoundaryEndpointNode>();
        Dictionary<string, int> indexes =
            new Dictionary<string, int>();
        int index = 0;
        while (index < multiCurve.Curves.Length)
        {
            AddCurveEndpoints(
                multiCurve.Curves[index], nodes, indexes);
            index++;
        }
        return CreateEndpointBoundary(
            nodes, boundaryNodeRule, options);
    }

    internal static void AddCurveEndpoints(
        Geometry curve, List<BoundaryEndpointNode> nodes,
        Dictionary<string, int> indexes)
    {
        CurveAggregateAlgorithms.ValidateCurveValue(curve);
        Coordinate[] coordinates = CoordinateSequences.ExtractCoordinates(
            curve, GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        if (coordinates.Length == 0) return;
        AddEndpointNode(coordinates[0], nodes, indexes);
        AddEndpointNode(
            coordinates[coordinates.Length - 1], nodes, indexes);
    }

    internal static Geometry CreateCurvePolygonBoundaryValue(
        CurvePolygon polygon, GeometryFactoryOptions options)
    {
        CurveAggregateAlgorithms.ValidateCurvePolygonValue(polygon);
        List<Geometry> curves = new List<Geometry>();
        if (polygon.Shell != null)
        {
            curves.Add(GeometryCopy.CopyGeometry(polygon.Shell));
        }
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            curves.Add(GeometryCopy.CopyGeometry(polygon.Holes[index]));
            index++;
        }
        return ExtendedGeometryBuilder.CreateMultiCurveValue(
            curves.ToArray(), options);
    }

    internal static Geometry CreateMultiSurfaceBoundaryValue(
        MultiSurface multiSurface, GeometryFactoryOptions options)
    {
        if (multiSurface == null || multiSurface.Surfaces == null)
        {
            throw new InvalidGeometryException(
                "MultiSurface payload or component array is null.");
        }
        List<Geometry> curves = new List<Geometry>();
        int index = 0;
        while (index < multiSurface.Surfaces.Length)
        {
            AddSurfaceBoundaryCurves(
                multiSurface.Surfaces[index], curves, options);
            index++;
        }
        return ExtendedGeometryBuilder.CreateMultiCurveValue(
            curves.ToArray(), options);
    }

    internal static void AddSurfaceBoundaryCurves(
        Geometry surface, List<Geometry> curves,
        GeometryFactoryOptions options)
    {
        object payload = GeometryDispatch.GetSelectedPayload(surface);
        if (surface.GeometryType == GeometryType.Polygon)
        {
            Polygon polygon = (Polygon)payload;
            curves.Add(GeometryBuilder.CreateLineStringValue(
                polygon.Shell.Coordinates, options));
            int index = 0;
            while (index < polygon.Holes.Length)
            {
                curves.Add(GeometryBuilder.CreateLineStringValue(
                    polygon.Holes[index].Coordinates, options));
                index++;
            }
            return;
        }
        if (surface.GeometryType == GeometryType.Triangle)
        {
            Triangle triangle = (Triangle)payload;
            curves.Add(GeometryBuilder.CreateLineStringValue(
                triangle.Shell.Coordinates, options));
            return;
        }
        if (surface.GeometryType == GeometryType.CurvePolygon)
        {
            CurvePolygon polygon = (CurvePolygon)payload;
            CurveAggregateAlgorithms.ValidateCurvePolygonValue(polygon);
            if (polygon.Shell != null)
            {
                curves.Add(GeometryCopy.CopyGeometry(polygon.Shell));
            }
            int index = 0;
            while (index < polygon.Holes.Length)
            {
                curves.Add(GeometryCopy.CopyGeometry(
                    polygon.Holes[index]));
                index++;
            }
            return;
        }
        if (surface.GeometryType == GeometryType.Tin)
        {
            Geometry boundary = CreateTinBoundaryValue(
                (Tin)payload, options);
            AddLinearBoundaryCurves(boundary, curves, options);
            return;
        }
        if (surface.GeometryType == GeometryType.PolyhedralSurface)
        {
            Geometry boundary = CreatePolyhedralBoundaryValue(
                (PolyhedralSurface)payload, options);
            AddLinearBoundaryCurves(boundary, curves, options);
            return;
        }
        throw new InvalidGeometryException(
            "MultiSurface contains an unsupported surface component.");
    }

    internal static void AddLinearBoundaryCurves(
        Geometry boundary, List<Geometry> curves,
        GeometryFactoryOptions options)
    {
        if (boundary.GeometryType != GeometryType.MultiLineString ||
            boundary.MultiLineString == null)
        {
            throw new InvalidGeometryException(
                "Surface boundary did not produce linear curves.");
        }
        LineString[] lines = boundary.MultiLineString.LineStrings;
        int index = 0;
        while (index < lines.Length)
        {
            curves.Add(GeometryBuilder.CreateLineStringValue(
                lines[index].Coordinates, options));
            index++;
        }
    }

    internal static Geometry CreateEndpointBoundary(List<BoundaryEndpointNode> nodes, BoundaryNodeRule boundaryNodeRule, GeometryFactoryOptions options)
    {
        List<Point> points = new List<Point>();
        int index = 0;
        while (index < nodes.Count)
        {
            BoundaryEndpointNode node = nodes[index];
            if (ShouldIncludeEndpointNode(node.Valence, boundaryNodeRule))
            {
                Point point = new Point();
                point.Coordinate = CoordinateSequences.CopyCoordinateValue(node.Coordinate);
                points.Add(point);
            }

            index++;
        }

        return GeometryBuilder.CreateMultiPointValue(points.ToArray(), options);
    }

    internal static Geometry CreatePolygonBoundaryValue(Polygon polygon, GeometryFactoryOptions options)
    {
        List<LineString> lines = new List<LineString>();
        AddPolygonBoundaryLines(polygon, lines, options);
        return GeometryBuilder.CreateMultiLineStringValue(lines.ToArray(), options);
    }

    internal static Geometry CreateMultiPolygonBoundaryValue(MultiPolygon multiPolygon, GeometryFactoryOptions options)
    {
        if (multiPolygon == null || multiPolygon.Polygons == null)
        {
            throw new InvalidGeometryException("MultiPolygon payload or component array is null.");
        }

        List<LineString> lines = new List<LineString>();
        int index = 0;
        while (index < multiPolygon.Polygons.Length)
        {
            AddPolygonBoundaryLines(multiPolygon.Polygons[index], lines, options);
            index++;
        }

        return GeometryBuilder.CreateMultiLineStringValue(lines.ToArray(), options);
    }

    internal static Geometry CreateTriangleBoundaryValue(
        Triangle triangle, GeometryFactoryOptions options)
    {
        Polygon polygon = new Polygon();
        polygon.Shell = triangle.Shell;
        return CreatePolygonBoundaryValue(polygon, options);
    }

    internal static Geometry CreateTinBoundaryValue(
        Tin tin, GeometryFactoryOptions options)
    {
        Polygon[] patches = new Polygon[tin.Triangles.Length];
        int index = 0;
        while (index < tin.Triangles.Length)
        {
            Polygon patch = new Polygon();
            patch.Shell = tin.Triangles[index].Shell;
            patches[index] = patch;
            index++;
        }
        return CreateSurfaceBoundaryValue(
            patches, "Tin/Triangles", options);
    }

    internal static Geometry CreatePolyhedralBoundaryValue(
        PolyhedralSurface surface, GeometryFactoryOptions options)
    {
        return CreateSurfaceBoundaryValue(
            surface.Patches, "PolyhedralSurface/Patches", options);
    }

    internal static Geometry CreateSurfaceBoundaryValue(
        Polygon[] patches, string pathPrefix,
        GeometryFactoryOptions options)
    {
        string[] paths = new string[patches.Length];
        int index = 0;
        while (index < paths.Length)
        {
            paths[index] = pathPrefix + "[" + index + "]";
            index++;
        }
        Dictionary<string, SurfaceEdgeUse> uses =
            SurfaceTopologyValidator.CreateSurfaceEdges(
                patches, paths);
        List<SurfaceEdgeUse> exposed =
            new List<SurfaceEdgeUse>();
        foreach (SurfaceEdgeUse edge in uses.Values)
        {
            if (edge.Count > 2)
            {
                throw new InvalidGeometryException(
                    "More than two surface patches share an edge.");
            }
            if (edge.Count == 1) exposed.Add(edge);
        }
        LineString[] lines = CreateSurfaceBoundaryLines(exposed);
        return GeometryBuilder.CreateMultiLineStringValue(
            lines, options);
    }

    internal static LineString[] CreateSurfaceBoundaryLines(
        List<SurfaceEdgeUse> edges)
    {
        List<LineString> lines = new List<LineString>();
        bool[] used = new bool[edges.Count];
        int index = 0;
        while (index < edges.Count)
        {
            if (!used[index])
            {
                List<Coordinate> coordinates =
                    new List<Coordinate>();
                coordinates.Add(
                    CoordinateSequences.CopyCoordinateValue(
                        edges[index].Start));
                coordinates.Add(
                    CoordinateSequences.CopyCoordinateValue(
                        edges[index].End));
                used[index] = true;
                AppendSurfaceBoundaryEdges(
                    coordinates, edges, used);
                PrependSurfaceBoundaryEdges(
                    coordinates, edges, used);
                LineString line = new LineString();
                line.Coordinates = coordinates.ToArray();
                lines.Add(line);
            }
            index++;
        }
        return lines.ToArray();
    }

    internal static void AppendSurfaceBoundaryEdges(
        List<Coordinate> coordinates,
        List<SurfaceEdgeUse> edges, bool[] used)
    {
        bool connected = true;
        while (connected)
        {
            connected = false;
            Coordinate endpoint =
                coordinates[coordinates.Count - 1];
            int index = FindConnectedSurfaceEdge(
                endpoint, edges, used);
            if (index >= 0)
            {
                SurfaceEdgeUse edge = edges[index];
                string endpointIdentity = SurfaceTopologyValidator.
                    GetSurfaceCoordinateIdentity(endpoint);
                string startIdentity = SurfaceTopologyValidator.
                    GetSurfaceCoordinateIdentity(edge.Start);
                Coordinate next = endpointIdentity == startIdentity ?
                    edge.End : edge.Start;
                coordinates.Add(
                    CoordinateSequences.CopyCoordinateValue(next));
                used[index] = true;
                connected = true;
            }
        }
    }

    internal static void PrependSurfaceBoundaryEdges(
        List<Coordinate> coordinates,
        List<SurfaceEdgeUse> edges, bool[] used)
    {
        bool connected = true;
        while (connected)
        {
            connected = false;
            Coordinate endpoint = coordinates[0];
            int index = FindConnectedSurfaceEdge(
                endpoint, edges, used);
            if (index >= 0)
            {
                SurfaceEdgeUse edge = edges[index];
                string endpointIdentity = SurfaceTopologyValidator.
                    GetSurfaceCoordinateIdentity(endpoint);
                string startIdentity = SurfaceTopologyValidator.
                    GetSurfaceCoordinateIdentity(edge.Start);
                Coordinate next = endpointIdentity == startIdentity ?
                    edge.End : edge.Start;
                coordinates.Insert(0,
                    CoordinateSequences.CopyCoordinateValue(next));
                used[index] = true;
                connected = true;
            }
        }
    }

    internal static int FindConnectedSurfaceEdge(
        Coordinate endpoint, List<SurfaceEdgeUse> edges,
        bool[] used)
    {
        string endpointIdentity = SurfaceTopologyValidator.
            GetSurfaceCoordinateIdentity(endpoint);
        int index = 0;
        while (index < edges.Count)
        {
            if (!used[index])
            {
                string startIdentity = SurfaceTopologyValidator.
                    GetSurfaceCoordinateIdentity(edges[index].Start);
                string endIdentity = SurfaceTopologyValidator.
                    GetSurfaceCoordinateIdentity(edges[index].End);
                if (endpointIdentity == startIdentity ||
                    endpointIdentity == endIdentity)
                {
                    return index;
                }
            }
            index++;
        }
        return -1;
    }

    internal static void ValidateSurfaceBoundaryStructure(
        Geometry geometry)
    {
        ValidationResult validation =
            StructuralValidator.ValidateGeometry(geometry);
        if (!validation.IsValid)
        {
            throw new InvalidGeometryException(
                "Surface boundary requires structurally valid geometry: " +
                validation.Message);
        }
    }

    internal static void AddLineEndpoints(LineString lineString, List<BoundaryEndpointNode> nodes, Dictionary<string, int> indexes, CoordinateLayout coordinateLayout)
    {
        if (lineString == null || lineString.Coordinates == null)
        {
            throw new InvalidGeometryException("LineString payload or coordinate sequence is null.");
        }

        if (lineString.Coordinates.Length == 0)
        {
            return;
        }

        if (lineString.Coordinates.Length < 2)
        {
            throw new InvalidGeometryException("A LineString must be empty or contain at least two coordinates.");
        }

        int coordinateIndex = 0;
        while (coordinateIndex < lineString.Coordinates.Length)
        {
            GeometryBuilder.ValidateCoordinateForLayout(lineString.Coordinates[coordinateIndex], coordinateLayout);
            coordinateIndex++;
        }

        AddEndpointNode(lineString.Coordinates[0], nodes, indexes);
        AddEndpointNode(lineString.Coordinates[lineString.Coordinates.Length - 1], nodes, indexes);
    }

    internal static void AddEndpointNode(Coordinate coordinate, List<BoundaryEndpointNode> nodes, Dictionary<string, int> indexes)
    {
        string identity = GetPlanarEndpointIdentity(coordinate);
        if (indexes.ContainsKey(identity))
        {
            int existingIndex = indexes[identity];
            nodes[existingIndex].Valence++;
            return;
        }

        BoundaryEndpointNode node = new BoundaryEndpointNode();
        node.Coordinate = CoordinateSequences.CopyCoordinateValue(coordinate);
        node.Valence = 1;
        indexes.Add(identity, nodes.Count);
        nodes.Add(node);
    }

    internal static void AddPolygonBoundaryLines(Polygon polygon, List<LineString> lines, GeometryFactoryOptions options)
    {
        if (polygon == null)
        {
            throw new InvalidGeometryException("Polygon payload is null.");
        }

        Polygon copy = GeometryBuilder.CopyPolygonPayload(polygon.Shell, polygon.Holes, options);
        if (copy.Shell.Coordinates.Length == 0)
        {
            return;
        }

        LineString shell = new LineString();
        shell.Coordinates = CoordinateSequences.CopyCoordinateSequence(copy.Shell.Coordinates);
        lines.Add(shell);
        int index = 0;
        while (index < copy.Holes.Length)
        {
            LineString hole = new LineString();
            hole.Coordinates = CoordinateSequences.CopyCoordinateSequence(copy.Holes[index].Coordinates);
            lines.Add(hole);
            index++;
        }
    }

    internal static GeometryFactoryOptions CreateBoundaryOptions(Geometry geometry)
    {
        GeometryFactoryOptions options = new GeometryFactoryOptions();
        options.CoordinateLayout = geometry.CoordinateLayout;
        options.SpatialReferenceId = geometry.SpatialReferenceId;
        options.PrecisionModelType = PrecisionModelType.Floating;
        return options;
    }

    internal static bool ShouldIncludeEndpointNode(int valence, BoundaryNodeRule boundaryNodeRule)
    {
        switch (boundaryNodeRule)
        {
            case BoundaryNodeRule.Mod2:
                return valence % 2 == 1;
            case BoundaryNodeRule.Endpoint:
                return valence > 0;
            case BoundaryNodeRule.MultivalentEndpoint:
                return valence > 1;
            case BoundaryNodeRule.MonovalentEndpoint:
                return valence == 1;
            default:
                throw new InvalidGeometryException("Boundary node rule is not supported.");
        }
    }

    internal static void ValidateBoundaryNodeRule(BoundaryNodeRule boundaryNodeRule)
    {
        switch (boundaryNodeRule)
        {
            case BoundaryNodeRule.Mod2:
            case BoundaryNodeRule.Endpoint:
            case BoundaryNodeRule.MultivalentEndpoint:
            case BoundaryNodeRule.MonovalentEndpoint:
                return;
            default:
                throw new InvalidGeometryException("Boundary node rule is not supported.");
        }
    }

    internal static string GetPlanarEndpointIdentity(Coordinate coordinate)
    {
        return $"{GetPlanarOrdinateIdentity(coordinate.X)}:{GetPlanarOrdinateIdentity(coordinate.Y)}";
    }

    internal static string GetPlanarOrdinateIdentity(double ordinate)
    {
        if (ordinate == 0.0)
        {
            return "0";
        }

        return System.BitConverter.DoubleToInt64Bits(ordinate).ToString();
    }
}
