using System;
using System.Collections.Generic;
using System.Text.Json;

namespace geo;

internal static class AntimeridianCutter
{
    internal static GeoJsonDocument CutAntimeridian(
        GeoJsonDocument document)
    {
        GeoJsonBoundingBox.ValidateDocumentSelection(document);
        GeoJsonDocument result = new GeoJsonDocument();
        result.ObjectType = document.ObjectType;
        result.BoundingBox = CopyBoundingBox(document.BoundingBox);
        result.ForeignMembers = CopyForeignMembers(
            document.ForeignMembers);
        switch (document.ObjectType)
        {
            case GeoJsonObjectType.Geometry:
                result.Geometry = CutGeometryMetadata(document.Geometry);
                break;
            case GeoJsonObjectType.Feature:
                result.Feature = CopyFeatureValue(document.Feature);
                break;
            case GeoJsonObjectType.FeatureCollection:
                result.FeatureCollection = CopyFeatureCollectionValue(
                    document.FeatureCollection);
                break;
            default:
                throw new GeoJsonException(
                    "GeoJSON document object type is invalid.");
        }
        return result;
    }

    internal static GeoJsonFeature CopyFeatureValue(
        GeoJsonFeature? source)
    {
        if (source == null)
        {
            throw new GeoJsonException("GeoJSON Feature is null.");
        }
        GeoJsonFeature result = new GeoJsonFeature();
        if (source.Geometry != null)
        {
            result.Geometry = CutGeometryMetadata(source.Geometry);
        }
        if (source.Id.ValueKind != JsonValueKind.Undefined)
        {
            result.Id = source.Id.Clone();
        }
        if (source.Properties.ValueKind != JsonValueKind.Undefined)
        {
            result.Properties = source.Properties.Clone();
        }
        result.BoundingBox = CopyBoundingBox(source.BoundingBox);
        result.ForeignMembers = CopyForeignMembers(
            source.ForeignMembers);
        return result;
    }

    internal static GeoJsonFeatureCollection CopyFeatureCollectionValue(
        GeoJsonFeatureCollection? source)
    {
        if (source == null || source.Features == null)
        {
            throw new GeoJsonException(
                "GeoJSON FeatureCollection or feature array is null.");
        }
        GeoJsonFeatureCollection result =
            new GeoJsonFeatureCollection();
        result.Features = new GeoJsonFeature[source.Features.Length];
        int index = 0;
        while (index < source.Features.Length)
        {
            result.Features[index] = CopyFeatureValue(
                source.Features[index]);
            index++;
        }
        result.BoundingBox = CopyBoundingBox(source.BoundingBox);
        result.ForeignMembers = CopyForeignMembers(
            source.ForeignMembers);
        return result;
    }

