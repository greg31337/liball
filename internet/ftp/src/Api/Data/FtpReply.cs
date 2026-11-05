namespace ftp;

/// <summary>
/// Represents a complete single-line or multiline FTP reply.
/// </summary>
public class FtpReply
{
    /// <summary>
    /// The terminating three-digit reply code.
    /// </summary>
    public int Code;

    /// <summary>
    /// The terminating reply text.
    /// </summary>
    public string Text;

    /// <summary>
    /// All physical reply lines in wire order.
    /// </summary>
    public FtpReplyLine[] Lines;

    /// <summary>
    /// The exception-less reply classification.
    /// </summary>
    public FtpReplyClassification Classification;

    /// <summary>
    /// Indicates that the reply used multiline framing.
    /// </summary>
    public bool Multiline;

    /// <summary>
    /// The complete retained reply text without discarded content.
    /// </summary>
    public string RawText;

    /// <summary>
    /// Creates an empty, unclassified reply.
    /// </summary>
    public FtpReply()
    {
        Code = 0;
        Text = "";
        Lines = new FtpReplyLine[0];
        Classification = new FtpReplyClassification();
        Multiline = false;
        RawText = "";
    }
}

