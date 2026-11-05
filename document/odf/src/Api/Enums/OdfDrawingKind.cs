namespace odf;

/// <summary>
/// Identifies a drawing object.
/// </summary>
public enum OdfDrawingKind
{
    /// <summary>
    /// The drawing kind is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// A drawing page.
    /// </summary>
    Page = 1,
    /// <summary>
    /// A drawing layer.
    /// </summary>
    Layer = 2,
    /// <summary>
    /// A drawing frame.
    /// </summary>
    Frame = 3,
    /// <summary>
    /// A drawing group.
    /// </summary>
    Group = 4,
    /// <summary>
    /// A rectangle.
    /// </summary>
    Rectangle = 5,
    /// <summary>
    /// An ellipse or circle.
    /// </summary>
    Ellipse = 6,
    /// <summary>
    /// A line.
    /// </summary>
    Line = 7,
    /// <summary>
    /// A polyline.
    /// </summary>
    Polyline = 8,
    /// <summary>
    /// A polygon.
    /// </summary>
    Polygon = 9,
    /// <summary>
    /// A regular polygon.
    /// </summary>
    RegularPolygon = 10,
    /// <summary>
    /// An SVG path.
    /// </summary>
    Path = 11,
    /// <summary>
    /// A connector.
    /// </summary>
    Connector = 12,
    /// <summary>
    /// A measurement shape.
    /// </summary>
    Measure = 13,
    /// <summary>
    /// A caption shape.
    /// </summary>
    Caption = 14,
    /// <summary>
    /// A form-control shape.
    /// </summary>
    Control = 15,
    /// <summary>
    /// A text box.
    /// </summary>
    TextBox = 16,
    /// <summary>
    /// A custom or enhanced-geometry shape.
    /// </summary>
    CustomShape = 17,
    /// <summary>
    /// A three-dimensional scene.
    /// </summary>
    Scene3D = 18,
    /// <summary>
    /// A three-dimensional cube.
    /// </summary>
    Cube = 19,
    /// <summary>
    /// A three-dimensional sphere.
    /// </summary>
    Sphere = 20,
    /// <summary>
    /// A three-dimensional rotation object.
    /// </summary>
    Rotate = 21,
    /// <summary>
    /// A three-dimensional extrusion object.
    /// </summary>
    Extrude = 22,
    /// <summary>
    /// An image object.
    /// </summary>
    Image = 23,
    /// <summary>
    /// An audio or video object.
    /// </summary>
    Media = 24,
    /// <summary>
    /// An embedded object.
    /// </summary>
    EmbeddedObject = 25
}
