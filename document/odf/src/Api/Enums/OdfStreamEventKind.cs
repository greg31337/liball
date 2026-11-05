namespace odf;

/// <summary>
/// Identifies an event produced by a streaming reader.
/// </summary>
public enum OdfStreamEventKind
{
    /// <summary>
    /// The event kind is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// The start of a package entry.
    /// </summary>
    PackageEntryStart = 1,
    /// <summary>
    /// The end of a package entry.
    /// </summary>
    PackageEntryEnd = 2,
    /// <summary>
    /// The start of an XML element.
    /// </summary>
    ElementStart = 3,
    /// <summary>
    /// The end of an XML element.
    /// </summary>
    ElementEnd = 4,
    /// <summary>
    /// XML text content.
    /// </summary>
    Text = 5,
    /// <summary>
    /// An XML comment.
    /// </summary>
    Comment = 6,
    /// <summary>
    /// An XML processing instruction.
    /// </summary>
    ProcessingInstruction = 7,
    /// <summary>
    /// The end of the selected input.
    /// </summary>
    EndOfStream = 8
}
