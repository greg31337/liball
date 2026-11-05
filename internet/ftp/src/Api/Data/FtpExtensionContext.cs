namespace ftp;

/// <summary>
/// Carries redacted session and command context to an FTP extension handler.
/// </summary>
public class FtpExtensionContext
{
    /// <summary>
    /// The command being offered to the handler.
    /// </summary>
    public FtpCommand Command;

    /// <summary>
    /// The optional authenticated principal.
    /// </summary>
    public FtpPrincipal? Principal;

    /// <summary>
    /// The current session state.
    /// </summary>
    public FtpSessionState SessionState;

    /// <summary>
    /// The current virtual working directory.
    /// </summary>
    public string CurrentDirectory;

    /// <summary>
    /// Indicates that the control channel is protected.
    /// </summary>
    public bool ControlProtected;

    /// <summary>
    /// Indicates that a data transfer is in progress.
    /// </summary>
    public bool TransferInProgress;

    /// <summary>
    /// The currently negotiated feature set.
    /// </summary>
    public FtpFeatureSet Features;

    /// <summary>
    /// Creates context for a disconnected, unauthenticated session.
    /// </summary>
    public FtpExtensionContext()
    {
        Command = new FtpCommand();
        Principal = null;
        SessionState = FtpSessionState.Disconnected;
        CurrentDirectory = "";
        ControlProtected = false;
        TransferInProgress = false;
        Features = new FtpFeatureSet();
    }
}

