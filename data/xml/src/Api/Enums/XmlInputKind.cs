namespace xml;

/// <summary>
/// Identifies the storage used for XML input.
/// </summary>
public enum XmlInputKind
{
    /// <summary>
    /// Uses a string.
    /// </summary>
    String = 0,
    /// <summary>
    /// Uses character memory.
    /// </summary>
    CharacterMemory = 1,
    /// <summary>
    /// Uses byte memory.
    /// </summary>
    ByteMemory = 2,
    /// <summary>
    /// Uses a byte stream.
    /// </summary>
    Stream = 3,
    /// <summary>
    /// Uses a text reader.
    /// </summary>
    TextReader = 4
}
