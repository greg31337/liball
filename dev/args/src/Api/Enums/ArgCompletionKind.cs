namespace args;

/// <summary>
/// Identifies what a completion candidate represents.
/// </summary>
public enum ArgCompletionKind
{
    /// <summary>
    /// No applicable category or source.
    /// </summary>
    None = 0,
    /// <summary>
    /// A command name.
    /// </summary>
    Command = 1,
    /// <summary>
    /// An option alias.
    /// </summary>
    Option = 2,
    /// <summary>
    /// An option or positional-argument value.
    /// </summary>
    Value = 3
}
