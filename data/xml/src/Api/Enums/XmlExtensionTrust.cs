namespace xml;

/// <summary>
/// Specifies which explicitly registered extension implementations may execute.
/// </summary>
public enum XmlExtensionTrust
{
    /// <summary>
    /// Denies extension execution.
    /// </summary>
    Denied = 0,
    /// <summary>
    /// Allows extensions only in configured namespace URIs.
    /// </summary>
    AllowListed = 1,
    /// <summary>
    /// Allows every explicitly registered extension.
    /// </summary>
    Trusted = 2
}
