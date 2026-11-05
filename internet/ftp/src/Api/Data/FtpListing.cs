namespace ftp;

/// <summary>
/// Carries parsed entries and retained raw lines from one FTP listing.
/// </summary>
public class FtpListing
{
    /// <summary>
    /// The parsed entries in server order.
    /// </summary>
    public FtpEntry[] Entries;

    /// <summary>
    /// The original listing lines in server order.
    /// </summary>
    public string[] RawLines;

    /// <summary>
    /// Indicates that the listing used the MLSx machine-readable format.
    /// </summary>
    public bool MachineReadable;

    /// <summary>
    /// Indicates that the complete listing was received and parsed within configured bounds.
    /// </summary>
    public bool Complete;

    /// <summary>
    /// The optional name of the selected human-listing format.
    /// </summary>
    public string? FormatName;

    /// <summary>
    /// The optional command result that produced the listing.
    /// </summary>
    public FtpCommandResult? CommandResult;

    /// <summary>
    /// The optional local failure description.
    /// </summary>
    public string? FailureMessage;

    /// <summary>
    /// Creates an empty, incomplete listing.
    /// </summary>
    public FtpListing()
    {
        Entries = new FtpEntry[0];
        RawLines = new string[0];
        MachineReadable = false;
        Complete = false;
        FormatName = null;
        CommandResult = null;
        FailureMessage = null;
    }
}

