namespace ftp;

/// <summary>
/// Carries an exception-less authentication provider result.
/// </summary>
public class FtpAuthenticationResult
{
    /// <summary>
    /// The authentication outcome.
    /// </summary>
    public FtpAuthenticationStatus Status;

    /// <summary>
    /// The optional authenticated or intermediate principal.
    /// </summary>
    public FtpPrincipal? Principal;

    /// <summary>
    /// The optional challenge or next-step prompt.
    /// </summary>
    public string? Challenge;

    /// <summary>
    /// The optional public failure description.
    /// </summary>
    public string? FailureMessage;

    /// <summary>
    /// The recommended delay before another attempt in milliseconds.
    /// </summary>
    public int RetryDelayMilliseconds;

    /// <summary>
    /// Creates a result that has not been handled by a provider.
    /// </summary>
    public FtpAuthenticationResult()
    {
        Status = FtpAuthenticationStatus.NotHandled;
        Principal = null;
        Challenge = null;
        FailureMessage = null;
        RetryDelayMilliseconds = 0;
    }
}

