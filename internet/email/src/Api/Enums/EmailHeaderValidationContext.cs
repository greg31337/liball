namespace email;
/// <summary>
/// Specifies the supported header validation context values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailHeaderValidationContext
{
    /// <summary>
    /// Represents the message choice or state.
    /// </summary>
    Message = 0,
    /// <summary>
    /// Represents the MIME entity choice or state.
    /// </summary>
    MimeEntity = 1
}
