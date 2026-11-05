namespace svg;

/// <summary>Identifies one supported SVG shape element.</summary>
public enum SvgShapeKind
{
    /// <summary>No supported shape.</summary>
    None = 0,
    /// <summary>A <c>circle</c> shape.</summary>
    Circle = 1,
    /// <summary>An <c>ellipse</c> shape.</summary>
    Ellipse = 2,
    /// <summary>A <c>line</c> shape.</summary>
    Line = 3,
    /// <summary>A <c>path</c> shape.</summary>
    Path = 4,
    /// <summary>A closed <c>polygon</c> point list.</summary>
    Polygon = 5,
    /// <summary>An open <c>polyline</c> point list.</summary>
    Polyline = 6,
    /// <summary>A possibly rounded <c>rect</c> shape.</summary>
    Rectangle = 7
}
