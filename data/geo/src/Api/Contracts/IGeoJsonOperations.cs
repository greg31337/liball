namespace geo;

/// <summary>Computes RFC 7946 bounding boxes and transforms or joins GeoJSON features.</summary>
public interface IGeoJsonOperations
{
    /// <summary>Computes the wrapped bounding box of all coordinates in a GeoJSON document.</summary>
    /// <param name="document">The GeoJSON document.</param>
    /// <returns>The two- or three-dimensional bounding box, or an empty array when there are no coordinates.</returns>
    double[] ComputeGeoJsonBoundingBox(GeoJsonDocument document);

    /// <summary>Cuts geometry at the antimeridian and normalizes longitudes.</summary>
    /// <param name="document">The GeoJSON document.</param>
    /// <returns>An owning transformed document.</returns>
    GeoJsonDocument CutAntimeridian(GeoJsonDocument document);

    /// <summary>Rewinds polygon shells and holes to the RFC 7946 right-hand rule.</summary>
    /// <param name="document">The GeoJSON document.</param>
    /// <returns>An owning transformed document.</returns>
    GeoJsonDocument RewindGeoJson(GeoJsonDocument document);

    /// <summary>Rounds every coordinate ordinate to a decimal precision.</summary>
    /// <param name="document">The GeoJSON document.</param>
    /// <param name="precision">The number of fractional decimal digits from zero through fifteen.</param>
    /// <returns>An owning transformed document.</returns>
    GeoJsonDocument TruncateGeoJsonCoordinates(
        GeoJsonDocument document, int precision);

    /// <summary>Expands multi-geometries and geometry collections into simple-geometry features.</summary>
    /// <param name="document">The GeoJSON document.</param>
    /// <returns>An owning FeatureCollection document.</returns>
    GeoJsonDocument FlattenFeatures(GeoJsonDocument document);

    /// <summary>Expands feature geometries into one point feature per coordinate.</summary>
    /// <param name="document">The GeoJSON document.</param>
    /// <returns>An owning FeatureCollection document.</returns>
    GeoJsonDocument ExplodeFeatures(GeoJsonDocument document);

    /// <summary>Combines compatible simple features by geometry family.</summary>
    /// <param name="document">The GeoJSON document.</param>
    /// <returns>An owning FeatureCollection document.</returns>
    GeoJsonDocument CombineFeatures(GeoJsonDocument document);

    /// <summary>Selects point features located in or on polygon features.</summary>
    /// <param name="points">The point feature input.</param>
    /// <param name="polygons">The polygon feature input.</param>
    /// <returns>An owning FeatureCollection containing matching point features.</returns>
    GeoJsonDocument PointsWithinPolygons(
        GeoJsonDocument points, GeoJsonDocument polygons);

    /// <summary>Selects features from the first document that intersect any feature in the second.</summary>
    /// <param name="first">The features to select.</param>
    /// <param name="second">The features used as the intersection filter.</param>
    /// <returns>An owning FeatureCollection containing matching features from the first document.</returns>
    GeoJsonDocument IntersectingFeatures(
        GeoJsonDocument first, GeoJsonDocument second);
}
