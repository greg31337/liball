namespace xml;

/// <summary>
/// Identifies the destination used for XML output.
/// </summary>
public enum XmlOutputKind
{
    /// <summary>
    /// Uses a byte stream.
    /// </summary>
    Stream = 0,
    /// <summary>
    /// Uses a text writer.
    /// </summary>
    TextWriter = 1,
    /// <summary>
    /// Uses a byte sink.
    /// </summary>
    ByteSink = 2
}
