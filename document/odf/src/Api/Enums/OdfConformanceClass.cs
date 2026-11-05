namespace odf;

/// <summary>
/// Identifies an OpenDocument conformance class.
/// </summary>
public enum OdfConformanceClass
{
    /// <summary>
    /// The conformance class is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// The document uses the strict conformance class.
    /// </summary>
    Strict = 1,
    /// <summary>
    /// The document uses the conforming class.
    /// </summary>
    Conforming = 2,
    /// <summary>
    /// The document uses the extended-conforming class.
    /// </summary>
    Extended = 3
}
