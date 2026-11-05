namespace args;

/// <summary>
/// Identifies a shell targeted by generated command-line completion scripts.
/// </summary>
public enum ArgCompletionShell
{
    /// <summary>
    /// The GNU Bash shell.
    /// </summary>
    Bash = 1,
    /// <summary>
    /// The Z shell.
    /// </summary>
    Zsh = 2,
    /// <summary>
    /// The fish shell.
    /// </summary>
    Fish = 3,
    /// <summary>
    /// Microsoft PowerShell.
    /// </summary>
    PowerShell = 4
}
