namespace email;
/// <summary>
/// Specifies the supported line read values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailLineReadStatus
{
    /// <summary>
    /// Represents the need more data choice or state.
    /// </summary>
    NeedMoreData = 0,
    /// <summary>
    /// Represents the line choice or state.
    /// </summary>
    Line = 1,
    /// <summary>
    /// Represents the end of input choice or state.
    /// </summary>
    EndOfInput = 2
}
