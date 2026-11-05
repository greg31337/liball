namespace geo;

internal static class GeometryNormalization
{
    internal static Geometry CloneGeometryValue(Geometry geometry)
    {
        GeometryInspection.EnsureGeometryGraph(geometry);
        return GeometryCopy.CopyGeometry(geometry);
    }

    internal static Geometry NormalizeGeometryValue(Geometry geometry)
    {
        GeometryInspection.EnsureGeometryGraph(geometry);
        Geometry copy = GeometryCopy.CopyGeometry(geometry);
        return NormalizeCopiedGeometry(copy);
    }

    internal static Geometry NormalizeCopiedGeometry(Geometry geometry)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                break;
            case GeometryType.LineString:
                LineString lineString = (LineString)payload;
                ValidateLineCoordinates(lineString.Coordinates);
                lineString.Coordinates = NormalizeOpenCoordinateSequence(lineString.Coordinates);
                break;
            case GeometryType.LinearRing:
                LinearRing linearRing = (LinearRing)payload;
                linearRing.Coordinates = NormalizeRingCoordinateSequence(linearRing.Coordinates, false);
                break;
            case GeometryType.Polygon:
                NormalizePolygonPayload((Polygon)payload);
                break;
            case GeometryType.MultiPoint:
                NormalizeMultiPointPayload((MultiPoint)payload);
                break;
            case GeometryType.MultiLineString:
                NormalizeMultiLineStringPayload((MultiLineString)payload);
                break;
            case GeometryType.MultiPolygon:
                NormalizeMultiPolygonPayload((MultiPolygon)payload);
                break;
            case GeometryType.GeometryCollection:
                GeometryCollection collection = (GeometryCollection)payload;
                NormalizeGeometryArray(collection.Geometries);
                SortGeometryValues(collection.Geometries);
                if (CanNarrowCoreCollection(collection.Geometries))
                {
                    return FactoryConstructions.CreateNarrowestCollectionValue(collection.Geometries, CreateNormalizationOptions(geometry));
                }
                break;
            case GeometryType.Triangle:
                Triangle triangle = (Triangle)payload;
                triangle.Shell.Coordinates = NormalizeRingCoordinateSequence(triangle.Shell.Coordinates, false);
                break;
            case GeometryType.Tin:
                NormalizeTinPayload((Tin)payload);
                break;
            case GeometryType.PolyhedralSurface:
                NormalizePolyhedralSurfacePayload((PolyhedralSurface)payload);
                break;
            case GeometryType.CircularString:
                CircularString circularString = (CircularString)payload;
                CircularStringAlgorithms.ValidateValue(circularString);
                circularString.Coordinates = NormalizeOpenCoordinateSequence(circularString.Coordinates);
                break;
            case GeometryType.CompoundCurve:
                NormalizeCompoundCurvePayload((CompoundCurve)payload);
                break;
            case GeometryType.CurvePolygon:
                NormalizeCurvePolygonPayload((CurvePolygon)payload);
                break;
            case GeometryType.MultiCurve:
                MultiCurve multiCurve = (MultiCurve)payload;
                NormalizeGeometryArray(multiCurve.Curves);
                SortGeometryValues(multiCurve.Curves);
                break;
            case GeometryType.MultiSurface:
                MultiSurface multiSurface = (MultiSurface)payload;
                NormalizeGeometryArray(multiSurface.Surfaces);
                SortGeometryValues(multiSurface.Surfaces);
                break;
            default:
                throw new InvalidGeometryException("Geometry type is not supported for normalization.");
        }

        return geometry;
    }

    internal static void NormalizePolygonPayload(Polygon polygon)
    {
        if (polygon == null || polygon.Shell == null || polygon.Holes == null)
        {
            throw new InvalidGeometryException("Polygon payload, shell, or hole array is null.");
        }

        polygon.Shell.Coordinates = NormalizeRingCoordinateSequence(polygon.Shell.Coordinates, false);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            if (polygon.Holes[index] == null)
            {
                throw new InvalidGeometryException("Polygon hole is null.");
            }

            polygon.Holes[index].Coordinates = NormalizeRingCoordinateSequence(polygon.Holes[index].Coordinates, true);
            index++;
        }

        SortLinearRingValues(polygon.Holes);
    }

    internal static void NormalizeMultiPointPayload(MultiPoint multiPoint)
    {
        if (multiPoint == null || multiPoint.Points == null)
        {
            throw new InvalidGeometryException("MultiPoint payload or component array is null.");
        }

        SortPointValues(multiPoint.Points);
    }

    internal static void NormalizeMultiLineStringPayload(MultiLineString multiLineString)
    {
        if (multiLineString == null || multiLineString.LineStrings == null)
        {
            throw new InvalidGeometryException("MultiLineString payload or component array is null.");
        }

        int index = 0;
        while (index < multiLineString.LineStrings.Length)
        {
            LineString lineString = multiLineString.LineStrings[index];
            if (lineString == null)
            {
                throw new InvalidGeometryException("LineString component is null.");
            }

            ValidateLineCoordinates(lineString.Coordinates);
            lineString.Coordinates = NormalizeOpenCoordinateSequence(lineString.Coordinates);
            index++;
        }

        SortLineStringValues(multiLineString.LineStrings);
    }

    internal static void NormalizeMultiPolygonPayload(MultiPolygon multiPolygon)
    {
        if (multiPolygon == null || multiPolygon.Polygons == null)
        {
            throw new InvalidGeometryException("MultiPolygon payload or component array is null.");
        }

        int index = 0;
        while (index < multiPolygon.Polygons.Length)
        {
            NormalizePolygonPayload(multiPolygon.Polygons[index]);
            index++;
        }

        SortPolygonValues(multiPolygon.Polygons);
    }

    internal static void NormalizeTinPayload(Tin tin)
    {
        if (tin == null || tin.Triangles == null)
        {
            throw new InvalidGeometryException("TIN payload or patch array is null.");
        }

        int index = 0;
        while (index < tin.Triangles.Length)
        {
            Triangle triangle = tin.Triangles[index];
            if (triangle == null || triangle.Shell == null)
            {
                throw new InvalidGeometryException("Triangle patch or shell is null.");
            }

            triangle.Shell.Coordinates = NormalizeRingCoordinateSequence(triangle.Shell.Coordinates, false);
            index++;
        }

        SortTriangleValues(tin.Triangles);
    }

    internal static void NormalizePolyhedralSurfacePayload(PolyhedralSurface surface)
    {
        if (surface == null || surface.Patches == null)
        {
            throw new InvalidGeometryException("PolyhedralSurface payload or patch array is null.");
        }

        int index = 0;
        while (index < surface.Patches.Length)
        {
            NormalizePolygonPayload(surface.Patches[index]);
            index++;
        }

        SortPolygonValues(surface.Patches);
    }

    internal static void NormalizeCurvePolygonPayload(CurvePolygon polygon)
    {
        if (polygon == null || polygon.Holes == null)
        {
            throw new InvalidGeometryException("CurvePolygon payload or hole array is null.");
        }

        CurveAggregateAlgorithms.ValidateCurvePolygonValue(polygon);

        if (polygon.Shell != null)
        {
            NormalizeCurveRingValue(polygon.Shell, false);
        }
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            NormalizeCurveRingValue(polygon.Holes[index], true);
            index++;
        }
        SortGeometryValues(polygon.Holes);
    }

    internal static void NormalizeCompoundCurvePayload(
        CompoundCurve compound)
    {
        CurveAggregateAlgorithms.ValidateCompoundValue(compound);
        Coordinate[] coordinates = new Coordinate[0];
        if (compound.Curves.Length != 0)
        {
            Geometry holder = new Geometry();
            holder.Point = null;
            holder.GeometryType = GeometryType.CompoundCurve;
            holder.CompoundCurve = compound;
            coordinates = CoordinateSequences.ExtractCoordinates(
                holder, GeometryConstants.DefaultMaximumNestingDepth,
                GeometryConstants.DefaultMaximumComponents);
        }
        if (coordinates.Length != 0 &&
            CompareCoordinateSequenceWithReverse(coordinates) > 0)
        {
            GeometryTransforms.ReverseGeometryComponents(
                compound.Curves, true);
        }
    }

    internal static void NormalizeCurveRingValue(
        Geometry curve, bool clockwise)
    {
        CurveAggregateAlgorithms.ValidateCurveRingValue(curve, false);
        object payload = GeometryDispatch.GetSelectedPayload(curve);
        switch (curve.GeometryType)
        {
            case GeometryType.LineString:
                LineString line = (LineString)payload;
                line.Coordinates = NormalizeRingCoordinateSequence(
                    line.Coordinates, clockwise);
                return;
            case GeometryType.LinearRing:
                LinearRing ring = (LinearRing)payload;
                ring.Coordinates = NormalizeRingCoordinateSequence(
                    ring.Coordinates, clockwise);
                return;
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
                double area =
                    CurveAggregateAlgorithms.GetSignedCurveRingArea(curve);
                if ((clockwise && area > 0.0) ||
                    (!clockwise && area < 0.0))
                {
                    GeometryTransforms.ReverseCopiedGeometry(curve);
                }
                return;
            default:
                throw new InvalidGeometryException(
                    "Geometry is not a supported curve ring.");
        }
    }

    internal static void NormalizeGeometryArray(Geometry[] geometries)
    {
        if (geometries == null)
        {
            throw new InvalidGeometryException("Geometry component array is null.");
        }

        int index = 0;
        while (index < geometries.Length)
        {
            if (geometries[index] == null)
            {
                throw new InvalidGeometryException("Geometry component is null.");
            }

            geometries[index] = NormalizeCopiedGeometry(geometries[index]);
            index++;
        }
    }

    internal static Coordinate[] NormalizeOpenCoordinateSequence(Coordinate[] coordinates)
    {
        if (coordinates == null)
        {
            throw new InvalidGeometryException("Coordinate sequence is null.");
        }

        if (CompareCoordinateSequenceWithReverse(coordinates) <= 0)
        {
            return coordinates;
        }

        Coordinate[] reversed = new Coordinate[coordinates.Length];
        int index = 0;
        while (index < coordinates.Length)
        {
            reversed[index] = coordinates[coordinates.Length - 1 - index];
            index++;
        }

        return reversed;
    }

    internal static Coordinate[] NormalizeRingCoordinateSequence(Coordinate[] coordinates, bool clockwise)
    {
        if (coordinates == null)
        {
            throw new InvalidGeometryException("Ring coordinate sequence is null.");
        }

        GeometryBuilder.ValidateLinearRingCoordinates(coordinates);
        if (coordinates.Length == 0)
        {
            return coordinates;
        }

        Coordinate[] oriented = coordinates;
        double signedArea = GetSignedRingArea(coordinates);
        if ((clockwise && signedArea > 0.0) || (!clockwise && signedArea < 0.0))
        {
            oriented = ReverseClosedRingCoordinates(coordinates);
        }

        Coordinate[] rotated = RotateRingToCanonicalStart(oriented);
        if (signedArea == 0.0)
        {
            Coordinate[] reversed = RotateRingToCanonicalStart(ReverseClosedRingCoordinates(coordinates));
            if (CompareCoordinateSequences(reversed, rotated) < 0)
            {
                return reversed;
            }
        }

        return rotated;
    }

    internal static Coordinate[] ReverseClosedRingCoordinates(Coordinate[] coordinates)
    {
        Coordinate[] reversed = new Coordinate[coordinates.Length];
        int vertexCount = coordinates.Length - 1;
        int index = 0;
        while (index < vertexCount)
        {
            reversed[index] = coordinates[vertexCount - index];
            index++;
        }

        reversed[vertexCount] = CoordinateSequences.CopyCoordinateValue(reversed[0]);
        return reversed;
    }

    internal static Coordinate[] RotateRingToCanonicalStart(Coordinate[] coordinates)
    {
        int vertexCount = coordinates.Length - 1;
        int best = 0;
        int candidate = 1;
        while (candidate < vertexCount)
        {
            if (CompareRingRotations(coordinates, candidate, best) < 0)
            {
                best = candidate;
            }

            candidate++;
        }

        Coordinate[] rotated = new Coordinate[coordinates.Length];
        int index = 0;
        while (index < vertexCount)
        {
            rotated[index] = coordinates[(best + index) % vertexCount];
            index++;
        }

        rotated[vertexCount] = CoordinateSequences.CopyCoordinateValue(rotated[0]);
        return rotated;
    }

    internal static int CompareRingRotations(Coordinate[] coordinates, int firstStart, int secondStart)
    {
        int vertexCount = coordinates.Length - 1;
        int offset = 0;
        while (offset < vertexCount)
        {
            Coordinate first = coordinates[(firstStart + offset) % vertexCount];
            Coordinate second = coordinates[(secondStart + offset) % vertexCount];
            int comparison = CompareCoordinateValues(first, second);
            if (comparison != 0)
            {
                return comparison;
            }

            offset++;
        }

        return 0;
    }

    internal static double GetSignedRingArea(Coordinate[] coordinates)
    {
        return RingMath.GetSignedArea(coordinates);
    }

    internal static void ValidateLineCoordinates(Coordinate[] coordinates)
    {
        if (coordinates == null)
        {
            throw new InvalidGeometryException("LineString coordinate sequence is null.");
        }

        if (coordinates.Length == 1)
        {
            throw new InvalidGeometryException("A LineString must be empty or contain at least two coordinates.");
        }
    }

    internal static int CompareCoordinateSequenceWithReverse(Coordinate[] coordinates)
    {
        int index = 0;
        while (index < coordinates.Length)
        {
            int comparison = CompareCoordinateValues(coordinates[index], coordinates[coordinates.Length - 1 - index]);
            if (comparison != 0)
            {
                return comparison;
            }

            index++;
        }

        return 0;
    }

    internal static int CompareCoordinateSequences(Coordinate[] first, Coordinate[] second)
    {
        int length = first.Length;
        if (second.Length < length) length = second.Length;
        int index = 0;
        while (index < length)
        {
            int comparison = CompareCoordinateValues(first[index], second[index]);
            if (comparison != 0)
            {
                return comparison;
            }

            index++;
        }

        if (first.Length < second.Length) return -1;
        if (first.Length > second.Length) return 1;
        return 0;
    }

    internal static int CompareCoordinateValues(Coordinate first, Coordinate second)
    {
        return VectorMath.CompareCoordinateValues(first, second);
    }

    internal static int CompareOrdinateValues(double first, double second)
    {
        return VectorMath.CompareOrdinateValues(first, second);
    }

    internal static int ComparePointValues(Point first, Point second)
    {
        if (first == null || second == null)
        {
            throw new InvalidGeometryException("Point component is null.");
        }

        if (first.Coordinate == null && second.Coordinate == null) return 0;
        if (first.Coordinate == null) return -1;
        if (second.Coordinate == null) return 1;
        return CompareCoordinateValues(first.Coordinate, second.Coordinate);
    }

    internal static int ComparePolygonValues(Polygon first, Polygon second)
    {
        if (first == null || second == null || first.Shell == null || second.Shell == null || first.Holes == null || second.Holes == null)
        {
            throw new InvalidGeometryException("Polygon comparison encountered a null payload member.");
        }

        int comparison = CompareCoordinateSequences(first.Shell.Coordinates, second.Shell.Coordinates);
        if (comparison != 0) return comparison;
        int length = first.Holes.Length;
        if (second.Holes.Length < length) length = second.Holes.Length;
        int index = 0;
        while (index < length)
        {
            comparison = CompareCoordinateSequences(first.Holes[index].Coordinates, second.Holes[index].Coordinates);
            if (comparison != 0) return comparison;
            index++;
        }

        if (first.Holes.Length < second.Holes.Length) return -1;
        if (first.Holes.Length > second.Holes.Length) return 1;
        return 0;
    }

    internal static int CompareGeometryValues(Geometry first, Geometry second)
    {
        if ((int)first.GeometryType < (int)second.GeometryType) return -1;
        if ((int)first.GeometryType > (int)second.GeometryType) return 1;
        object firstPayload = GeometryDispatch.GetSelectedPayload(first);
        object secondPayload = GeometryDispatch.GetSelectedPayload(second);
        switch (first.GeometryType)
        {
            case GeometryType.Point:
                return ComparePointValues((Point)firstPayload, (Point)secondPayload);
            case GeometryType.LineString:
                return CompareCoordinateSequences(((LineString)firstPayload).Coordinates, ((LineString)secondPayload).Coordinates);
            case GeometryType.LinearRing:
                return CompareCoordinateSequences(((LinearRing)firstPayload).Coordinates, ((LinearRing)secondPayload).Coordinates);
            case GeometryType.Polygon:
                return ComparePolygonValues((Polygon)firstPayload, (Polygon)secondPayload);
            case GeometryType.MultiPoint:
                return ComparePointArrays(((MultiPoint)firstPayload).Points, ((MultiPoint)secondPayload).Points);
            case GeometryType.MultiLineString:
                return CompareLineStringArrays(((MultiLineString)firstPayload).LineStrings, ((MultiLineString)secondPayload).LineStrings);
            case GeometryType.MultiPolygon:
                return ComparePolygonArrays(((MultiPolygon)firstPayload).Polygons, ((MultiPolygon)secondPayload).Polygons);
            case GeometryType.GeometryCollection:
                return CompareGeometryArrays(((GeometryCollection)firstPayload).Geometries, ((GeometryCollection)secondPayload).Geometries);
            case GeometryType.Triangle:
                return CompareCoordinateSequences(((Triangle)firstPayload).Shell.Coordinates, ((Triangle)secondPayload).Shell.Coordinates);
            case GeometryType.Tin:
                return CompareTriangleArrays(((Tin)firstPayload).Triangles, ((Tin)secondPayload).Triangles);
            case GeometryType.PolyhedralSurface:
                return ComparePolygonArrays(((PolyhedralSurface)firstPayload).Patches, ((PolyhedralSurface)secondPayload).Patches);
            case GeometryType.CircularString:
                return CompareCoordinateSequences(((CircularString)firstPayload).Coordinates, ((CircularString)secondPayload).Coordinates);
            case GeometryType.CompoundCurve:
                return CompareGeometryArrays(((CompoundCurve)firstPayload).Curves, ((CompoundCurve)secondPayload).Curves);
            case GeometryType.CurvePolygon:
                return CompareCurvePolygonValues((CurvePolygon)firstPayload, (CurvePolygon)secondPayload);
            case GeometryType.MultiCurve:
                return CompareGeometryArrays(((MultiCurve)firstPayload).Curves, ((MultiCurve)secondPayload).Curves);
            case GeometryType.MultiSurface:
                return CompareGeometryArrays(((MultiSurface)firstPayload).Surfaces, ((MultiSurface)secondPayload).Surfaces);
            default:
                throw new InvalidGeometryException("Geometry type is not supported for canonical comparison.");
        }
    }

    internal static int ComparePointArrays(Point[] first, Point[] second)
    {
        int length = first.Length;
        if (second.Length < length) length = second.Length;
        int index = 0;
        while (index < length)
        {
            int comparison = ComparePointValues(first[index], second[index]);
            if (comparison != 0) return comparison;
            index++;
        }
        if (first.Length < second.Length) return -1;
        if (first.Length > second.Length) return 1;
        return 0;
    }

    internal static int CompareLineStringArrays(LineString[] first, LineString[] second)
    {
        int length = first.Length;
        if (second.Length < length) length = second.Length;
        int index = 0;
        while (index < length)
        {
            int comparison = CompareCoordinateSequences(first[index].Coordinates, second[index].Coordinates);
            if (comparison != 0) return comparison;
            index++;
        }
        if (first.Length < second.Length) return -1;
        if (first.Length > second.Length) return 1;
        return 0;
    }

    internal static int ComparePolygonArrays(Polygon[] first, Polygon[] second)
    {
        int length = first.Length;
        if (second.Length < length) length = second.Length;
        int index = 0;
        while (index < length)
        {
            int comparison = ComparePolygonValues(first[index], second[index]);
            if (comparison != 0) return comparison;
            index++;
        }
        if (first.Length < second.Length) return -1;
        if (first.Length > second.Length) return 1;
        return 0;
    }

    internal static int CompareTriangleArrays(Triangle[] first, Triangle[] second)
    {
        int length = first.Length;
        if (second.Length < length) length = second.Length;
        int index = 0;
        while (index < length)
        {
            int comparison = CompareCoordinateSequences(first[index].Shell.Coordinates, second[index].Shell.Coordinates);
            if (comparison != 0) return comparison;
            index++;
        }
        if (first.Length < second.Length) return -1;
        if (first.Length > second.Length) return 1;
        return 0;
    }

    internal static int CompareGeometryArrays(Geometry[] first, Geometry[] second)
    {
        int length = first.Length;
        if (second.Length < length) length = second.Length;
        int index = 0;
        while (index < length)
        {
            int comparison = CompareGeometryValues(first[index], second[index]);
            if (comparison != 0) return comparison;
            index++;
        }
        if (first.Length < second.Length) return -1;
        if (first.Length > second.Length) return 1;
        return 0;
    }

    internal static int CompareCurvePolygonValues(CurvePolygon first, CurvePolygon second)
    {
        if (first.Shell == null && second.Shell == null) return CompareGeometryArrays(first.Holes, second.Holes);
        if (first.Shell == null) return -1;
        if (second.Shell == null) return 1;
        int comparison = CompareGeometryValues(first.Shell, second.Shell);
        if (comparison != 0) return comparison;
        return CompareGeometryArrays(first.Holes, second.Holes);
    }

    internal static void SortPointValues(Point[] points)
    {
        System.Array.Sort(points, (first, second) => ComparePointValues(first, second));
    }

    internal static void SortLineStringValues(LineString[] lineStrings)
    {
        System.Array.Sort(lineStrings, (first, second) => CompareCoordinateSequences(first.Coordinates, second.Coordinates));
    }

    internal static void SortLinearRingValues(LinearRing[] rings)
    {
        System.Array.Sort(rings, (first, second) => CompareCoordinateSequences(first.Coordinates, second.Coordinates));
    }

    internal static void SortPolygonValues(Polygon[] polygons)
    {
        System.Array.Sort(polygons, (first, second) => ComparePolygonValues(first, second));
    }

    internal static void SortTriangleValues(Triangle[] triangles)
    {
        System.Array.Sort(triangles, (first, second) => CompareCoordinateSequences(first.Shell.Coordinates, second.Shell.Coordinates));
    }

    internal static void SortGeometryValues(Geometry[] geometries)
    {
        System.Array.Sort(geometries, (first, second) => CompareGeometryValues(first, second));
    }

    internal static bool CanNarrowCoreCollection(Geometry[] geometries)
    {
        int index = 0;
        while (index < geometries.Length)
        {
            if (!IsCoreNormalizationType(geometries[index].GeometryType))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool IsCoreNormalizationType(GeometryType geometryType)
    {
        switch (geometryType)
        {
            case GeometryType.Point:
            case GeometryType.LineString:
            case GeometryType.Polygon:
            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                return true;
            default:
                return false;
        }
    }

    internal static GeometryFactoryOptions CreateNormalizationOptions(Geometry geometry)
    {
        GeometryFactoryOptions options = new GeometryFactoryOptions();
        options.CoordinateLayout = geometry.CoordinateLayout;
        options.SpatialReferenceId = geometry.SpatialReferenceId;
        options.PrecisionModelType = PrecisionModelType.Floating;
        return options;
    }
}
