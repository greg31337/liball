namespace email;
/// <summary>
/// Specifies the supported forward body values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailForwardBodyPolicy
{
    /// <summary>
    /// Represents the include original choice or state.
    /// </summary>
    IncludeOriginal = 0,
    /// <summary>
    /// Represents the headers only choice or state.
    /// </summary>
    HeadersOnly = 1,
    /// <summary>
    /// Represents the omit original choice or state.
    /// </summary>
    OmitOriginal = 2
}
