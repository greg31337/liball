using System;
using System.Collections.Generic;

namespace geo;

internal class StructuralValidationState
{
    internal HashSet<Geometry> Active = new HashSet<Geometry>();
    internal ValidationResult? Failure;
    internal CoordinateLayout RootLayout;
    internal int RootSpatialReferenceId;
    internal int ComponentCount;
    internal int CoordinateCount;
    internal int MaximumDepth;
    internal int MaximumComponents;
    internal int MaximumCoordinates;
}

internal static class StructuralValidator
{
    internal static ValidationResult ValidateGeometry(Geometry? geometry)
    {
        return ValidateGeometryWithLimits(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents,
            GeometryConstants.DefaultMaximumCoordinates);
    }

    internal static ValidationResult ValidateGeometryWithLimits(
        Geometry? geometry, int maximumDepth,
        int maximumComponents, int maximumCoordinates)
    {
        if (maximumDepth < 0 || maximumComponents < 1 ||
            maximumCoordinates < 1)
        {
            throw new InvalidGeometryException(
                "Structural validation limits are invalid.");
        }
        if (geometry == null)
        {
            return CreateFailureResult(
                ValidationError.NullGeometry,
                "Geometry is null.", null, "Geometry");
        }
        StructuralValidationState state =
            new StructuralValidationState();
        state.RootLayout = geometry.CoordinateLayout;
        state.RootSpatialReferenceId = geometry.SpatialReferenceId;
        state.MaximumDepth = maximumDepth;
        state.MaximumComponents = maximumComponents;
        state.MaximumCoordinates = maximumCoordinates;
        VisitGeometryNode(
            geometry, 0, GetGeometryPath(geometry.GeometryType), state);
        if (state.Failure != null) return state.Failure;
        return new ValidationResult();
    }

    internal static void VisitGeometryNode(
        Geometry? geometry, int depth, string path,
        StructuralValidationState state)
    {
        if (state.Failure != null) return;
        if (geometry == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "Geometry component is null.", null, path);
            return;
        }
        if (state.Active.Contains(geometry))
        {
            SetFailure(state, ValidationError.CyclicGeometry,
                "Geometry contains a cyclic component reference.",
                null, path);
            return;
        }
        if (depth > state.MaximumDepth)
        {
            SetFailure(state, ValidationError.NestingLimitExceeded,
                "Maximum geometry nesting depth was exceeded.",
                null, path);
            return;
        }
        state.ComponentCount++;
        if (state.ComponentCount > state.MaximumComponents)
        {
            SetFailure(state, ValidationError.NestingLimitExceeded,
                "Maximum geometry component count was exceeded.",
                null, path);
            return;
        }
        if (!IsKnownGeometryType(geometry.GeometryType))
        {
            SetFailure(state, ValidationError.InvalidGeometryType,
                "Geometry type is not recognized.", null, path);
            return;
        }
        if (!PayloadMatchesGeometryType(geometry))
        {
            SetFailure(state, ValidationError.PayloadMismatch,
                "Geometry must contain exactly its selected payload.",
                null, path);
            return;
        }
        if (!IsKnownCoordinateLayout(geometry.CoordinateLayout))
        {
            SetFailure(state, ValidationError.InvalidCoordinateLayout,
                "Geometry coordinate layout is not recognized.",
                null, path);
            return;
        }
        if (geometry.SpatialReferenceId <
            GeometryConstants.UnknownSpatialReferenceId)
        {
            SetFailure(state, ValidationError.InvalidCoordinateLayout,
                "Geometry spatial reference identifier is invalid.",
                null, path);
            return;
        }
        if (depth > 0 &&
            (geometry.CoordinateLayout != state.RootLayout ||
            geometry.SpatialReferenceId !=
                state.RootSpatialReferenceId))
        {
            SetFailure(state, ValidationError.InvalidCoordinateLayout,
                "Geometry component metadata does not match the root geometry.",
                null, path);
            return;
        }