    internal static GeoJsonGeometry CutGeometryMetadata(
        GeoJsonGeometry? source)
    {
        if (source == null || source.Value == null)
        {
            throw new GeoJsonException(
                "GeoJSON geometry metadata or value is null.");
        }
        if (source.GeometryType != source.Value.GeometryType)
        {
            throw new GeoJsonException(
                "GeoJSON geometry metadata type does not match its value.");
        }
        GeoJsonBoundingBox.ValidateGeoJsonGeometryType(
            source.GeometryType);

        GeoJsonGeometry result = new GeoJsonGeometry();
        result.BoundingBox = CopyBoundingBox(source.BoundingBox);
        result.ForeignMembers = CopyForeignMembers(
            source.ForeignMembers);
        if (source.GeometryType == GeometryType.GeometryCollection)
        {
            GeometryCollection collection = source.Value.GeometryCollection ==
                null ? throw new GeoJsonException(
                    "GeoJSON GeometryCollection payload is null.") :
                source.Value.GeometryCollection;
            if (collection.Geometries == null)
            {
                throw new GeoJsonException(
                    "GeoJSON GeometryCollection geometry array is null.");
            }
            GeoJsonGeometry[] sourceMetadata = source.Geometries;
            if (sourceMetadata == null)
            {
                throw new GeoJsonException(
                    "GeoJSON child geometry metadata array is null.");
            }
            int metadataCount = sourceMetadata.Length;
            if (metadataCount != 0 &&
                metadataCount != collection.Geometries.Length)
            {
                throw new GeoJsonException(
                    "GeoJSON child geometry metadata count does not match its geometry count.");
            }
            result.Geometries =
                new GeoJsonGeometry[collection.Geometries.Length];
            Geometry[] values =
                new Geometry[collection.Geometries.Length];
            int index = 0;
            while (index < collection.Geometries.Length)
            {
                if (metadataCount != 0 &&
                    (sourceMetadata[index] == null ||
                        sourceMetadata[index].Value !=
                            collection.Geometries[index]))
                {
                    throw new GeoJsonException(
                        "GeoJSON child metadata does not reference its corresponding geometry.");
                }
                GeoJsonGeometry child = metadataCount == 0 ?
                    CreateGeometryMetadata(collection.Geometries[index]) :
                    sourceMetadata[index];
                result.Geometries[index] = CutGeometryMetadata(child);
                Geometry? value = result.Geometries[index].Value;
                if (value == null)
                {
                    throw new GeoJsonException(
                        "Cut GeoJSON child geometry value is null.");
                }
                values[index] = value;
                index++;
            }
            GeometryCollection resultCollection =
                new GeometryCollection();
            resultCollection.Geometries = values;
            result.Value = CreateGeometryCollectionValue(
                source.Value, resultCollection);
            result.GeometryType = GeometryType.GeometryCollection;
            return result;
        }
        if (source.Geometries == null || source.Geometries.Length != 0)
        {
            throw new GeoJsonException(
                "Non-collection GeoJSON geometry cannot contain child metadata.");
        }

        result.Value = CutGeometryValue(source.Value);
        result.GeometryType = result.Value.GeometryType;
        return result;
    }

    internal static GeoJsonGeometry CreateGeometryMetadata(
        Geometry geometry)
    {
        if (geometry == null)
        {
            throw new GeoJsonException(
                "GeoJSON GeometryCollection child is null.");
        }
        GeoJsonGeometry metadata = new GeoJsonGeometry();
        metadata.GeometryType = geometry.GeometryType;
        metadata.Value = geometry;
        return metadata;
    }

    internal static Geometry CutGeometryValue(Geometry source)
    {
        ValidationResult validation =
            StructuralValidator.ValidateGeometry(source);
        if (!validation.IsValid)
        {
            throw new InvalidGeometryException(
                "Antimeridian cutting requires structurally valid geometry: " +
                validation.Message);
        }
        GeoJsonBoundingBox.ValidateGeoJsonGeometryType(
            source.GeometryType);
        ValidateCoordinateLayout(source.CoordinateLayout);
        object payload = GeometryDispatch.GetSelectedPayload(source);
        switch (source.GeometryType)
        {
            case GeometryType.Point:
                return CutPointValue(source, (Point)payload);
            case GeometryType.LineString:
                return CutLineStringValue(
                    source, (LineString)payload);
            case GeometryType.Polygon:
                return CutPolygonValue(source, (Polygon)payload);
            case GeometryType.MultiPoint:
                return CutMultiPointValue(source, (MultiPoint)payload);
            case GeometryType.MultiLineString:
                return CutMultiLineStringValue(
                    source, (MultiLineString)payload);
            case GeometryType.MultiPolygon:
                return CutMultiPolygonValue(
                    source, (MultiPolygon)payload);
            case GeometryType.GeometryCollection:
                return CutGeometryCollectionRawValue(
                    source, (GeometryCollection)payload);
            default:
                throw new GeoJsonException(
                    "Geometry type cannot be represented by GeoJSON.");
        }
    }

    internal static void ValidateCoordinateLayout(CoordinateLayout layout)
    {
        if (layout != CoordinateLayout.XY &&
            layout != CoordinateLayout.XYZ)
        {
            throw new GeoJsonException(
                "GeoJSON geometry must use XY or XYZ coordinates.");
        }
    }

    internal static Geometry CutPointValue(
        Geometry source, Point point)
    {
        Point result = new Point();
        if (point.Coordinate != null)
        {
            result.Coordinate = CreateNormalizedCoordinate(
                point.Coordinate);
        }
        Geometry geometry = CreateResultShell(
            source, GeometryType.Point);
        geometry.Point = result;
        return geometry;
    }

