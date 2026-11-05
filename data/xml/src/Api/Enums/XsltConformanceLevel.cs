namespace xml;

/// <summary>
/// Specifies an XSLT 3.0 conformance level.
/// </summary>
public enum XsltConformanceLevel
{
    /// <summary>
    /// Provides basic XSLT 3.0 processing.
    /// </summary>
    Basic = 0,
    /// <summary>
    /// Provides schema-aware XSLT 3.0 processing.
    /// </summary>
    SchemaAware = 1,
    /// <summary>
    /// Provides guaranteed-streaming XSLT 3.0 processing.
    /// </summary>
    Streaming = 2
}
