namespace args;

/// <summary>
/// Identifies the kind of command-line symbol associated with a result or error.
/// </summary>
public enum ArgSymbolKind
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
    /// A positional argument.
    /// </summary>
    Argument = 3
}
