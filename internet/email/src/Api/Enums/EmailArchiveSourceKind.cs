namespace email;
/// <summary>
/// Specifies the supported archive source values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailArchiveSourceKind
{
    /// <summary>
    /// Represents the standalone message choice or state.
    /// </summary>
    StandaloneMessage = 0,
    /// <summary>
    /// Represents the eml file choice or state.
    /// </summary>
    EmlFile = 1,
    /// <summary>
    /// Represents the mbox choice or state.
    /// </summary>
    Mbox = 2,
    /// <summary>
    /// Represents the maildir choice or state.
    /// </summary>
    Maildir = 3
}
