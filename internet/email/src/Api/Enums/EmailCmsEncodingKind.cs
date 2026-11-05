namespace email;
/// <summary>
/// Specifies the supported CMS encoding values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailCmsEncodingKind
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the der choice or state.
    /// </summary>
    Der = 1,
    /// <summary>
    /// Represents the ber choice or state.
    /// </summary>
    Ber = 2
}
