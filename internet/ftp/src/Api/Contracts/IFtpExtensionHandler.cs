namespace ftp;

/// <summary>
/// Handles extension-owned, security, OPTS, or SITE commands.
/// </summary>
public interface IFtpExtensionHandler
{
    /// <summary>
    /// Processes one extension command.
    /// </summary>
    /// <param name="context">The command and redacted session context.</param>
    /// <returns>The exception-less handler result.</returns>
    FtpExtensionResult HandleExtensionCommand(FtpExtensionContext context);
}

