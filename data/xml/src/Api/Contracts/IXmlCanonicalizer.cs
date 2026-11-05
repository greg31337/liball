namespace xml;

/// <summary>
/// Canonicalizes an XML document or node selection.
/// </summary>
public interface IXmlCanonicalizer
{
    /// <summary>
    /// Writes canonical bytes for a document or configured subset.
    /// </summary>
    /// <param name="document">The document to canonicalize.</param>
    /// <param name="output">The canonical byte destination.</param>
    /// <param name="options">The canonicalization method and subset controls.</param>
    void CanonicalizeXml(XmlDocument document, XmlOutput output, XmlCanonicalizationOptions options);
    /// <summary>
    /// Writes canonical bytes for a normalized event batch.
    /// </summary>
    /// <param name="events">The normalized XML events.</param>
    /// <param name="output">The canonical byte destination.</param>
    /// <param name="options">The canonicalization method and controls.</param>
    void CanonicalizeXmlEvents(XmlEventBatch events, XmlOutput output, XmlCanonicalizationOptions options);
}
