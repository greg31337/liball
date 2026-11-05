namespace ftp;

/// <summary>
/// Carries a command and its ordered FTP replies without requiring exceptions for expected failures.
/// </summary>
public class FtpCommandResult
{
    /// <summary>
    /// The command that produced this result.
    /// </summary>
    public FtpCommand Command;

    /// <summary>
    /// All replies received for the command in wire order.
    /// </summary>
    public FtpReply[] Replies;

    /// <summary>
    /// The optional positive preliminary reply.
    /// </summary>
    public FtpReply? PreliminaryReply;

    /// <summary>
    /// The optional terminal reply.
    /// </summary>
    public FtpReply? FinalReply;

    /// <summary>
    /// Indicates that the command reached positive completion.
    /// </summary>
    public bool Succeeded;

    /// <summary>
    /// Indicates that the control connection closed while processing the command.
    /// </summary>
    public bool ConnectionClosed;

    /// <summary>
    /// The optional local failure description when no suitable FTP reply exists.
    /// </summary>
    public string? FailureMessage;

    /// <summary>
    /// Creates an empty, non-successful command result.
    /// </summary>
    public FtpCommandResult()
    {
        Command = new FtpCommand();
        Replies = new FtpReply[0];
        PreliminaryReply = null;
        FinalReply = null;
        Succeeded = false;
        ConnectionClosed = false;
        FailureMessage = null;
    }
}

