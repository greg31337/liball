namespace geo;

/// <summary>Identifies the root object represented by a GeoJSON document.</summary>
public enum GeoJsonObjectType
{
    /// <summary>A GeoJSON geometry object.</summary>
    Geometry = 0,

    /// <summary>A GeoJSON Feature object.</summary>
    Feature = 1,

    /// <summary>A GeoJSON FeatureCollection object.</summary>
    FeatureCollection = 2
}
