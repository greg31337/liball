namespace xml;

/// <summary>
/// Serializes normalized XML events to an output destination.
/// </summary>
public interface IXmlSerializer
{
    /// <summary>
    /// Serializes an XML document.
    /// </summary>
    /// <param name="document">The document to serialize.</param>
    /// <param name="output">The output destination.</param>
    /// <param name="options">The serialization parameters.</param>
    void SerializeXmlDocument(XmlDocument document, XmlOutput output, XmlSerializationOptions options);
    /// <summary>
    /// Serializes an XML node.
    /// </summary>
    /// <param name="node">The node to serialize.</param>
    /// <param name="output">The output destination.</param>
    /// <param name="options">The serialization parameters.</param>
    void SerializeXmlNode(XmlNode node, XmlOutput output, XmlSerializationOptions options);
    /// <summary>
    /// Serializes an XDM sequence.
    /// </summary>
    /// <param name="sequence">The sequence to serialize.</param>
    /// <param name="output">The output destination.</param>
    /// <param name="options">The serialization parameters.</param>
    void SerializeXdmSequence(XdmSequence sequence, XmlOutput output, XmlSerializationOptions options);
    /// <summary>
    /// Serializes one normalized event batch.
    /// </summary>
    /// <param name="events">The normalized events to serialize.</param>
    /// <param name="output">The output destination.</param>
    /// <param name="options">The serialization parameters.</param>
    void SerializeXmlEvents(XmlEventBatch events, XmlOutput output, XmlSerializationOptions options);
}
