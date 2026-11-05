namespace geo;

/// <summary>Identifies a parsing or bounded-input failure.</summary>
public enum ParseErrorKind
{
    /// <summary>No parsing failure.</summary>
    None = 0,

    /// <summary>The input ended before the value was complete.</summary>
    UnexpectedEnd = 1,

    /// <summary>An unexpected token was encountered.</summary>
    UnexpectedToken = 2,

    /// <summary>A numeric token is malformed or outside the supported range.</summary>
    InvalidNumber = 3,

    /// <summary>A coordinate or position is malformed.</summary>
    InvalidCoordinate = 4,

    /// <summary>A geometry type name or code is invalid.</summary>
    InvalidGeometryType = 5,

    /// <summary>A dimensional layout is invalid or inconsistent.</summary>
    InvalidCoordinateLayout = 6,

    /// <summary>An encoded element count is invalid.</summary>
    InvalidCount = 7,

    /// <summary>A configured input limit was exceeded.</summary>
    LimitExceeded = 8,

    /// <summary>Non-whitespace content follows the parsed value.</summary>
    TrailingContent = 9,

    /// <summary>The JSON representation violates GeoJSON requirements.</summary>
    InvalidGeoJson = 10,

    /// <summary>The WKB representation is malformed.</summary>
    InvalidWkb = 11,

    /// <summary>The WKT representation is malformed.</summary>
    InvalidWkt = 12,

    /// <summary>The encoded geometry kind is recognized but unsupported.</summary>
    UnsupportedType = 13
}
