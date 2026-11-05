namespace email;
/// <summary>
/// Specifies the supported body selection context values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailBodySelectionContext
{
    /// <summary>
    /// Represents the alternative choice or state.
    /// </summary>
    Alternative = 0,
    /// <summary>
    /// Represents the related root choice or state.
    /// </summary>
    RelatedRoot = 1,
    /// <summary>
    /// Represents the mixed choice or state.
    /// </summary>
    Mixed = 2,
    /// <summary>
    /// Represents the signed content choice or state.
    /// </summary>
    SignedContent = 3,
    /// <summary>
    /// Represents the nested message choice or state.
    /// </summary>
    NestedMessage = 4,
    /// <summary>
    /// Represents the unknown multipart choice or state.
    /// </summary>
    UnknownMultipart = 5
}
