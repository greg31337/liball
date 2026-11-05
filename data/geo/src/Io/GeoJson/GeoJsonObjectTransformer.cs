using System;
using System.Collections.Generic;

namespace geo;

internal enum GeoJsonTransformOperation
{
    Copy = 0,
    Rewind = 1,
    Truncate = 2
}

internal static class GeoJsonObjectTransformer
{
    internal static GeoJsonDocument RewindDocument(
        GeoJsonDocument document)
    {
        return TransformDocument(
            document, GeoJsonTransformOperation.Rewind, 0);
    }

    internal static GeoJsonDocument TruncateDocumentCoordinates(
        GeoJsonDocument document, int precision)
    {
        ValidatePrecision(precision);
        return TransformDocument(
            document, GeoJsonTransformOperation.Truncate, precision);
    }

    internal static GeoJsonDocument CopyDocument(
        GeoJsonDocument document)
    {
        return TransformDocument(
            document, GeoJsonTransformOperation.Copy, 0);
    }

    internal static GeoJsonDocument TransformDocument(
        GeoJsonDocument document,
        GeoJsonTransformOperation operation, int precision)
    {
        GeoJsonBoundingBox.ValidateDocumentSelection(document);
        GeoJsonDocument result = new GeoJsonDocument();
        result.ObjectType = document.ObjectType;
        result.BoundingBox = AntimeridianCutter.CopyBoundingBox(
            document.BoundingBox);
        result.ForeignMembers = AntimeridianCutter.CopyForeignMembers(
            document.ForeignMembers);
        switch (document.ObjectType)
        {
            case GeoJsonObjectType.Geometry:
                result.Geometry = TransformGeometryMetadata(
                    document.Geometry, operation, precision);
                break;
            case GeoJsonObjectType.Feature:
                result.Feature = TransformFeatureValue(
                    document.Feature, operation, precision);
                break;
            case GeoJsonObjectType.FeatureCollection:
                result.FeatureCollection = TransformCollectionValue(
                    document.FeatureCollection, operation, precision);
                break;
            default:
                throw new GeoJsonException(
                    "GeoJSON document object type is invalid.");
        }
        return result;
    }

    internal static GeoJsonFeature TransformFeatureValue(
        GeoJsonFeature? source,
        GeoJsonTransformOperation operation, int precision)
    {
        if (source == null)
        {
            throw new GeoJsonException("GeoJSON Feature is null.");
        }
        GeoJsonFeature result = CopyFeatureMetadata(source);
        if (source.Geometry != null)
        {
            result.Geometry = TransformGeometryMetadata(
                source.Geometry, operation, precision);
        }
        return result;
    }

