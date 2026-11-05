namespace geo;

/// <summary>Provides fixed RFC 7946 and RFC 8142 names and media types.</summary>
public static class GeoJsonNames
{
    /// <summary>The GeoJSON media type.</summary>
    public const string MediaType = "application/geo+json";

    /// <summary>The GeoJSON text sequence media type.</summary>
    public const string SequenceMediaType = "application/geo+json-seq";

    /// <summary>The type member name.</summary>
    public const string TypeMember = "type";

    /// <summary>The coordinates member name.</summary>
    public const string CoordinatesMember = "coordinates";

    /// <summary>The geometries member name.</summary>
    public const string GeometriesMember = "geometries";

    /// <summary>The geometry member name.</summary>
    public const string GeometryMember = "geometry";

    /// <summary>The properties member name.</summary>
    public const string PropertiesMember = "properties";

    /// <summary>The features member name.</summary>
    public const string FeaturesMember = "features";

    /// <summary>The feature identifier member name.</summary>
    public const string IdMember = "id";

    /// <summary>The bounding box member name.</summary>
    public const string BoundingBoxMember = "bbox";

    /// <summary>The Point type name.</summary>
    public const string PointType = "Point";

    /// <summary>The LineString type name.</summary>
    public const string LineStringType = "LineString";

    /// <summary>The Polygon type name.</summary>
    public const string PolygonType = "Polygon";

    /// <summary>The MultiPoint type name.</summary>
    public const string MultiPointType = "MultiPoint";

    /// <summary>The MultiLineString type name.</summary>
    public const string MultiLineStringType = "MultiLineString";

    /// <summary>The MultiPolygon type name.</summary>
    public const string MultiPolygonType = "MultiPolygon";

    /// <summary>The GeometryCollection type name.</summary>
    public const string GeometryCollectionType = "GeometryCollection";

    /// <summary>The Feature type name.</summary>
    public const string FeatureType = "Feature";

    /// <summary>The FeatureCollection type name.</summary>
    public const string FeatureCollectionType = "FeatureCollection";

    /// <summary>The RFC 8142 record separator character.</summary>
    public const char RecordSeparator = '\u001e';

    /// <summary>The RFC 8142 record terminator character.</summary>
    public const char RecordTerminator = '\n';
}
