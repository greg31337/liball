namespace ftp;

/// <summary>
/// Carries all credential-free connection profiles discovered for an endpoint.
/// </summary>
public class FtpConnectionProbeResult
{
    /// <summary>
    /// The probed profiles in configured preference order.
    /// </summary>
    public FtpConnectionProfile[] Profiles;

    /// <summary>
    /// Indicates that every requested profile probe reached a terminal result.
    /// </summary>
    public bool Complete;

    /// <summary>
    /// The optional aggregate probe failure description.
    /// </summary>
    public string? FailureMessage;

    /// <summary>
    /// Creates an empty, incomplete probe result.
    /// </summary>
    public FtpConnectionProbeResult()
    {
        Profiles = new FtpConnectionProfile[0];
        Complete = false;
        FailureMessage = null;
    }
}

