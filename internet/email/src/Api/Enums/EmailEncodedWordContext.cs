namespace email;
/// <summary>
/// Specifies the supported encoded word context values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailEncodedWordContext
{
    /// <summary>
    /// Represents the unstructured text choice or state.
    /// </summary>
    UnstructuredText = 0,
    /// <summary>
    /// Represents the phrase choice or state.
    /// </summary>
    Phrase = 1,
    /// <summary>
    /// Represents the comment choice or state.
    /// </summary>
    Comment = 2,
    /// <summary>
    /// Represents the forbidden choice or state.
    /// </summary>
    Forbidden = 3
}
