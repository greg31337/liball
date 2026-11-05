namespace email;
/// <summary>
/// Specifies the supported header block parse values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailHeaderBlockParseStatus
{
    /// <summary>
    /// Represents the complete choice or state.
    /// </summary>
    Complete = 0,
    /// <summary>
    /// Represents the need more data choice or state.
    /// </summary>
    NeedMoreData = 1,
    /// <summary>
    /// Represents the end of input choice or state.
    /// </summary>
    EndOfInput = 2,
    /// <summary>
    /// Represents the recovered choice or state.
    /// </summary>
    Recovered = 3,
    /// <summary>
    /// Represents the limit reached choice or state.
    /// </summary>
    LimitReached = 4
}
