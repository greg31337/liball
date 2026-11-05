namespace ftp;

/// <summary>
/// Represents one physical line from an FTP reply.
/// </summary>
public class FtpReplyLine
{
    /// <summary>
    /// Indicates whether this line carries a three-digit reply code.
    /// </summary>
    public bool HasCode;

    /// <summary>
    /// The three-digit reply code when <see cref="HasCode"/> is true.
    /// </summary>
    public int Code;

    /// <summary>
    /// The reply text after any code and separator.
    /// </summary>
    public string Text;

    /// <summary>
    /// The original line without its line terminator.
    /// </summary>
    public string RawText;

    /// <summary>
    /// Indicates that this line terminates a multiline reply.
    /// </summary>
    public bool TerminatesMultiline;

    /// <summary>
    /// Creates an empty reply line with no reply code.
    /// </summary>
    public FtpReplyLine()
    {
        HasCode = false;
        Code = 0;
        Text = "";
        RawText = "";
        TerminatesMultiline = false;
    }
}

