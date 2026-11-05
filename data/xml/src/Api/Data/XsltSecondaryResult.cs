namespace xml;

/// <summary>
/// Carries one secondary XSLT result destination and value.
/// </summary>
public class XsltSecondaryResult
{
    /// <summary>
    /// The absolute secondary result URI.
    /// </summary>
    public string? Uri;
    /// <summary>
    /// The allocated output destination.
    /// </summary>
    public XmlOutput? Output;
    /// <summary>
    /// The selected serialization parameters.
    /// </summary>
    public XmlSerializationOptions? SerializationOptions;
    /// <summary>
    /// The produced result sequence.
    /// </summary>
    public XdmSequence? Value;
    /// <summary>
    /// The commit or rollback disposition.
    /// </summary>
    public XsltResultDisposition Disposition;
    /// <summary>
    /// Whether production of the result succeeded.
    /// </summary>
    public bool Succeeded;
}

