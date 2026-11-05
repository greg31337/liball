namespace whois;

/// <summary>
/// Classifies a line in a generic WHOIS response view.
/// </summary>
public enum WhoisLineKind
{
    /// <summary>
    /// The line contains no content.
    /// </summary>
    Blank = 0,

    /// <summary>
    /// The line is a recognized comment.
    /// </summary>
    Comment = 1,

    /// <summary>
    /// The line contains a colon-delimited field.
    /// </summary>
    Field = 2,

    /// <summary>
    /// The line continues the preceding field value.
    /// </summary>
    Continuation = 3,

    /// <summary>
    /// The line is unstructured text.
    /// </summary>
    Text = 4
}