    internal static Geometry CutMultiPointValue(
        Geometry source, MultiPoint multiPoint)
    {
        MultiPoint result = new MultiPoint();
        result.Points = new Point[multiPoint.Points.Length];
        int index = 0;
        while (index < multiPoint.Points.Length)
        {
            Point point = multiPoint.Points[index];
            Point copy = new Point();
            if (point.Coordinate != null)
            {
                copy.Coordinate = CreateNormalizedCoordinate(
                    point.Coordinate);
            }
            result.Points[index] = copy;
            index++;
        }
        Geometry geometry = CreateResultShell(
            source, GeometryType.MultiPoint);
        geometry.MultiPoint = result;
        return geometry;
    }

    internal static Geometry CutLineStringValue(
        Geometry source, LineString line)
    {
        List<LineString> pieces = CutLineCoordinates(line.Coordinates);
        if (pieces.Count == 1)
        {
            Geometry result = CreateResultShell(
                source, GeometryType.LineString);
            result.LineString = pieces[0];
            return result;
        }
        return CreateMultiLineResult(source, pieces);
    }

    internal static Geometry CutMultiLineStringValue(
        Geometry source, MultiLineString multiLine)
    {
        List<LineString> pieces = new List<LineString>();
        int index = 0;
        while (index < multiLine.LineStrings.Length)
        {
            List<LineString> linePieces = CutLineCoordinates(
                multiLine.LineStrings[index].Coordinates);
            int pieceIndex = 0;
            while (pieceIndex < linePieces.Count)
            {
                pieces.Add(linePieces[pieceIndex]);
                pieceIndex++;
            }
            index++;
        }
        return CreateMultiLineResult(source, pieces);
    }

    internal static List<LineString> CutLineCoordinates(
        Coordinate[] coordinates)
    {
        List<LineString> result = new List<LineString>();
        if (coordinates.Length == 0)
        {
            result.Add(new LineString());
            return result;
        }

        List<Coordinate> current = new List<Coordinate>();
        Coordinate first = CreateNormalizedCoordinate(coordinates[0]);
        current.Add(first);
        double currentLongitude = first.X;
        int world = 0;
        int index = 1;
        while (index < coordinates.Length)
        {
            Coordinate nextSource = coordinates[index];
            GeographicMath.ValidateGeographicCoordinate(nextSource);
            double nextNormalized =
                GeographicMath.NormalizeLongitude(nextSource.X);
            double localCurrent = currentLongitude - world * 360.0;
            double delta = GeographicMath.NormalizeLongitude(
                nextNormalized - localCurrent);
            double nextLongitude = currentLongitude + delta;
            double leftBoundary = -180.0 + world * 360.0;
            double rightBoundary = 180.0 + world * 360.0;
            Coordinate previousSource = coordinates[index - 1];
            if (nextLongitude > rightBoundary)
            {
                double fraction = (rightBoundary - currentLongitude) /
                    (nextLongitude - currentLongitude);
                Coordinate east = CreateCutCoordinate(
                    previousSource, nextSource, fraction, 180.0);
                AddDistinctLineCoordinate(current, east);
                AddCompletedLine(current, result);
                current = new List<Coordinate>();
                Coordinate west = CreateCutCoordinate(
                    previousSource, nextSource, fraction, -180.0);
                current.Add(west);
                world++;
            }
            else if (nextLongitude < leftBoundary)
            {
                double fraction = (leftBoundary - currentLongitude) /
                    (nextLongitude - currentLongitude);
                Coordinate west = CreateCutCoordinate(
                    previousSource, nextSource, fraction, -180.0);
                AddDistinctLineCoordinate(current, west);
                AddCompletedLine(current, result);
                current = new List<Coordinate>();
                Coordinate east = CreateCutCoordinate(
                    previousSource, nextSource, fraction, 180.0);
                current.Add(east);
                world--;
            }
            Coordinate next =
                CoordinateSequences.CopyCoordinateValue(nextSource);
            next.X = nextLongitude - world * 360.0;
            if (next.X == 0.0) next.X = 0.0;
            AddDistinctLineCoordinate(current, next);
            currentLongitude = nextLongitude;
            index++;
        }
        AddCompletedLine(current, result);
        if (result.Count == 0)
        {
            LineString empty = new LineString();
            empty.Coordinates = [];
            result.Add(empty);
        }
        return result;
    }

