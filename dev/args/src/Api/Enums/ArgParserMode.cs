namespace args;

/// <summary>
/// Selects a deterministic command-line parsing convention or preserves individually configured behavior.
/// </summary>
public enum ArgParserMode
{
    /// <summary>
    /// Uses the individual recognition, matching, and option-ordering fields in <see cref="ArgParserSettings"/>.
    /// </summary>
    Custom = 0,
    /// <summary>
    /// Uses POSIX-style short options, case-sensitive matching, and options before operands.
    /// </summary>
    Posix = 1,
    /// <summary>
    /// Uses GNU-friendly short options, case-sensitive matching, and interspersed options and operands.
    /// </summary>
    GnuFriendly = 2,
    /// <summary>
    /// Uses slash options, case-insensitive matching, interspersed options and operands, and no short clusters or attached short values.
    /// </summary>
    WindowsFriendly = 3
}
