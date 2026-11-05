namespace xml;

/// <summary>
/// Specifies whether external resource access is permitted.
/// </summary>
public enum XmlResourceAccess
{
    /// <summary>
    /// Denies access.
    /// </summary>
    Denied = 0,
    /// <summary>
    /// Allows access subject to configured limits.
    /// </summary>
    Allowed = 1
}