    internal static void AddCompletedLine(
        List<Coordinate> coordinates, List<LineString> result)
    {
        if (coordinates.Count < 2) return;
        LineString line = new LineString();
        line.Coordinates = coordinates.ToArray();
        result.Add(line);
    }

    internal static void AddDistinctLineCoordinate(
        List<Coordinate> coordinates, Coordinate coordinate)
    {
        if (coordinates.Count == 0 ||
            !CoordinateValuesEqual(
                coordinates[coordinates.Count - 1], coordinate))
        {
            coordinates.Add(coordinate);
        }
    }

    internal static bool CoordinateValuesEqual(
        Coordinate first, Coordinate second)
    {
        return first.X == second.X && first.Y == second.Y &&
            OptionalOrdinatesEqual(first.Z, second.Z) &&
            OptionalOrdinatesEqual(first.M, second.M);
    }

    internal static bool OptionalOrdinatesEqual(
        double first, double second)
    {
        return first == second ||
            double.IsNaN(first) && double.IsNaN(second);
    }

    internal static Coordinate CreateCutCoordinate(
        Coordinate first, Coordinate second,
        double fraction, double longitude)
    {
        Coordinate result = new Coordinate();
        result.X = longitude;
        result.Y = RobustLineIntersection.InterpolateLineOrdinate(
            first.Y, second.Y, fraction);
        result.Z = VectorMath.InterpolateOptionalOrdinate(
            first.Z, second.Z, fraction);
        result.M = VectorMath.InterpolateOptionalOrdinate(
            first.M, second.M, fraction);
        return result;
    }

    internal static Geometry CreateMultiLineResult(
        Geometry source, List<LineString> pieces)
    {
        MultiLineString multiLine = new MultiLineString();
        multiLine.LineStrings = pieces.ToArray();
        Geometry result = CreateResultShell(
            source, GeometryType.MultiLineString);
        result.MultiLineString = multiLine;
        return result;
    }

    internal static Geometry CutPolygonValue(
        Geometry source, Polygon polygon)
    {
        List<Polygon> pieces = CutPolygonPayload(source, polygon);
        if (pieces.Count == 1)
        {
            Geometry result = CreateResultShell(
                source, GeometryType.Polygon);
            result.Polygon = pieces[0];
            return result;
        }
        return CreateMultiPolygonResult(source, pieces);
    }

    internal static Geometry CutMultiPolygonValue(
        Geometry source, MultiPolygon multiPolygon)
    {
        List<Polygon> pieces = new List<Polygon>();
        int index = 0;
        while (index < multiPolygon.Polygons.Length)
        {
            List<Polygon> polygonPieces = CutPolygonPayload(
                source, multiPolygon.Polygons[index]);
            int pieceIndex = 0;
            while (pieceIndex < polygonPieces.Count)
            {
                pieces.Add(polygonPieces[pieceIndex]);
                pieceIndex++;
            }
            index++;
        }
        return CreateMultiPolygonResult(source, pieces);
    }

