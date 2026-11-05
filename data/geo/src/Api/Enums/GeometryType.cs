namespace geo;

/// <summary>Identifies a supported geometry kind.</summary>
public enum GeometryType
{
    /// <summary>A zero-dimensional point.</summary>
    Point = 1,

    /// <summary>A linear coordinate sequence.</summary>
    LineString = 2,

    /// <summary>A polygon with one shell and optional holes.</summary>
    Polygon = 3,

    /// <summary>A collection of points.</summary>
    MultiPoint = 4,

    /// <summary>A collection of line strings.</summary>
    MultiLineString = 5,

    /// <summary>A collection of polygons.</summary>
    MultiPolygon = 6,

    /// <summary>A heterogeneous geometry collection.</summary>
    GeometryCollection = 7,

    /// <summary>A sequence of circular arcs.</summary>
    CircularString = 8,

    /// <summary>A continuous sequence of curve components.</summary>
    CompoundCurve = 9,

    /// <summary>A polygon whose rings may be curved.</summary>
    CurvePolygon = 10,

    /// <summary>A collection of curves.</summary>
    MultiCurve = 11,

    /// <summary>A collection of surfaces.</summary>
    MultiSurface = 12,

    /// <summary>A polyhedral surface.</summary>
    PolyhedralSurface = 15,

    /// <summary>A triangulated irregular network.</summary>
    Tin = 16,

    /// <summary>A triangular surface.</summary>
    Triangle = 17,

    /// <summary>A closed line string used as a polygon component.</summary>
    LinearRing = 101
}
