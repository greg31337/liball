namespace odf;

/// <summary>
/// Identifies an OpenDocument note class.
/// </summary>
public enum OdfNoteKind
{
    /// <summary>
    /// The note class is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// A footnote.
    /// </summary>
    Footnote = 1,
    /// <summary>
    /// An endnote.
    /// </summary>
    Endnote = 2
}