    internal static List<Polygon> CutPolygonPayload(
        Geometry source, Polygon polygon)
    {
        List<Polygon> pieces = new List<Polygon>();
        if (polygon.Shell.Coordinates.Length == 0)
        {
            pieces.Add(GeometryCopy.CopyPolygonValue(polygon));
            return pieces;
        }

        Polygon unwrapped = CreateUnwrappedPolygon(polygon);
        Geometry unwrappedGeometry = CreateResultShell(
            source, GeometryType.Polygon);
        unwrappedGeometry.Polygon = unwrapped;
        Coordinate[] coordinates =
            CoordinateSequences.ExtractCoordinates(
                unwrappedGeometry,
                GeometryConstants.DefaultMaximumNestingDepth,
                GeometryConstants.DefaultMaximumComponents);
        double minimumX = double.PositiveInfinity;
        double maximumX = double.NegativeInfinity;
        int coordinateIndex = 0;
        while (coordinateIndex < coordinates.Length)
        {
            if (coordinates[coordinateIndex].X < minimumX)
            {
                minimumX = coordinates[coordinateIndex].X;
            }
            if (coordinates[coordinateIndex].X > maximumX)
            {
                maximumX = coordinates[coordinateIndex].X;
            }
            coordinateIndex++;
        }

        int minimumWorld = (int)Math.Floor(
            (minimumX + 180.0) / 360.0);
        int maximumWorld = (int)Math.Floor(
            (maximumX + 180.0 - 1.0e-12) / 360.0);
        if (maximumWorld < minimumWorld)
        {
            maximumWorld = minimumWorld;
        }
        int world = minimumWorld;
        while (world <= maximumWorld)
        {
            Envelope strip = EnvelopeAlgorithms.CreateBoundsEnvelope(
                -180.0 + world * 360.0, -90.0,
                180.0 + world * 360.0, 90.0);
            Geometry clipped = RectangleClipper.ClipToEnvelope(
                unwrappedGeometry, strip);
            if (!GeometryInspection.IsEmptyValue(clipped))
            {
                AffineTransform translation = new AffineTransform();
                translation.M02 = -world * 360.0;
                Geometry shifted =
                    GeometryTransforms.ApplyAffineTransformValue(
                        clipped, translation);
                AddPolygonComponents(shifted, pieces);
            }
            world++;
        }
        return pieces;
    }

    internal static Polygon CreateUnwrappedPolygon(Polygon source)
    {
        Polygon result = new Polygon();
        result.Shell = new LinearRing();
        result.Shell.Coordinates = CreateUnwrappedRing(
            source.Shell.Coordinates);
        double shellCenter = GetAverageLongitude(
            result.Shell.Coordinates);
        result.Holes = new LinearRing[source.Holes.Length];
        int index = 0;
        while (index < source.Holes.Length)
        {
            LinearRing hole = new LinearRing();
            hole.Coordinates = CreateUnwrappedRing(
                source.Holes[index].Coordinates);
            double holeCenter = GetAverageLongitude(hole.Coordinates);
            double shift = 360.0 * Math.Round(
                (shellCenter - holeCenter) / 360.0);
            ShiftCoordinateSequence(hole.Coordinates, shift);
            result.Holes[index] = hole;
            index++;
        }
        return result;
    }

    internal static Coordinate[] CreateUnwrappedRing(
        Coordinate[] coordinates)
    {
        if (coordinates.Length == 0) return [];
        Coordinate[] result = new Coordinate[coordinates.Length];
        result[0] = CreateNormalizedCoordinate(coordinates[0]);
        double currentLongitude = result[0].X;
        double latitudeSum = result[0].Y;
        int index = 1;
        while (index < coordinates.Length)
        {
            GeographicMath.ValidateGeographicCoordinate(
                coordinates[index]);
            double normalized = GeographicMath.NormalizeLongitude(
                coordinates[index].X);
            double localCurrent =
                GeographicMath.NormalizeLongitude(currentLongitude);
            double delta = GeographicMath.NormalizeLongitude(
                normalized - localCurrent);
            currentLongitude += delta;
            result[index] =
                CoordinateSequences.CopyCoordinateValue(
                    coordinates[index]);
            result[index].X = currentLongitude;
            if (index + 1 < coordinates.Length)
            {
                latitudeSum += result[index].Y;
            }
            index++;
        }

        double drift = result[result.Length - 1].X - result[0].X;
        if (Math.Abs(drift) <= 180.0)
        {
            result[result.Length - 1].X = result[0].X;
            return result;
        }

        double averageLatitude = latitudeSum /
            (coordinates.Length - 1);
        double poleLatitude = averageLatitude < 0.0 ? -90.0 : 90.0;
        Coordinate[] polar = new Coordinate[result.Length + 3];
        result.CopyTo(polar, 0);
        Coordinate farPole = CoordinateSequences.CopyCoordinateValue(
            result[result.Length - 1]);
        farPole.Y = poleLatitude;
        Coordinate nearPole = CoordinateSequences.CopyCoordinateValue(
            result[0]);
        nearPole.Y = poleLatitude;
        polar[result.Length] = farPole;
        polar[result.Length + 1] = nearPole;
        polar[result.Length + 2] =
            CoordinateSequences.CopyCoordinateValue(result[0]);
        return polar;
    }

