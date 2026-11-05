using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace geo;

internal static class GeoJsonFeatureOperations
{
    internal static GeoJsonDocument FlattenFeatures(
        GeoJsonDocument document)
    {
        List<GeoJsonFeature> sourceFeatures =
            GetDocumentFeatures(document);
        List<GeoJsonFeature> results =
            new List<GeoJsonFeature>();
        int index = 0;
        while (index < sourceFeatures.Count)
        {
            GeoJsonFeature source = sourceFeatures[index];
            if (source.Geometry == null)
            {
                results.Add(GeoJsonObjectTransformer.TransformFeatureValue(
                    source, GeoJsonTransformOperation.Copy, 0));
            }
            else
            {
                List<GeoJsonGeometry> geometries =
                    new List<GeoJsonGeometry>();
                FlattenGeometryMetadata(source.Geometry, geometries);
                int geometryIndex = 0;
                while (geometryIndex < geometries.Count)
                {
                    results.Add(CreateFeatureWithGeometry(
                        source, geometries[geometryIndex]));
                    geometryIndex++;
                }
            }
            index++;
        }
        return CreateFeatureCollectionDocument(
            document, results.ToArray());
    }

    internal static void FlattenGeometryMetadata(
        GeoJsonGeometry source, List<GeoJsonGeometry> result)
    {
        if (source == null || source.Value == null)
        {
            throw new GeoJsonException(
                "GeoJSON geometry metadata or value is null.");
        }
        Geometry value = source.Value;
        if (source.GeometryType != value.GeometryType)
        {
            throw new GeoJsonException(
                "GeoJSON geometry metadata type does not match its value.");
        }
        object payload = GeometryDispatch.GetSelectedPayload(value);
        switch (value.GeometryType)
        {
            case GeometryType.Point:
            case GeometryType.LineString:
            case GeometryType.Polygon:
                result.Add(GeoJsonObjectTransformer.TransformGeometryMetadata(
                    source, GeoJsonTransformOperation.Copy, 0));
                return;
            case GeometryType.MultiPoint:
                FlattenMultiPointMetadata(
                    source, value, (MultiPoint)payload, result);
                return;
            case GeometryType.MultiLineString:
                FlattenMultiLineMetadata(
                    source, value, (MultiLineString)payload, result);
                return;
            case GeometryType.MultiPolygon:
                FlattenMultiPolygonMetadata(
                    source, value, (MultiPolygon)payload, result);
                return;
            case GeometryType.GeometryCollection:
                FlattenCollectionMetadata(
                    source, value, (GeometryCollection)payload, result);
                return;
            default:
                throw new GeoJsonException(
                    "Geometry type cannot be represented by GeoJSON.");
        }
    }

    internal static void FlattenMultiPointMetadata(
        GeoJsonGeometry source, Geometry owner, MultiPoint multiPoint,
        List<GeoJsonGeometry> result)
    {
        int index = 0;
        while (index < multiPoint.Points.Length)
        {
            Geometry value = AntimeridianCutter.CreateResultShell(
                owner, GeometryType.Point);
            value.Point = GeometryCopy.CopyPointValue(
                multiPoint.Points[index]);
            result.Add(CreateComponentMetadata(source, value));
            index++;
        }
    }

    internal static void FlattenMultiLineMetadata(
        GeoJsonGeometry source, Geometry owner,
        MultiLineString multiLine,
        List<GeoJsonGeometry> result)
    {
        int index = 0;
        while (index < multiLine.LineStrings.Length)
        {
            Geometry value = AntimeridianCutter.CreateResultShell(
                owner, GeometryType.LineString);
            value.LineString = GeometryCopy.CopyLineStringValue(
                multiLine.LineStrings[index]);
            result.Add(CreateComponentMetadata(source, value));
            index++;
        }
    }

    internal static void FlattenMultiPolygonMetadata(
        GeoJsonGeometry source, Geometry owner,
        MultiPolygon multiPolygon,
        List<GeoJsonGeometry> result)
    {
        int index = 0;
        while (index < multiPolygon.Polygons.Length)
        {
            Geometry value = AntimeridianCutter.CreateResultShell(
                owner, GeometryType.Polygon);
            value.Polygon = GeometryCopy.CopyPolygonValue(
                multiPolygon.Polygons[index]);
            result.Add(CreateComponentMetadata(source, value));
            index++;
        }
    }

