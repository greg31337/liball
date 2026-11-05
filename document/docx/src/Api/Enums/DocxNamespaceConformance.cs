namespace docx;

/// <summary>
/// Specifies the namespace conformance used by DOCX operations.
/// </summary>
public enum DocxNamespaceConformance
{
    /// <summary>
    /// No recognized Strict or Transitional namespace was found.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Only Transitional Open XML namespaces and relationship types were found.
    /// </summary>
    Transitional = 1,
    /// <summary>
    /// Only Strict Open XML namespaces and relationship types were found.
    /// </summary>
    Strict = 2,
    /// <summary>
    /// Both Strict and Transitional identifiers were found in the package.
    /// </summary>
    Mixed = 3
}
