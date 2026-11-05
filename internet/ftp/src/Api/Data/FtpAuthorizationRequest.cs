namespace ftp;

/// <summary>
/// Carries one filesystem, metadata, SITE, or FXP authorization decision.
/// </summary>
public class FtpAuthorizationRequest
{
    /// <summary>
    /// The principal requesting the operation.
    /// </summary>
    public FtpPrincipal Principal;

    /// <summary>
    /// The requested operation.
    /// </summary>
    public FtpAuthorizationOperation Operation;

    /// <summary>
    /// The primary decoded and normalized path.
    /// </summary>
    public string Path;

    /// <summary>
    /// The optional destination path for rename or copy operations.
    /// </summary>
    public string? DestinationPath;

    /// <summary>
    /// The optional resolved entry.
    /// </summary>
    public FtpEntry? Entry;

    /// <summary>
    /// Indicates that the control channel is protected.
    /// </summary>
    public bool ControlProtected;

    /// <summary>
    /// Creates a denied-by-default listing request for an unauthenticated principal.
    /// </summary>
    public FtpAuthorizationRequest()
    {
        Principal = new FtpPrincipal();
        Operation = FtpAuthorizationOperation.List;
        Path = "";
        DestinationPath = null;
        Entry = null;
        ControlProtected = false;
    }
}