    internal static void FlattenCollectionMetadata(
        GeoJsonGeometry source, Geometry owner,
        GeometryCollection collection,
        List<GeoJsonGeometry> result)
    {
        if (source.Geometries == null || collection.Geometries == null)
        {
            throw new GeoJsonException(
                "GeoJSON GeometryCollection arrays are null.");
        }
        int metadataCount = source.Geometries.Length;
        if (metadataCount != 0 &&
            metadataCount != collection.Geometries.Length)
        {
            throw new GeoJsonException(
                "GeoJSON child geometry metadata count does not match its geometry count.");
        }
        int index = 0;
        while (index < collection.Geometries.Length)
        {
            GeoJsonGeometry child = metadataCount == 0 ?
                AntimeridianCutter.CreateGeometryMetadata(
                    collection.Geometries[index]) :
                source.Geometries[index];
            if (metadataCount != 0 &&
                (child == null ||
                    child.Value != collection.Geometries[index]))
            {
                throw new GeoJsonException(
                    "GeoJSON child metadata does not reference its corresponding geometry.");
            }
            int firstResult = result.Count;
            FlattenGeometryMetadata(child, result);
            int resultIndex = firstResult;
            while (resultIndex < result.Count)
            {
                MergeForeignMembers(
                    result[resultIndex].ForeignMembers,
                    source.ForeignMembers);
                resultIndex++;
            }
            index++;
        }
    }

    internal static GeoJsonGeometry CreateComponentMetadata(
        GeoJsonGeometry source, Geometry value)
    {
        GeoJsonGeometry result = new GeoJsonGeometry();
        result.GeometryType = value.GeometryType;
        result.Value = value;
        result.BoundingBox = AntimeridianCutter.CopyBoundingBox(
            source.BoundingBox);
        result.ForeignMembers = AntimeridianCutter.CopyForeignMembers(
            source.ForeignMembers);
        return result;
    }

    internal static GeoJsonFeature CreateFeatureWithGeometry(
        GeoJsonFeature source, GeoJsonGeometry geometry)
    {
        GeoJsonFeature result =
            GeoJsonObjectTransformer.CopyFeatureMetadata(source);
        result.Geometry = geometry;
        return result;
    }

    internal static GeoJsonDocument ExplodeFeatures(
        GeoJsonDocument document)
    {
        GeoJsonDocument flattened = FlattenFeatures(document);
        GeoJsonFeatureCollection collection =
            flattened.FeatureCollection == null ?
                throw new GeoJsonException(
                    "Flattened GeoJSON FeatureCollection is null.") :
                flattened.FeatureCollection;
        List<GeoJsonFeature> points = new List<GeoJsonFeature>();
        int featureIndex = 0;
        while (featureIndex < collection.Features.Length)
        {
            GeoJsonFeature feature = collection.Features[featureIndex];
            if (feature.Geometry != null &&
                feature.Geometry.Value != null)
            {
                Geometry value = feature.Geometry.Value;
                Coordinate[] coordinates =
                    GeometryInspection.ExtractValidatedCoordinates(value);
                int coordinateIndex = 0;
                while (coordinateIndex < coordinates.Length)
                {
                    Geometry pointValue =
                        AntimeridianCutter.CreateResultShell(
                            value, GeometryType.Point);
                    Point point = new Point();
                    point.Coordinate =
                        CoordinateSequences.CopyCoordinateValue(
                            coordinates[coordinateIndex]);
                    pointValue.Point = point;
                    GeoJsonGeometry pointMetadata =
                        CreateComponentMetadata(
                            feature.Geometry, pointValue);
                    points.Add(CreateFeatureWithGeometry(
                        feature, pointMetadata));
                    coordinateIndex++;
                }
            }
            featureIndex++;
        }
        return CreateFeatureCollectionDocument(
            flattened, points.ToArray());
    }

