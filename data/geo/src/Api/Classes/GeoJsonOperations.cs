namespace geo;

/// <summary>Provides RFC 7946 bounding-box, transformation, and feature-join operations.</summary>
public class GeoJsonOperations : IGeoJsonOperations
{
    /// <summary>Creates a GeoJSON operation provider.</summary>
    public GeoJsonOperations()
    {
    }

    /// <summary>Computes the wrapped bounding box of all coordinates in a GeoJSON document.</summary>
    /// <param name="document">The GeoJSON document.</param>
    /// <returns>The two- or three-dimensional bounding box, or an empty array when there are no coordinates.</returns>
    /// <exception cref="GeoJsonException">The document contains a value that cannot be represented by RFC 7946 GeoJSON.</exception>
    public double[] ComputeGeoJsonBoundingBox(
        GeoJsonDocument document)
    {
        GeoJsonOperationValidator.ValidateDocument(document);
        return GeoJsonBoundingBox.ComputeBoundingBox(document);
    }

    /// <summary>Cuts geometry at the antimeridian and normalizes longitudes.</summary>
    /// <param name="document">The GeoJSON document.</param>
    /// <returns>An owning transformed document.</returns>
    /// <exception cref="GeoJsonException">The document contains a value that cannot be represented by RFC 7946 GeoJSON.</exception>
    public GeoJsonDocument CutAntimeridian(
        GeoJsonDocument document)
    {
        GeoJsonOperationValidator.ValidateDocument(document);
        return AntimeridianCutter.CutAntimeridian(document);
    }

    /// <summary>Rewinds polygon shells and holes to the RFC 7946 right-hand rule.</summary>
    /// <param name="document">The GeoJSON document.</param>
    /// <returns>An owning transformed document.</returns>
    /// <exception cref="GeoJsonException">The document contains a value that cannot be represented by RFC 7946 GeoJSON.</exception>
    public GeoJsonDocument RewindGeoJson(
        GeoJsonDocument document)
    {
        GeoJsonOperationValidator.ValidateDocument(document);
        return GeoJsonObjectTransformer.RewindDocument(document);
    }

    /// <summary>Rounds every coordinate ordinate to a decimal precision.</summary>
    /// <param name="document">The GeoJSON document.</param>
    /// <param name="precision">The number of fractional decimal digits from zero through fifteen.</param>
    /// <returns>An owning transformed document.</returns>
    /// <exception cref="GeoJsonException">The document contains a value that cannot be represented by RFC 7946 GeoJSON.</exception>
    public GeoJsonDocument TruncateGeoJsonCoordinates(
        GeoJsonDocument document, int precision)
    {
        GeoJsonOperationValidator.ValidateDocument(document);
        return GeoJsonObjectTransformer.TruncateDocumentCoordinates(
            document, precision);
    }

    /// <summary>Expands multi-geometries and geometry collections into simple-geometry features.</summary>
    /// <param name="document">The GeoJSON document.</param>
    /// <returns>An owning FeatureCollection document.</returns>
    /// <exception cref="GeoJsonException">The document contains a value that cannot be represented by RFC 7946 GeoJSON.</exception>
    public GeoJsonDocument FlattenFeatures(
        GeoJsonDocument document)
    {
        GeoJsonOperationValidator.ValidateDocument(document);
        return GeoJsonFeatureOperations.FlattenFeatures(document);
    }

    /// <summary>Expands feature geometries into one point feature per coordinate.</summary>
    /// <param name="document">The GeoJSON document.</param>
    /// <returns>An owning FeatureCollection document.</returns>
    /// <exception cref="GeoJsonException">The document contains a value that cannot be represented by RFC 7946 GeoJSON.</exception>
    public GeoJsonDocument ExplodeFeatures(
        GeoJsonDocument document)
    {
        GeoJsonOperationValidator.ValidateDocument(document);
        return GeoJsonFeatureOperations.ExplodeFeatures(document);
    }

    /// <summary>Combines compatible simple features by geometry family.</summary>
    /// <param name="document">The GeoJSON document.</param>
    /// <returns>An owning FeatureCollection document.</returns>
    /// <exception cref="GeoJsonException">The document contains a value that cannot be represented by RFC 7946 GeoJSON.</exception>
    public GeoJsonDocument CombineFeatures(
        GeoJsonDocument document)
    {
        GeoJsonOperationValidator.ValidateDocument(document);
        return GeoJsonFeatureOperations.CombineFeatures(document);
    }

    /// <summary>Selects point features located in or on polygon features.</summary>
    /// <param name="points">The point feature input.</param>
    /// <param name="polygons">The polygon feature input.</param>
    /// <returns>An owning FeatureCollection containing matching point features.</returns>
    /// <exception cref="GeoJsonException">An input contains a value that cannot be represented by RFC 7946 GeoJSON.</exception>
    public GeoJsonDocument PointsWithinPolygons(
        GeoJsonDocument points, GeoJsonDocument polygons)
    {
        GeoJsonOperationValidator.ValidateDocument(points);
        GeoJsonOperationValidator.ValidateDocument(polygons);
        return GeoJsonFeatureOperations.PointsWithinPolygons(
            points, polygons);
    }

    /// <summary>Selects features from the first document that intersect any feature in the second.</summary>
    /// <param name="first">The features to select.</param>
    /// <param name="second">The features used as the intersection filter.</param>
    /// <returns>An owning FeatureCollection containing matching features from the first document.</returns>
    /// <exception cref="GeoJsonException">An input contains a value that cannot be represented by RFC 7946 GeoJSON.</exception>
    public GeoJsonDocument IntersectingFeatures(
        GeoJsonDocument first, GeoJsonDocument second)
    {
        GeoJsonOperationValidator.ValidateDocument(first);
        GeoJsonOperationValidator.ValidateDocument(second);
        return GeoJsonFeatureOperations.IntersectingFeatures(
            first, second);
    }
}
