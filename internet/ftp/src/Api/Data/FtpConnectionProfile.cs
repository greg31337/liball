namespace ftp;

/// <summary>
/// Describes one credential-free FTP connection profile probe.
/// </summary>
public class FtpConnectionProfile
{
    /// <summary>
    /// The probed control endpoint.
    /// </summary>
    public FtpEndpoint Endpoint;

    /// <summary>
    /// The probed control-channel security mode.
    /// </summary>
    public FtpSecurityMode SecurityMode;

    /// <summary>
    /// Indicates that the profile reached a valid FTP greeting without credentials.
    /// </summary>
    public bool Viable;

    /// <summary>
    /// Indicates that the control channel was protected by TLS.
    /// </summary>
    public bool ControlProtected;

    /// <summary>
    /// The optional server greeting.
    /// </summary>
    public FtpReply? Greeting;

    /// <summary>
    /// Features discovered without authenticating.
    /// </summary>
    public FtpFeatureSet Features;

    /// <summary>
    /// The optional public probe failure description.
    /// </summary>
    public string? FailureMessage;

    /// <summary>
    /// Creates a non-viable explicit-TLS profile.
    /// </summary>
    public FtpConnectionProfile()
    {
        Endpoint = new FtpEndpoint();
        SecurityMode = FtpSecurityMode.ExplicitTlsRequired;
        Viable = false;
        ControlProtected = false;
        Greeting = null;
        Features = new FtpFeatureSet();
        FailureMessage = null;
    }
}

