namespace ftp;

/// <summary>
/// Carries an exception-less virtual-host resolution result.
/// </summary>
public class FtpVirtualHostResult
{
    /// <summary>
    /// Indicates that a resolver handled the host request.
    /// </summary>
    public bool Handled;

    /// <summary>
    /// Indicates that a matching host was found.
    /// </summary>
    public bool Found;

    /// <summary>
    /// The optional matching virtual-host options.
    /// </summary>
    public FtpVirtualHostOptions? Host;

    /// <summary>
    /// The optional public failure description.
    /// </summary>
    public string? FailureMessage;

    /// <summary>
    /// Creates an unhandled result with no matching host.
    /// </summary>
    public FtpVirtualHostResult()
    {
        Handled = false;
        Found = false;
        Host = null;
        FailureMessage = null;
    }
}