    internal static GeoJsonDocument CombineFeatures(
        GeoJsonDocument document)
    {
        GeoJsonDocument flattened = FlattenFeatures(document);
        GeoJsonFeatureCollection collection =
            flattened.FeatureCollection == null ?
                throw new GeoJsonException(
                    "Flattened GeoJSON FeatureCollection is null.") :
                flattened.FeatureCollection;
        List<GeoJsonFeature> pointFeatures =
            new List<GeoJsonFeature>();
        List<GeoJsonFeature> lineFeatures =
            new List<GeoJsonFeature>();
        List<GeoJsonFeature> polygonFeatures =
            new List<GeoJsonFeature>();
        List<GeoJsonFeature> nullFeatures =
            new List<GeoJsonFeature>();
        int index = 0;
        while (index < collection.Features.Length)
        {
            GeoJsonFeature feature = collection.Features[index];
            if (feature.Geometry == null ||
                feature.Geometry.Value == null)
            {
                nullFeatures.Add(feature);
            }
            else if (feature.Geometry.GeometryType == GeometryType.Point)
            {
                pointFeatures.Add(feature);
            }
            else if (feature.Geometry.GeometryType == GeometryType.LineString)
            {
                lineFeatures.Add(feature);
            }
            else if (feature.Geometry.GeometryType == GeometryType.Polygon)
            {
                polygonFeatures.Add(feature);
            }
            else
            {
                throw new GeoJsonException(
                    "Flattened feature has an unsupported geometry type.");
            }
            index++;
        }

        List<GeoJsonFeature> results = new List<GeoJsonFeature>();
        if (pointFeatures.Count != 0)
        {
            results.Add(CreateCombinedPointFeature(pointFeatures));
        }
        if (lineFeatures.Count != 0)
        {
            results.Add(CreateCombinedLineFeature(lineFeatures));
        }
        if (polygonFeatures.Count != 0)
        {
            results.Add(CreateCombinedPolygonFeature(polygonFeatures));
        }
        index = 0;
        while (index < nullFeatures.Count)
        {
            results.Add(GeoJsonObjectTransformer.TransformFeatureValue(
                nullFeatures[index], GeoJsonTransformOperation.Copy, 0));
            index++;
        }
        return CreateFeatureCollectionDocument(
            flattened, results.ToArray());
    }

    internal static GeoJsonFeature CreateCombinedPointFeature(
        List<GeoJsonFeature> features)
    {
        ValidateCompatibleFeatures(features);
        Geometry owner = GetFeatureGeometryValue(features[0]);
        MultiPoint multiPoint = new MultiPoint();
        multiPoint.Points = new Point[features.Count];
        int index = 0;
        while (index < features.Count)
        {
            Geometry value = GetFeatureGeometryValue(features[index]);
            multiPoint.Points[index] = GeometryCopy.CopyPointValue(
                value.Point == null ?
                    throw new GeoJsonException(
                        "Combined Point payload is null.") : value.Point);
            index++;
        }
        Geometry combined = AntimeridianCutter.CreateResultShell(
            owner, GeometryType.MultiPoint);
        combined.MultiPoint = multiPoint;
        return CreateCombinedFeatureValue(features, combined);
    }

    internal static GeoJsonFeature CreateCombinedLineFeature(
        List<GeoJsonFeature> features)
    {
        ValidateCompatibleFeatures(features);
        Geometry owner = GetFeatureGeometryValue(features[0]);
        MultiLineString multiLine = new MultiLineString();
        multiLine.LineStrings = new LineString[features.Count];
        int index = 0;
        while (index < features.Count)
        {
            Geometry value = GetFeatureGeometryValue(features[index]);
            multiLine.LineStrings[index] =
                GeometryCopy.CopyLineStringValue(
                    value.LineString == null ?
                        throw new GeoJsonException(
                            "Combined LineString payload is null.") :
                        value.LineString);
            index++;
        }
        Geometry combined = AntimeridianCutter.CreateResultShell(
            owner, GeometryType.MultiLineString);
        combined.MultiLineString = multiLine;
        return CreateCombinedFeatureValue(features, combined);
    }

