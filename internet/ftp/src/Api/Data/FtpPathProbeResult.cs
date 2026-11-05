namespace ftp;

/// <summary>
/// Carries the exception-less result of probing an FTP path.
/// </summary>
public class FtpPathProbeResult
{
    /// <summary>
    /// Indicates that the probe completed without protocol or transport failure.
    /// </summary>
    public bool Succeeded;

    /// <summary>
    /// Indicates that the path exists when <see cref="Succeeded"/> is true.
    /// </summary>
    public bool Exists;

    /// <summary>
    /// The optional entry metadata when the path exists.
    /// </summary>
    public FtpEntry? Entry;

    /// <summary>
    /// The ordered command results used by the probe and any fallback.
    /// </summary>
    public FtpCommandResult[] Commands;

    /// <summary>
    /// The optional local failure description.
    /// </summary>
    public string? FailureMessage;

    /// <summary>
    /// Creates an unsuccessful probe with unknown existence.
    /// </summary>
    public FtpPathProbeResult()
    {
        Succeeded = false;
        Exists = false;
        Entry = null;
        Commands = new FtpCommandResult[0];
        FailureMessage = null;
    }
}

