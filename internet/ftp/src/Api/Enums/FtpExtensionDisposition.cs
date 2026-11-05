namespace ftp;

/// <summary>
/// Describes how an extension handler processed a command.
/// </summary>
public enum FtpExtensionDisposition
{
    /// <summary>
    /// Indicates that the handler did not recognize the command.
    /// </summary>
    NotHandled = 0,
    /// <summary>
    /// Indicates that the handler produced a successful or intermediate result.
    /// </summary>
    Handled = 1,
    /// <summary>
    /// Indicates that the handler recognized and rejected the command.
    /// </summary>
    Rejected = 2
}
