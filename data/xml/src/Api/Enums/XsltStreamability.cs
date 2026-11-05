namespace xml;

/// <summary>
/// Specifies the outcome of XSLT streamability analysis.
/// </summary>
public enum XsltStreamability
{
    /// <summary>
    /// Indicates that streamability was not analyzed.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Indicates guaranteed streamability.
    /// </summary>
    Streamable = 1,
    /// <summary>
    /// Indicates that guaranteed streamability requirements were not met.
    /// </summary>
    NotStreamable = 2
}
