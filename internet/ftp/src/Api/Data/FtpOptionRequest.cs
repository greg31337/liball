namespace ftp;

/// <summary>
/// Represents an OPTS request for a standard or extension-owned command.
/// </summary>
public class FtpOptionRequest
{
    /// <summary>
    /// The command or feature name whose options are being configured.
    /// </summary>
    public string CommandName;

    /// <summary>
    /// The optional option text; null means absent and an empty string means present but empty.
    /// </summary>
    public string? Value;

    /// <summary>
    /// Creates an empty option request with no option text.
    /// </summary>
    public FtpOptionRequest()
    {
        CommandName = "";
        Value = null;
    }
}

