namespace ftp;

/// <summary>
/// Carries an exception-less custom certificate validation decision.
/// </summary>
public class FtpCertificateValidationResult
{
    /// <summary>
    /// Indicates that a validator handled the request.
    /// </summary>
    public bool Handled;

    /// <summary>
    /// Indicates that the remote certificate is accepted.
    /// </summary>
    public bool Accepted;

    /// <summary>
    /// The optional public rejection description.
    /// </summary>
    public string? FailureMessage;

    /// <summary>
    /// Creates an unhandled, unaccepted certificate result.
    /// </summary>
    public FtpCertificateValidationResult()
    {
        Handled = false;
        Accepted = false;
        FailureMessage = null;
    }
}

