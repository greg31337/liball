namespace ftp;

/// <summary>
/// Carries exception-less classification of an FTP reply.
/// </summary>
public class FtpReplyClassification
{
    /// <summary>
    /// The first-digit reply category.
    /// </summary>
    public FtpReplyCategory Category;

    /// <summary>
    /// The second-digit functional group.
    /// </summary>
    public FtpReplyKind Kind;

    /// <summary>
    /// Indicates that the reply is malformed.
    /// </summary>
    public bool Malformed;

    /// <summary>
    /// Indicates positive completion.
    /// </summary>
    public bool Succeeded;

    /// <summary>
    /// Indicates a transient negative completion.
    /// </summary>
    public bool TransientFailure;

    /// <summary>
    /// Indicates a permanent negative completion.
    /// </summary>
    public bool PermanentFailure;

    /// <summary>
    /// Indicates an authentication or accounting failure.
    /// </summary>
    public bool AuthenticationFailure;

    /// <summary>
    /// Creates an unknown, non-successful classification.
    /// </summary>
    public FtpReplyClassification()
    {
        Category = FtpReplyCategory.Unknown;
        Kind = FtpReplyKind.Unknown;
        Malformed = false;
        Succeeded = false;
        TransientFailure = false;
        PermanentFailure = false;
        AuthenticationFailure = false;
    }
}