    internal static GeoJsonFeatureCollection TransformCollectionValue(
        GeoJsonFeatureCollection? source,
        GeoJsonTransformOperation operation, int precision)
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
            result.Features[index] = TransformFeatureValue(
                source.Features[index], operation, precision);
            index++;
        }
        result.BoundingBox = AntimeridianCutter.CopyBoundingBox(
            source.BoundingBox);
        result.ForeignMembers = AntimeridianCutter.CopyForeignMembers(
            source.ForeignMembers);
        return result;
    }

    internal static GeoJsonGeometry TransformGeometryMetadata(
        GeoJsonGeometry? source,
        GeoJsonTransformOperation operation, int precision)
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
        GeoJsonGeometry result = CopyGeometryMetadata(source);
        if (source.GeometryType != GeometryType.GeometryCollection)
        {
            result.Value = TransformGeometryValue(
                source.Value, operation, precision);
            result.GeometryType = result.Value.GeometryType;
            return result;
        }

        GeometryCollection collection = source.Value.GeometryCollection ==
            null ? throw new GeoJsonException(
                "GeoJSON GeometryCollection payload is null.") :
            source.Value.GeometryCollection;
        if (collection.Geometries == null || source.Geometries == null)
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
        result.Geometries =
            new GeoJsonGeometry[collection.Geometries.Length];
        Geometry[] values = new Geometry[collection.Geometries.Length];
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
            result.Geometries[index] = TransformGeometryMetadata(
                child, operation, precision);
            Geometry? value = result.Geometries[index].Value;
            if (value == null)
            {
                throw new GeoJsonException(
                    "Transformed GeoJSON child geometry is null.");
            }
            values[index] = value;
            index++;
        }
        GeometryCollection transformedCollection =
            new GeometryCollection();
        transformedCollection.Geometries = values;
        result.Value = AntimeridianCutter.CreateGeometryCollectionValue(
            source.Value, transformedCollection);
        result.GeometryType = GeometryType.GeometryCollection;
        return result;
    }

    internal static Geometry TransformGeometryValue(
        Geometry geometry,
        GeoJsonTransformOperation operation, int precision)
    {
        GeoJsonBoundingBox.ValidateGeoJsonGeometryType(
            geometry.GeometryType);
        if (operation == GeoJsonTransformOperation.Copy)
        {
            return GeometryCopy.CopyGeometry(geometry);
        }
        if (operation == GeoJsonTransformOperation.Rewind)
        {
            return GeometryTransforms.OrientRingValues(
                geometry, RingOrientation.CounterClockwise);
        }
        if (operation == GeoJsonTransformOperation.Truncate)
        {
            return TruncateGeometryValue(geometry, precision);
        }
        throw new GeoJsonException(
            "GeoJSON transform operation is invalid.");
    }

    internal static Geometry TruncateGeometryValue(
        Geometry geometry, int precision)
    {
        Geometry copy = GeometryCopy.CopyGeometry(geometry);
        Coordinate[] coordinates =
            CoordinateSequences.ExtractCoordinates(
                copy,
                GeometryConstants.DefaultMaximumNestingDepth,
                GeometryConstants.DefaultMaximumComponents);
        bool hasElevation =
            copy.CoordinateLayout == CoordinateLayout.XYZ;
        int index = 0;
        while (index < coordinates.Length)
        {
            Coordinate coordinate = coordinates[index];
            coordinate.X = RoundOrdinate(coordinate.X, precision);
            coordinate.Y = RoundOrdinate(coordinate.Y, precision);
            if (hasElevation)
            {
                coordinate.Z = RoundOrdinate(coordinate.Z, precision);
            }
            index++;
        }
        ValidationResult validation =
            StructuralValidator.ValidateGeometry(copy);
        if (!validation.IsValid)
        {
            throw new InvalidGeometryException(
                "Coordinate truncation produced invalid geometry: " +
                validation.Message);
        }
        return copy;
    }

    internal static double RoundOrdinate(
        double value, int precision)
    {
        if (!double.IsFinite(value))
        {
            throw new GeoJsonException(
                "GeoJSON coordinate ordinates must be finite.");
        }
        double result = Math.Round(
            value, precision, MidpointRounding.ToEven);
        if (result == 0.0) return 0.0;
        return result;
    }

    internal static void ValidatePrecision(int precision)
    {
        if (precision < 0 || precision > 15)
        {
            throw new GeoJsonException(
                "GeoJSON coordinate precision must be from zero through fifteen.");
        }
    }

    internal static GeoJsonFeature CopyFeatureMetadata(
        GeoJsonFeature source)
    {
        GeoJsonFeature result = new GeoJsonFeature();
        if (source.Id.ValueKind !=
            System.Text.Json.JsonValueKind.Undefined)
        {
            result.Id = source.Id.Clone();
        }
        if (source.Properties.ValueKind !=
            System.Text.Json.JsonValueKind.Undefined)
        {
            result.Properties = source.Properties.Clone();
        }
        result.BoundingBox = AntimeridianCutter.CopyBoundingBox(
            source.BoundingBox);
        result.ForeignMembers = AntimeridianCutter.CopyForeignMembers(
            source.ForeignMembers);
        return result;
    }

    internal static GeoJsonGeometry CopyGeometryMetadata(
        GeoJsonGeometry source)
    {
        GeoJsonGeometry result = new GeoJsonGeometry();
        result.GeometryType = source.GeometryType;
        result.BoundingBox = AntimeridianCutter.CopyBoundingBox(
            source.BoundingBox);
        result.ForeignMembers = AntimeridianCutter.CopyForeignMembers(
            source.ForeignMembers);
        return result;
    }

    internal static GeoJsonDocument AssignBoundingBoxes(
        GeoJsonDocument document)
    {
        GeoJsonDocument result = CopyDocument(document);
        switch (result.ObjectType)
        {
            case GeoJsonObjectType.Geometry:
                AssignGeometryBoundingBoxes(result.Geometry);
                break;
            case GeoJsonObjectType.Feature:
                AssignFeatureBoundingBoxes(result.Feature);
                break;
            case GeoJsonObjectType.FeatureCollection:
                AssignCollectionBoundingBoxes(
                    result.FeatureCollection);
                break;
            default:
                throw new GeoJsonException(
                    "GeoJSON document object type is invalid.");
        }
        result.BoundingBox = GeoJsonBoundingBox.ComputeBoundingBox(result);
        return result;
    }

    internal static void AssignGeometryBoundingBoxes(
        GeoJsonGeometry? geometry)
    {
        if (geometry == null)
        {
            throw new GeoJsonException(
                "GeoJSON geometry metadata is null.");
        }
        int index = 0;
        while (index < geometry.Geometries.Length)
        {
            AssignGeometryBoundingBoxes(geometry.Geometries[index]);
            index++;
        }
        GeoJsonDocument document = new GeoJsonDocument();
        document.ObjectType = GeoJsonObjectType.Geometry;
        document.Geometry = geometry;
        geometry.BoundingBox =
            GeoJsonBoundingBox.ComputeBoundingBox(document);
    }

    internal static void AssignFeatureBoundingBoxes(
        GeoJsonFeature? feature)
    {
        if (feature == null)
        {
            throw new GeoJsonException("GeoJSON Feature is null.");
        }
        if (feature.Geometry != null)
        {
            AssignGeometryBoundingBoxes(feature.Geometry);
        }
        GeoJsonDocument document = new GeoJsonDocument();
        document.ObjectType = GeoJsonObjectType.Feature;
        document.Feature = feature;
        feature.BoundingBox =
            GeoJsonBoundingBox.ComputeBoundingBox(document);
    }

    internal static void AssignCollectionBoundingBoxes(
        GeoJsonFeatureCollection? collection)
    {
        if (collection == null)
        {
            throw new GeoJsonException(
                "GeoJSON FeatureCollection is null.");
        }
        int index = 0;
        while (index < collection.Features.Length)
        {
            AssignFeatureBoundingBoxes(collection.Features[index]);
            index++;
        }
        GeoJsonDocument document = new GeoJsonDocument();
        document.ObjectType = GeoJsonObjectType.FeatureCollection;
        document.FeatureCollection = collection;
        collection.BoundingBox =
            GeoJsonBoundingBox.ComputeBoundingBox(document);
    }
}
