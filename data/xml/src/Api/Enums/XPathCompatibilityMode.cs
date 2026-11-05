namespace xml;

/// <summary>
/// Specifies XPath compatibility behavior.
/// </summary>
public enum XPathCompatibilityMode
{
    /// <summary>
    /// Uses XPath 3.1 rules without backwards compatibility.
    /// </summary>
    Strict = 0,
    /// <summary>
    /// Uses backwards-compatible behavior where defined.
    /// </summary>
    BackwardsCompatible = 1
}