    internal static GeoJsonFeature CreateCombinedPolygonFeature(
        List<GeoJsonFeature> features)
    {
        ValidateCompatibleFeatures(features);
        Geometry owner = GetFeatureGeometryValue(features[0]);
        MultiPolygon multiPolygon = new MultiPolygon();
        multiPolygon.Polygons = new Polygon[features.Count];
        int index = 0;
        while (index < features.Count)
        {
            Geometry value = GetFeatureGeometryValue(features[index]);
            multiPolygon.Polygons[index] = GeometryCopy.CopyPolygonValue(
                value.Polygon == null ?
                    throw new GeoJsonException(
                        "Combined Polygon payload is null.") :
                    value.Polygon);
            index++;
        }
        Geometry combined = AntimeridianCutter.CreateResultShell(
            owner, GeometryType.MultiPolygon);
        combined.MultiPolygon = multiPolygon;
        return CreateCombinedFeatureValue(features, combined);
    }

    internal static GeoJsonFeature CreateCombinedFeatureValue(
        List<GeoJsonFeature> features, Geometry geometry)
    {
        GeoJsonFeature result = new GeoJsonFeature();
        result.Properties = CreateCollectedProperties(features);
        if (FeatureIdsAreEqual(features))
        {
            result.Id = features[0].Id.Clone();
        }
        Dictionary<string, JsonElement> featureForeign =
            new Dictionary<string, JsonElement>();
        Dictionary<string, JsonElement> geometryForeign =
            new Dictionary<string, JsonElement>();
        int index = 0;
        while (index < features.Count)
        {
            MergeForeignMembers(
                featureForeign, features[index].ForeignMembers);
            GeoJsonGeometry? metadata = features[index].Geometry;
            if (metadata == null)
            {
                throw new GeoJsonException(
                    "Combined feature geometry is null.");
            }
            MergeForeignMembers(
                geometryForeign, metadata.ForeignMembers);
            index++;
        }
        result.ForeignMembers = featureForeign;
        GeoJsonGeometry combinedMetadata = new GeoJsonGeometry();
        combinedMetadata.GeometryType = geometry.GeometryType;
        combinedMetadata.Value = geometry;
        combinedMetadata.ForeignMembers = geometryForeign;
        result.Geometry = combinedMetadata;
        return result;
    }