    internal static double GetAverageLongitude(
        Coordinate[] coordinates)
    {
        if (coordinates.Length == 0) return 0.0;
        CompensatedMeasureSum sum = new CompensatedMeasureSum();
        int count = coordinates.Length - 1;
        int index = 0;
        while (index < count)
        {
            sum.AddValue(coordinates[index].X);
            index++;
        }
        return count == 0 ? coordinates[0].X :
            sum.GetValue() / count;
    }

    internal static void ShiftCoordinateSequence(
        Coordinate[] coordinates, double shift)
    {
        int index = 0;
        while (index < coordinates.Length)
        {
            coordinates[index].X += shift;
            index++;
        }
    }

    internal static void AddPolygonComponents(
        Geometry geometry, List<Polygon> result)
    {
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        int index = 0;
        while (index < leaves.Length)
        {
            if (leaves[index].GeometryType == GeometryType.Polygon &&
                !GeometryInspection.IsEmptyValue(leaves[index]))
            {
                Polygon polygon = (Polygon)
                    GeometryDispatch.GetSelectedPayload(leaves[index]);
                result.Add(GeometryCopy.CopyPolygonValue(polygon));
            }
            index++;
        }
    }

    internal static Geometry CreateMultiPolygonResult(
        Geometry source, List<Polygon> pieces)
    {
        MultiPolygon multiPolygon = new MultiPolygon();
        multiPolygon.Polygons = pieces.ToArray();
        Geometry result = CreateResultShell(
            source, GeometryType.MultiPolygon);
        result.MultiPolygon = multiPolygon;
        return result;
    }

    internal static Geometry CutGeometryCollectionRawValue(
        Geometry source, GeometryCollection collection)
    {
        GeometryCollection result = new GeometryCollection();
        result.Geometries = new Geometry[collection.Geometries.Length];
        int index = 0;
        while (index < collection.Geometries.Length)
        {
            result.Geometries[index] = CutGeometryValue(
                collection.Geometries[index]);
            index++;
        }
        return CreateGeometryCollectionValue(source, result);
    }

    internal static Geometry CreateGeometryCollectionValue(
        Geometry source, GeometryCollection collection)
    {
        Geometry result = CreateResultShell(
            source, GeometryType.GeometryCollection);
        result.GeometryCollection = collection;
        return result;
    }

    internal static Geometry CreateResultShell(
        Geometry source, GeometryType type)
    {
        Geometry result = new Geometry();
        result.Point = null;
        result.GeometryType = type;
        result.CoordinateLayout = source.CoordinateLayout;
        result.SpatialReferenceId = source.SpatialReferenceId;
        return result;
    }

    internal static Coordinate CreateNormalizedCoordinate(
        Coordinate source)
    {
        GeographicMath.ValidateGeographicCoordinate(source);
        Coordinate result =
            CoordinateSequences.CopyCoordinateValue(source);
        result.X = GeographicMath.NormalizeLongitude(source.X);
        return result;
    }

    internal static double[] CopyBoundingBox(double[] source)
    {
        if (source == null)
        {
            throw new GeoJsonException(
                "GeoJSON bounding box array is null.");
        }
        return (double[])source.Clone();
    }

    internal static Dictionary<string, JsonElement> CopyForeignMembers(
        Dictionary<string, JsonElement> source)
    {
        if (source == null)
        {
            throw new GeoJsonException(
                "GeoJSON foreign member map is null.");
        }
        Dictionary<string, JsonElement> result =
            new Dictionary<string, JsonElement>(source.Count);
        foreach (KeyValuePair<string, JsonElement> member in source)
        {
            result.Add(member.Key, member.Value.Clone());
        }
        return result;
    }
}
