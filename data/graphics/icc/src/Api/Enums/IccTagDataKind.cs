namespace icc;

/// <summary>
/// Identifies the representation stored by an <see cref="IccTagData"/> value.
/// </summary>
public enum IccTagDataKind
{
    /// <summary>
    /// Indicates that no representation has been assigned.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Indicates a losslessly preserved raw tag payload.
    /// </summary>
    Raw = 1,

    /// <summary>
    /// Identifies an integer-array payload.
    /// </summary>
    IntegerArray = 2,

    /// <summary>
    /// Identifies a fixed-point-array payload.
    /// </summary>
    FixedPointArray = 3,

    /// <summary>
    /// Identifies an XYZ-array payload.
    /// </summary>
    XyzArray = 4,

    /// <summary>
    /// Identifies a signature payload.
    /// </summary>
    Signature = 5,

    /// <summary>
    /// Identifies a date/time payload.
    /// </summary>
    DateTime = 6,

    /// <summary>
    /// Identifies an opaque binary-data payload.
    /// </summary>
    Binary = 7,

    /// <summary>
    /// Identifies a text payload.
    /// </summary>
    Text = 8,

    /// <summary>
    /// Identifies a localized-text payload.
    /// </summary>
    LocalizedText = 9,

    /// <summary>
    /// Identifies a dictionary payload.
    /// </summary>
    Dictionary = 10,

    /// <summary>
    /// Identifies a tone-curve payload.
    /// </summary>
    Curve = 11,

    /// <summary>
    /// Identifies a lookup-table payload.
    /// </summary>
    Lut = 12,

    /// <summary>
    /// Identifies a multi-process-elements payload.
    /// </summary>
    MultiProcessElements = 13,

    /// <summary>
    /// Identifies a measurement payload.
    /// </summary>
    Measurement = 14,

    /// <summary>
    /// Identifies a viewing-conditions payload.
    /// </summary>
    ViewingConditions = 15,

    /// <summary>
    /// Identifies a chromaticity payload.
    /// </summary>
    Chromaticity = 16,

    /// <summary>
    /// Identifies a CICP payload.
    /// </summary>
    Cicp = 17,

    /// <summary>
    /// Identifies a named-colours payload.
    /// </summary>
    NamedColors = 18,

    /// <summary>
    /// Identifies a colorants payload.
    /// </summary>
    Colorants = 19,

    /// <summary>
    /// Identifies a response-curves payload.
    /// </summary>
    ResponseCurves = 20,

    /// <summary>
    /// Identifies a profile-sequence payload.
    /// </summary>
    ProfileSequence = 21,

    /// <summary>
    /// Identifies an adaptive-gain-curve payload.
    /// </summary>
    AdaptiveGainCurve = 22,

    /// <summary>
    /// Identifies a spectral payload.
    /// </summary>
    Spectral = 23,

    /// <summary>
    /// Identifies an ICC.2 tag-array payload.
    /// </summary>
    TagArray = 24,

    /// <summary>
    /// Identifies an ICC.2 tag-structure payload.
    /// </summary>
    TagStructure = 25,

    /// <summary>
    /// Identifies an ICC.2 gamut-boundary payload.
    /// </summary>
    GamutBoundary = 26,

    /// <summary>
    /// Identifies an ICC.2 embedded-image payload.
    /// </summary>
    EmbeddedImage = 27,

    /// <summary>
    /// Identifies an ICC.2 calculator-program payload.
    /// </summary>
    Calculator = 28,

    /// <summary>
    /// Identifies legacy PostScript CRD information.
    /// </summary>
    CrdInfo = 29,

    /// <summary>
    /// Identifies legacy screening parameters.
    /// </summary>
    Screening = 30,

    /// <summary>
    /// Identifies legacy under-colour-removal and black-generation data.
    /// </summary>
    UcrBg = 31,

    /// <summary>
    /// Identifies an ICC.2 floating-point-array payload.
    /// </summary>
    FloatingPointArray = 32,

    /// <summary>
    /// Identifies an ICC.2 sparse-matrix-array payload.
    /// </summary>
    SparseMatrixArray = 33,

    /// <summary>
    /// Identifies an ICC.2 spectral-viewing-conditions payload.
    /// </summary>
    SpectralViewingConditions = 34,

    /// <summary>
    /// Identifies an ICC.2 spectral-range payload.
    /// </summary>
    SpectralRange = 35,

    /// <summary>
    /// Identifies an ICC.1 embedded ICC.2 profile payload.
    /// </summary>
    EmbeddedProfile = 36
}
