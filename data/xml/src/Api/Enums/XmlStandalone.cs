namespace xml;

/// <summary>
/// Specifies the standalone declaration value.
/// </summary>
public enum XmlStandalone
{
    /// <summary>
    /// Does not specify a standalone value.
    /// </summary>
    Unspecified = 0,
    /// <summary>
    /// Declares that the document is standalone.
    /// </summary>
    Yes = 1,
    /// <summary>
    /// Declares that the document is not standalone.
    /// </summary>
    No = 2
}
