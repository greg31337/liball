namespace geo;

internal static class GeoJsonOperationValidator
{
    internal static void ValidateDocument(GeoJsonDocument document)
    {
        GeoJsonBoundingBox.ValidateDocumentSelection(document);
        switch (document.ObjectType)
        {
            case GeoJsonObjectType.Geometry:
                ValidateGeometryMetadata(document.Geometry);
                return;
            case GeoJsonObjectType.Feature:
                ValidateFeature(document.Feature);
                return;
            case GeoJsonObjectType.FeatureCollection:
                ValidateFeatureCollection(document.FeatureCollection);
                return;
            default:
                throw new GeoJsonException(
                    "GeoJSON document object type is invalid.");
        }
    }

    internal static void ValidateFeature(GeoJsonFeature? feature)
    {
        if (feature == null)
        {
            throw new GeoJsonException("GeoJSON Feature is null.");
        }
        if (feature.Geometry != null)
        {
            ValidateGeometryMetadata(feature.Geometry);
        }
    }

    internal static void ValidateFeatureCollection(
        GeoJsonFeatureCollection? collection)
    {
        if (collection == null || collection.Features == null)
        {
            throw new GeoJsonException(
                "GeoJSON FeatureCollection or feature array is null.");
        }
        int index = 0;
        while (index < collection.Features.Length)
        {
            ValidateFeature(collection.Features[index]);
            index++;
        }
    }

    internal static void ValidateGeometryMetadata(
        GeoJsonGeometry? metadata)
    {
        if (metadata == null || metadata.Value == null)
        {
            throw new GeoJsonException(
                "GeoJSON geometry metadata or value is null.");
        }
        Geometry value = metadata.Value;
        if (metadata.GeometryType != value.GeometryType)
        {
            throw new GeoJsonException(
                "GeoJSON geometry metadata type does not match its value.");
        }
        if (metadata.Geometries == null)
        {
            throw new GeoJsonException(
                "GeoJSON child geometry metadata array is null.");
        }
        ValidateGeometryValue(value);
        if (value.GeometryType != GeometryType.GeometryCollection)
        {
            if (metadata.Geometries.Length != 0)
            {
                throw new GeoJsonException(
                    "Non-collection GeoJSON geometry cannot contain child metadata.");
            }
            return;
        }

        GeometryCollection? collection = value.GeometryCollection;
        if (collection == null || collection.Geometries == null)
        {
            throw new GeoJsonException(
                "GeoJSON GeometryCollection geometry array is null.");
        }
        int metadataCount = metadata.Geometries.Length;
        if (metadataCount != 0 &&
            metadataCount != collection.Geometries.Length)
        {
            throw new GeoJsonException(
                "GeoJSON child geometry metadata count does not match its geometry count.");
        }
        int index = 0;
        while (index < metadataCount)
        {
            GeoJsonGeometry child = metadata.Geometries[index];
            if (child == null ||
                child.Value != collection.Geometries[index])
            {
                throw new GeoJsonException(
                    "GeoJSON child metadata does not reference its corresponding geometry.");
            }
            ValidateGeometryMetadata(child);
            index++;
        }
    }

    internal static void ValidateGeometryValue(Geometry geometry)
    {
        GeometryCycleGuard.EnsureAcyclic(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        ValidateGeometryTree(geometry);
    }

    internal static void ValidateGeometryTree(Geometry geometry)
    {
        if (geometry == null)
        {
            throw new GeoJsonException(
                "GeoJSON geometry component is null.");
        }
        GeoJsonBoundingBox.ValidateGeoJsonGeometryType(
            geometry.GeometryType);
        AntimeridianCutter.ValidateCoordinateLayout(
            geometry.CoordinateLayout);
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        if (geometry.GeometryType == GeometryType.GeometryCollection)
        {
            GeometryCollection collection =
                (GeometryCollection)payload;
            int index = 0;
            while (index < collection.Geometries.Length)
            {
                ValidateGeometryTree(collection.Geometries[index]);
                index++;
            }
        }
    }
}
