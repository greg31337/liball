namespace odf;

/// <summary>
/// Identifies the unit carried by a lexical measurement.
/// </summary>
public enum OdfUnitKind
{
    /// <summary>
    /// The unit is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// The value has no unit.
    /// </summary>
    Unitless = 1,
    /// <summary>
    /// Millimetres.
    /// </summary>
    Millimetre = 2,
    /// <summary>
    /// Centimetres.
    /// </summary>
    Centimetre = 3,
    /// <summary>
    /// Inches.
    /// </summary>
    Inch = 4,
    /// <summary>
    /// Typographic points.
    /// </summary>
    Point = 5,
    /// <summary>
    /// Typographic picas.
    /// </summary>
    Pica = 6,
    /// <summary>
    /// Pixels.
    /// </summary>
    Pixel = 7,
    /// <summary>
    /// Font-relative em units.
    /// </summary>
    Em = 8,
    /// <summary>
    /// Font-relative ex units.
    /// </summary>
    Ex = 9,
    /// <summary>
    /// Percentage units.
    /// </summary>
    Percentage = 10,
    /// <summary>
    /// Angular degrees.
    /// </summary>
    Degree = 11,
    /// <summary>
    /// Radians.
    /// </summary>
    Radian = 12,
    /// <summary>
    /// Gradians.
    /// </summary>
    Grad = 13
}
