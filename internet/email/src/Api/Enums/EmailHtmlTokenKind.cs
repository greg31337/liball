namespace email;
/// <summary>
/// Specifies the supported HTML token values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailHtmlTokenKind
{
    /// <summary>
    /// Represents the text choice or state.
    /// </summary>
    Text = 0,
    /// <summary>
    /// Represents the start tag choice or state.
    /// </summary>
    StartTag = 1,
    /// <summary>
    /// Represents the end tag choice or state.
    /// </summary>
    EndTag = 2,
    /// <summary>
    /// Represents the comment choice or state.
    /// </summary>
    Comment = 3,
    /// <summary>
    /// Represents the doctype choice or state.
    /// </summary>
    Doctype = 4
}
