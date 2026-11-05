namespace email;
/// <summary>
/// Specifies the supported cms ASN.1 tag class values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailCmsAsn1TagClass
{
    /// <summary>
    /// Represents the universal choice or state.
    /// </summary>
    Universal = 0,
    /// <summary>
    /// Represents the application choice or state.
    /// </summary>
    Application = 1,
    /// <summary>
    /// Represents the context specific choice or state.
    /// </summary>
    ContextSpecific = 2,
    /// <summary>
    /// Represents the private choice or state.
    /// </summary>
    Private = 3
}
