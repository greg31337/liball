namespace http;

/// <summary>
/// Represents one ordered HTTP/2 setting with its exact wire value.
/// </summary>
public class Http2Setting
{
    /// <summary>
    /// The unsigned 16-bit setting identifier represented as a signed integer.
    /// </summary>
    public int Identifier;

    /// <summary>
    /// The exact unsigned 32-bit setting value.
    /// </summary>
    public uint Value;

    /// <summary>
    /// Indicates that the identifier is registered and recognized.
    /// </summary>
    public bool IsKnown;

    /// <summary>
    /// Indicates that the value is valid for the recognized setting.
    /// </summary>
    public bool IsValid;
}
