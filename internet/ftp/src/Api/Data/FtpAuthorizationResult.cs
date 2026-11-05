namespace ftp;

/// <summary>
/// Carries an exception-less authorization provider result.
/// </summary>
public class FtpAuthorizationResult
{
    /// <summary>
    /// Indicates that a provider made an authorization decision.
    /// </summary>
    public bool Handled;

    /// <summary>
    /// Indicates that the requested operation is allowed.
    /// </summary>
    public bool Allowed;

    /// <summary>
    /// The optional public denial reason.
    /// </summary>
    public string? Reason;

    /// <summary>
    /// Creates an unhandled, denied authorization result.
    /// </summary>
    public FtpAuthorizationResult()
    {
        Handled = false;
        Allowed = false;
        Reason = null;
    }
}

