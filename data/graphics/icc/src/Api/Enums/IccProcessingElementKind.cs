namespace icc;

/// <summary>
/// Identifies a multi-process element representation.
/// </summary>
public enum IccProcessingElementKind
{
    /// <summary>
    /// Identifies an unknown or future element.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Identifies a curve-set element.
    /// </summary>
    CurveSet = 1,

    /// <summary>
    /// Identifies a matrix element.
    /// </summary>
    Matrix = 2,

    /// <summary>
    /// Identifies a colour lookup-table element.
    /// </summary>
    Clut = 3,

    /// <summary>
    /// Identifies a begin ACS element.
    /// </summary>
    BeginAcs = 4,

    /// <summary>
    /// Identifies an end ACS element.
    /// </summary>
    EndAcs = 5,

    /// <summary>
    /// Identifies an ICC.2 calculator element.
    /// </summary>
    Calculator = 6,

    /// <summary>
    /// Identifies an ICC.2 extended CLUT element.
    /// </summary>
    ExtendedClut = 7,

    /// <summary>
    /// Identifies an ICC.2 sparse-matrix element.
    /// </summary>
    SparseMatrix = 8,

    /// <summary>
    /// Identifies an ICC.2 tint-array element.
    /// </summary>
    TintArray = 9,

    /// <summary>
    /// Identifies an ICC.2 tone-map element.
    /// </summary>
    ToneMap = 10,

    /// <summary>
    /// Identifies an ICC.2 XYZ-to-Jab element.
    /// </summary>
    XyzToJab = 11,

    /// <summary>
    /// Identifies an ICC.2 Jab-to-XYZ element.
    /// </summary>
    JabToXyz = 12,

    /// <summary>
    /// Identifies an ICC.2 emission CLUT element.
    /// </summary>
    EmissionClut = 13,

    /// <summary>
    /// Identifies an ICC.2 emission matrix element.
    /// </summary>
    EmissionMatrix = 14,

    /// <summary>
    /// Identifies an ICC.2 emission observer element.
    /// </summary>
    EmissionObserver = 15,

    /// <summary>
    /// Identifies an ICC.2 inverse emission matrix element.
    /// </summary>
    InverseEmissionMatrix = 16,

    /// <summary>
    /// Identifies an ICC.2 reflectance CLUT element.
    /// </summary>
    ReflectanceClut = 17,

    /// <summary>
    /// Identifies an ICC.2 reflectance observer element.
    /// </summary>
    ReflectanceObserver = 18
}
