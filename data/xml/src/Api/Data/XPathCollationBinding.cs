namespace xml;

/// <summary>
/// Associates an absolute collation URI with its comparison implementation.
/// </summary>
public class XPathCollationBinding
{
    /// <summary>
    /// The absolute collation URI.
    /// </summary>
    public string? Uri;
    /// <summary>
    /// The collation implementation.
    /// </summary>
    public IXPathCollation? Implementation;
}
