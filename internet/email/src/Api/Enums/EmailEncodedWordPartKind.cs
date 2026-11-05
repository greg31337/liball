namespace email;
/// <summary>
/// Specifies the supported encoded word part values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailEncodedWordPartKind
{
    /// <summary>
    /// Represents the literal choice or state.
    /// </summary>
    Literal = 0,
    /// <summary>
    /// Represents the encoded word choice or state.
    /// </summary>
    EncodedWord = 1
}
