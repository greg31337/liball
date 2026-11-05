namespace email;
/// <summary>
/// Specifies the supported text charset values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailTextCharsetSource
{
    /// <summary>
    /// No text charset is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the byte order mark choice or state.
    /// </summary>
    ByteOrderMark = 1,
    /// <summary>
    /// Represents the declared choice or state.
    /// </summary>
    Declared = 2,
    /// <summary>
    /// Represents the fallback choice or state.
    /// </summary>
    Fallback = 3
}