        state.Active.Add(geometry);
        ValidateSelectedPayload(geometry, depth, path, state);
        state.Active.Remove(geometry);
    }

    internal static void ValidateSelectedPayload(
        Geometry geometry, int depth, string path,
        StructuralValidationState state)
    {
        int index = 0;
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                ValidatePointPayload(geometry.Point, path, state);
                return;
            case GeometryType.LineString:
                ValidateLinePayload(
                    geometry.LineString, path, state);
                return;
            case GeometryType.LinearRing:
                ValidateLinearRingPayload(
                    geometry.LinearRing, path, state);
                return;
            case GeometryType.Polygon:
                ValidatePolygonPayload(
                    geometry.Polygon, path, state);
                return;
            case GeometryType.MultiPoint:
                ValidateMultiPointPayload(
                    geometry.MultiPoint, path, state);
                return;
            case GeometryType.MultiLineString:
                ValidateMultiLinePayload(
                    geometry.MultiLineString, path, state);
                return;
            case GeometryType.MultiPolygon:
                ValidateMultiPolygonPayload(
                    geometry.MultiPolygon, path, state);
                return;
            case GeometryType.GeometryCollection:
                GeometryCollection collection =
                    (GeometryCollection)
                    GeometryDispatch.GetSelectedPayload(geometry);
                if (collection.Geometries == null)
                {
                    SetFailure(state, ValidationError.NullGeometry,
                        "GeometryCollection component array is null.",
                        null, path + "/Geometries");
                    return;
                }
                while (index < collection.Geometries.Length)
                {
                    VisitGeometryNode(
                        collection.Geometries[index], depth + 1,
                        path + "/Geometries[" + index + "]", state);
                    index++;
                }
                return;
            case GeometryType.Triangle:
                ValidateTrianglePayload(
                    geometry.Triangle, path, state);
                return;
            case GeometryType.Tin:
                ValidateTinPayload(geometry.Tin, path, state);
                return;
            case GeometryType.PolyhedralSurface:
                ValidatePolyhedralPayload(
                    geometry.PolyhedralSurface, path, state);
                return;
            case GeometryType.CircularString:
                ValidateCircularPayload(
                    geometry.CircularString, path, state);
                return;
            case GeometryType.CompoundCurve:
                ValidateCompoundPayload(
                    geometry.CompoundCurve, depth, path, state);
                return;
            case GeometryType.CurvePolygon:
                ValidateCurvePolygonPayload(
                    geometry.CurvePolygon, depth, path, state);
                return;
            case GeometryType.MultiCurve:
                ValidateMultiCurvePayload(
                    geometry.MultiCurve, depth, path, state);
                return;
            case GeometryType.MultiSurface:
                ValidateMultiSurfacePayload(
                    geometry.MultiSurface, depth, path, state);
                return;
            default:
                SetFailure(state, ValidationError.InvalidGeometryType,
                    "Geometry type is not recognized.", null, path);
                return;
        }
    }

    internal static void ValidatePointPayload(
        Point? point, string path, StructuralValidationState state)
    {
        if (point == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "Point payload is null.", null, path);
            return;
        }
        if (point.Coordinate != null)
        {
            ValidateCoordinateValue(
                point.Coordinate, path + "/Coordinate", state);
        }
    }

    internal static void ValidateLinePayload(
        LineString? line, string path,
        StructuralValidationState state)
    {
        if (line == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "LineString payload is null.", null, path);
            return;
        }
        ValidateCoordinateArray(
            line.Coordinates, 2, path + "/Coordinates", state);
    }

    internal static void ValidateLinearRingPayload(
        LinearRing? ring, string path,
        StructuralValidationState state)
    {
        if (ring == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "LinearRing payload is null.", null, path);
            return;
        }
        ValidateRingCoordinates(
            ring.Coordinates, path + "/Coordinates", state);
    }

    internal static void ValidatePolygonPayload(
        Polygon? polygon, string path,
        StructuralValidationState state)
    {
        if (polygon == null || polygon.Shell == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "Polygon payload or shell is null.",
                null, path + "/Shell");
            return;
        }
        if (polygon.Holes == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "Polygon hole array is null.",
                null, path + "/Holes");
            return;
        }
        RegisterPayloadComponent(path + "/Shell", state);
        ValidateRingCoordinates(
            polygon.Shell.Coordinates, path + "/Shell", state);
        if (state.Failure != null) return;
        if (polygon.Shell.Coordinates.Length == 0 &&
            polygon.Holes.Length != 0)
        {
            SetFailure(state, ValidationError.TooFewCoordinates,
                "An empty polygon shell cannot contain holes.",
                null, path + "/Shell");
            return;
        }
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            string holePath = path + "/Holes[" + index + "]";
            RegisterPayloadComponent(holePath, state);
            LinearRing? hole = polygon.Holes[index];
            if (hole == null)
            {
                SetFailure(state, ValidationError.NullGeometry,
                    "Polygon hole is null.", null, holePath);
                return;
            }
            ValidateRingCoordinates(
                hole.Coordinates, holePath, state);
            if (state.Failure != null) return;
            index++;
        }
    }

    internal static void ValidateMultiPointPayload(
        MultiPoint? multiPoint, string path,
        StructuralValidationState state)
    {
        if (multiPoint == null || multiPoint.Points == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "MultiPoint payload or component array is null.",
                null, path + "/Points");
            return;
        }
        int index = 0;
        while (index < multiPoint.Points.Length)
        {
            string itemPath = path + "/Points[" + index + "]";
            RegisterPayloadComponent(itemPath, state);
            ValidatePointPayload(
                multiPoint.Points[index], itemPath, state);
            if (state.Failure != null) return;
            index++;
        }
    }

    internal static void ValidateMultiLinePayload(
        MultiLineString? multiLine, string path,
        StructuralValidationState state)
    {
        if (multiLine == null || multiLine.LineStrings == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "MultiLineString payload or component array is null.",
                null, path + "/LineStrings");
            return;
        }
        int index = 0;
        while (index < multiLine.LineStrings.Length)
        {
            string itemPath = path + "/LineStrings[" + index + "]";
            RegisterPayloadComponent(itemPath, state);
            ValidateLinePayload(
                multiLine.LineStrings[index], itemPath, state);
            if (state.Failure != null) return;
            index++;
        }
    }

    internal static void ValidateMultiPolygonPayload(
        MultiPolygon? multiPolygon, string path,
        StructuralValidationState state)
    {
        if (multiPolygon == null || multiPolygon.Polygons == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "MultiPolygon payload or component array is null.",
                null, path + "/Polygons");
            return;
        }
        int index = 0;
        while (index < multiPolygon.Polygons.Length)
        {
            string itemPath = path + "/Polygons[" + index + "]";
            RegisterPayloadComponent(itemPath, state);
            ValidatePolygonPayload(
                multiPolygon.Polygons[index], itemPath, state);
            if (state.Failure != null) return;
            index++;
        }
    }

    internal static void ValidateTrianglePayload(
        Triangle? triangle, string path,
        StructuralValidationState state)
    {
        if (triangle == null || triangle.Shell == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "Triangle payload or shell is null.",
                null, path + "/Shell");
            return;
        }
        RegisterPayloadComponent(path + "/Shell", state);
        ValidateRingCoordinates(
            triangle.Shell.Coordinates, path + "/Shell", state);
    }

    internal static void ValidateTinPayload(
        Tin? tin, string path, StructuralValidationState state)
    {
        if (tin == null || tin.Triangles == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "TIN payload or patch array is null.",
                null, path + "/Triangles");
            return;
        }
        int index = 0;
        while (index < tin.Triangles.Length)
        {
            string itemPath = path + "/Triangles[" + index + "]";
            RegisterPayloadComponent(itemPath, state);
            ValidateTrianglePayload(
                tin.Triangles[index], itemPath, state);
            if (state.Failure != null) return;
            index++;
        }
    }

    internal static void ValidatePolyhedralPayload(
        PolyhedralSurface? surface, string path,
        StructuralValidationState state)
    {
        if (surface == null || surface.Patches == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "PolyhedralSurface payload or patch array is null.",
                null, path + "/Patches");
            return;
        }
        int index = 0;
        while (index < surface.Patches.Length)
        {
            string itemPath = path + "/Patches[" + index + "]";
            RegisterPayloadComponent(itemPath, state);
            ValidatePolygonPayload(
                surface.Patches[index], itemPath, state);
            if (state.Failure != null) return;
            index++;
        }
    }

    internal static void ValidateCircularPayload(
        CircularString? circular, string path,
        StructuralValidationState state)
    {
        if (circular == null || circular.Coordinates == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "CircularString payload or coordinate array is null.",
                null, path + "/Coordinates");
            return;
        }
        ValidateCoordinateArray(
            circular.Coordinates, 0,
            path + "/Coordinates", state);
        if (state.Failure != null ||
            circular.Coordinates.Length == 0) return;
        if (circular.Coordinates.Length < 3 ||
            circular.Coordinates.Length % 2 == 0)
        {
            SetFailure(state, ValidationError.InvalidArcCardinality,
                "CircularString must have an odd number of at least three coordinates.",
                null, path + "/Coordinates");
        }
    }

    internal static void ValidateCompoundPayload(
        CompoundCurve? compound, int depth, string path,
        StructuralValidationState state)
    {
        if (compound == null || compound.Curves == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "CompoundCurve payload or component array is null.",
                null, path + "/Curves");
            return;
        }
        int index = 0;
        while (index < compound.Curves.Length)
        {
            string itemPath = path + "/Curves[" + index + "]";
            Geometry? curve = compound.Curves[index];
            if (curve != null &&
                IsKnownGeometryType(curve.GeometryType) &&
                !IsSimpleCurveType(curve.GeometryType))
            {
                SetFailure(state, ValidationError.PayloadMismatch,
                    "CompoundCurve contains a non-curve component.",
                    null, itemPath);
                return;
            }
            VisitGeometryNode(curve, depth + 1, itemPath, state);
            if (state.Failure != null) return;
            index++;
        }
    }

    internal static void ValidateCurvePolygonPayload(
        CurvePolygon? polygon, int depth, string path,
        StructuralValidationState state)
    {
        if (polygon == null || polygon.Holes == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "CurvePolygon payload or hole array is null.",
                null, path + "/Holes");
            return;
        }
        if (polygon.Shell == null)
        {
            if (polygon.Holes.Length != 0)
            {
                SetFailure(state, ValidationError.NullGeometry,
                    "An empty CurvePolygon cannot contain holes.",
                    null, path + "/Shell");
            }
            return;
        }
        ValidateCurveRingGeometry(
            polygon.Shell, depth + 1,
            path + "/Shell", state);
        if (state.Failure != null) return;
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            ValidateCurveRingGeometry(
                polygon.Holes[index], depth + 1,
                path + "/Holes[" + index + "]", state);
            if (state.Failure != null) return;
            index++;
        }
    }

    internal static void ValidateCurveRingGeometry(
        Geometry? curve, int depth, string path,
        StructuralValidationState state)
    {
        if (curve != null &&
            IsKnownGeometryType(curve.GeometryType) &&
            !IsRingCurveType(curve.GeometryType))
        {
            SetFailure(state, ValidationError.PayloadMismatch,
                "CurvePolygon ring contains a non-curve component.",
                null, path);
            return;
        }
        VisitGeometryNode(curve, depth, path, state);
        if (state.Failure != null || curve == null) return;
        List<Coordinate> controls = new List<Coordinate>();
        CollectCurveCoordinates(curve, controls);
        if (controls.Count == 0) return;
        if (controls.Count < 4)
        {
            SetFailure(state, ValidationError.TooFewCoordinates,
                "A curve polygon ring has too few coordinates.",
                null, path);
            return;
        }
        if (!RobustLineIntersection.CoordinatesEqualPlanar(
            controls[0], controls[controls.Count - 1]))
        {
            SetFailure(state, ValidationError.RingNotClosed,
                "A curve polygon ring is not closed.",
                controls[controls.Count - 1], path);
            return;
        }
        if (CountDistinctPlanarCoordinates(controls.ToArray()) < 3)
        {
            SetFailure(state,
                ValidationError.TooFewDistinctCoordinates,
                "A curve polygon ring has fewer than three distinct positions.",
                controls[0], path);
        }
    }

    internal static void ValidateMultiCurvePayload(
        MultiCurve? multiCurve, int depth, string path,
        StructuralValidationState state)
    {
        if (multiCurve == null || multiCurve.Curves == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "MultiCurve payload or component array is null.",
                null, path + "/Curves");
            return;
        }
        int index = 0;
        while (index < multiCurve.Curves.Length)
        {
            string itemPath = path + "/Curves[" + index + "]";
            Geometry? curve = multiCurve.Curves[index];
            if (curve != null &&
                IsKnownGeometryType(curve.GeometryType) &&
                !IsCurveType(curve.GeometryType))
            {
                SetFailure(state, ValidationError.PayloadMismatch,
                    "MultiCurve contains a non-curve component.",
                    null, itemPath);
                return;
            }
            VisitGeometryNode(curve, depth + 1, itemPath, state);
            if (state.Failure != null) return;
            index++;
        }
    }

    internal static void ValidateMultiSurfacePayload(
        MultiSurface? multiSurface, int depth, string path,
        StructuralValidationState state)
    {
        if (multiSurface == null || multiSurface.Surfaces == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "MultiSurface payload or component array is null.",
                null, path + "/Surfaces");
            return;
        }
        int index = 0;
        while (index < multiSurface.Surfaces.Length)
        {
            string itemPath = path + "/Surfaces[" + index + "]";
            Geometry? surface = multiSurface.Surfaces[index];
            if (surface != null &&
                IsKnownGeometryType(surface.GeometryType) &&
                !IsSurfaceType(surface.GeometryType))
            {
                SetFailure(state, ValidationError.PayloadMismatch,
                    "MultiSurface contains a non-surface component.",
                    null, itemPath);
                return;
            }
            VisitGeometryNode(surface, depth + 1, itemPath, state);
            if (state.Failure != null) return;
            index++;
        }
    }

    internal static void ValidateCoordinateArray(
        Coordinate[]? coordinates, int minimum,
        string path, StructuralValidationState state)
    {
        if (coordinates == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "Coordinate array is null.", null, path);
            return;
        }
        int index = 0;
        while (index < coordinates.Length)
        {
            ValidateCoordinateValue(
                coordinates[index], path + "[" + index + "]", state);
            if (state.Failure != null) return;
            index++;
        }
        if (coordinates.Length != 0 &&
            coordinates.Length < minimum)
        {
            SetFailure(state, ValidationError.TooFewCoordinates,
                "Coordinate sequence has too few positions.",
                coordinates[coordinates.Length - 1], path);
        }
    }

    internal static void ValidateRingCoordinates(
        Coordinate[]? coordinates, string path,
        StructuralValidationState state)
    {
        ValidateCoordinateArray(coordinates, 4, path, state);
        if (state.Failure != null || coordinates == null ||
            coordinates.Length == 0 || coordinates.Length < 4) return;
        if (!RobustLineIntersection.CoordinatesEqualPlanar(
            coordinates[0], coordinates[coordinates.Length - 1]))
        {
            SetFailure(state, ValidationError.RingNotClosed,
                "Ring coordinate sequence is not closed.",
                coordinates[coordinates.Length - 1], path);
            return;
        }
        if (CountDistinctPlanarCoordinates(coordinates) < 3)
        {
            SetFailure(state,
                ValidationError.TooFewDistinctCoordinates,
                "Ring has fewer than three distinct positions.",
                coordinates[0], path);
        }
    }

    internal static void ValidateCoordinateValue(
        Coordinate? coordinate, string path,
        StructuralValidationState state)
    {
        if (coordinate == null)
        {
            SetFailure(state, ValidationError.NullGeometry,
                "Coordinate is null.", null, path);
            return;
        }
        state.CoordinateCount++;
        if (state.CoordinateCount > state.MaximumCoordinates)
        {
            SetFailure(state, ValidationError.CoordinateLimitExceeded,
                "Maximum geometry coordinate count was exceeded.",
                coordinate, path);
            return;
        }
        if (!double.IsFinite(coordinate.X) ||
            !double.IsFinite(coordinate.Y))
        {
            SetFailure(state, ValidationError.NonFiniteCoordinate,
                "Coordinate X and Y ordinates must be finite.",
                coordinate, path);
            return;
        }
        bool valid = false;
        switch (state.RootLayout)
        {
            case CoordinateLayout.XY:
                valid = double.IsNaN(coordinate.Z) &&
                    double.IsNaN(coordinate.M);
                break;
            case CoordinateLayout.XYZ:
                valid = double.IsFinite(coordinate.Z) &&
                    double.IsNaN(coordinate.M);
                break;
            case CoordinateLayout.XYM:
                valid = double.IsNaN(coordinate.Z) &&
                    double.IsFinite(coordinate.M);
                break;
            case CoordinateLayout.XYZM:
                valid = double.IsFinite(coordinate.Z) &&
                    double.IsFinite(coordinate.M);
                break;
            default:
                break;
        }
        if (!valid)
        {
            SetFailure(state, ValidationError.InvalidCoordinateLayout,
                "Coordinate ordinates do not match the declared layout.",
                coordinate, path);
        }
    }

    internal static void RegisterPayloadComponent(
        string path, StructuralValidationState state)
    {
        if (state.Failure != null) return;
        state.ComponentCount++;
        if (state.ComponentCount > state.MaximumComponents)
        {
            SetFailure(state, ValidationError.NestingLimitExceeded,
                "Maximum geometry component count was exceeded.",
                null, path);
        }
    }

    internal static int CountDistinctPlanarCoordinates(
        Coordinate[] coordinates)
    {
        HashSet<string> distinct = new HashSet<string>();
        int index = 0;
        while (index < coordinates.Length)
        {
            PlanarNodeKey key = new PlanarNodeKey(coordinates[index]);
            distinct.Add(key.Identity);
            index++;
        }
        return distinct.Count;
    }

    internal static void CollectCurveCoordinates(
        Geometry curve, List<Coordinate> coordinates)
    {
        int index = 0;
        switch (curve.GeometryType)
        {
            case GeometryType.LineString:
                LineString line = (LineString)
                    GeometryDispatch.GetSelectedPayload(curve);
                AddCurveCoordinateSequence(
                    line.Coordinates, coordinates);
                return;
            case GeometryType.LinearRing:
                LinearRing ring = (LinearRing)
                    GeometryDispatch.GetSelectedPayload(curve);
                AddCurveCoordinateSequence(
                    ring.Coordinates, coordinates);
                return;
            case GeometryType.CircularString:
                CircularString circular = (CircularString)
                    GeometryDispatch.GetSelectedPayload(curve);
                AddCurveCoordinateSequence(
                    circular.Coordinates, coordinates);
                return;
            case GeometryType.CompoundCurve:
                CompoundCurve compound = (CompoundCurve)
                    GeometryDispatch.GetSelectedPayload(curve);
                while (index < compound.Curves.Length)
                {
                    CollectCurveCoordinates(
                        compound.Curves[index], coordinates);
                    index++;
                }
                return;
            default:
                return;
        }
    }

    internal static void AddCurveCoordinateSequence(
        Coordinate[] source, List<Coordinate> destination)
    {
        int index = 0;
        if (destination.Count != 0 && source.Length != 0 &&
            RobustLineIntersection.CoordinatesEqualPlanar(
                destination[destination.Count - 1], source[0]))
        {
            index = 1;
        }
        while (index < source.Length)
        {
            destination.Add(source[index]);
            index++;
        }
    }

    internal static bool IsKnownGeometryType(GeometryType type)
    {
        switch (type)
        {
            case GeometryType.Point:
            case GeometryType.LineString:
            case GeometryType.LinearRing:
            case GeometryType.Polygon:
            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
            case GeometryType.Triangle:
            case GeometryType.Tin:
            case GeometryType.PolyhedralSurface:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            case GeometryType.MultiCurve:
            case GeometryType.MultiSurface:
                return true;
            default:
                return false;
        }
    }

    internal static bool IsKnownCoordinateLayout(CoordinateLayout layout)
    {
        switch (layout)
        {
            case CoordinateLayout.XY:
            case CoordinateLayout.XYZ:
            case CoordinateLayout.XYM:
            case CoordinateLayout.XYZM:
                return true;
            default:
                return false;
        }
    }

    internal static bool PayloadMatchesGeometryType(Geometry geometry)
    {
        if (GeometryDispatch.CountPopulatedPayloads(geometry) != 1)
        {
            return false;
        }
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                return geometry.Point != null;
            case GeometryType.LineString:
                return geometry.LineString != null;
            case GeometryType.LinearRing:
                return geometry.LinearRing != null;
            case GeometryType.Polygon:
                return geometry.Polygon != null;
            case GeometryType.MultiPoint:
                return geometry.MultiPoint != null;
            case GeometryType.MultiLineString:
                return geometry.MultiLineString != null;
            case GeometryType.MultiPolygon:
                return geometry.MultiPolygon != null;
            case GeometryType.GeometryCollection:
                return geometry.GeometryCollection != null;
            case GeometryType.Triangle:
                return geometry.Triangle != null;
            case GeometryType.Tin:
                return geometry.Tin != null;
            case GeometryType.PolyhedralSurface:
                return geometry.PolyhedralSurface != null;
            case GeometryType.CircularString:
                return geometry.CircularString != null;
            case GeometryType.CompoundCurve:
                return geometry.CompoundCurve != null;
            case GeometryType.CurvePolygon:
                return geometry.CurvePolygon != null;
            case GeometryType.MultiCurve:
                return geometry.MultiCurve != null;
            case GeometryType.MultiSurface:
                return geometry.MultiSurface != null;
            default:
                return false;
        }
    }

    internal static bool IsSimpleCurveType(GeometryType type)
    {
        return type == GeometryType.LineString ||
            type == GeometryType.CircularString;
    }

    internal static bool IsRingCurveType(GeometryType type)
    {
        return IsSimpleCurveType(type) ||
            type == GeometryType.LinearRing ||
            type == GeometryType.CompoundCurve;
    }

    internal static bool IsCurveType(GeometryType type)
    {
        return IsRingCurveType(type);
    }

    internal static bool IsSurfaceType(GeometryType type)
    {
        return type == GeometryType.Polygon ||
            type == GeometryType.CurvePolygon ||
            type == GeometryType.Triangle ||
            type == GeometryType.Tin ||
            type == GeometryType.PolyhedralSurface;
    }

    internal static string GetGeometryPath(GeometryType type)
    {
        if (!IsKnownGeometryType(type)) return "Geometry";
        return type.ToString();
    }

    internal static void SetFailure(
        StructuralValidationState state, ValidationError error,
        string message, Coordinate? coordinate, string path)
    {
        if (state.Failure != null) return;
        state.Failure = CreateFailureResult(
            error, message, coordinate, path);
    }

    internal static ValidationResult CreateFailureResult(
        ValidationError error, string message,
        Coordinate? coordinate, string path)
    {
        ValidationResult result = new ValidationResult();
        result.IsValid = false;
        result.Error = error;
        result.Message = message;
        result.ComponentPath = path;
        if (coordinate != null)
        {
            Coordinate copy = new Coordinate();
            copy.X = coordinate.X;
            copy.Y = coordinate.Y;
            copy.Z = coordinate.Z;
            copy.M = coordinate.M;
            result.Coordinate = copy;
        }
        return result;
    }
}
