namespace xml;

/// <summary>
/// Specifies how document type definitions are processed.
/// </summary>
public enum XmlDtdProcessing
{
    /// <summary>
    /// Rejects a document type definition.
    /// </summary>
    Prohibit = 0,
    /// <summary>
    /// Ignores a document type definition.
    /// </summary>
    Ignore = 1,
    /// <summary>
    /// Parses a document type definition.
    /// </summary>
    Parse = 2
}
