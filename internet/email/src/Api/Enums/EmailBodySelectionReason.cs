namespace email;
/// <summary>
/// Specifies the supported body selection values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailBodySelectionReason
{
    /// <summary>
    /// Represents the preferred media type choice or state.
    /// </summary>
    PreferredMediaType = 0,
    /// <summary>
    /// Represents the fallback media type choice or state.
    /// </summary>
    FallbackMediaType = 1,
    /// <summary>
    /// Represents the encrypted content unavailable choice or state.
    /// </summary>
    EncryptedContentUnavailable = 2,
    /// <summary>
    /// Represents the no supported body choice or state.
    /// </summary>
    NoSupportedBody = 3
}
