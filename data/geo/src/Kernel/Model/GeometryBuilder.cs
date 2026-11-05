namespace geo;

internal static class GeometryBuilder
{
    internal static GeometryFactoryOptions CopyAndValidateOptions(GeometryFactoryOptions source)
    {
        GeometryFactoryOptions copy = CopyOptions(source);
        ValidateOptions(copy);
        return copy;
    }

    internal static GeometryFactoryOptions CopyOptions(GeometryFactoryOptions source)
    {
        if (source == null)
        {
            throw new PrecisionException("Geometry factory options are null.");
        }

        GeometryFactoryOptions copy = new GeometryFactoryOptions();
        copy.SpatialReferenceId = source.SpatialReferenceId;
        copy.CoordinateLayout = source.CoordinateLayout;
        copy.PrecisionModelType = source.PrecisionModelType;
        copy.RingOrientation = source.RingOrientation;
        copy.BoundaryNodeRule = source.BoundaryNodeRule;
        copy.PrecisionScale = source.PrecisionScale;
        copy.PrecisionOffsetX = source.PrecisionOffsetX;
        copy.PrecisionOffsetY = source.PrecisionOffsetY;
        return copy;
    }

    internal static void ValidateOptions(GeometryFactoryOptions options)
    {
        if (options.SpatialReferenceId < GeometryConstants.UnknownSpatialReferenceId)
        {
            throw new InvalidGeometryException("Spatial reference identifier cannot be negative.");
        }

        ValidateCoordinateLayout(options.CoordinateLayout);
        ValidatePrecisionModelType(options.PrecisionModelType);
        ValidateRingOrientation(options.RingOrientation);
        ValidateBoundaryNodeRule(options.BoundaryNodeRule);

        if (!double.IsFinite(options.PrecisionScale) || options.PrecisionScale <= 0.0)
        {
            throw new PrecisionException("Precision scale must be finite and positive.");
        }

        if (!double.IsFinite(options.PrecisionOffsetX) || !double.IsFinite(options.PrecisionOffsetY))
        {
            throw new PrecisionException("Precision offsets must be finite.");
        }
    }

    internal static void ValidateCoordinateLayout(CoordinateLayout coordinateLayout)
    {
        switch (coordinateLayout)
        {
            case CoordinateLayout.XY:
            case CoordinateLayout.XYZ:
            case CoordinateLayout.XYM:
            case CoordinateLayout.XYZM:
                return;
            default:
                throw new InvalidGeometryException("Coordinate layout is not supported.");
        }
    }

    internal static void ValidatePrecisionModelType(PrecisionModelType precisionModelType)
    {
        switch (precisionModelType)
        {
            case PrecisionModelType.Floating:
            case PrecisionModelType.FloatingSingle:
            case PrecisionModelType.Fixed:
                return;
            default:
                throw new PrecisionException("Precision model type is not supported.");
        }
    }

