namespace email;
/// <summary>
/// Specifies the supported body preference values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailBodyPreference
{
    /// <summary>
    /// Represents the HTML choice or state.
    /// </summary>
    Html = 0,
    /// <summary>
    /// Represents the plain text choice or state.
    /// </summary>
    PlainText = 1
}
