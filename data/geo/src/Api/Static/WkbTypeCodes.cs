namespace geo;

/// <summary>Provides OGC and EWKB geometry type codes and dimensional markers.</summary>
public static class WkbTypeCodes
{
    /// <summary>The OGC Point type code.</summary>
    public const int Point = 1;

    /// <summary>The OGC LineString type code.</summary>
    public const int LineString = 2;

    /// <summary>The OGC Polygon type code.</summary>
    public const int Polygon = 3;

    /// <summary>The OGC MultiPoint type code.</summary>
    public const int MultiPoint = 4;

    /// <summary>The OGC MultiLineString type code.</summary>
    public const int MultiLineString = 5;

    /// <summary>The OGC MultiPolygon type code.</summary>
    public const int MultiPolygon = 6;

    /// <summary>The OGC GeometryCollection type code.</summary>
    public const int GeometryCollection = 7;

    /// <summary>The OGC CircularString type code.</summary>
    public const int CircularString = 8;

    /// <summary>The OGC CompoundCurve type code.</summary>
    public const int CompoundCurve = 9;

    /// <summary>The OGC CurvePolygon type code.</summary>
    public const int CurvePolygon = 10;

    /// <summary>The OGC MultiCurve type code.</summary>
    public const int MultiCurve = 11;

    /// <summary>The OGC MultiSurface type code.</summary>
    public const int MultiSurface = 12;

    /// <summary>The OGC Curve type code.</summary>
    public const int Curve = 13;

    /// <summary>The OGC Surface type code.</summary>
    public const int Surface = 14;

    /// <summary>The OGC PolyhedralSurface type code.</summary>
    public const int PolyhedralSurface = 15;

    /// <summary>The OGC TIN type code.</summary>
    public const int Tin = 16;

    /// <summary>The OGC Triangle type code.</summary>
    public const int Triangle = 17;

    /// <summary>The ISO WKB offset for XYZ coordinates.</summary>
    public const int ZOffset = 1000;

    /// <summary>The ISO WKB offset for XYM coordinates.</summary>
    public const int MOffset = 2000;

    /// <summary>The ISO WKB offset for XYZM coordinates.</summary>
    public const int ZmOffset = 3000;

    /// <summary>The EWKB flag indicating a Z ordinate.</summary>
    public const uint EwkbZFlag = 0x80000000u;

    /// <summary>The EWKB flag indicating an M ordinate.</summary>
    public const uint EwkbMFlag = 0x40000000u;

    /// <summary>The EWKB flag indicating an encoded SRID.</summary>
    public const uint EwkbSridFlag = 0x20000000u;

    /// <summary>The EWKB mask selecting the base geometry type code.</summary>
    public const uint EwkbTypeMask = 0x1fffffffu;
}
