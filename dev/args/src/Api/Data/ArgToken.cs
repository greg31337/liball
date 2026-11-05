namespace args;

/// <summary>
/// Represents one raw command-line token and the parser state associated with it.
/// </summary>
public class ArgToken
{
    /// <summary>
    /// The token text exactly as supplied in the argument array.
    /// </summary>
    public string RawText;
    /// <summary>
    /// The category represented by this value.
    /// </summary>
    public ArgTokenKind Kind;
    /// <summary>
    /// The zero-based position of the token in the argument array.
    /// </summary>
    public int Index;
    /// <summary>
    /// Whether parsing assigned or otherwise handled this token.
    /// </summary>
    public bool Consumed;

    internal ArgToken(string rawText, ArgTokenKind kind, int index)
    {
        if (rawText == null)
        {
            throw new System.ArgumentNullException("rawText");
        }

        RawText = rawText;
        Kind = kind;
        Index = index;
        Consumed = false;
    }

    internal void MarkConsumed()
    {
        Consumed = true;
    }
}
