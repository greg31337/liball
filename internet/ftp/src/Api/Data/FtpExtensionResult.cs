namespace ftp;

/// <summary>
/// Carries an exception-less extension-handler result.
/// </summary>
public class FtpExtensionResult
{
    /// <summary>
    /// Describes whether the command was handled or rejected.
    /// </summary>
    public FtpExtensionDisposition Disposition;

    /// <summary>
    /// Replies produced by the handler in send order.
    /// </summary>
    public FtpReply[] Replies;

    /// <summary>
    /// Indicates that the control connection closes after these replies.
    /// </summary>
    public bool CloseConnection;

    /// <summary>
    /// The optional local handler failure description.
    /// </summary>
    public string? FailureMessage;

    /// <summary>
    /// Creates a result that does not handle the command.
    /// </summary>
    public FtpExtensionResult()
    {
        Disposition = FtpExtensionDisposition.NotHandled;
        Replies = new FtpReply[0];
        CloseConnection = false;
        FailureMessage = null;
    }
}