    internal static bool FeatureIdsAreEqual(
        List<GeoJsonFeature> features)
    {
        if (features.Count == 0 ||
            features[0].Id.ValueKind == JsonValueKind.Undefined)
        {
            return false;
        }
        string first = features[0].Id.GetRawText();
        int index = 1;
        while (index < features.Count)
        {
            if (features[index].Id.ValueKind ==
                    JsonValueKind.Undefined ||
                features[index].Id.GetRawText() != first)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static JsonElement CreateCollectedProperties(
        List<GeoJsonFeature> features)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (Utf8JsonWriter writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("collectedProperties");
                writer.WriteStartArray();
                int index = 0;
                while (index < features.Count)
                {
                    if (features[index].Properties.ValueKind ==
                        JsonValueKind.Undefined)
                    {
                        writer.WriteNullValue();
                    }
                    else
                    {
                        features[index].Properties.WriteTo(writer);
                    }
                    index++;
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.Flush();
            }
            using (JsonDocument document =
                JsonDocument.Parse(stream.ToArray()))
            {
                return document.RootElement.Clone();
            }
        }
    }

    internal static void MergeForeignMembers(
        Dictionary<string, JsonElement> target,
        Dictionary<string, JsonElement> source)
    {
        foreach (KeyValuePair<string, JsonElement> member in source)
        {
            if (target.ContainsKey(member.Key))
            {
                if (target[member.Key].GetRawText() !=
                    member.Value.GetRawText())
                {
                    throw new GeoJsonException(
                        "Combined features contain conflicting foreign members.");
                }
            }
            else
            {
                target.Add(member.Key, member.Value.Clone());
            }
        }
    }

    internal static void ValidateCompatibleFeatures(
        List<GeoJsonFeature> features)
    {
        Geometry first = GetFeatureGeometryValue(features[0]);
        int index = 1;
        while (index < features.Count)
        {
            Geometry next = GetFeatureGeometryValue(features[index]);
            if (next.CoordinateLayout != first.CoordinateLayout ||
                next.SpatialReferenceId != first.SpatialReferenceId)
            {
                throw new GeoJsonException(
                    "Combined features must share coordinate layout and spatial reference metadata.");
            }
            index++;
        }
    }

    internal static Geometry GetFeatureGeometryValue(
        GeoJsonFeature feature)
    {
        if (feature.Geometry == null || feature.Geometry.Value == null)
        {
            throw new GeoJsonException(
                "GeoJSON Feature geometry is null.");
        }
        return feature.Geometry.Value;
    }

    internal static GeoJsonDocument PointsWithinPolygons(
        GeoJsonDocument points, GeoJsonDocument polygons)
    {
        GeoJsonDocument cutPoints =
            AntimeridianCutter.CutAntimeridian(points);
        GeoJsonDocument cutPolygons =
            AntimeridianCutter.CutAntimeridian(polygons);
        GeoJsonDocument explodedPoints = ExplodeFeatures(cutPoints);
        GeoJsonDocument flattenedPolygons = FlattenFeatures(cutPolygons);
        GeoJsonFeatureCollection pointCollection =
            GetResultCollection(explodedPoints);
        GeoJsonFeatureCollection polygonCollection =
            GetResultCollection(flattenedPolygons);
        ValidateFeatureGeometryType(
            pointCollection.Features, GeometryType.Point);
        ValidateFeatureGeometryType(
            polygonCollection.Features, GeometryType.Polygon);

        List<GeoJsonFeature> result = new List<GeoJsonFeature>();
        int pointIndex = 0;
        while (pointIndex < pointCollection.Features.Length)
        {
            GeoJsonFeature pointFeature =
                pointCollection.Features[pointIndex];
            if (pointFeature.Geometry != null &&
                pointFeature.Geometry.Value != null)
            {
                Point? point = pointFeature.Geometry.Value.Point;
                Coordinate? coordinate = point == null ?
                    null : point.Coordinate;
                if (coordinate != null && PointIsInPolygonFeatures(
                    coordinate, polygonCollection.Features))
                {
                    result.Add(
                        GeoJsonObjectTransformer.TransformFeatureValue(
                            pointFeature,
                            GeoJsonTransformOperation.Copy, 0));
                }
            }
            pointIndex++;
        }
        return CreateFeatureCollectionDocument(
            explodedPoints, result.ToArray());
    }

    internal static bool PointIsInPolygonFeatures(
        Coordinate point, GeoJsonFeature[] polygons)
    {
        int index = 0;
        while (index < polygons.Length)
        {
            GeoJsonGeometry? metadata = polygons[index].Geometry;
            if (metadata != null && metadata.Value != null)
            {
                Geometry geometry = metadata.Value;
                Polygon? polygon = geometry.Polygon;
                if (polygon != null &&
                    GeometryPointLocation.LocatePointInPolygon(
                        point, polygon) != TopologicalLocation.Exterior)
                {
                    return true;
                }
            }
            index++;
        }
        return false;
    }

    internal static GeoJsonDocument IntersectingFeatures(
        GeoJsonDocument first, GeoJsonDocument second)
    {
        GeoJsonDocument cutFirst =
            AntimeridianCutter.CutAntimeridian(first);
        GeoJsonDocument cutSecond =
            AntimeridianCutter.CutAntimeridian(second);
        List<GeoJsonFeature> firstFeatures =
            GetDocumentFeatures(cutFirst);
        List<GeoJsonFeature> secondFeatures =
            GetDocumentFeatures(cutSecond);
        List<GeoJsonFeature> result = new List<GeoJsonFeature>();
        int firstIndex = 0;
        while (firstIndex < firstFeatures.Count)
        {
            GeoJsonFeature candidate = firstFeatures[firstIndex];
            if (candidate.Geometry != null &&
                candidate.Geometry.Value != null &&
                FeatureIntersectsAny(candidate, secondFeatures))
            {
                result.Add(GeoJsonObjectTransformer.TransformFeatureValue(
                    candidate, GeoJsonTransformOperation.Copy, 0));
            }
            firstIndex++;
        }
        return CreateFeatureCollectionDocument(
            cutFirst, result.ToArray());
    }

    internal static bool FeatureIntersectsAny(
        GeoJsonFeature feature, List<GeoJsonFeature> candidates)
    {
        Geometry first = GetFeatureGeometryValue(feature);
        int index = 0;
        while (index < candidates.Count)
        {
            GeoJsonFeature candidate = candidates[index];
            if (candidate.Geometry != null &&
                candidate.Geometry.Value != null &&
                GeometryPredicateAlgorithms.Intersects(
                    first, candidate.Geometry.Value))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static void ValidateFeatureGeometryType(
        GeoJsonFeature[] features, GeometryType requiredType)
    {
        int index = 0;
        while (index < features.Length)
        {
            GeoJsonFeature feature = features[index];
            if (feature.Geometry == null ||
                feature.Geometry.Value == null)
            {
                throw new GeoJsonException(
                    "Spatial join inputs cannot contain null geometry.");
            }
            if (feature.Geometry.GeometryType != requiredType)
            {
                throw new GeoJsonException(
                    "Spatial join input contains an unexpected geometry type.");
            }
            index++;
        }
    }

    internal static List<GeoJsonFeature> GetDocumentFeatures(
        GeoJsonDocument document)
    {
        GeoJsonBoundingBox.ValidateDocumentSelection(document);
        List<GeoJsonFeature> result = new List<GeoJsonFeature>();
        if (document.ObjectType == GeoJsonObjectType.Geometry)
        {
            GeoJsonFeature feature = new GeoJsonFeature();
            feature.Geometry =
                GeoJsonObjectTransformer.TransformGeometryMetadata(
                    document.Geometry, GeoJsonTransformOperation.Copy, 0);
            feature.Properties = CreateNullProperties();
            result.Add(feature);
            return result;
        }
        if (document.ObjectType == GeoJsonObjectType.Feature)
        {
            result.Add(GeoJsonObjectTransformer.TransformFeatureValue(
                document.Feature, GeoJsonTransformOperation.Copy, 0));
            return result;
        }
        GeoJsonFeatureCollection? collection =
            document.FeatureCollection;
        if (collection == null || collection.Features == null)
        {
            throw new GeoJsonException(
                "GeoJSON FeatureCollection or feature array is null.");
        }
        int index = 0;
        while (index < collection.Features.Length)
        {
            result.Add(GeoJsonObjectTransformer.TransformFeatureValue(
                collection.Features[index],
                GeoJsonTransformOperation.Copy, 0));
            index++;
        }
        return result;
    }

    internal static JsonElement CreateNullProperties()
    {
        using (JsonDocument document = JsonDocument.Parse("null"))
        {
            return document.RootElement.Clone();
        }
    }

    internal static GeoJsonFeatureCollection GetResultCollection(
        GeoJsonDocument document)
    {
        if (document.ObjectType != GeoJsonObjectType.FeatureCollection ||
            document.FeatureCollection == null)
        {
            throw new GeoJsonException(
                "GeoJSON feature operation did not produce a FeatureCollection.");
        }
        return document.FeatureCollection;
    }

    internal static GeoJsonDocument CreateFeatureCollectionDocument(
        GeoJsonDocument source, GeoJsonFeature[] features)
    {
        GeoJsonDocument result = new GeoJsonDocument();
        result.ObjectType = GeoJsonObjectType.FeatureCollection;
        result.BoundingBox = AntimeridianCutter.CopyBoundingBox(
            source.BoundingBox);
        result.ForeignMembers = AntimeridianCutter.CopyForeignMembers(
            source.ForeignMembers);
        GeoJsonFeatureCollection collection =
            new GeoJsonFeatureCollection();
        collection.Features = features;
        if (source.ObjectType == GeoJsonObjectType.FeatureCollection &&
            source.FeatureCollection != null)
        {
            collection.BoundingBox =
                AntimeridianCutter.CopyBoundingBox(
                    source.FeatureCollection.BoundingBox);
            collection.ForeignMembers =
                AntimeridianCutter.CopyForeignMembers(
                    source.FeatureCollection.ForeignMembers);
        }
        result.FeatureCollection = collection;
        return result;
    }
}
