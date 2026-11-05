namespace xml;

/// <summary>
/// Manages transactional secondary XSLT result destinations.
/// </summary>
public interface IXsltResultDocumentResolver
{
    /// <summary>
    /// Allocates a secondary result destination.
    /// </summary>
    /// <param name="uri">The absolute result URI.</param>
    /// <param name="options">The serialization parameters.</param>
    /// <returns>The pending secondary result.</returns>
    XsltSecondaryResult AllocateXsltResultDocument(string uri, XmlSerializationOptions options);

    /// <summary>
    /// Commits a completed secondary result.
    /// </summary>
    /// <param name="result">The secondary result.</param>
    void CommitXsltResultDocument(XsltSecondaryResult result);

    /// <summary>
    /// Rejects a secondary result request.
    /// </summary>
    /// <param name="result">The secondary result.</param>
    /// <param name="diagnostic">The rejection diagnostic.</param>
    void RejectXsltResultDocument(XsltSecondaryResult result, XmlDiagnostic diagnostic);

    /// <summary>
    /// Rolls back a failed secondary result.
    /// </summary>
    /// <param name="result">The secondary result.</param>
    void RollbackXsltResultDocument(XsltSecondaryResult result);
}