    internal static void ValidateRingOrientation(RingOrientation ringOrientation)
    {
        switch (ringOrientation)
        {
            case RingOrientation.Undefined:
            case RingOrientation.Clockwise:
            case RingOrientation.CounterClockwise:
                return;
            default:
                throw new InvalidGeometryException("Ring orientation is not supported.");
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

    internal static Geometry CreateEmptyValue(GeometryType geometryType, GeometryFactoryOptions options)
    {
        Geometry geometry = CreateGeometryShell(geometryType, options);
        switch (geometryType)
        {
            case GeometryType.Point:
                geometry.Point = new Point();
                break;
            case GeometryType.LineString:
                geometry.LineString = new LineString();
                break;
            case GeometryType.LinearRing:
                geometry.LinearRing = new LinearRing();
                break;
            case GeometryType.Polygon:
                geometry.Polygon = new Polygon();
                break;
            case GeometryType.MultiPoint:
                geometry.MultiPoint = new MultiPoint();
                break;
            case GeometryType.MultiLineString:
                geometry.MultiLineString = new MultiLineString();
                break;
            case GeometryType.MultiPolygon:
                geometry.MultiPolygon = new MultiPolygon();
                break;
            case GeometryType.GeometryCollection:
                geometry.GeometryCollection = new GeometryCollection();
                break;
            case GeometryType.Triangle:
                geometry.Triangle = new Triangle();
                break;
            case GeometryType.Tin:
                geometry.Tin = new Tin();
                break;
            case GeometryType.PolyhedralSurface:
                geometry.PolyhedralSurface = new PolyhedralSurface();
                break;
            case GeometryType.CircularString:
                geometry.CircularString = new CircularString();
                break;
            case GeometryType.CompoundCurve:
                geometry.CompoundCurve = new CompoundCurve();
                break;
            case GeometryType.CurvePolygon:
                geometry.CurvePolygon = new CurvePolygon();
                break;
            case GeometryType.MultiCurve:
                geometry.MultiCurve = new MultiCurve();
                break;
            case GeometryType.MultiSurface:
                geometry.MultiSurface = new MultiSurface();
                break;
            default:
                throw new InvalidGeometryException("Geometry type is not supported.");
        }

        return geometry;
    }

    internal static Geometry CreatePointValue(Coordinate? coordinate, GeometryFactoryOptions options)
    {
        Geometry geometry = CreateGeometryShell(GeometryType.Point, options);
        Point point = new Point();
        if (coordinate != null)
        {
            point.Coordinate = CopyCoordinateForFactory(coordinate, options);
        }

        geometry.Point = point;
        return geometry;
    }

    internal static Geometry CreateLineStringValue(Coordinate[] coordinates, GeometryFactoryOptions options)
    {
        Geometry geometry = CreateGeometryShell(GeometryType.LineString, options);
        geometry.LineString = CopyLineStringPayload(coordinates, options);
        return geometry;
    }

    internal static Geometry CreatePolygonValue(LinearRing shell, LinearRing[] holes, GeometryFactoryOptions options)
    {
        Geometry geometry = CreateGeometryShell(GeometryType.Polygon, options);
        geometry.Polygon = CopyPolygonPayload(shell, holes, options);
        return geometry;
    }

    internal static Geometry CreateMultiPointValue(Point[] points, GeometryFactoryOptions options)
    {
        if (points == null)
        {
            throw new InvalidGeometryException("MultiPoint component array is null.");
        }

        ValidateComponentCount(points.Length);
        MultiPoint payload = new MultiPoint();
        payload.Points = new Point[points.Length];
        int index = 0;
        while (index < points.Length)
        {
            payload.Points[index] = CopyPointPayload(points[index], options);
            index++;
        }

        Geometry geometry = CreateGeometryShell(GeometryType.MultiPoint, options);
        geometry.MultiPoint = payload;
        return geometry;
    }

    internal static Geometry CreateMultiLineStringValue(LineString[] lineStrings, GeometryFactoryOptions options)
    {
        if (lineStrings == null)
        {
            throw new InvalidGeometryException("MultiLineString component array is null.");
        }

        ValidateComponentCount(lineStrings.Length);
        MultiLineString payload = new MultiLineString();
        payload.LineStrings = new LineString[lineStrings.Length];
        int index = 0;
        while (index < lineStrings.Length)
        {
            if (lineStrings[index] == null)
            {
                throw new InvalidGeometryException("LineString component is null.");
            }

            payload.LineStrings[index] = CopyLineStringPayload(lineStrings[index].Coordinates, options);
            index++;
        }

        Geometry geometry = CreateGeometryShell(GeometryType.MultiLineString, options);
        geometry.MultiLineString = payload;
        return geometry;
    }

    internal static Geometry CreateMultiPolygonValue(Polygon[] polygons, GeometryFactoryOptions options)
    {
        if (polygons == null)
        {
            throw new InvalidGeometryException("MultiPolygon component array is null.");
        }

        ValidateComponentCount(polygons.Length);
        MultiPolygon payload = new MultiPolygon();
        payload.Polygons = new Polygon[polygons.Length];
        int index = 0;
        while (index < polygons.Length)
        {
            if (polygons[index] == null)
            {
                throw new InvalidGeometryException("Polygon component is null.");
            }

            payload.Polygons[index] = CopyPolygonPayload(polygons[index].Shell, polygons[index].Holes, options);
            index++;
        }

        Geometry geometry = CreateGeometryShell(GeometryType.MultiPolygon, options);
        geometry.MultiPolygon = payload;
        return geometry;
    }

    internal static Geometry CreateGeometryCollectionValue(Geometry[] geometries, GeometryFactoryOptions options)
    {
        if (geometries == null)
        {
            throw new InvalidGeometryException("GeometryCollection component array is null.");
        }

        ValidateComponentCount(geometries.Length);
        GeometryCollection payload = new GeometryCollection();
        if (geometries.Length == 0)
        {
            payload.Geometries = [];
        }
        else
        {
            Geometry holder = new Geometry();
            holder.Point = null;
            holder.GeometryType = GeometryType.GeometryCollection;
            holder.CoordinateLayout = options.CoordinateLayout;
            holder.SpatialReferenceId = options.SpatialReferenceId;
            GeometryCollection holderPayload = new GeometryCollection();
            holderPayload.Geometries = geometries;
            holder.GeometryCollection = holderPayload;
            GeometryCycleGuard.EnsureAcyclic(holder, GeometryConstants.DefaultMaximumNestingDepth, GeometryConstants.DefaultMaximumComponents);
            payload.Geometries = new Geometry[geometries.Length];
            int index = 0;
            while (index < geometries.Length)
            {
                payload.Geometries[index] =
                    ExtendedGeometryBuilder.CopyGeometryForFactory(
                        geometries[index], options);
                index++;
            }
        }

        Geometry geometry = CreateGeometryShell(GeometryType.GeometryCollection, options);
        geometry.GeometryCollection = payload;
        ValidationResult validation =
            StructuralValidator.ValidateGeometry(geometry);
        if (!validation.IsValid)
        {
            throw new InvalidGeometryException(
                "GeometryCollection construction failed: " +
                validation.Message);
        }
        CurveAggregateAlgorithms.ValidateExtendedTreeValue(geometry);
        return geometry;
    }

    internal static Geometry CreateGeometryShell(GeometryType geometryType, GeometryFactoryOptions options)
    {
        Geometry geometry = new Geometry();
        geometry.Point = null;
        geometry.GeometryType = geometryType;
        geometry.CoordinateLayout = options.CoordinateLayout;
        geometry.SpatialReferenceId = options.SpatialReferenceId;
        return geometry;
    }

    internal static Coordinate CopyCoordinateForFactory(Coordinate coordinate, GeometryFactoryOptions options)
    {
        if (coordinate == null)
        {
            throw new InvalidGeometryException("Coordinate is null.");
        }

        ValidateCoordinateForLayout(coordinate, options.CoordinateLayout);
        Coordinate copy = new Coordinate();
        copy.X = ApplyPrecision(coordinate.X, options.PrecisionOffsetX, options);
        copy.Y = ApplyPrecision(coordinate.Y, options.PrecisionOffsetY, options);
        copy.Z = coordinate.Z;
        copy.M = coordinate.M;
        return copy;
    }

    internal static void ValidateCoordinateForLayout(Coordinate coordinate, CoordinateLayout coordinateLayout)
    {
        if (!double.IsFinite(coordinate.X) || !double.IsFinite(coordinate.Y))
        {
            throw new InvalidGeometryException("Coordinate X and Y ordinates must be finite.");
        }

        switch (coordinateLayout)
        {
            case CoordinateLayout.XY:
                if (!double.IsNaN(coordinate.Z) || !double.IsNaN(coordinate.M))
                {
                    throw new InvalidGeometryException("XY coordinates cannot contain Z or M ordinates.");
                }
                break;
            case CoordinateLayout.XYZ:
                if (!double.IsFinite(coordinate.Z) || !double.IsNaN(coordinate.M))
                {
                    throw new InvalidGeometryException("XYZ coordinates require finite Z and absent M ordinates.");
                }
                break;
            case CoordinateLayout.XYM:
                if (!double.IsNaN(coordinate.Z) || !double.IsFinite(coordinate.M))
                {
                    throw new InvalidGeometryException("XYM coordinates require absent Z and finite M ordinates.");
                }
                break;
            case CoordinateLayout.XYZM:
                if (!double.IsFinite(coordinate.Z) || !double.IsFinite(coordinate.M))
                {
                    throw new InvalidGeometryException("XYZM coordinates require finite Z and M ordinates.");
                }
                break;
            default:
                throw new InvalidGeometryException("Coordinate layout is not supported.");
        }
    }

    internal static double ApplyPrecision(double value, double offset, GeometryFactoryOptions options)
    {
        double result = value;
        switch (options.PrecisionModelType)
        {
            case PrecisionModelType.Floating:
                break;
            case PrecisionModelType.FloatingSingle:
                result = (double)(float)value;
                break;
            case PrecisionModelType.Fixed:
                result = System.Math.Round((value - offset) * options.PrecisionScale, System.MidpointRounding.AwayFromZero) / options.PrecisionScale + offset;
                break;
            default:
                throw new PrecisionException("Precision model type is not supported.");
        }

        if (!double.IsFinite(result))
        {
            throw new PrecisionException("Precision application produced a non-finite ordinate.");
        }

        return result;
    }

    internal static Coordinate[] CopyCoordinateArray(Coordinate[] coordinates, GeometryFactoryOptions options)
    {
        if (coordinates == null)
        {
            throw new InvalidGeometryException("Coordinate sequence is null.");
        }

        if (coordinates.Length > GeometryConstants.DefaultMaximumCoordinates)
        {
            throw new InvalidGeometryException("Maximum coordinate count was exceeded.");
        }

        Coordinate[] copy = new Coordinate[coordinates.Length];
        int index = 0;
        while (index < coordinates.Length)
        {
            copy[index] = CopyCoordinateForFactory(coordinates[index], options);
            index++;
        }

        return copy;
    }

    internal static Point CopyPointPayload(Point point, GeometryFactoryOptions options)
    {
        if (point == null)
        {
            throw new InvalidGeometryException("Point component is null.");
        }

        Point copy = new Point();
        if (point.Coordinate != null)
        {
            copy.Coordinate = CopyCoordinateForFactory(point.Coordinate, options);
        }

        return copy;
    }

    internal static LineString CopyLineStringPayload(Coordinate[] coordinates, GeometryFactoryOptions options)
    {
        Coordinate[] copy = CopyCoordinateArray(coordinates, options);
        if (copy.Length == 1)
        {
            throw new InvalidGeometryException("A LineString must be empty or contain at least two coordinates.");
        }

        LineString lineString = new LineString();
        lineString.Coordinates = copy;
        return lineString;
    }

    internal static LinearRing CopyLinearRingPayload(LinearRing ring, GeometryFactoryOptions options)
    {
        if (ring == null)
        {
            throw new InvalidGeometryException("LinearRing is null.");
        }

        Coordinate[] copy = CopyCoordinateArray(ring.Coordinates, options);
        ValidateLinearRingCoordinates(copy);
        LinearRing result = new LinearRing();
        result.Coordinates = copy;
        return result;
    }

    internal static void ValidateLinearRingCoordinates(Coordinate[] coordinates)
    {
        if (coordinates.Length == 0)
        {
            return;
        }

        if (coordinates.Length < 4)
        {
            throw new InvalidGeometryException("A LinearRing must be empty or contain at least four coordinates.");
        }

        Coordinate first = coordinates[0];
        Coordinate last = coordinates[coordinates.Length - 1];
        if (first.X != last.X || first.Y != last.Y)
        {
            throw new InvalidGeometryException("A LinearRing must be closed in X and Y.");
        }

        Coordinate firstDistinct = coordinates[0];
        Coordinate? secondDistinct = null;
        int index = 1;
        while (index < coordinates.Length - 1)
        {
            Coordinate current = coordinates[index];
            if (current.X != firstDistinct.X || current.Y != firstDistinct.Y)
            {
                if (secondDistinct == null)
                {
                    secondDistinct = current;
                }
                else if (current.X != secondDistinct.X || current.Y != secondDistinct.Y)
                {
                    return;
                }
            }

            index++;
        }

        throw new InvalidGeometryException("A LinearRing must contain at least three distinct X/Y positions.");
    }

    internal static Polygon CopyPolygonPayload(LinearRing shell, LinearRing[] holes, GeometryFactoryOptions options)
    {
        if (holes == null)
        {
            throw new InvalidGeometryException("Polygon hole array is null.");
        }

        ValidateComponentCount(holes.Length);
        Polygon polygon = new Polygon();
        polygon.Shell = CopyLinearRingPayload(shell, options);
        if (polygon.Shell.Coordinates.Length == 0 && holes.Length != 0)
        {
            throw new InvalidGeometryException("An empty polygon shell cannot contain holes.");
        }

        polygon.Holes = new LinearRing[holes.Length];
        int index = 0;
        while (index < holes.Length)
        {
            polygon.Holes[index] = CopyLinearRingPayload(holes[index], options);
            if (polygon.Holes[index].Coordinates.Length == 0)
            {
                throw new InvalidGeometryException("A polygon hole cannot be empty.");
            }

            index++;
        }

        return polygon;
    }

    internal static Geometry CopyCoreGeometry(Geometry source, GeometryFactoryOptions options)
    {
        if (source == null)
        {
            throw new InvalidGeometryException("Geometry component is null.");
        }

        object payload = GeometryDispatch.GetSelectedPayload(source);
        if (source.CoordinateLayout != options.CoordinateLayout || source.SpatialReferenceId != options.SpatialReferenceId)
        {
            throw new InvalidGeometryException("GeometryCollection members must use the factory layout and spatial reference identifier.");
        }

        switch (source.GeometryType)
        {
            case GeometryType.Point:
                return CreatePointValue(((Point)payload).Coordinate, options);
            case GeometryType.LineString:
                return CreateLineStringValue(((LineString)payload).Coordinates, options);
            case GeometryType.Polygon:
                Polygon polygon = (Polygon)payload;
                return CreatePolygonValue(polygon.Shell, polygon.Holes, options);
            case GeometryType.MultiPoint:
                return CreateMultiPointValue(((MultiPoint)payload).Points, options);
            case GeometryType.MultiLineString:
                return CreateMultiLineStringValue(((MultiLineString)payload).LineStrings, options);
            case GeometryType.MultiPolygon:
                return CreateMultiPolygonValue(((MultiPolygon)payload).Polygons, options);
            case GeometryType.GeometryCollection:
                return CreateGeometryCollectionValue(((GeometryCollection)payload).Geometries, options);
            default:
                throw new InvalidGeometryException("GeometryCollection members must be core linear geometry types.");
        }
    }

    internal static void ValidateComponentCount(int count)
    {
        if (count > GeometryConstants.DefaultMaximumComponents)
        {
            throw new InvalidGeometryException("Maximum geometry component count was exceeded.");
        }
    }
}
