namespace xml;

/// <summary>
/// Specifies XML Schema wildcard processing.
/// </summary>
public enum XmlSchemaProcessContents
{
    /// <summary>
    /// Requires declaration-based assessment.
    /// </summary>
    Strict = 0,
    /// <summary>
    /// Assesses when a declaration is available.
    /// </summary>
    Lax = 1,
    /// <summary>
    /// Skips assessment.
    /// </summary>
    Skip = 2
}
