namespace geo;

/// <summary>Stores geometry metadata and exactly one payload selected by the geometry kind.</summary>
public class Geometry
{
    /// <summary>The geometry kind selecting the populated payload field.</summary>
    public GeometryType GeometryType = GeometryType.Point;

    /// <summary>The coordinate layout shared by the geometry payload.</summary>
    public CoordinateLayout CoordinateLayout = CoordinateLayout.XY;

    /// <summary>The spatial reference identifier, or the unknown-SRID convention.</summary>
    public int SpatialReferenceId = GeometryConstants.UnknownSpatialReferenceId;

    /// <summary>The point payload when the geometry kind is Point.</summary>
    public Point? Point = new Point();

    /// <summary>The line string payload when the geometry kind is LineString.</summary>
    public LineString? LineString;

    /// <summary>The linear ring payload when the geometry kind is LinearRing.</summary>
    public LinearRing? LinearRing;

    /// <summary>The polygon payload when the geometry kind is Polygon.</summary>
    public Polygon? Polygon;

    /// <summary>The multi-point payload when the geometry kind is MultiPoint.</summary>
    public MultiPoint? MultiPoint;

    /// <summary>The multi-line string payload when the geometry kind is MultiLineString.</summary>
    public MultiLineString? MultiLineString;

    /// <summary>The multi-polygon payload when the geometry kind is MultiPolygon.</summary>
    public MultiPolygon? MultiPolygon;

    /// <summary>The collection payload when the geometry kind is GeometryCollection.</summary>
    public GeometryCollection? GeometryCollection;

    /// <summary>The triangle payload when the geometry kind is Triangle.</summary>
    public Triangle? Triangle;

    /// <summary>The TIN payload when the geometry kind is Tin.</summary>
    public Tin? Tin;

    /// <summary>The polyhedral surface payload when the geometry kind is PolyhedralSurface.</summary>
    public PolyhedralSurface? PolyhedralSurface;

    /// <summary>The circular string payload when the geometry kind is CircularString.</summary>
    public CircularString? CircularString;

    /// <summary>The compound curve payload when the geometry kind is CompoundCurve.</summary>
    public CompoundCurve? CompoundCurve;

    /// <summary>The curved polygon payload when the geometry kind is CurvePolygon.</summary>
    public CurvePolygon? CurvePolygon;

    /// <summary>The multi-curve payload when the geometry kind is MultiCurve.</summary>
    public MultiCurve? MultiCurve;

    /// <summary>The multi-surface payload when the geometry kind is MultiSurface.</summary>
    public MultiSurface? MultiSurface;
}
