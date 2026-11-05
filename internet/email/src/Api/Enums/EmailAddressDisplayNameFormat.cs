namespace email;
/// <summary>
/// Specifies the supported address display name values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailAddressDisplayNameFormat
{
    /// <summary>
    /// Represents the encoded word choice or state.
    /// </summary>
    EncodedWord = 0,
    /// <summary>
    /// Represents the unicode choice or state.
    /// </summary>
    Unicode = 1
}
