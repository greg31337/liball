namespace xml;

/// <summary>
/// Serializes an XDM sequence using a registered extension method.
/// </summary>
public interface IXmlSerializationMethod
{
    /// <summary>
    /// Serializes one sequence to the supplied output.
    /// </summary>
    /// <param name="sequence">The sequence to serialize.</param>
    /// <param name="output">The output destination.</param>
    /// <param name="options">The effective serialization options.</param>
    void SerializeXmlSequence(XdmSequence sequence, XmlOutput output, XmlSerializationOptions options);
}
