namespace email;
/// <summary>
/// Specifies the supported address domain values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailAddressDomainFormat
{
    /// <summary>
    /// Represents the ascii choice or state.
    /// </summary>
    Ascii = 0,
    /// <summary>
    /// Represents the unicode choice or state.
    /// </summary>
    Unicode = 1
}
