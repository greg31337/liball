namespace args;

/// <summary>
/// Identifies how a raw command-line token was classified.
/// </summary>
public enum ArgTokenKind
{
    /// <summary>
    /// Ordinary positional input.
    /// </summary>
    Positional = 1,
    /// <summary>
    /// A long option token beginning with two hyphens.
    /// </summary>
    LongOption = 2,
    /// <summary>
    /// A short option token beginning with one hyphen.
    /// </summary>
    ShortOption = 3,
    /// <summary>
    /// A slash-prefixed option token.
    /// </summary>
    SlashOption = 4,
    /// <summary>
    /// The delimiter that ends option recognition.
    /// </summary>
    EndOfOptions = 5
}
