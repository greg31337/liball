namespace email;
/// <summary>
/// Specifies the supported standard header value families.
/// </summary>
public enum EmailStandardHeaderValueKind
{
    /// <summary>
    /// Represents an SMTP return path.
    /// </summary>
    ReturnPath = 0,
    /// <summary>
    /// Represents a MIME version.
    /// </summary>
    MimeVersion = 1,
    /// <summary>
    /// Represents a content transfer encoding name.
    /// </summary>
    ContentTransferEncoding = 2,
    /// <summary>
    /// Represents a content location URI reference.
    /// </summary>
    ContentLocation = 3,
    /// <summary>
    /// Represents a Newsgroups value.
    /// </summary>
    Newsgroups = 4,
    /// <summary>
    /// Represents a Followup-To value.
    /// </summary>
    FollowupTo = 5,
    /// <summary>
    /// Represents a mailing-list identifier.
    /// </summary>
    ListId = 6,
    /// <summary>
    /// Represents a commonly deployed trace value.
    /// </summary>
    Trace = 7
}
