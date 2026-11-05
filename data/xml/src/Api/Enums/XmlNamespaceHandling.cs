namespace xml;

/// <summary>
/// Specifies namespace processing behavior.
/// </summary>
public enum XmlNamespaceHandling
{
    /// <summary>
    /// Disables namespace processing.
    /// </summary>
    Disabled = 0,
    /// <summary>
    /// Uses Namespaces in XML 1.0 rules.
    /// </summary>
    Namespaces10 = 1,
    /// <summary>
    /// Uses Namespaces in XML 1.1 rules.
    /// </summary>
    Namespaces11 = 2
}
