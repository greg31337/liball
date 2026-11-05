namespace ftp;

/// <summary>
/// Parses machine-readable, human-readable, and name-only FTP listings.
/// </summary>
public interface IFtpListingParser
{
    /// <summary>
    /// Parses bounded RFC 3659 MLSx listing lines.
    /// </summary>
    /// <param name="lines">The raw listing lines in server order.</param>
    /// <returns>The normalized listing and retained raw lines.</returns>
    FtpListing ParseMachineListing(string[] lines);

    /// <summary>
    /// Detects and parses bounded human-readable listing lines.
    /// </summary>
    /// <param name="lines">The raw listing lines in server order.</param>
    /// <returns>The normalized listing and retained raw lines.</returns>
    FtpListing ParseHumanListing(string[] lines);

    /// <summary>
    /// Parses bounded NLST name-only lines without inferring entry type.
    /// </summary>
    /// <param name="lines">The raw name lines in server order.</param>
    /// <returns>The unknown-type entries and retained raw lines.</returns>
    FtpListing ParseNameListing(string[] lines);
}

